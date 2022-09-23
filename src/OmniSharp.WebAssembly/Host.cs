using System;
using System.IO;

using Microsoft.Extensions.Logging;

using OmniSharp.Eventing;
using OmniSharp.Services;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using OmniSharp.Stdio;
using OmniSharp.Protocol;

namespace OmniSharp.WebAssembly;

public class Program
{
    // todo dispose / shutdown /etc.
    private static CancellationTokenSource _source = new();
    private static Host? _host;

    public static async Task<string> InitializeAsync(byte[] compilerLogBytes, TextReader input, ISharedTextWriter outputWriter, ILoggerProvider? loggerProvider = null)
    {
        try
        {
            loggerProvider ??= new SimpleWasmConsoleLoggerProvider();
            var logger = loggerProvider.CreateLogger("InitializeAsync");
            logger.LogInformation($"hello from {typeof(Program).AssemblyQualifiedName}");

            var environment = new OmniSharpEnvironment(logLevel: LogLevel.Trace);
            CompilerLoggerProjectSystem.CompilerLogBytes = compilerLogBytes;
            var configurationResult = new ConfigurationBuilder(environment).Build((builder) => { });
            if (configurationResult.HasError())
            {
                logger.LogInformation("config exception: " + configurationResult.Exception);
                throw configurationResult.Exception;
            }

            var section = configurationResult.Configuration.GetSection("CompilerLogger");

            var serviceProvider = WasmCompositionHostBuilder.CreateDefaultServiceProvider(environment, configurationResult.Configuration, new ConsoleEventEmitter(),
                configureLogging: builder => builder.AddProvider(loggerProvider ?? new SimpleWasmConsoleLoggerProvider()));
            var compositionHostBuilder = new WasmCompositionHostBuilder(serviceProvider)
                        .WithOmniSharpAssemblies().WithAssemblies(new System.Reflection.Assembly[] { typeof(Program).Assembly });

            var composition = compositionHostBuilder.Build(Environment.CurrentDirectory);

            // probably don't need - should be done in languageserver host / start / etc.
            WorkspaceInitializer.Initialize(serviceProvider, composition);

            var workspace = composition.GetExport<OmniSharpWorkspace>();
            var compilation = await workspace.CurrentSolution.Projects.First().GetCompilationAsync();
            var symbols = compilation!.GetSymbolsWithName("Program");
            logger.LogInformation($"Symbol: {symbols.First().Name}, {symbols.First().Kind}");

            // todo cancellation token?
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _host = new Host(input, outputWriter, environment, serviceProvider, compositionHostBuilder, loggerFactory, _source);
            // something weird with tasks / cancellation / readline in the host.start.
            // instead just implement simpler version where we just write start, then call HandleRequest whenever JS calls into us.
            outputWriter.WriteLine(new EventPacket()
            {
                Event = "started"
            });

            return "done";
        }
        catch (Exception ex)
        {
            // Catch the exception and write it out to prevent a huge JS stack trace.
            return ex.ToString();

        }
    }

    public static Task InvokeRequestAsync(string json)
    {
        return _host!.HandleRequestAsync(json);
    }

    // todo stdio emitter
    class ConsoleEventEmitter : IEventEmitter
    {
        public void Emit(string kind, object args)
        {
            Console.WriteLine($"[{kind}]{args}");
        }
    }
}
