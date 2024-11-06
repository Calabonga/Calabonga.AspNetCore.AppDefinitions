using Microsoft.AspNetCore.Builder;

namespace Calabonga.AspNetCore.AppDefinitions;

/// <summary>
/// Application Definition interface abstraction. A unit of the application.
/// </summary>
public interface IAppDefinition
{
    /// <summary>
    /// Configure services for current application
    /// </summary>
    /// <param name="builder">instance of <see cref="WebApplicationBuilder"/></param>
    void ConfigureServices(WebApplicationBuilder builder);

    /// <summary>
    /// Configure application for current application
    /// </summary>
    /// <param name="app"></param>
    void ConfigureApplication(WebApplication app);

    /// <summary>
    /// Order index for including into pipeline for ConfigureServices(). Default value is 0 for, that's why order index can be undefined.
    /// </summary>
    int ServiceOrderIndex { get; }

    /// <summary>
    /// Order index for including into pipeline for ConfigureApplication() . Default value is 0 for, that's why order index can be undefined.
    /// </summary>
    int ApplicationOrderIndex { get; }

    /// <summary>
    /// Enable or disable to register into pipeline for the current application Definition.
    /// </summary>
    /// <remarks>Default values is <c>True</c></remarks>
    bool Enabled { get; }

    /// <summary>
    /// Enables or disables export definition as a content for module that can be exported.
    /// </summary>
    /// /// <remarks>Default values is <c>False</c></remarks>
    bool Exported { get; }
}
