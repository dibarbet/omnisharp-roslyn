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
internal sealed class CompilerLoggerProjectSystem : IProjectSystem, IDisposable
{
    private readonly CompilerLoggerOptions _options = new();
    private readonly OmniSharpWorkspace _workspace;
    private readonly TaskCompletionSource _completionSource = new TaskCompletionSource();
    private readonly ILogger _logger;
    private SolutionReader? _reader;

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
        _reader = SolutionReader.Create(_options.LogUri.IsAbsoluteUri ? _options.LogUri.AbsolutePath : _options.LogUri.OriginalString);
        var solution = _reader.ReadSolution();
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

    private void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }
}
