using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using OmniSharp.Eventing;
using OmniSharp.Services;

namespace OmniSharp.WebAssembly.Driver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"hello from {typeof(Program).AssemblyQualifiedName}");

                var environment = new OmniSharpEnvironment(logLevel: LogLevel.Trace);
                var configurationResult = new ConfigurationBuilder(environment).Build((builder) =>
                {
                    _ = builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(
@"{
  ""CompilerLogger"": {
    ""LogUri"": ""C:\\Users\\dabarbet\\source\\repos\\ConsoleApp4\\msbuild.compilerlog""
  }
}")));

                });
                if (configurationResult.HasError())
                {
                    Console.WriteLine("config exception: " + configurationResult.Exception);
                }

                var section = configurationResult.Configuration.GetSection("CompilerLogger");
                Console.WriteLine("section: " + section.GetValue<Uri>("LogUri"));

                var serviceProvider = WasmCompositionHostBuilder.CreateDefaultServiceProvider(environment, configurationResult.Configuration, new ConsoleEventEmitter(),
                    configureLogging: builder => builder.AddProvider(new SimpleWasmConsoleLoggerProvider()));
                var compositionHostBuilder = new WasmCompositionHostBuilder(serviceProvider)
                            .WithOmniSharpAssemblies().WithAssemblies(new System.Reflection.Assembly[] { typeof(Host).Assembly });
                            
                var composition = compositionHostBuilder.Build(Environment.CurrentDirectory);

                WorkspaceInitializer.Initialize(serviceProvider, composition);

                Console.WriteLine($"project systems: {string.Join(",", composition.GetExports<IProjectSystem>())}");
                
            }
            catch (Exception ex)
            {
                // Catch the exception and write it out to prevent a huge JS stack trace.
                Console.WriteLine(ex.ToString());
            }
        }

        [JSInvokable] // The method is invoked from JavaScript.
        public static string GetName() => $"{nameof(GetName)} from Omnisharp";
    }

    class ConsoleEventEmitter : IEventEmitter
    {
        public void Emit(string kind, object args)
        {
            Console.WriteLine($"[{kind}]{args}");
        }
    }
}
