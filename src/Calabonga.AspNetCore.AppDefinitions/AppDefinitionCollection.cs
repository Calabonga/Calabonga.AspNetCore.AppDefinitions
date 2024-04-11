namespace Calabonga.AspNetCore.AppDefinitions;

/// <summary>
/// Information about collection of <see cref="IAppDefinition"/>
/// </summary>
internal sealed class AppDefinitionCollection
{
    private IList<AppDefinitionItem> Items { get; } = new List<AppDefinitionItem>();

    /// <summary>
    /// Entry points found names
    /// </summary>
    internal IList<string> EntryPoints { get; } = new List<string>();

    /// <summary>
    /// Adds collected information to collection
    /// </summary>
    /// <param name="definition"></param>
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

    /// <summary>
    /// Returns ordered and enabled items 
    /// </summary>
    internal IEnumerable<AppDefinitionItem> GetEnabled()
        => Items
            .Where(x => x.Definition.Enabled)
            .OrderBy(x => x.Definition.ServiceOrderIndex);

    /// <summary>
    /// 
    /// </summary>
    internal IEnumerable<AppDefinitionItem> GetDistinct()
        => GetEnabled()
            .Select(x => new { x.Definition.GetType().Name, AppDefinitionItem = x })
            .DistinctBy(x => x.Name)
            .Select(x => x.AppDefinitionItem);
}