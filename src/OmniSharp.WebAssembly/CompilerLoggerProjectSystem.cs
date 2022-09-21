using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;

using Basic.CompilerLog.Util;

using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OmniSharp.Mef;
using OmniSharp.Models.WorkspaceInformation;
using OmniSharp.Services;

namespace OmniSharp.WebAssembly;

[ExportProjectSystem(ProjectSystemNames.CompilerLoggerProjectSystem)]
internal class CompilerLoggerProjectSystem : IProjectSystem
{
    private readonly CompilerLoggerOptions _options  = new();
    private readonly OmniSharpWorkspace _workspace;
    private readonly TaskCompletionSource _completionSource = new TaskCompletionSource();
    private readonly ILogger _logger;

    public string Key => "CompilerLogger";
    public string Language => LanguageNames.CSharp;
    public IEnumerable<string> Extensions { get; } = new[] { ".cs" };
    public bool EnabledByDefault => true;
    public bool Initialized { get; private set; }

    [ImportingConstructor]
    public CompilerLoggerProjectSystem(OmniSharpWorkspace workspace, ILoggerFactory loggerFactory)
    {
        this._workspace = workspace;
        this._logger = loggerFactory?.CreateLogger<CompilerLoggerProjectSystem>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public void Initalize(IConfiguration configuration)
    {
        if (Initialized) return;

        configuration.Bind(_options);
        _logger.LogInformation($"Reading compiler log from {_options.LogUri}");
        using var compilerLogStream = CompilerLogUtil.GetOrCreateCompilerLogStream(_options.LogUri.IsAbsoluteUri ? _options.LogUri.AbsolutePath : _options.LogUri.OriginalString);
        using var reader = SolutionReader.Create(compilerLogStream);
        var solution = reader.ReadSolution();
        _workspace.AddSolution(solution);
        _completionSource.SetResult();
    }

    public Task<object?> GetProjectModelAsync(string filePath)
    {
        return Task.FromResult<object?>(null);
    }

    public Task<object?> GetWorkspaceModelAsync(WorkspaceInformationRequest request)
    {
        return Task.FromResult<object?>(null);
    }

    public Task WaitForIdleAsync()
    {
        return _completionSource.Task;
    }
}
