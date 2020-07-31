using System.ComponentModel;

namespace Kombatant.Enums
{
    public enum WaypointGenerationMode
    {
        [Description("无")]
        None = 0,
        [Description("使用 NavGraph")]
        NavGraph = 10,
        [Description("使用 Offmesh navigation")]
        Offmesh = 20
    }
}