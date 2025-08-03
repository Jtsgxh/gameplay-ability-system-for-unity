using System;
using GAS.General;

namespace GAS.Runtime
{
    public sealed class CueAnimationSpeedModifier : GameplayCueDurational
    {
        const int LabelWidth = 120;

        public string animatorRelativePath;

        public bool includeChildrenAnimator;

        public float speed = 1f;

        public float defaultSpeed = 1f;

        public override GameplayCueDurationalSpec CreateSpec(GameplayCueParameters parameters)
        {
            return new GCS_ChangeAnimationSpeed(this, parameters);
        }
    }


    public sealed class GCS_ChangeAnimationSpeed : GameplayCueDurationalSpec<CueAnimationSpeedModifier>
    {
        public GCS_ChangeAnimationSpeed(CueAnimationSpeedModifier cue, GameplayCueParameters parameters)
            : base(cue, parameters)
        {
            // 在纯.NET环境中，Unity的Animator和Transform不可用
            // 这里提供占位符实现，实际使用时需要替换为.NET动画库
            throw new NotSupportedException("Unity动画系统在纯.NET环境中不可用。请使用第三方动画库。");
        }

        public override void OnAdd()
        {
        }

        public override void OnRemove()
        {
        }

        public override void OnGameplayEffectActivate()
        {
            throw new NotSupportedException("Unity动画系统在纯.NET环境中不可用。");
        }

        public override void OnGameplayEffectDeactivate()
        {
            throw new NotSupportedException("Unity动画系统在纯.NET环境中不可用。");
        }

        public override void OnTick()
        {
        }
    }
}