using System;
using GAS.General;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GAS.Runtime
{
    public class CuePlaySound : GameplayCueDurational
    {
#if UNITY_EDITOR
        [BoxGroup]
        [LabelText(GASTextDefine.CUE_SOUND_EFFECT)]
#endif
        /// <summary>
        /// 音效文件
        /// </summary>
        public AudioClip soundEffect; 
        
#if UNITY_EDITOR
        [BoxGroup]
        [LabelText(GASTextDefine.CUE_ATTACH_TO_OWNER)]
#endif
        /// <summary>
        /// 是否附加到拥有者
        /// </summary>
        public bool isAttachToOwner = true;
        
        public override GameplayCueDurationalSpec CreateSpec(GameplayCueParameters parameters)
        {
            return new CuePlaySoundSpec(this, parameters);
        }
    }
    
    public class CuePlaySoundSpec : GameplayCueDurationalSpec<CuePlaySound>
    {
        /// <summary>
        /// 音频源组件
        /// </summary>
        private AudioSource _audioSource;
        
        public CuePlaySoundSpec(CuePlaySound cue, GameplayCueParameters parameters) : base(cue,
            parameters)
        {
            // 在纯.NET环境中，Unity的AudioSource和GameObject不可用
            // 这里提供占位符实现，实际使用时需要替换为.NET音频库
            throw new NotSupportedException("Unity音频系统在纯.NET环境中不可用。请使用第三方音频库如NAudio等。");
        }

        public override void OnAdd()
        {
            _audioSource.clip = cue.soundEffect;
            _audioSource.Play();
        }

        public override void OnRemove()
        {
            if (!cue.isAttachToOwner)
            {
                Object.Destroy(_audioSource.gameObject);
            }else
            {
                _audioSource.Stop();
                _audioSource.clip = null;
            }
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