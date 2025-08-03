using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS.Runtime
{
#if UNITY_EDITOR
    [CreateAssetMenu( fileName = "StackModCalculation", menuName = "GAS/MMC/StackModCalculation" )]
#endif
    public class StackModCalculation:ModifierMagnitudeCalculation
    {
#if UNITY_EDITOR
        [InfoBox("计算逻辑与ScalableFloatModCalculation一致, 公式：(StackCount) * k + b")]
        [TabGroup("Default", "StackModCalculation")]
        [LabelText("系数(k)")]
#endif
        /// <summary>
        /// 系数k，用于计算堆叠修正值
        /// </summary>
        public float k = 1;

#if UNITY_EDITOR
        [TabGroup("Default", "StackModCalculation")]
        [LabelText("常量(b)")]
#endif
        /// <summary>
        /// 常量b，用于计算堆叠修正值
        /// </summary>
        public float b = 0;
        
        public override float CalculateMagnitude(GameplayEffectSpec spec, float modifierMagnitude)
        {
            if (spec.Stacking.stackingType == StackingType.None) return 0;
            
            var stackCount = spec.StackCount;
            return stackCount * k + b;
        }
    }
}