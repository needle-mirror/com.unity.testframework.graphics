namespace UnityEditor.TestTools.Graphics.UI
{
    // HeatmapManager has been extracted to HeatmapManager.cs as a standalone class.
    // This partial class file maintains the instance field for backward compatibility.
    sealed partial class GraphicsTestsWindow
    {
        readonly HeatmapManager m_HeatmapManager = new();
    }
}
