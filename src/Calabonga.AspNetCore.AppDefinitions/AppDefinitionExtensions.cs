using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Calabonga.AspNetCore.AppDefinitions;

public static class AppDefinitionExtensions
{
    /// <summary>
    /// Finding all definitions in your project and include their into pipeline.<br/>
    /// Using <see cref="IServiceCollection"/> for registration.
    /// </summary>
    /// <remarks>
    /// When executing on development environment there are more diagnostic information available on console.
    /// </remarks>
    /// <param name="source"></param>
    /// <param name="builder"></param>
    /// <param name="entryPointsAssembly"></param>
    public static void AddDefinitions(this IServiceCollection source, WebApplicationBuilder builder, params Type[] entryPointsAssembly)
    {
        var logger = source.BuildServiceProvider().GetRequiredService<ILogger<AppDefinition>>();
        var definitions = new List<IAppDefinition>();
        var appDefinitionInfo = source.BuildServiceProvider().GetService<AppDefinitionCollection>();
        var info = appDefinitionInfo ?? new AppDefinitionCollection();

        foreach (var entryPoint in entryPointsAssembly)
        {
            info.AddEntryPoint(entryPoint.Name);


            var types = entryPoint.Assembly.ExportedTypes.Where(x => !x.IsAbstract && typeof(IAppDefinition).IsAssignableFrom(x));
            var instances = types.Select(Activator.CreateInstance).Cast<IAppDefinition>().ToList();
            //if (logger.IsEnabled(LogLevel.Debug))
            //{
            //    logger.LogDebug("AppDefinitions Founded: {@AppDefinitionsCountTotal}.", instances.Count);
            //}

            foreach (var definition in instances)
            {
                info.AddInfo(new AppDefinitionItem(definition));
            }

            var instancesOrdered = instances.Where(x => x.Enabled).OrderBy(x => x.OrderIndex).ToList();
            definitions.AddRange(instancesOrdered);
        }

        foreach (var definition in definitions)
        {
            definition.ConfigureServices(source, builder);
        }
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("[AppDefinitions]: From {@items}", string.Join(", ", info.EntryPoints));


            foreach (var item in info.Items.OrderBy(x => x.Definition.GetType().Name))
            {
                logger.LogDebug("[AppDefinitions]: {@AppDefinitionName} (Enabled: {@Enabled})", item.Definition.GetType().Name, item.Definition.Enabled ? "Yes" : "No");
            }
        }

        source.AddSingleton(info);
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