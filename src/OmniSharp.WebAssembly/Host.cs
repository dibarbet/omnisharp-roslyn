using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OmniSharp.Eventing;
using OmniSharp.Services;
using System.Threading.Tasks;
using System.Linq;

namespace OmniSharp.WebAssembly;

public class Host : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public static async Task<string> InitializeAsync(byte[] compilerLogBytes, ILoggerProvider? loggerProvider = null)
    {
        try
        {
            Console.WriteLine($"hello from {typeof(Host).AssemblyQualifiedName}");

            var environment = new OmniSharpEnvironment(logLevel: LogLevel.Trace);
            CompilerLoggerProjectSystem.CompilerLogBytes = compilerLogBytes;
            var configurationResult = new ConfigurationBuilder(environment).Build((builder) => { });
            if (configurationResult.HasError())
            {
                Console.WriteLine("config exception: " + configurationResult.Exception);
                throw configurationResult.Exception;
            }

            var section = configurationResult.Configuration.GetSection("CompilerLogger");
            Console.WriteLine("section: " + section.GetValue<Uri>("LogUri"));

            var serviceProvider = WasmCompositionHostBuilder.CreateDefaultServiceProvider(environment, configurationResult.Configuration, new ConsoleEventEmitter(),
                configureLogging: builder => builder.AddProvider(loggerProvider ?? new SimpleWasmConsoleLoggerProvider()));
            var compositionHostBuilder = new WasmCompositionHostBuilder(serviceProvider)
                        .WithOmniSharpAssemblies().WithAssemblies(new System.Reflection.Assembly[] { typeof(Host).Assembly });

            var composition = compositionHostBuilder.Build(Environment.CurrentDirectory);

            WorkspaceInitializer.Initialize(serviceProvider, composition);

            var workspace = composition.GetExport<OmniSharpWorkspace>();
            var compilation = await workspace.CurrentSolution.Projects.First().GetCompilationAsync();
            // var diagnostics = compilation.GetDiagnostics();
            // Console.WriteLine($"Diagnostics:{string.Join(Environment.NewLine, diagnostics)}");
            var symbols = compilation!.GetSymbolsWithName("Program");
            return $"Symbol: {symbols.First().Name}, {symbols.First().Kind}";
        }
        catch (Exception ex)
        {
            // Catch the exception and write it out to prevent a huge JS stack trace.
            return ex.ToString();

        }
    }

    class ConsoleEventEmitter : IEventEmitter
    {
        public void Emit(string kind, object args)
        {
            Console.WriteLine($"[{kind}]{args}");
        }
    }
}
