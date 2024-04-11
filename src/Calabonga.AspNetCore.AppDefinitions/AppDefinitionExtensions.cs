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
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<IAppDefinition>>();

        try
        {
            var modulesFolder = Path.Combine(builder.Environment.ContentRootPath, modulesFolderPath);

            if (!Directory.Exists(modulesFolder))
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("[Error]: Directory not exists {ModuleName}", modulesFolder);
                }
                throw new DirectoryNotFoundException(modulesFolder);
            }

            var types = new List<Type>();
            types.AddRange(entryPointsAssembly);

            var modulesDirectory = new DirectoryInfo(modulesFolderPath);
            var modules = modulesDirectory.GetFiles("*.dll");
            if (!modules.Any())
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("[Warning]: No modules found in folder {ModuleName}", modulesFolder);
                }
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
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);
            throw;
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
        try
        {
            var appDefinitionInfo = builder.Services.BuildServiceProvider().GetService<AppDefinitionCollection>();
            var definitionCollection = appDefinitionInfo ?? new AppDefinitionCollection();

            foreach (var entryPoint in entryPointsAssembly)
            {
                definitionCollection.AddEntryPoint(entryPoint.Name);

                var types = entryPoint.Assembly.ExportedTypes.Where(Predicate);
                var instances = types.Select(Activator.CreateInstance).Cast<IAppDefinition>().Where(x => x.Enabled).OrderBy(x => x.ServiceOrderIndex).ToList();

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
                    logger.LogDebug("[AppDefinitions ConfigureServices with order index {@ServiceOrderIndex}]: {@AssemblyName}:{@AppDefinitionName} is {EnabledOrDisabled} {ExportEnabled}",
                        item.Definition.ServiceOrderIndex,
                        item.AssemblyName,
                        item.Definition.GetType().Name,
                        item.Enabled ? "enabled" : "disabled",
                        item.Exported ? "(exportable)" : string.Empty);
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

            logger.LogWarning("[AppDefinitions skipped ConfigureServices: {Count}", skipped.Count);
            foreach (var item in skipped)
            {
                logger.LogWarning("[AppDefinitions skipped ConfigureServices]: {@AssemblyName}:{@AppDefinitionName} is {EnabledOrDisabled}",
                    item.AssemblyName,
                    item.Definition.GetType().Name,
                    item.Enabled ? "enabled" : "disabled");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);
            throw;
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

        var items = definitionCollection.GetDistinct().OrderBy(x => x.Definition.ApplicationOrderIndex).ToList();

        foreach (var item in items)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("[AppDefinitions ConfigureApplication with order index {@ApplicationOrderIndex}]: {@AssemblyName}:{@AppDefinitionName} is {EnabledOrDisabled}",
                    item.Definition.ApplicationOrderIndex,
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

        logger.LogWarning("[AppDefinitions skipped ConfigureApplication: {Count}", skipped.Count);
        foreach (var item in skipped)
        {
            logger.LogWarning("[AppDefinitions skipped ConfigureApplication]: {@AssemblyName}:{@AppDefinitionName} is {EnabledOrDisabled} {ExportEnabled}",
                item.AssemblyName,
                item.Definition.GetType().Name,
                item.Enabled ? "enabled" : "disabled",
                item.Exported ? "(exportable)" : string.Empty);
        }

        logger.LogInformation("[AppDefinitions applied: {Count} of {Total}", items.Count, definitionCollection.GetEnabled().Count());

    }
}