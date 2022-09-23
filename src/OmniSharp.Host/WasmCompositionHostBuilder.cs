using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Eventing;
using OmniSharp.FileSystem;
using OmniSharp.FileWatching;
using OmniSharp.Mef;
using OmniSharp.MSBuild.Discovery;
using OmniSharp.Options;
using OmniSharp.Roslyn;
using OmniSharp.Roslyn.Utilities;
using OmniSharp.Services;

namespace OmniSharp
{
    public class WasmCompositionHostBuilder : CompositionHostBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<Assembly> _assemblies;
        private readonly IEnumerable<ExportDescriptorProvider> _exportDescriptorProviders;

        public WasmCompositionHostBuilder(
            IServiceProvider serviceProvider,
            IEnumerable<Assembly> assemblies = null,
            IEnumerable<ExportDescriptorProvider> exportDescriptorProviders = null) : base(serviceProvider, assemblies, exportDescriptorProviders)
        {
            _serviceProvider = serviceProvider;
            _assemblies = assemblies ?? Array.Empty<Assembly>();
            _exportDescriptorProviders = exportDescriptorProviders ?? Array.Empty<ExportDescriptorProvider>();
        }

        public override CompositionHost Build(string workingDirectory)
        {
            var options = _serviceProvider.GetRequiredService<IOptionsMonitor<OmniSharpOptions>>();
            var memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var assemblyLoader = _serviceProvider.GetRequiredService<IAssemblyLoader>();
            var analyzerAssemblyLoader = _serviceProvider.GetRequiredService<IAnalyzerAssemblyLoader>();
            var environment = _serviceProvider.GetRequiredService<IOmniSharpEnvironment>();
            var eventEmitter = _serviceProvider.GetRequiredService<IEventEmitter>();
            var config = new ContainerConfiguration();

            var fileSystemWatcher = _serviceProvider.GetRequiredService<IFileSystemWatcher>();
            var fileSystemNotifier = _serviceProvider.GetRequiredService<IFileSystemNotifier>();

            var logger = loggerFactory.CreateLogger<CompositionHostBuilder>();

            config = config
                .WithProvider(MefValueProvider.From(_serviceProvider))
                .WithProvider(MefValueProvider.From(fileSystemWatcher))
                .WithProvider(MefValueProvider.From(fileSystemNotifier))
                .WithProvider(MefValueProvider.From(memoryCache))
                .WithProvider(MefValueProvider.From(loggerFactory))
                .WithProvider(MefValueProvider.From(environment))
                .WithProvider(MefValueProvider.From(options.CurrentValue))
                .WithProvider(MefValueProvider.From(options))
                .WithProvider(MefValueProvider.From(options.CurrentValue.FormattingOptions))
                .WithProvider(MefValueProvider.From(assemblyLoader))
                .WithProvider(MefValueProvider.From(analyzerAssemblyLoader))
                .WithProvider(MefValueProvider.From(eventEmitter));

            foreach (var exportDescriptorProvider in _exportDescriptorProviders)
            {
                config = config.WithProvider(exportDescriptorProvider);
            }

            var parts = _assemblies
                .Where(a => a != null)
                .Concat(new[]
                {
                    typeof(OmniSharpWorkspace).GetTypeInfo().Assembly, typeof(IRequest).GetTypeInfo().Assembly,
                    typeof(FileSystemHelper).GetTypeInfo().Assembly
                })
                .Distinct()
                .SelectMany(a => SafeGetTypes(a))
                .ToArray();

            config = config.WithParts(parts);

            return config.CreateContainer();
        }

        public new static IServiceProvider CreateDefaultServiceProvider(
            IOmniSharpEnvironment environment,
            IConfigurationRoot configuration,
            IEventEmitter eventEmitter,
            IServiceCollection services = null,
            Action<ILoggingBuilder> configureLogging = null)
        {
            services ??= new ServiceCollection();

            services.AddSingleton(environment);
            services.AddSingleton(eventEmitter);

            // Required by omnisharp workspace.
            services.TryAddSingleton(_ => new ManualFileSystemWatcher());
            services.TryAddSingleton<IFileSystemNotifier>(sp => sp.GetRequiredService<ManualFileSystemWatcher>());
            services.TryAddSingleton<IFileSystemWatcher>(sp => sp.GetRequiredService<ManualFileSystemWatcher>());

            // Caching
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
            services.AddSingleton(sp => ShadowCopyAnalyzerAssemblyLoader.Instance);
            services.AddOptions();

            // Setup the options from configuration
            services.Configure<OmniSharpOptions>(configuration)
                .PostConfigure<OmniSharpOptions>(OmniSharpOptions.PostConfigure);
            services.AddSingleton(configuration);
            services.AddSingleton<IConfiguration>(configuration);

            services.AddLogging(builder =>
            {
                var workspaceInformationServiceName = typeof(WorkspaceInformationService).FullName;
                var projectEventForwarder = typeof(ProjectEventForwarder).FullName;

                builder.AddFilter(
                    (category, logLevel) =>
                        environment.LogLevel <= logLevel &&
                        category.StartsWith("OmniSharp", StringComparison.OrdinalIgnoreCase) &&
                        !category.Equals(workspaceInformationServiceName, StringComparison.OrdinalIgnoreCase) &&
                        !category.Equals(projectEventForwarder, StringComparison.OrdinalIgnoreCase));

                configureLogging?.Invoke(builder);
            });

            return services.BuildServiceProvider();
        }

        public override CompositionHostBuilder WithOmniSharpAssemblies()
        {
            var assemblies = DiscoverOmnisharpAssembliesWasm();

            return new WasmCompositionHostBuilder(
                _serviceProvider,
                _assemblies.Concat(assemblies).Distinct()
            );
        }

        public override CompositionHostBuilder WithAssemblies(params Assembly[] assemblies)
        {
            return new WasmCompositionHostBuilder(
                _serviceProvider,
                _assemblies.Concat(assemblies).Distinct()
            );
        }

        private List<Assembly> DiscoverOmnisharpAssembliesWasm()
        {
            var logger = _serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<WasmCompositionHostBuilder>();
            // Iterate through all runtime libraries in the dependency context and
            // load them if they depend on OmniSharp.
            var assemblies = new List<Assembly>();

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in loadedAssemblies)
            {
                logger.LogDebug($"Finding dependencies for {assembly.GetName().Name}");
                var referencedAssemblies = assembly.GetReferencedAssemblies();
                foreach (var referencedAssembly in referencedAssemblies)
                {
                    if (CompositionHostBuilder.DependsOnOmniSharp(referencedAssembly.Name))
                    {
                        logger.LogDebug($"Assembly {referencedAssembly.Name} references omnisharp");
                        assemblies.Add(assembly);
                        continue;
                    }
                }
            }

            return assemblies;
        }
    }
}
