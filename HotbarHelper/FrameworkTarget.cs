// Direct Roslyn builds do not generate this attribute. Both entry assemblies need
// the intended target to select modern .NET defaults, including OS-managed TLS.
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]
