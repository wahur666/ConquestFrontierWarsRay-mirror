using System.Numerics;

namespace RaySharp;

internal sealed class UiState
{
    public bool MenuOpen { get; set; }
    public bool IsSelecting { get; set; }
    public bool ShowTestSphereWire { get; set; }
    public Vector2 SelectionStart { get; set; }
    public Vector2 SelectionCurrent { get; set; }
    public string Status { get; set; } = "Unit selected. Right-click the plane to move, or left-drag to reselect.";
}
