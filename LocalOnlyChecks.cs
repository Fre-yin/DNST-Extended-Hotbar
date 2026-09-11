using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ExtendedHotbar.Helper
{
    // Test-only audit of the built executable, not a feature switch. Inspect CLR
    // signatures and IL without executing the helper or contacting any endpoint.
    internal static class LocalOnlyChecks
    {
        internal static void Verify(string executable)
        {
            // Reflection-only loading does not use normal runtime facade binding.
            // Resolve against the same Developer Pack references used by Build.ps1.
            ResolveEventHandler resolver = (sender, args) => {
                var name = new AssemblyName(args.Name);
                if (name.Name != "mscorlib" && name.Name != "System" && !name.Name.StartsWith("System.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected audit dependency: " + name.Name);
                var references = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies", "Microsoft", "Framework", ".NETFramework", "v4.8");
                var file = Path.Combine(references, name.Name + ".dll");
                if (!File.Exists(file)) throw new FileNotFoundException("Audit reference missing.", file);
                var result = Assembly.ReflectionOnlyLoadFrom(file);
                if (result.FullName != name.FullName) throw new InvalidOperationException("Audit reference identity differs: " + name.Name);
                return result;
            };
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += resolver;
            try
            {
                var assembly = Assembly.ReflectionOnlyLoadFrom(Path.GetFullPath(executable));
                var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(x => x.FieldType == typeof(OpCode)).Select(x => (OpCode)x.GetValue(null))
                    .ToDictionary(x => unchecked((ushort)x.Value));
                const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                foreach (var type in assembly.GetTypes())
                {
                    if (new[] { "GitHubTransport", "UpdateClient", "IUpdateTransport" }.Contains(type.Name))
                        throw new Exception("Removed online component remains in executable: " + type.FullName);
                    CheckType(type.BaseType);
                    foreach (var item in type.GetInterfaces()) CheckType(item);
                    foreach (var field in type.GetFields(members)) CheckType(field.FieldType);
                    foreach (var method in type.GetMethods(members).Cast<MethodBase>().Concat(type.GetConstructors(members)))
                    {
                        var info = method as MethodInfo;
                        if (info != null) CheckType(info.ReturnType);
                        foreach (var parameter in method.GetParameters()) CheckType(parameter.ParameterType);
                        foreach (var attribute in CustomAttributeData.GetCustomAttributes(method))
                            if (attribute.AttributeType.FullName == "System.Runtime.InteropServices.DllImportAttribute")
                            {
                                var library = Path.GetFileName((string)attribute.ConstructorArguments[0].Value).ToLowerInvariant();
                                if (new[] { "winhttp.dll", "wininet.dll", "ws2_32.dll", "urlmon.dll" }.Contains(library))
                                    throw new Exception("Network DLL import: " + library);
                            }
                        var body = method.GetMethodBody();
                        if (body == null) continue;
                        foreach (var local in body.LocalVariables) CheckType(local.LocalType);
                        var bytes = body.GetILAsByteArray();
                        for (int offset = 0; offset < bytes.Length;)
                        {
                            ushort code = bytes[offset++];
                            if (code == 0xfe) code = (ushort)(0xfe00 | bytes[offset++]);
                            var opcode = opcodes[code];
                            switch (opcode.OperandType)
                            {
                                case OperandType.InlineField: case OperandType.InlineMethod:
                                case OperandType.InlineTok: case OperandType.InlineType:
                                    var member = method.Module.ResolveMember(BitConverter.ToInt32(bytes, offset), type.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
                                    CheckType(member as Type ?? member.DeclaringType);
                                    offset += 4; break;
                                case OperandType.InlineNone: break;
                                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: offset++; break;
                                case OperandType.InlineVar: offset += 2; break;
                                case OperandType.InlineBrTarget: case OperandType.InlineI: case OperandType.InlineString: case OperandType.ShortInlineR: offset += 4; break;
                                case OperandType.InlineI8: case OperandType.InlineR: offset += 8; break;
                                case OperandType.InlineSwitch: offset += 4 + checked(4 * BitConverter.ToInt32(bytes, offset)); break;
                                default: throw new Exception("Unaudited IL operand in " + method.Name + ": " + opcode.OperandType);
                            }
                        }
                    }
                }
            }
            finally { AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= resolver; }
        }

        private static void CheckType(Type type)
        {
            if (type == null || type.IsGenericParameter) return;
            if (type.Namespace == "System.Net" || (type.Namespace ?? "").StartsWith("System.Net.", StringComparison.Ordinal))
                throw new Exception("Network API remains in executable: " + type.FullName);
            if (type.HasElementType) CheckType(type.GetElementType());
            if (type.IsGenericType) foreach (var argument in type.GetGenericArguments()) CheckType(argument);
        }
    }
}
