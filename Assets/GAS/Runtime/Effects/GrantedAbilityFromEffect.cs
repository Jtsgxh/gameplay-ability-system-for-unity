using System;
using GAS.General;

namespace GAS.Runtime
{
    /// <summary>
    /// 授予能力的激活策略
    /// </summary>
    public enum GrantedAbilityActivationPolicy
    {
        /// <summary>
        /// 不激活, 等待用户调用ASC激活
        /// </summary>
        None,

        /// <summary>
        /// 能力添加时激活（GE添加时激活）
        /// </summary>
        WhenAdded,

        /// <summary>
        /// 同步GE激活时激活
        /// </summary>
        SyncWithEffect,
    }

    /// <summary>
    /// 授予能力的取消激活策略
    /// </summary>
    public enum GrantedAbilityDeactivationPolicy
    {
        /// <summary>
        /// 无相关取消激活逻辑, 需要用户调用ASC取消激活
        /// </summary>
        None,

        /// <summary>
        /// 同步GE，GE失活时取消激活
        /// </summary>
        SyncWithEffect,
    }

    /// <summary>
    /// 授予能力的移除策略
    /// </summary>
    public enum GrantedAbilityRemovePolicy
    {
        /// <summary>
        /// 不移除
        /// </summary>
        None,

        /// <summary>
        /// 同步GE，GE移除时移除
        /// </summary>
        SyncWithEffect,

        /// <summary>
        /// 能力结束时自己移除
        /// </summary>
        WhenEnd,

        /// <summary>
        /// 能力取消时自己移除
        /// </summary>
        WhenCancel,

        /// <summary>
        /// 能力结束或取消时自己移除
        /// </summary>
        WhenCancelOrEnd,
    }

    /// <summary>
    /// 授予技能配置结构体
    /// 定义了由游戏效果授予的技能的各种策略配置
    /// </summary>
    [Serializable]
    public struct GrantedAbilityConfig
    {
        /// <summary>
        /// 编辑器标签宽度常量
        /// </summary>
        private const int LABEL_WIDTH = 50;

        /// <summary>
        /// 要授予的技能资产
        /// </summary>
        public AbilityAsset AbilityAsset;

        /// <summary>
        /// 授予技能的等级
        /// </summary>
        public int AbilityLevel;

        /// <summary>
        /// 技能激活策略
        /// 定义技能何时被激活
        /// </summary>
        public GrantedAbilityActivationPolicy ActivationPolicy;

        /// <summary>
        /// 技能失活策略
        /// 定义技能何时被失活
        /// </summary>
        public GrantedAbilityDeactivationPolicy DeactivationPolicy;

        /// <summary>
        /// 技能移除策略
        /// 定义技能何时被移除
        /// </summary>
        public GrantedAbilityRemovePolicy RemovePolicy;
    }

    /// <summary>
    /// 游戏效果授予的技能类
    /// 封装由游戏效果授予的技能实例及其相关策略
    /// </summary>
    public class GrantedAbilityFromEffect
    {
        /// <summary>
        /// 授予的技能实例
        /// </summary>
        public readonly AbstractAbility Ability;
        
        /// <summary>
        /// 技能等级
        /// </summary>
        public readonly int AbilityLevel;
        
        /// <summary>
        /// 技能激活策略
        /// </summary>
        public readonly GrantedAbilityActivationPolicy ActivationPolicy;
        
        /// <summary>
        /// 技能失活策略
        /// </summary>
        public readonly GrantedAbilityDeactivationPolicy DeactivationPolicy;
        
        /// <summary>
        /// 技能移除策略
        /// </summary>
        public readonly GrantedAbilityRemovePolicy RemovePolicy;

        public GrantedAbilityFromEffect(GrantedAbilityConfig config)
        {
            Ability =
                Activator.CreateInstance(config.AbilityAsset.AbilityType(), args: config.AbilityAsset) as
                    AbstractAbility;
            AbilityLevel = config.AbilityLevel;
            ActivationPolicy = config.ActivationPolicy;
            DeactivationPolicy = config.DeactivationPolicy;
            RemovePolicy = config.RemovePolicy;
        }

        public GrantedAbilityFromEffect(
            AbstractAbility ability,
            int abilityLevel,
            GrantedAbilityActivationPolicy activationPolicy,
            GrantedAbilityDeactivationPolicy deactivationPolicy,
            GrantedAbilityRemovePolicy removePolicy)
        {
            Ability = ability;
            AbilityLevel = abilityLevel;
            ActivationPolicy = activationPolicy;
            DeactivationPolicy = deactivationPolicy;
            RemovePolicy = removePolicy;
        }

        public GrantedAbilitySpecFromEffect CreateSpec(GameplayEffectSpec sourceEffectSpec)
        {
            var grantedAbility = new GrantedAbilitySpecFromEffect(this, sourceEffectSpec);
            return grantedAbility;
        }
    }

    /// <summary>
    /// 游戏效果授予技能的具体实例
    /// 代表由特定游戏效果授予给特定目标的技能实例
    /// </summary>
    public class GrantedAbilitySpecFromEffect
    {
        /// <summary>
        /// 存储事件订阅的处理器，用于清理
        /// </summary>
        private System.Action<GameplayEventData> _endAbilityHandler;
        private System.Action<GameplayEventData> _cancelAbilityHandler;
        /// <summary>
        /// 授予技能的原始定义
        /// </summary>
        public readonly GrantedAbilityFromEffect GrantedAbility;
        
        /// <summary>
        /// 源游戏效果实例
        /// 指向授予此技能的游戏效果
        /// </summary>
        public readonly GameplayEffectSpec SourceEffectSpec;
        
        /// <summary>
        /// 技能拥有者
        /// 接受此技能的技能系统组件
        /// </summary>
        public readonly AbilitySystemComponent Owner;

        /// <summary>
        /// 技能名称
        /// </summary>
        public readonly string AbilityName;
        
        /// <summary>
        /// 技能等级（只读属性）
        /// </summary>
        public int AbilityLevel => GrantedAbility.AbilityLevel;
        
        /// <summary>
        /// 技能激活策略（只读属性）
        /// </summary>
        public GrantedAbilityActivationPolicy ActivationPolicy => GrantedAbility.ActivationPolicy;
        
        /// <summary>
        /// 技能失活策略（只读属性）
        /// </summary>
        public GrantedAbilityDeactivationPolicy DeactivationPolicy => GrantedAbility.DeactivationPolicy;
        
        /// <summary>
        /// 技能移除策略（只读属性）
        /// </summary>
        public GrantedAbilityRemovePolicy RemovePolicy => GrantedAbility.RemovePolicy;
        
        /// <summary>
        /// 技能规格实例（只读属性）
        /// </summary>
        public AbilitySpec AbilitySpec => Owner.AbilityContainer.AbilitySpecs()[AbilityName];

        public GrantedAbilitySpecFromEffect(GrantedAbilityFromEffect grantedAbility,
            GameplayEffectSpec sourceEffectSpec)
        {
            GrantedAbility = grantedAbility;
            SourceEffectSpec = sourceEffectSpec;
            AbilityName = GrantedAbility.Ability.Name;
            Owner = SourceEffectSpec.Owner;
            if (Owner.AbilityContainer.HasAbility(AbilityName))
            {
                Console.WriteLine($"GrantedAbilitySpecFromEffect: {Owner.Name} already has ability {AbilityName}");
            }

            Owner.GrantAbility(GrantedAbility.Ability);
            AbilitySpec.SetLevel(AbilityLevel);

            // 是否添加时激活
            if (ActivationPolicy == GrantedAbilityActivationPolicy.WhenAdded)
            {
                Owner.TryActivateAbility(AbilityName);
            }

            // 通过EventBus订阅技能结束和取消事件
            switch (RemovePolicy)
            {
                case GrantedAbilityRemovePolicy.WhenEnd:
                    _endAbilityHandler = (eventData) => {
                        var abilityName = eventData.GetParameter<string>("abilityName");
                        if (abilityName == AbilityName) RemoveSelf();
                    };
                    Owner.EventBus?.Subscribe(GameplayEvents.OnAbilityEnded, _endAbilityHandler);
                    break;
                case GrantedAbilityRemovePolicy.WhenCancel:
                    _cancelAbilityHandler = (eventData) => {
                        var abilityName = eventData.GetParameter<string>("abilityName");
                        if (abilityName == AbilityName) RemoveSelf();
                    };
                    Owner.EventBus?.Subscribe(GameplayEvents.OnAbilityCancelled, _cancelAbilityHandler);
                    break;
                case GrantedAbilityRemovePolicy.WhenCancelOrEnd:
                    _endAbilityHandler = (eventData) => {
                        var abilityName = eventData.GetParameter<string>("abilityName");
                        if (abilityName == AbilityName) RemoveSelf();
                    };
                    _cancelAbilityHandler = (eventData) => {
                        var abilityName = eventData.GetParameter<string>("abilityName");
                        if (abilityName == AbilityName) RemoveSelf();
                    };
                    Owner.EventBus?.Subscribe(GameplayEvents.OnAbilityEnded, _endAbilityHandler);
                    Owner.EventBus?.Subscribe(GameplayEvents.OnAbilityCancelled, _cancelAbilityHandler);
                    break;
            }
        }

        private void RemoveSelf()
        {
            // 清理事件订阅
            CleanupEventSubscriptions();
            Owner.RemoveAbility(AbilityName);
        }
        
        /// <summary>
        /// 清理事件订阅，防止内存泄漏
        /// </summary>
        public void CleanupEventSubscriptions()
        {
            if (_endAbilityHandler != null)
            {
                Owner.EventBus?.Unsubscribe(GameplayEvents.OnAbilityEnded, _endAbilityHandler);
                _endAbilityHandler = null;
            }
            
            if (_cancelAbilityHandler != null)
            {
                Owner.EventBus?.Unsubscribe(GameplayEvents.OnAbilityCancelled, _cancelAbilityHandler);
                _cancelAbilityHandler = null;
            }
        }
    }
}