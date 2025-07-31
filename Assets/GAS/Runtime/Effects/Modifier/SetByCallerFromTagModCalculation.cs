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