using UnityEngine;

namespace GAS.Runtime
{
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "SetByCallerFromName", menuName = "GAS/MMC/SetByCallerFromNameModCalculation")]
#endif
    public class SetByCallerFromNameModCalculation : ModifierMagnitudeCalculation
    {
        /// <summary>
        /// 用于从调用者设置的值映射中获取数值的字符串键名
        /// </summary>
        /// <remarks>
        /// 该字符串作为键值，用于在GameplayEffectSpec的名称映射中查找对应的数值。
        /// 调用者需要在应用游戏效果时通过该名称设置相应的数值
        /// </remarks>
        [SerializeField] private string valueName;
        public override float CalculateMagnitude(GameplayEffectSpec spec,float input)
        {
            var value = spec.GetMapValue(valueName);
#if UNITY_EDITOR
            if(value==null) Debug.LogWarning($"[EX] SetByCallerModCalculation: GE's '{valueName}' value(name map) is not set");
#endif
            return value ?? 0;
        }
    }
}