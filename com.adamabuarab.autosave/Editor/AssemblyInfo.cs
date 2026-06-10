using System.Runtime.CompilerServices;

// Grants the test assembly access to all internal types in the main editor assembly.
// This is the standard .NET pattern for unit-testing internal classes without
// exposing them publicly.
[assembly: InternalsVisibleTo("DevTools.AutoSave.Tests.Editor")]
