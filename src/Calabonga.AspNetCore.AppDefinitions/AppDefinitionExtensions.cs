using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Calabonga.AspNetCore.AppDefinitions;

/// <summary>
/// Extension for <see cref="WebApplicationBuilder"/>
/// </summary>
public static class AppDefinitionExtensions
{
    /// <summary>
    /// Finding all definitions in your project and include their into pipeline. Modules from third party *.dll find too.<br/>
    /// Using <see cref="IServiceCollection"/> for registration.
    /// </summary>
    /// <remarks>
    /// When executing on development environment there are more diagnostic information available on console.
    /// </remarks>
    /// <param name="builder"></param>
    /// <param name="modulesFolderPath"></param>
    /// <param name="entryPointsAssembly"></param>
    public static void AddDefinitionsWithModules(this WebApplicationBuilder builder, string modulesFolderPath, params Type[] entryPointsAssembly)
    {
        var modulesFolder = Path.Combine(builder.Environment.ContentRootPath, modulesFolderPath);

        if (!Directory.Exists(modulesFolder))
        {
            throw new DirectoryNotFoundException(modulesFolder);
        }

        var types = new List<Type>();
        types.AddRange(entryPointsAssembly);

        var modulesDirectory = new DirectoryInfo(modulesFolderPath);
        var modules = modulesDirectory.GetFiles("*.dll");
        if (!modules.Any())
        {
            return;
        }

        foreach (var fileInfo in modules)
        {
            var module = Assembly.LoadFile(fileInfo.FullName);
            var typesAll = module.GetExportedTypes();
            var typesDefinition = typesAll
                .Where(Predicate)
                .ToList();

            var instances = typesDefinition.Select(Activator.CreateInstance)
                .Cast<IAppDefinition>()
                .Where(x => x.Enabled && x.Exported)
                .Select(x => x.GetType())
                .ToList();

            types.AddRange(instances);
        }

        if (types.Any())
        {
            AddDefinitions(builder, types.ToArray());
        }
    }

    /// <summary>
    /// Finds an AppDefinition in the list of types
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool Predicate(Type type) => type is { IsAbstract: false, IsInterface: false } && typeof(AppDefinition).IsAssignableFrom(type);

    /// <summary>
    /// Finding all definitions in your project and include their into pipeline.<br/>
    /// Using <see cref="IServiceCollection"/> for registration.
    /// </summary>
    /// <remarks>
    /// When executing on development environment there are more diagnostic information available on console.
    /// </remarks>
    /// <param name="builder"></param>
    /// <param name="entryPointsAssembly"></param>
    public static void AddDefinitions(this WebApplicationBuilder builder, params Type[] entryPointsAssembly)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<IAppDefinition>>();
        var appDefinitionInfo = builder.Services.BuildServiceProvider().GetService<AppDefinitionCollection>();
        var definitionCollection = appDefinitionInfo ?? new AppDefinitionCollection();

        foreach (var entryPoint in entryPointsAssembly)
        {
            definitionCollection.AddEntryPoint(entryPoint.Name);

            var types = entryPoint.Assembly.ExportedTypes.Where(Predicate);
            var instances = types.Select(Activator.CreateInstance).Cast<IAppDefinition>().Where(x => x.Enabled).OrderBy(x => x.OrderIndex).ToList();

            foreach (var definition in instances)
            {
                definitionCollection.AddInfo(new AppDefinitionItem(definition, entryPoint.Name, definition.Enabled, definition.Exported));
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[AppDefinitions entry points found]: {@items}", string.Join(", ", definitionCollection.EntryPoints));
        }

        var items = definitionCollection.GetDistinct().ToList();

        foreach (var item in items)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("[AppDefinitions for ConfigureServices]: {@AssemblyName}:{@AppDefinitionName} is {EnabledOrDisabled} {ExportEnabled}",
                    item.AssemblyName,
                    item.Definition.GetType().Name,
                    item.Enabled ? "enabled" : "disabled",
                    item.Exported ? "(exported)" : "export disabled");
            }

            item.Definition.ConfigureServices(builder);
        }

        builder.Services.AddSingleton(definitionCollection);

        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var skipped = definitionCollection.GetEnabled().Except(items).ToList();
        if (!skipped.Any())
        {
            return;
        }

        logger.LogWarning("[AppDefinitions skipped for ConfigureServices: {Count}", skipped.Count);
        foreach (var item in skipped)
        {
            logger.LogWarning("[AppDefinitions skipped for ConfigureServices]: {@AssemblyName}:{@AppDefinitionName} is {EnabledOrDisabled}",
                item.AssemblyName,
                item.Definition.GetType().Name,
                item.Enabled ? "enabled" : "disabled");
        }

    }

    /// <summary>
    /// Finding all definitions in your project and include their into pipeline.<br/>
    /// Using <see cref="WebApplication"/> for registration.
    /// </summary>
    /// <remarks>
    /// When executing on development environment there are more diagnostic information available on console.
    /// </remarks>
    /// <param name="source"></param>
    public static void UseDefinitions(this WebApplication source)
    {
        var logger = source.Services.GetRequiredService<ILogger<AppDefinition>>();
        var definitionCollection = source.Services.GetRequiredService<AppDefinitionCollection>();

        var items = definitionCollection.GetDistinct().ToList();

        foreach (var item in items)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("[AppDefinitions for ConfigureApplication]: {@AssemblyName}:{@AppDefinitionName} is {EnabledOrDisabled}",
                    item.AssemblyName,
                    item.Definition.GetType().Name,
                    item.Enabled
                        ? "enabled"
                        : "disabled");
            }

            item.Definition.ConfigureApplication(source);
        }

        if (!logger.IsEnabled(LogLevel.Debug))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("[AppDefinitions applied: {Count} of {Total}", items.Count, definitionCollection.GetEnabled().Count());
            }
            return;
        }

        var skipped = definitionCollection.GetEnabled().Except(items).ToList();
        if (!skipped.Any())
        {
            logger.LogInformation("[AppDefinitions applied: {Count} of {Total}", items.Count, definitionCollection.GetEnabled().Count());
            return;
        }

        logger.LogWarning("[AppDefinitions skipped for ConfigureApplication: {Count}", skipped.Count);
        foreach (var item in skipped)
        {
            logger.LogWarning("[AppDefinitions skipped for ConfigureApplication]: {@AssemblyName}:{@AppDefinitionName} is {EnabledOrDisabled} {ExportEnabled}",
                item.AssemblyName,
                item.Definition.GetType().Name,
                item.Enabled ? "enabled" : "disabled",
                item.Exported ? "(exported)" : "export disabled");
        }

        logger.LogInformation("[AppDefinitions applied: {Count} of {Total}", items.Count, definitionCollection.GetEnabled().Count());

    }
}