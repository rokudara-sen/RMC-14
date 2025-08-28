using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Flipchart;

/// <summary>
/// Stores multiple pages of text for a flipchart.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlipchartComponent : Component
{
    /// <summary>
    /// The text content of each page.
    /// </summary>
    [DataField]
    public List<string> Pages = new() { "" };

    /// <summary>
    /// Index of the currently displayed page.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentPage;
}
