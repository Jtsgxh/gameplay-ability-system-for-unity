using System;
using System.Collections.Generic;
using System.Reflection;
using GAS.General;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GAS.Runtime
{
    public abstract class TimelineAbilityAssetBase : AbilityAsset
    {
#if UNITY_EDITOR
        [TitleGroup("Data")]
        [HorizontalGroup("Data/H1", 1 / 3f)]
        [TabGroup("Data/H1/V1", "Timeline", SdfIconType.ClockHistory, TextColor = "#00FF00")]
        [Button("查看/编辑能力时间轴", ButtonSizes.Large, Icon = SdfIconType.Hammer)]
        [PropertyOrder(-1)]
        private void EditAbilityTimeline()
        {
            try
            {
                var assembly = Assembly.Load("com.exhard.exgas.editor");
                var type = assembly.GetType("GAS.Editor.AbilityTimelineEditorWindow");
                var methodInfo = type.GetMethod("ShowWindow", BindingFlags.Public | BindingFlags.Static);
                methodInfo!.Invoke(null, new object[] { this });
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"调用\"GAS.Editor.AbilityTimelineEditorWindow\"类的静态方法ShowWindow(TimelineAbilityAsset asset)失败, 代码可能被重构了: {e}");
            }
        }

        [TabGroup("Data/H1/V1", "Timeline")]
        [LabelText(GASTextDefine.ABILITY_MANUAL_ENDABILITY)]
        [LabelWidth(100)]
#endif
        /// <summary>
        /// 是否手动结束技能，如果为true则技能不会在时间轴播放完成后自动结束
        /// </summary>
        public bool manualEndAbility;

#if UNITY_EDITOR
        [HideInInspector]
#endif
        /// <summary>
        /// 时间轴总帧数，定义技能的持续时间长度
        /// </summary>
        public int FrameCount;

#if UNITY_EDITOR
        [HideInInspector]
#endif
        /// <summary>
        /// 持续性Cue轨道数据列表，用于播放持续时间内的视听效果
        /// </summary>
        public List<DurationalCueTrackData> DurationalCues = new List<DurationalCueTrackData>();

#if UNITY_EDITOR
        [HideInInspector]
#endif
        /// <summary>
        /// 瞬时Cue轨道数据列表，用于播放瞬间触发的视听效果
        /// </summary>
        public List<InstantCueTrackData> InstantCues = new List<InstantCueTrackData>();

#if UNITY_EDITOR
        [HideInInspector]
#endif
        /// <summary>
        /// 释放游戏效果轨道数据列表，用于在特定时刻对目标应用游戏效果
        /// </summary>
        public List<ReleaseGameplayEffectTrackData> ReleaseGameplayEffect = new List<ReleaseGameplayEffectTrackData>();

#if UNITY_EDITOR
        [HideInInspector]
#endif
        /// <summary>
        /// Buff游戏效果轨道数据列表，用于在技能执行期间对施法者应用持续性效果
        /// </summary>
        public List<BuffGameplayEffectTrackData> BuffGameplayEffects = new List<BuffGameplayEffectTrackData>();

#if UNITY_EDITOR
        [HideInInspector]
#endif
        /// <summary>
        /// 瞬时任务轨道数据列表，用于在特定时刻执行瞬间任务逻辑
        /// </summary>
        public List<TaskMarkEventTrackData> InstantTasks = new List<TaskMarkEventTrackData>();

#if UNITY_EDITOR
        [HideInInspector]
#endif
        /// <summary>
        /// 持续性任务轨道数据列表，用于在指定时间段内执行持续性任务逻辑
        /// </summary>
        public List<TaskClipEventTrackData> OngoingTasks = new List<TaskClipEventTrackData>();

#if UNITY_EDITOR
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }

    public abstract class TimelineAbilityAssetT<T> : TimelineAbilityAssetBase where T : class
    {
        public sealed override Type AbilityType()
        {
            return typeof(T);
        }
    }

    /// <summary>
    /// 这是一个最朴素的TimelineAbilityAsset实现, 如果要实现更复杂的TimelineAbilityAsset, 请用TimelineAbilityAssetBase或TimelineAbilityAssetT为基类
    /// </summary>
    public sealed class TimelineAbilityAsset : TimelineAbilityAssetT<TimelineAbility>
    {
    }
}