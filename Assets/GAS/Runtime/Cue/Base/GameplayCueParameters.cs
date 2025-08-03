namespace GAS.Runtime
{
    public struct GameplayCueParameters
    {
        /// <summary>
        /// 源游戏效果规格
        /// </summary>
        public GameplayEffectSpec sourceGameplayEffectSpec;
        
        /// <summary>
        /// 源能力规格
        /// </summary>
        public AbilitySpec sourceAbilitySpec;
        
        /// <summary>
        /// 自定义参数
        /// </summary>
        public object[] customArguments;
        // AggregatedSourceTags
        // AggregatedTargetTags
        // EffectContext
        // Magnitude
    }
}