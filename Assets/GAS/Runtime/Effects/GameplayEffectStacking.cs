using System;
using GAS.General;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS.Runtime
{
    public enum StackingType
    {
#if UNITY_EDITOR
        [LabelText("独立", SdfIconType.XCircleFill)]
#endif
        None, //不会叠加，如果多次释放则每个Effect相当于单个Effect

#if UNITY_EDITOR
        [LabelText("来源", SdfIconType.Magic)]
#endif
        AggregateBySource, //目标(Target)上的每个源(Source)ASC都有一个单独的堆栈实例, 每个源(Source)可以应用堆栈中的X个GameplayEffect.

#if UNITY_EDITOR
        [LabelText("目标", SdfIconType.Person)]
#endif
        AggregateByTarget //目标(Target)上只有一个堆栈实例而不管源(Source)如何, 每个源(Source)都可以在共享堆栈限制(Shared Stack Limit)内应用堆栈.
    }

    public enum DurationRefreshPolicy
    {
#if UNITY_EDITOR
        [LabelText("NeverRefresh - 不刷新Effect的持续时间", SdfIconType.XCircleFill)]
#endif
        NeverRefresh, //不刷新Effect的持续时间

#if UNITY_EDITOR
        [LabelText(
            "RefreshOnSuccessfulApplication - 每次apply成功后刷新持续时间",
            SdfIconType.HourglassTop)]
#endif
        RefreshOnSuccessfulApplication //每次apply成功后刷新Effect的持续时间, denyOverflowApplication如果为True则多余的Apply不会刷新Duration
    }

    public enum PeriodResetPolicy
    {
#if UNITY_EDITOR
        [LabelText("NeverReset - 不重置Effect的周期计时", SdfIconType.XCircleFill)]
#endif
        NeverRefresh, //不重置Effect的周期计时

#if UNITY_EDITOR
        [LabelText("ResetOnSuccessfulApplication - 每次apply成功后重置Effect的周期计时", SdfIconType.HourglassTop)]
#endif
        ResetOnSuccessfulApplication //每次apply成功后重置Effect的周期计时
    }

    public enum ExpirationPolicy
    {
#if UNITY_EDITOR
        [LabelText("ClearEntireStack - 持续时间结束时, 清除所有层数", SdfIconType.TrashFill)]
#endif
        ClearEntireStack, //持续时间结束时,清除所有层数

#if UNITY_EDITOR
        [LabelText("RemoveSingleStackAndRefreshDuration - 持续时间结束时减少一层，然后重新经历一个Duration", SdfIconType.EraserFill)]
#endif
        RemoveSingleStackAndRefreshDuration, //持续时间结束时减少一层，然后重新经历一个Duration，一直持续到层数减为0

#if UNITY_EDITOR
        [LabelText("RefreshDuration - 持续时间结束时,再次刷新Duration", SdfIconType.HourglassTop)]
#endif
        RefreshDuration //持续时间结束时,再次刷新Duration，这相当于无限Duration，
        //TODO :可以通过调用GameplayEffectsContainer的OnStackCountChange(GameplayEffect ActiveEffect, int OldStackCount, int NewStackCount)来处理层数，
        //TODO :可以达到Duration结束时减少两层并刷新Duration这样复杂的效果。
    }

    /// <summary>
    /// 游戏效果堆叠数据结构
    /// 定义游戏效果的堆叠行为和策略
    /// </summary>
    public struct GameplayEffectStacking
    {
        /// <summary>
        /// 堆叠识别码名称（实际不会直接使用，而是使用其哈希值）
        /// </summary>
        public string stackingCodeName; // 实际允许不会使用，而是使用stackingCodeName的hash值, 即stackingHashCode
        
        /// <summary>
        /// 堆叠识别码的哈希值，用于实际的堆叠判断
        /// </summary>
        public int stackingHashCode;
        
        /// <summary>
        /// 堆叠类型
        /// </summary>
        public StackingType stackingType;
        
        /// <summary>
        /// 堆叠层数限制
        /// </summary>
        public int limitCount;
        
        /// <summary>
        /// 持续时间刷新策略
        /// </summary>
        public DurationRefreshPolicy durationRefreshPolicy;
        
        /// <summary>
        /// 周期重置策略
        /// </summary>
        public PeriodResetPolicy periodResetPolicy;
        
        /// <summary>
        /// 过期策略
        /// </summary>
        public ExpirationPolicy expirationPolicy;

        // Overflow 溢出逻辑处理
        /// <summary>
        /// 拒绝溢出应用
        /// 对应于StackDurationRefreshPolicy，如果为True则多余的Apply不会刷新Duration
        /// </summary>
        public bool denyOverflowApplication; //对应于StackDurationRefreshPolicy，如果为True则多余的Apply不会刷新Duration
        
        /// <summary>
        /// 溢出时清除堆叠
        /// 当DenyOverflowApplication为True时才有效，当Overflow时是否直接删除所有层数
        /// </summary>
        public bool clearStackOnOverflow; //当DenyOverflowApplication为True是才有效，当Overflow时是否直接删除所有层数
        
        /// <summary>
        /// 溢出时触发的效果数组
        /// 超过StackLimitCount数量的Effect被Apply时将会调用该OverflowEffects
        /// </summary>
        public GameplayEffect[] overflowEffects; // 超过StackLimitCount数量的Effect被Apply时将会调用该OverflowEffects

        public void SetStackingCodeName(string stackingCodeName)
        {
            this.stackingCodeName = stackingCodeName;
            this.stackingHashCode = !string.IsNullOrEmpty(stackingCodeName) ? StringHashUtil.GetStableHashCode(stackingCodeName) : 0; // 兼容旧的SO数据
        }

        public void SetStackingHashCode(int stackingHashCode)
        {
            this.stackingHashCode = stackingHashCode;
        }

        public void SetStackingType(StackingType stackingType)
        {
            this.stackingType = stackingType;
        }

        public void SetLimitCount(int limitCount)
        {
            this.limitCount = limitCount;
        }

        public void SetDurationRefreshPolicy(DurationRefreshPolicy durationRefreshPolicy)
        {
            this.durationRefreshPolicy = durationRefreshPolicy;
        }

        public void SetPeriodResetPolicy(PeriodResetPolicy periodResetPolicy)
        {
            this.periodResetPolicy = periodResetPolicy;
        }

        public void SetExpirationPolicy(ExpirationPolicy expirationPolicy)
        {
            this.expirationPolicy = expirationPolicy;
        }

        public void SetOverflowEffects(GameplayEffect[] overflowEffects)
        {
            this.overflowEffects = overflowEffects;
        }

        public void SetOverflowEffects(GameplayEffectAsset[] overflowEffectAssets)
        {
            overflowEffects = new GameplayEffect[overflowEffectAssets.Length];
            for (var i = 0; i < overflowEffectAssets.Length; ++i)
            {
                overflowEffects[i] = new GameplayEffect(overflowEffectAssets[i]);
            }
        }

        public void SetDenyOverflowApplication(bool denyOverflowApplication)
        {
            this.denyOverflowApplication = denyOverflowApplication;
        }

        public void SetClearStackOnOverflow(bool clearStackOnOverflow)
        {
            this.clearStackOnOverflow = clearStackOnOverflow;
        }

        public static GameplayEffectStacking None
        {
            get
            {
                var stack = new GameplayEffectStacking();
                stack.SetStackingType(StackingType.None);
                return stack;
            }
        }
    }

    /// <summary>
    /// 游戏效果堆叠配置类
    /// 用于在编辑器中配置游戏效果的堆叠行为
    /// </summary>
    [Serializable]
    public sealed class GameplayEffectStackingConfig
    {
        /// <summary>
        /// 编辑器标签宽度常量
        /// </summary>
        private const int LABEL_WIDTH = 100;

        /// <summary>
        /// 堆叠类型
        /// </summary>
#if UNITY_EDITOR
        [LabelWidth(LABEL_WIDTH)]
        [VerticalGroup]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_TYPE)]
        [EnumToggleButtons]
#endif
        public StackingType stackingType;

        /// <summary>
        /// 堆叠识别码名称
        /// </summary>
#if UNITY_EDITOR
        [LabelWidth(LABEL_WIDTH)]
        [VerticalGroup]
        [HideIf("IsNoStacking")]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_CODENAME)]
        [InlineButton(@"@stackingCodeName = """"", SdfIconType.EraserFill, "")]
#endif
        public string stackingCodeName;

        /// <summary>
        /// 堆叠层数限制
        /// </summary>
#if UNITY_EDITOR
        [LabelWidth(LABEL_WIDTH)]
        [VerticalGroup]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_COUNT)]
        [HideIf("IsNoStacking")]
        [InlineButton(@"@limitCount = int.MaxValue", SdfIconType.Hammer, "max")]
        [InlineButton(@"@limitCount = 0", SdfIconType.Hammer, "min")]
        [ValidateInput("@limitCount >= 0", "必须>=0")]
#endif
        public int limitCount;

        /// <summary>
        /// 持续时间刷新策略
        /// </summary>
#if UNITY_EDITOR
        [LabelWidth(LABEL_WIDTH)]
        [VerticalGroup]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_DURATION_REFRESH_POLICY)]
        [HideIf("IsNoStacking")]
        [InfoBox(GASTextDefine.LABEL_GE_STACKING_DENY_OVERFLOW_APPLICATION+"为True时多余的Apply不会刷新Duration", InfoMessageType.None,
            VisibleIf =
                "@durationRefreshPolicy == DurationRefreshPolicy.RefreshOnSuccessfulApplication && denyOverflowApplication")]
#endif
        public DurationRefreshPolicy durationRefreshPolicy;

        /// <summary>
        /// 周期重置策略
        /// </summary>
#if UNITY_EDITOR
        [LabelWidth(LABEL_WIDTH)]
        [VerticalGroup]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_PERIOD_RESET_POLICY)]
        [HideIf("IsNoStacking")]
#endif
        public PeriodResetPolicy periodResetPolicy;

        /// <summary>
        /// 过期策略
        /// </summary>
#if UNITY_EDITOR
        [LabelWidth(LABEL_WIDTH)]
        [VerticalGroup]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_EXPIRATION_POLICY)]
        [HideIf("IsNoStacking")]
#endif
        public ExpirationPolicy expirationPolicy;

        // Overflow 溢出逻辑处理
        /// <summary>
        /// 拒绝溢出应用
        /// </summary>
#if UNITY_EDITOR
        [LabelWidth(LABEL_WIDTH)]
        [VerticalGroup]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_DENY_OVERFLOW_APPLICATION)]
        [HideIf("@IsNoStacking() || IsNeverRefreshDuration()")]
#endif
        public bool denyOverflowApplication;

        /// <summary>
        /// 溢出时清除堆叠
        /// </summary>
#if UNITY_EDITOR
        [VerticalGroup]
        [LabelWidth(LABEL_WIDTH)]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_CLEAR_STACK_ON_OVERFLOW)]
        [ShowIf("IsDenyOverflowApplication")]
#endif
        public bool clearStackOnOverflow;

        /// <summary>
        /// 溢出时触发的效果资产数组
        /// </summary>
#if UNITY_EDITOR
        [VerticalGroup]
        [LabelWidth(LABEL_WIDTH)]
        [LabelText(GASTextDefine.LABEL_GE_STACKING_CLEAR_OVERFLOW_EFFECTS)]
        [HideIf("IsNoStacking")]
#endif
        public GameplayEffectAsset[] overflowEffects;

        /// <summary>
        /// 转换为运行时数据
        /// </summary>
        /// <returns></returns>
        public GameplayEffectStacking ToRuntimeData()
        {
            var stack = new GameplayEffectStacking();
            stack.SetStackingCodeName(stackingCodeName);
            stack.SetStackingType(stackingType);
            stack.SetLimitCount(limitCount);
            stack.SetDurationRefreshPolicy(durationRefreshPolicy);
            stack.SetPeriodResetPolicy(periodResetPolicy);
            stack.SetExpirationPolicy(expirationPolicy);
            stack.SetOverflowEffects(overflowEffects);
            stack.SetDenyOverflowApplication(denyOverflowApplication);
            stack.SetClearStackOnOverflow(clearStackOnOverflow);
            return stack;
        }

        #region UTIL FUNCTION FOR ODIN INSPECTOR

        public bool IsNoStacking() => stackingType == StackingType.None;

        public bool IsNeverRefreshDuration() =>
            IsNoStacking() || durationRefreshPolicy == DurationRefreshPolicy.NeverRefresh;

        public bool IsDenyOverflowApplication() => !IsNoStacking() && denyOverflowApplication;

        #endregion
    }
}