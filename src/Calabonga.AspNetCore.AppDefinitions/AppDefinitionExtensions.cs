using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        foreach (var entryPoint in entryPointsAssembly)
        {
            var types = entryPoint.Assembly.ExportedTypes.Where(x => !x.IsAbstract && typeof(IAppDefinition).IsAssignableFrom(x));
            var instances = types.Select(Activator.CreateInstance).Cast<IAppDefinition>().ToList();
            var instancesOrdered = instances.Where(x => x.Enabled).OrderBy(x => x.OrderIndex).ToList();
            if (builder.Environment.IsDevelopment())
            {
                logger.LogDebug("[AppDefinitions] Founded: {0}. Enabled: {1}", instances.Count, instancesOrdered.Count);
                logger.LogDebug("[AppDefinitions] Registered [{0}]", string.Join(", ", instancesOrdered.Select(x => x.GetType().Name).ToArray()));
            }

            definitions.AddRange(instancesOrdered);
        }

        definitions.ForEach(app => app.ConfigureServices(source, builder));
        source.AddSingleton(definitions as IReadOnlyCollection<IAppDefinition>);

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
        var environment = source.Services.GetRequiredService<IWebHostEnvironment>();
        var definitions = source.Services.GetRequiredService<IReadOnlyCollection<IAppDefinition>>();
        var instancesOrdered = definitions.Where(x => x.Enabled).OrderBy(x => x.OrderIndex).ToList();
        
        instancesOrdered.ForEach(x => x.ConfigureApplication(source));

        if (environment.IsDevelopment())
        {
            logger.LogDebug("Total application definitions configured {0}", instancesOrdered.Count);
        }
    }

}