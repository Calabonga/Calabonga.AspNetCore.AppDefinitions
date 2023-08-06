namespace Calabonga.AspNetCore.AppDefinitions;

/// <summary>
/// Information about collection of <see cref="IAppDefinition"/>
/// </summary>
internal sealed class AppDefinitionCollection
{
    internal IList<AppDefinitionItem> Items { get; } = new List<AppDefinitionItem>();

    internal IList<string> EntryPoints { get; } = new List<string>();

    internal void AddInfo(AppDefinitionItem definition)
    {
        var exists = Items.FirstOrDefault(x => x == definition);
        if (exists is null)
        {
            Items.Add(definition);
        }
    }

    /// <summary>
    /// Adding founded item to collection
    /// </summary>
    /// <param name="entryPointName"></param>
    public void AddEntryPoint(string entryPointName) => EntryPoints.Add(entryPointName);
}

/// <summary>
/// Information about <see cref="IAppDefinition"/>
/// </summary>
/// <param name="Definition"></param>
public sealed record AppDefinitionItem(IAppDefinition Definition, string AssemblyName, bool Enabled, bool Exported);