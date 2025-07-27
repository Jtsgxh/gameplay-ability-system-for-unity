using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 技能系统组件的核心接口，定义了GAS系统的所有基础功能
    /// </summary>
    /// <remarks>
    /// 这个接口是Unity GAS系统的核心，定义了：
    /// - 标签系统管理（GameplayTag）
    /// - 技能系统管理（Ability）
    /// - 游戏效果系统（GameplayEffect）
    /// - 属性系统管理（AttributeSet）
    /// 实现这个接口的类可以作为GAS系统中的游戏实体。
    /// </remarks>
    public interface IAbilitySystemComponent
    {
        void SetPreset(AbilitySystemComponentPreset ascPreset);
        
        /// <summary>
        /// 初始化技能系统组件
        /// </summary>
        /// <param name="baseTags">基础固定标签数组</param>
        /// <param name="attrSetTypes">属性集类型数组</param>
        /// <param name="baseAbilities">基础技能数组</param>
        /// <param name="level">初始等级</param>
        void Init(GameplayTag[] baseTags, Type[] attrSetTypes, AbilityAsset[] baseAbilities,int level);
        
        void SetLevel(int level);
        
        bool HasTag(GameplayTag tag);
        
        bool HasAllTags(GameplayTagSet tags);
        
        bool HasAnyTags(GameplayTagSet tags);
        
        void AddFixedTags(GameplayTagSet tags);
        void AddFixedTag(GameplayTag gameplayTag);
        
        void RemoveFixedTags(GameplayTagSet tags);
        void RemoveFixedTag(GameplayTag gameplayTag);

        /// <summary>
        /// 对目标应用游戏效果
        /// </summary>
        /// <param name="gameplayEffect">游戏效果</param>
        /// <param name="target">目标组件</param>
        /// <returns>创建的效果实例</returns>
        GameplayEffectSpec ApplyGameplayEffectTo(GameplayEffect gameplayEffect,AbilitySystemComponent target);
        
        /// <summary>
        /// 对自身应用游戏效果
        /// </summary>
        /// <param name="gameplayEffect">游戏效果</param>
        /// <returns>创建的效果实例</returns>
        GameplayEffectSpec ApplyGameplayEffectToSelf(GameplayEffect gameplayEffect);

        void ApplyModFromInstantGameplayEffect(GameplayEffectSpec spec);
        
        void RemoveGameplayEffect(GameplayEffectSpec spec);
        
        void Tick();
        
        Dictionary<string,float> DataSnapshot();
        
        AbilitySpec GrantAbility(AbstractAbility ability);
        
        void RemoveAbility(string abilityName);
        
        /// <summary>
        /// 获取属性当前值
        /// </summary>
        /// <param name="setName">属性集名称</param>
        /// <param name="attributeShortName">属性短名称</param>
        /// <returns>属性当前值</returns>
        float? GetAttributeCurrentValue(string setName,string attributeShortName);
        
        /// <summary>
        /// 获取属性基础值
        /// </summary>
        /// <param name="setName">属性集名称</param>
        /// <param name="attributeShortName">属性短名称</param>
        /// <returns>属性基础值</returns>
        float? GetAttributeBaseValue(string setName,string attributeShortName);

        /// <summary>
        /// 尝试激活指定技能
        /// </summary>
        /// <param name="abilityName">技能名称</param>
        /// <param name="args">激活参数</param>
        /// <returns>激活是否成功</returns>
        bool TryActivateAbility(string abilityName, params object[] args);
        void TryEndAbility(string abilityName);

        CooldownTimer CheckCooldownFromTags(GameplayTagSet tags);
        
        /// <summary>
        /// 获取指定类型的属性集
        /// </summary>
        /// <typeparam name="T">属性集类型</typeparam>
        /// <returns>属性集实例</returns>
        T AttrSet<T>() where T : AttributeSet;

        void ClearGameplayEffect();
    }
}