using System;

namespace OmniSharp.WebAssembly;

internal class CompilerLoggerOptions
{
    public Uri LogUri { get; set; } = null!;
    public string CompilerLogBasePath { get; set; } = null!;
    public string WorkspaceBasePath { get; set; } = null!;
}
