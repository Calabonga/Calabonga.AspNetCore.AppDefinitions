using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Calabonga.AspNetCore.AppDefinitions;

/// <summary>
/// Base implementation for <see cref="IAppDefinition"/>
/// </summary>
public abstract class AppDefinition : IAppDefinition
{
    /// <summary>
    /// Configure services for current application
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }

    /// <summary>
    /// Configure application for current application
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public virtual void ConfigureApplication(WebApplication app, IWebHostEnvironment env) { }

    /// <summary>
    /// Order index for including into pipeline. Default value is 0 for, that's why order index can be undefined.
    /// </summary>
    public virtual int OrderIndex => 0;

    /// <summary>
    /// Enable or disable to register into pipeline for the current application definition
    /// </summary>
    public virtual bool Enabled => true;
}