using Microsoft.AspNetCore.Builder;

namespace Calabonga.AspNetCore.AppDefinitions;

/// <summary>
/// Base implementation for <see cref="IAppDefinition"/>
/// </summary>
public abstract class AppDefinition : IAppDefinition
{
    /// <summary>
    /// Configure services for current application
    /// </summary>
    /// <param name="builder">instance of <see cref="WebApplicationBuilder"/></param>
    public virtual void ConfigureServices(WebApplicationBuilder builder) { }

    /// <summary>
    /// Configure application for current application
    /// </summary>
    /// <param name="app"></param>
    public virtual void ConfigureApplication(WebApplication app) { }

    /// <summary>
    /// Order index for including into pipeline. Default value is 0 for, that's why order index can be undefined.
    /// </summary>
    public virtual int ServiceOrderIndex => 0;

    /// <summary>
    /// Order index for including into pipeline for ConfigureApplication() . Default value is 0 for, that's why order index can be undefined.
    /// </summary>
    public int ApplicationOrderIndex => 0;

    /// <summary>
    /// Enable or disable to register into pipeline for the current application Definition.
    /// </summary>
    /// <remarks>Default values is <c>True</c></remarks>
    public virtual bool Enabled { get; protected set; } = true;

    /// <summary>
    /// Enables or disables export definition as a content for module that can be exported.
    /// </summary>
    /// /// <remarks>Default values is <c>False</c></remarks>
    public virtual bool Exported { get; protected set; }
}