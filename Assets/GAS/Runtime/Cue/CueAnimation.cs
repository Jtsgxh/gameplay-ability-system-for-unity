using System;
using GAS.General;

namespace GAS.Runtime
{
    public class CueAnimation : GameplayCueDurational
    {
        /// <summary>
        /// 动画机相对路径
        /// </summary>
        private string _animatorRelativePath;

        /// <summary>
        /// 是否包括子节点动画机
        /// </summary>
        private bool _includeChildrenAnimator;

        /// <summary>
        /// 动画状态名
        /// </summary>
        private string _stateName;

        public string AnimatorRelativePath => _animatorRelativePath;
        public bool IncludeChildrenAnimator => _includeChildrenAnimator;
        public string StateName => _stateName;


        public override GameplayCueDurationalSpec CreateSpec(GameplayCueParameters parameters)
        {
            return new CueAnimationSpec(this, parameters);
        }

    }

    public class CueAnimationSpec : GameplayCueDurationalSpec<CueAnimation>
    {
        public CueAnimationSpec(CueAnimation cue, GameplayCueParameters parameters) : base(cue, parameters)
        {
            // 在纯.NET环境中，Unity的Animator和Transform不可用
            // 这里提供占位符实现，实际使用时需要替换为.NET动画库
            throw new NotSupportedException("Unity动画系统在纯.NET环境中不可用。请使用第三方动画库。");
        }

        public override void OnAdd()
        {
            throw new NotSupportedException("Unity动画系统在纯.NET环境中不可用。");
        }

        public override void OnRemove()
        {
        }

        public override void OnGameplayEffectActivate()
        {
        }

        public override void OnGameplayEffectDeactivate()
        {
        }

        public override void OnTick()
        {
        }
    }
}