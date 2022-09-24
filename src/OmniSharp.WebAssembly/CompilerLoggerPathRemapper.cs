using System;
using System.Composition;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace OmniSharp.WebAssembly;

[Export(typeof(IPathRemapper)), Shared]
public class CompilerLoggerPathRemapper : IPathRemapper
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
        return Remap(path, _workspaceBasePath, _compilerLogBasePath);
    }

    public static string Remap(string path, string workspacePath, string compilerLogBasePath)
    {
        var inputPathParts = GetPathParts(path);
        var workspacePathParts = GetPathParts(workspacePath);

        for (var i = 0; i < workspacePathParts.Length; i++)
        {
            if (i > inputPathParts.Length)
            {
                // input path is shorter than the base workspace path.
                return path;
            }

            // VSCode gives paths on windows like 'c:\workspace\file.txt' while the input path to the workspace
            // will usually be uppercase drive letter.  We don't know what platform we're on, so we just do a
            // case insensitive comparison and hope for the best.
            if (!string.Equals(workspacePathParts[i], inputPathParts[i], StringComparison.OrdinalIgnoreCase))
            {
                // input path doesn't match workspace path.
                return path;
            }
        }

        var startingIndex = workspacePathParts.Length;

        // If the input path has no more parts remaining its the same and we can just return it.
        if (startingIndex > inputPathParts.Length - 1)
        {
            return path;
        }

        var remaining = inputPathParts[workspacePathParts.Length..];
        // input path begins with workspace base path.
        // combine the compilerLogBasePath with the remaining input path parts.
        // Note that the compiler log could have been generated on a different platform with
        // different directory separators, so use whatever it uses as thats what we need to match to in the workspace.
        var directorySeparator = compilerLogBasePath.Contains(@"\") ? @"\" : @"/";
        var remapped = $"{compilerLogBasePath}{directorySeparator}{string.Join(directorySeparator, remaining)}";
        return remapped;
    }

    private static string[] GetPathParts(string path)
    {
        return path.Split(new string[] { @"\", @"/" }, StringSplitOptions.RemoveEmptyEntries);
    }
}
