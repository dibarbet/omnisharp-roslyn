using System;
using System.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OmniSharp.WebAssembly;

[Export(typeof(IPathRemapper)), Shared]
internal class CompilerLoggerPathRemapper : IPathRemapper
{
    private readonly string _compilerLogBasePath;
    private readonly string _workspaceBasePath;
    private readonly CompilerLoggerOptions _options = new();

    [ImportingConstructor]
    public CompilerLoggerPathRemapper(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetService<IConfigurationRoot>();
        var section = configuration!.GetSection("CompilerLogger");
        section.Bind(_options);
        _compilerLogBasePath = _options.CompilerLogBasePath;
        _workspaceBasePath = _options.WorkspaceBasePath;
    }

    public string Remap(string path)
    {
        return path.Replace(_workspaceBasePath, _compilerLogBasePath);
    }
}
