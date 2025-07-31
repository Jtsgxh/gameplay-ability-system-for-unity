using UnityEngine;

namespace GAS.Runtime
{
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "SetByCallerFromName", menuName = "GAS/MMC/SetByCallerFromNameModCalculation")]
#endif
    public class SetByCallerFromNameModCalculation : ModifierMagnitudeCalculation
    {
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