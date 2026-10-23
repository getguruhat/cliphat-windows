namespace System.Runtime.CompilerServices;

/// Lets records and init-only properties (Strings.cs) compile for .NET Framework, which doesn't define it.
internal static class IsExternalInit { }
