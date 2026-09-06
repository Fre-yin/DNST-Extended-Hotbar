using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace ExtendedHotbar.Helper
{
    // Edit only explicitly selected spans. Unrelated values (including large
    // integers, floating point spellings and serializer metadata) stay verbatim.
    internal sealed class JsonNode
    {
        internal string Kind, Text;
        internal int Start, End;
        internal readonly List<JsonNode> Items = new List<JsonNode>();
        internal readonly List<JsonMember> Members = new List<JsonMember>();
        internal JsonNode Get(string name)
        {
            var member = Members.SingleOrDefault(x => x.Name == name);
            if (Kind != "object" || member == null) throw new FormatException("Fehlendes JSON-Feld: " + name);
            return member.Value;
        }
        internal JsonNode Optional(string name) { return Members.Where(x => x.Name == name).Select(x => x.Value).SingleOrDefault(); }
        internal string String { get { if (Kind != "string") throw new FormatException("Text erwartet."); return Text; } }
        internal int Integer { get { int value; if (Kind != "number" || !int.TryParse(Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value)) throw new FormatException("Ganzzahl erwartet."); return value; } }
    }

    internal sealed class JsonMember
    {
        internal string Name;
        internal int Start, End;
        internal JsonNode Value;
    }

    internal sealed class JsonEdit
    {
        internal int Start, End;
        internal string Text;
        internal JsonEdit(int start, int end, string text) { Start = start; End = end; Text = text; }
    }

    internal sealed class LosslessJson
    {
        internal readonly string Source;
        internal readonly JsonNode Root;
        private int position, nodes;
        private static readonly Regex Number = new Regex(@"\G-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?", RegexOptions.CultureInvariant);
        internal static string Quote(string value) { return new JavaScriptSerializer().Serialize(value); }
        internal LosslessJson(string source)
        {
            if (source == null || source.Length > 32 * 1024 * 1024) throw new FormatException("JSON-Datei fehlt oder ist zu groß.");
            Source = source;
            Root = Read(0); Space();
            if (position != Source.Length) throw new FormatException("Zusätzliche Daten hinter dem JSON.");
        }
        internal string Raw(JsonNode node) { return Source.Substring(node.Start, node.End - node.Start); }
        internal JsonEdit Replace(JsonNode node, string value) { return new JsonEdit(node.Start, node.End, value); }
        internal JsonEdit Remove(JsonNode parent, string name)
        {
            int i = parent.Members.FindIndex(x => x.Name == name);
            if (i < 0) throw new FormatException("Zu entfernendes Feld fehlt.");
            var member = parent.Members[i];
            if (i + 1 < parent.Members.Count) return new JsonEdit(member.Start, parent.Members[i + 1].Start, "");
            if (i > 0) return new JsonEdit(parent.Members[i - 1].End, member.End, "");
            return new JsonEdit(member.Start, member.End, "");
        }
        internal string Apply(IEnumerable<JsonEdit> changes)
        {
            var result = Source;
            int boundary = Source.Length;
            foreach (var change in changes.OrderByDescending(x => x.Start))
            {
                if (change.Start < 0 || change.End < change.Start || change.End > boundary) throw new FormatException("Überlappende JSON-Änderungen.");
                result = result.Substring(0, change.Start) + change.Text + result.Substring(change.End);
                boundary = change.Start;
            }
            new LosslessJson(result);
            return result;
        }
        private void Space() { while (position < Source.Length && " \t\r\n".IndexOf(Source[position]) >= 0) position++; }
        private bool Take(char c) { Space(); if (position >= Source.Length || Source[position] != c) return false; position++; return true; }
        private void Need(char c) { if (!Take(c)) throw new FormatException("Ungültiges JSON bei Zeichen " + position); }
        private JsonNode Read(int depth)
        {
            if (depth > 128 || ++nodes > 1000000) throw new FormatException("JSON-Struktur zu tief oder zu groß.");
            Space(); if (position >= Source.Length) throw new FormatException("Unvollständiges JSON.");
            var node = new JsonNode { Start = position };
            char c = Source[position];
            if (c == '{')
            {
                node.Kind = "object"; position++;
                var names = new HashSet<string>(StringComparer.Ordinal);
                if (!Take('}'))
                {
                    do
                    {
                        Space(); int start = position;
                        var key = ReadString();
                        if (!names.Add(key)) throw new FormatException("Doppeltes JSON-Feld: " + key);
                        Need(':'); var value = Read(depth + 1);
                        node.Members.Add(new JsonMember { Name = key, Start = start, End = value.End, Value = value });
                    } while (Take(','));
                    Need('}');
                }
            }
            else if (c == '[')
            {
                node.Kind = "array"; position++;
                if (!Take(']')) { do { node.Items.Add(Read(depth + 1)); } while (Take(',')); Need(']'); }
            }
            else if (c == '"') { node.Kind = "string"; node.Text = ReadString(); }
            else if (c == 't' || c == 'f' || c == 'n')
            {
                var value = c == 't' ? "true" : c == 'f' ? "false" : "null";
                if (position + value.Length > Source.Length || Source.Substring(position, value.Length) != value) throw new FormatException("Ungültiger JSON-Wert.");
                position += value.Length; node.Kind = value == "null" ? "null" : "bool"; node.Text = value;
            }
            else
            {
                var match = Number.Match(Source, position);
                if (!match.Success) throw new FormatException("Ungültige JSON-Zahl.");
                position += match.Length; node.Kind = "number"; node.Text = match.Value;
            }
            node.End = position; return node;
        }
        private string ReadString()
        {
            Need('"'); int start = position - 1;
            while (position < Source.Length)
            {
                char c = Source[position++];
                if (c == '"') return new JavaScriptSerializer().Deserialize<string>(Source.Substring(start, position - start));
                if (c < 32) throw new FormatException("Steuerzeichen im JSON-Text.");
                if (c != '\\') continue;
                if (position >= Source.Length) break;
                c = Source[position++];
                if (c == 'u')
                {
                    for (int i = 0; i < 4; i++)
                        if (position >= Source.Length || !Uri.IsHexDigit(Source[position++])) throw new FormatException("Ungültige Unicode-Escape-Sequenz.");
                }
                else if ("\"\\/bfnrt".IndexOf(c) < 0) throw new FormatException("Ungültige Escape-Sequenz.");
            }
            throw new FormatException("Unvollständiger JSON-Text.");
        }
    }
}
