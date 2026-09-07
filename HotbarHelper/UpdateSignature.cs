using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;

namespace ExtendedHotbar.Helper
{
    internal static class UpdateSignature
    {
        internal static string PublicKey()
        {
            using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("UpdateTrust.xml"))
            {
                if (input == null) throw Updates.Invalid("Pinned update signing key missing.");
                using (var reader = new StreamReader(input)) return reader.ReadToEnd();
            }
        }
        internal static void Verify(UpdateOffer offer, byte[] envelope, string publicKey, DateTime now)
        {
            try
            {
                if (envelope == null || envelope.Length != offer.SignatureSize || Files.Hash(envelope) != offer.SignatureHash || envelope.Length > 16384)
                    throw Updates.Invalid("Signed metadata size or hash mismatch.");
                var payload = VerifyPayload(envelope, publicKey);
                var fields = new LosslessJson(Files.Text(payload)).Root;
                if (fields.Members.Count != 9 || fields.Get("purpose").String != "ExtendedHotbarHelper.Update.v1"
                    || fields.Get("repository").String != Updates.Repository || fields.Get("helperVersion").String != offer.Version.ToString(3)
                    || fields.Get("fileName").String != offer.FileName || fields.Get("size").Integer != offer.Size
                    || fields.Get("sha256").String != offer.Hash || fields.Get("prerelease").Kind != "bool"
                    || (fields.Get("prerelease").Text == "true") != offer.Prerelease) throw Updates.Invalid("Signed metadata is not bound to this release package.");
                const string format = "yyyy-MM-ddTHH:mm:ssZ";
                var issued = DateTime.ParseExact(fields.Get("issuedUtc").String, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                var expires = DateTime.ParseExact(fields.Get("expiresUtc").String, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                if (issued > now.AddMinutes(5) || expires <= now || expires <= issued || expires - issued > TimeSpan.FromDays(180)) throw Updates.Invalid("Signature is expired, future-dated, or exceeds its maximum validity.");
            }
            catch (HelperFailure) { throw; }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException || ex is XmlException || ex is ArgumentException)
            { throw new HelperFailure("errorUpdateData", "Update signature could not be verified.", ex); }
        }
        internal static byte[] VerifyPayload(byte[] envelope, string publicKey)
        {
            if (envelope == null || envelope.Length > 16384) throw Updates.Invalid("Invalid signature envelope size.");
            var doc = new LosslessJson(Files.Text(envelope)).Root;
            if (doc.Members.Count != 3 || doc.Get("format").Integer != 1) throw Updates.Invalid("Unsupported signature envelope.");
            var payload = Convert.FromBase64String(doc.Get("payload").String);
            var signature = Convert.FromBase64String(doc.Get("signature").String);
            if (payload.Length > 4096 || signature.Length != 512) throw Updates.Invalid("Invalid RSA-4096 signature size.");
            using (var rsa = new RSACryptoServiceProvider(new CspParameters(24)))
            {
                rsa.PersistKeyInCsp = false;
                ValidatePublicKey(publicKey); rsa.FromXmlString(publicKey);
                if (rsa.KeySize != 4096 || !rsa.VerifyData(payload, CryptoConfig.MapNameToOID("SHA256"), signature)) throw Updates.Invalid("Update publisher signature is invalid.");
            }
            return payload;
        }
        internal static void ValidatePublicKey(string xml)
        {
            if (xml == null || xml.Length > 2048) throw Updates.Invalid("Invalid pinned key.");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (var reader = XmlReader.Create(new StringReader(xml), settings))
            {
                var document = new XmlDocument { XmlResolver = null }; document.Load(reader);
                var root = document.DocumentElement;
                if (root.Name != "RSAKeyValue" || root.Attributes.Count != 0 || root.ChildNodes.Count != 2
                    || root.ChildNodes.Cast<XmlNode>().Any(x => x.Name != "Modulus" && x.Name != "Exponent")
                    || root.SelectNodes("Modulus").Count != 1 || root.SelectNodes("Exponent").Count != 1) throw Updates.Invalid("Only an RSA public key may be embedded.");
            }
        }
    }
}
