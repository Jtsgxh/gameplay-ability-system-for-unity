using System;
using GAS.General;

namespace GAS.Runtime
{
    public class CueAnimationOneShot : GameplayCueInstant
    {
        private string _animatorRelativePath;

        private bool _includeChildrenAnimator;

        private string _stateName;

        public string AnimatorRelativePath => _animatorRelativePath;
        public bool IncludeChildrenAnimator => _includeChildrenAnimator;
        public string StateName => _stateName;


        public override GameplayCueInstantSpec CreateSpec(GameplayCueParameters parameters)
        {
            return new CueAnimationOneShotSpec(this, parameters);
        }

    }

    public class CueAnimationOneShotSpec : GameplayCueInstantSpec<CueAnimationOneShot>
    {
        public CueAnimationOneShotSpec(CueAnimationOneShot cue, GameplayCueParameters parameters)
            : base(cue, parameters)
        {
            // 在纯.NET环境中，Unity的Animator和Transform不可用
            // 这里提供占位符实现，实际使用时需要替换为.NET动画库
            throw new NotSupportedException("Unity动画系统在纯.NET环境中不可用。请使用第三方动画库。");
        }

        public override void Trigger()
        {
            throw new NotSupportedException("Unity动画系统在纯.NET环境中不可用。");
        }
    }
}