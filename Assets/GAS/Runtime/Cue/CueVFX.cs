using System;
using GAS.General;

namespace GAS.Runtime
{
    public class CueVFX : GameplayCueDurational
    {
        /// <summary>
        /// VFX特效预制件（.NET版本中不可用）
        /// </summary>
        public object VfxPrefab;

        /// <summary>
        /// 是否附加到目标对象
        /// </summary>
        public bool IsAttachToTarget = true;

        /// <summary>
        /// VFX相对于目标的位置偏移（.NET版本中作为占位符）
        /// </summary>
        public object Offset;

        /// <summary>
        /// VFX的旋转角度（.NET版本中作为占位符）
        /// </summary>
        public object Rotation;

        /// <summary>
        /// VFX的缩放大小（.NET版本中作为占位符）
        /// </summary>
        public object Scale = null;
        
        /// <summary>
        /// 是否在添加时就激活
        /// </summary>
        public bool ActiveWhenAdded = false;

        public override GameplayCueDurationalSpec CreateSpec(GameplayCueParameters parameters)
        {
            return new CueVFXSpec(this, parameters);
        }

    }

    public class CueVFXSpec : GameplayCueDurationalSpec<CueVFX>
    {
        public CueVFXSpec(CueVFX cue, GameplayCueParameters parameters) : base(cue, parameters)
        {
            // 在纯.NET环境中，Unity的VFX和GameObject不可用
            // 这里提供占位符实现，实际使用时需要替换为.NET图形库
            throw new NotSupportedException("Unity VFX系统在纯.NET环境中不可用。请使用第三方渲染库。");
        }

        public override void OnAdd()
        {
            throw new NotSupportedException("Unity VFX系统在纯.NET环境中不可用。");
        }

        public override void OnRemove()
        {
        }

        public override void OnGameplayEffectActivate()
        {
            throw new NotSupportedException("Unity VFX系统在纯.NET环境中不可用。");
        }

        public override void OnGameplayEffectDeactivate()
        {
            throw new NotSupportedException("Unity VFX系统在纯.NET环境中不可用。");
        }

        public override void OnTick()
        {
        }

        public void SetVisible(bool visible)
        {
            throw new NotSupportedException("Unity VFX系统在纯.NET环境中不可用。");
        }
    }
}