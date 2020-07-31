using System.ComponentModel;

namespace Kombatant.Enums
{
    /// <summary>
    /// Enum for the automatic target selection modes.
    /// </summary>
    public enum TargetingMode
    {
        [Description("不自动选择敌人")]
        None = 0,
        [Description("最近的敌人")]
        Nearest = 10,
        [Description("跟队长选择目标")]
        AssistLeader = 20,
        [Description("跟坦克选择目标")]
        AssistTank = 30,
        [Description("跟最高等级的队员选择目标")]
        AssistHighestLvl = 40,
        [Description("跟AoE选择目标")]
        BestAoE = 50,
        [Description("选择白名单内的敌人")]
        OnlyWhitelisted = 60,
        [Description("跟选定的角色选择目标")]
        AssistFixedCharacter = 70,
        [Description("血量最低的敌人")]
        LowestHealth = 100,
        [Description("血量最低百分比的敌人")]
        LowestHealthPercent = 110,
        [Description("血量最高的敌人")]
        HighestHealth = 120,
        [Description("血量最高百分比的敌人")]
        HighestHealthPercent = 130,
    }
}