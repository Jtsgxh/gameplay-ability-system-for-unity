using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS.Runtime
{
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "SetByCallerFromTag", menuName = "GAS/MMC/SetByCallerFromTagModCalculation")]
#endif
    public class SetByCallerFromTagModCalculation : ModifierMagnitudeCalculation
    {
#if UNITY_EDITOR
        /// <summary>
        /// 用于从调用者设置的值映射中获取数值的游戏标签
        /// </summary>
        /// <remarks>
        /// 该标签作为键值，用于在GameplayEffectSpec的标签映射中查找对应的数值。
        /// 调用者需要在应用游戏效果时通过标签设置相应的数值
        /// </remarks>
        [SerializeField]
        [ValueDropdown("@ValueDropdownHelper.GameplayTagChoices", HideChildProperties = true)]
#endif
        private GameplayTag _tag;

        public override float CalculateMagnitude(GameplayEffectSpec spec, float input)
        {
            var value = spec.GetMapValue(_tag);
#if UNITY_EDITOR
            if (value == null)
                Debug.LogWarning($"[EX] SetByCallerModCalculation: GE's '{_tag.Name}' value(tag map) is not set");
#endif
            return value ?? 0;
        }
    }
}