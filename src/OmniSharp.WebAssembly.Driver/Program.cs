using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using OmniSharp.Eventing;
using OmniSharp.Services;
using System.Threading.Tasks;
using System.Linq;

namespace OmniSharp.WebAssembly.Driver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("main");
        }

        [JSInvokable] // The method is invoked from JavaScript.
        public static string GetName() => $"{nameof(GetName)} from Omnisharp";

        [JSInvokable]
        public static async Task<string> InitializeAsync()
        {
            try
            {
                Console.WriteLine($"hello from {typeof(Program).AssemblyQualifiedName}");

                var compilerLog = await DownloadCompilerLogAsync();

                var environment = new OmniSharpEnvironment(logLevel: LogLevel.Trace);
                var configurationResult = new ConfigurationBuilder(environment).Build((builder) =>
                {
                    var jsonOptions =
$@"{{
  ""CompilerLogger"": {{
    ""LogUri"": ""{compilerLog}""
  }}
}}";
                    _ = builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(jsonOptions)));

                });
                if (configurationResult.HasError())
                {
                    Console.WriteLine("config exception: " + configurationResult.Exception);
                    throw configurationResult.Exception;
                }

                var section = configurationResult.Configuration.GetSection("CompilerLogger");
                Console.WriteLine("section: " + section.GetValue<Uri>("LogUri"));

                var serviceProvider = WasmCompositionHostBuilder.CreateDefaultServiceProvider(environment, configurationResult.Configuration, new ConsoleEventEmitter(),
                    configureLogging: builder => builder.AddProvider(new SimpleWasmConsoleLoggerProvider()));
                var compositionHostBuilder = new WasmCompositionHostBuilder(serviceProvider)
                            .WithOmniSharpAssemblies().WithAssemblies(new System.Reflection.Assembly[] { typeof(Host).Assembly });

                var composition = compositionHostBuilder.Build(Environment.CurrentDirectory);

                WorkspaceInitializer.Initialize(serviceProvider, composition);

                var workspace = composition.GetExport<OmniSharpWorkspace>();
                var compilation = await workspace.CurrentSolution.Projects.First().GetCompilationAsync();
                var diagnostics = compilation.GetDiagnostics();
                Console.WriteLine($"Diagnostics:{string.Join(Environment.NewLine, diagnostics)}");
                var symbols = compilation.GetSymbolsWithName("Program");
                Console.WriteLine($"Symbol: {symbols.First().Name}, {symbols.First().Kind}");
            }
            catch (Exception ex)
            {
                // Catch the exception and write it out to prevent a huge JS stack trace.
                Console.WriteLine(ex.ToString());
            }

            return "done!";
        }

        private static async Task<string> DownloadCompilerLogAsync()
        {
            var compilerLogSource = "http://localhost:8000/msbuild.compilerlog";
            Console.WriteLine($"Downloading compiler log from {compilerLogSource}...");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(@"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36 Edg/105.0.1343.42");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var req = new HttpRequestMessage(HttpMethod.Get, compilerLogSource);
                var response = await client.GetStreamAsync(compilerLogSource);

                Console.WriteLine("Done");
                var destination = Path.Combine(Environment.CurrentDirectory, "msbuild.compilerlog");
                Console.WriteLine($"Saving to {destination}...");

                var stream = response;
                using (var fs = File.OpenWrite(destination))
                {
                    stream.CopyTo(fs);
                }

                Console.WriteLine($"Done");
                Console.WriteLine("Downloaded exists? " + File.Exists(destination));

                // in json \ is an escape char
                destination = destination.Replace(@"\", "/");
                return destination;
            }
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
