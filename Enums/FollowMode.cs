using System.ComponentModel;

namespace Kombatant.Enums
{
    /// <summary>
    /// Enum for the different follow modes.
    /// </summary>
    public enum FollowMode
    {
        [Description("不跟随")]
        None = 0,
        [Description("Follow targeted character")]
        // [Description("Follow targeted character")]
        TargetedCharacter = 10,
        [Description("跟随特定的角色")]
        // [Description("Follow specified character")]
        FixedCharacter = 20,
        [Description("跟随队长")]
        // [Description("Follow party leader")]
        PartyLeader = 30,
        [Description("跟随坦克")]
        // [Description("Follow party tank")]
        Tank = 40
    }
}