using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS.Runtime
{
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "ScalableFloatModCalculation", menuName = "GAS/MMC/ScalableFloatModCalculation")]
#endif
    public class ScalableFloatModCalculation : ModifierMagnitudeCalculation
    {
        private const string Desc = "计算公式：ModifierMagnitude * k + b";

        private const string Detail =
            "ScalableFloatModCalculation：可缩放浮点数计算\n该类型是根据Magnitude计算Modifier模值的，计算公式为：ModifierMagnitude * k + b 实际上就是一个线性函数，k和b为可编辑参数，可以在编辑器中设置。";

#if UNITY_EDITOR
        [DetailedInfoBox(Desc, Detail, InfoMessageType.Info)]
#endif
        /// <summary>
        /// 线性函数的系数参数
        /// </summary>
        /// <remarks>
        /// 在公式 ModifierMagnitude * k + b 中的斜率系数，
        /// 用于缩放输入的修饰器幅值
        /// </remarks>
        [SerializeField]
        private float k = 1f;

        /// <summary>
        /// 线性函数的常数项参数
        /// </summary>
        /// <remarks>
        /// 在公式 ModifierMagnitude * k + b 中的偏移量，
        /// 用于在缩放后添加固定值
        /// </remarks>
        [SerializeField] private float b = 0f;

        public override float CalculateMagnitude(GameplayEffectSpec spec, float input)
        {
            return input * k + b;
        }
    }
}