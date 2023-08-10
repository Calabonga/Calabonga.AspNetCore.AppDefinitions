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
        var definitions = new List<IAppDefinition>();
        var appDefinitionInfo = builder.Services.BuildServiceProvider().GetService<AppDefinitionCollection>();
        var info = appDefinitionInfo ?? new AppDefinitionCollection();

        foreach (var entryPoint in entryPointsAssembly)
        {
            info.AddEntryPoint(entryPoint.Name);

            var types = entryPoint.Assembly.ExportedTypes.Where(Predicate);
            var instances = types.Select(Activator.CreateInstance).Cast<IAppDefinition>().ToList();

            foreach (var definition in instances)
            {
                info.AddInfo(new AppDefinitionItem(definition, entryPoint.Name, definition.Enabled, definition.Exported));
            }

            var instancesOrdered = instances.Where(x => x.Enabled).OrderBy(x => x.OrderIndex).ToList();
            definitions.AddRange(instancesOrdered);
        }

        foreach (var definition in definitions)
        {
            definition.ConfigureServices(builder);
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("[AppDefinitions]: From {@items}", string.Join(", ", info.EntryPoints));

            foreach (var item in info.Items.OrderBy(x => x.Definition.GetType().Name))
            {
                logger.LogDebug("[AppDefinitions]: {@AppDefinitionName} ({@AssemblyName}) (Enabled: {@Enabled})", item.Definition.GetType().Name, item.AssemblyName, item.Definition.Enabled ? "Yes" : "No");
            }
        }

        builder.Services.AddSingleton(info);
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
        var definitions = source.Services.GetRequiredService<AppDefinitionCollection>();

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("From {Modules} assemblies totally AppDefinitions found: {Count} ", string.Join(", ", definitions.EntryPoints), definitions.Items.Count);
        }

        var instancesOrdered = definitions.Items.Where(x => x.Definition.Enabled).OrderBy(x => x.Definition.OrderIndex).ToList();

        instancesOrdered.ForEach(x => x.Definition.ConfigureApplication(source));

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Total AppDefinitions applied: {Count}", instancesOrdered.Count);
        }
    }
}