using System.Linq;
using GAS.General;

namespace GAS.Runtime
{
    public class GameplayEffectAsset : IGameplayEffectData
    {
        private const string GRP_BASE = "Base";
        private const string GRP_BASE_H = "Base/H";
        private const string GRP_BASE_H_LEFT = "Base/H/Left";
        private const string GRP_BASE_H_RIGHT = "Base/H/Right";

        private const string GRP_DATA = "Data";
        private const string GRP_DATA_H = "Data/H";
        private const string GRP_DATA_TAG = "Data/H/Tags";
        private const string GRP_DATA_MOD = "Data/H/Modifiers";
        private const string GRP_DATA_CUE = "Data/H/Cues";
        private const string GRP_DATA_H2 = "Data/H2";
        private const string GRP_DATA_STACK = "Data/H2/Stack";
        private const string GRP_DATA_GRANTED_ABILITIES = "Data/H2/GrantedAbilities";

        private const int WIDTH_LABEL = 70;

        private const string ERROR_NONE_CUE = "Cue CAN NOT be NONE!";
        private const string ERROR_DURATION = "Duration must be > 0.";
        private const string ERROR_PERIOD_GE_NONE = "Period GameplayEffect CAN NOT be NONE!";
        private const string ERROR_GRANTED_ABILITY_INVALID = "存在无效的Ability!";

        #region Base Info

        /// <summary>
        /// 效果描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 资产名称
        /// </summary>
        public string Name { get; set; }

        #endregion Base Info

        #region Policy

        /// <summary>
        /// 效果持续策略
        /// </summary>
        public EffectsDurationPolicy DurationPolicy { get; set; } = EffectsDurationPolicy.Instant;

        /// <summary>
        /// 效果持续时间（秒）
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// 效果周期间隔（秒）
        /// </summary>
        public float Period { get; set; }

        /// <summary>
        /// 周期执行的游戏效果
        /// </summary>
        public GameplayEffectAsset PeriodExecution { get; set; }

        #endregion Policy

        #region Stack

        /// <summary>
        /// 效果堆叠配置
        /// </summary>
        public GameplayEffectStackingConfig Stacking { get; set; }

        #endregion Stack

        #region Granted Abilities

        /// <summary>
        /// 授予的技能配置
        /// </summary>
        public GrantedAbilityConfig[] GrantedAbilities { get; set; }
        
        /// <summary>
        /// 提前过期时触发的效果
        /// </summary>
        public GameplayEffectAsset[] PrematureExpirationEffects { get; set; }
        
        /// <summary>
        /// 正常过期时触发的效果
        /// </summary>
        public GameplayEffectAsset[] RoutineExpirationEffects { get; set; }

        #endregion Granted Abilities

        #region Modifiers

        /// <summary>
        /// 游戏效果修饰符数组
        /// </summary>
        public GameplayEffectModifier[] Modifiers { get; set; }

        #endregion Modifiers

        #region Tags

        /// <summary>
        /// 资产标签
        /// </summary>
        public GameplayTag[] AssetTags { get; set; }

        /// <summary>
        /// 授予的标签
        /// </summary>
        public GameplayTag[] GrantedTags { get; set; }

        /// <summary>
        /// 应用时需要的标签
        /// </summary>
        public GameplayTag[] ApplicationRequiredTags { get; set; }

        /// <summary>
        /// 持续期间需要的标签
        /// </summary>
        public GameplayTag[] OngoingRequiredTags { get; set; }

        /// <summary>
        /// 移除具有这些标签的游戏效果
        /// </summary>
        public GameplayTag[] RemoveGameplayEffectsWithTags { get; set; }

        /// <summary>
        /// 应用免疫标签
        /// </summary>
        public GameplayTag[] ApplicationImmunityTags { get; set; }

        #endregion Tags

        #region Cues

        /// <summary>
        /// 执行时触发的瞬时提示
        /// </summary>
        public GameplayCueInstant[] CueOnExecute { get; set; }

        /// <summary>
        /// 持续性提示
        /// </summary>
        public GameplayCueDurational[] CueDurational { get; set; }

        /// <summary>
        /// 添加时触发的提示
        /// </summary>
        public GameplayCueInstant[] CueOnAdd { get; set; }

        /// <summary>
        /// 移除时触发的提示
        /// </summary>
        public GameplayCueInstant[] CueOnRemove { get; set; }

        /// <summary>
        /// 激活时触发的提示
        /// </summary>
        public GameplayCueInstant[] CueOnActivate { get; set; }

        /// <summary>
        /// 停用时触发的提示
        /// </summary>
        public GameplayCueInstant[] CueOnDeactivate { get; set; }

        #endregion Cues

        /// <summary>
        /// 执行计算器数组
        /// </summary>
        public GameplayEffectExecutionCalculation[] Executions { get; set; }

        /// <summary>
        /// 检查是否为周期性效果
        /// </summary>
        public bool IsPeriodic()
        {
            return IsDurationalPolicy() && Period > 0;
        }

        /// <summary>
        /// 检查是否为持续性效果
        /// </summary>
        public bool IsDurationalPolicy()
        {
            return DurationPolicy == EffectsDurationPolicy.Duration || DurationPolicy == EffectsDurationPolicy.Infinite;
        }

        /// <summary>
        /// 检查是否为瞬时效果
        /// </summary>
        public bool IsInstantPolicy() => DurationPolicy == EffectsDurationPolicy.Instant;

        #region IGameplayEffectData

        public string GetDisplayName() => Name;

        public EffectsDurationPolicy GetDurationPolicy() => DurationPolicy;

        public float GetDuration() => Duration;

        public float GetPeriod() => Period;

        public IGameplayEffectData GetPeriodExecution() => PeriodExecution;

        public GameplayTag[] GetAssetTags() => AssetTags;

        public GameplayTag[] GetGrantedTags() => GrantedTags;

        public GameplayTag[] GetApplicationRequiredTags() => ApplicationRequiredTags;

        public GameplayTag[] GetOngoingRequiredTags() => OngoingRequiredTags;

        public GameplayTag[] GetRemoveGameplayEffectsWithTags() => RemoveGameplayEffectsWithTags;

        public GameplayTag[] GetApplicationImmunityTags() => ApplicationImmunityTags;

        public GameplayCueInstant[] GetCueOnExecute() => CueOnExecute;

        public GameplayCueInstant[] GetCueOnRemove() => CueOnRemove;

        public GameplayCueInstant[] GetCueOnAdd() => CueOnAdd;

        public GameplayCueInstant[] GetCueOnActivate() => CueOnActivate;

        public GameplayCueInstant[] GetCueOnDeactivate() => CueOnDeactivate;

        public GameplayCueDurational[] GetCueDurational() => CueDurational;

        public GameplayEffectModifier[] GetModifiers() => Modifiers;

        public GameplayEffectExecutionCalculation[] GetExecutions() => Executions;

        public GrantedAbilityConfig[] GetGrantedAbilities() => GrantedAbilities;

        public GameplayEffectStacking GetStacking() => Stacking.ToRuntimeData();
        
        public GameplayEffectAsset[] GetPrematureExpirationEffects() => PrematureExpirationEffects;
        
        public GameplayEffectAsset[] GetRoutineExpirationEffects() => RoutineExpirationEffects;

        #endregion IGameplayEffectData
    }
}