using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS.Runtime
{
    /// <summary>
    ///  基于属性混合GE堆栈的MMC
    /// </summary>
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "AttrBasedWithStackModCalculation", menuName = "GAS/MMC/AttrBasedWithStackModCalculation")]
#endif
    public class AttrBasedWithStackModCalculation:AttributeBasedModCalculation
    {
        public enum StackMagnitudeOperation
        {
            Add,
            Multiply
        }
        
#if UNITY_EDITOR
        [InfoBox(" 公式：StackCount * sK + sB")]
        [TabGroup("Default", "AttributeBasedModCalculation")]
        [Title("堆叠幅值计算")]
        [LabelText("系数(sK)")]
#endif
        public float sK = 1;

#if UNITY_EDITOR
        [TabGroup("Default", "AttributeBasedModCalculation")]
        [LabelText("常量(sB)")]
#endif
        public float sB = 0;

#if UNITY_EDITOR
        [TabGroup("Default", "AttributeBasedModCalculation")]
        [Title("最终结果")]
        [InfoBox(" 最终公式： \n" +
                 "Add:(AttributeValue * k + b)+(StackCount * sK + sB); \n" +
                 "Multiply:(AttributeValue * k + b)*(StackCount * sK + sB)")]
        [LabelText("Stack幅值与Attr幅值计算方式")]
#endif
        public StackMagnitudeOperation stackMagnitudeOperation;

#if UNITY_EDITOR
        [TabGroup("Default", "AttributeBasedModCalculation")]
        [LabelText("最终公式")]
        [ShowInInspector]
        [DisplayAsString(TextAlignment.Left, true)]
#endif
        public string FinalFormulae
        {
            get
            {
                var formulae = stackMagnitudeOperation switch
                {
                    StackMagnitudeOperation.Add => $"({attributeName} * {k} + {b}) + (StackCount * {sK} + {sB})",
                    StackMagnitudeOperation.Multiply => $"({attributeName} * {k} + {b}) * (StackCount * {sK} + {sB})",
                    _ => ""
                };

                return $"<size=15><b><color=green>{formulae}</color></b></size>";
            }
        }
        
        public override float CalculateMagnitude(GameplayEffectSpec spec, float modifierMagnitude)
        {
            var attrMagnitude = base.CalculateMagnitude(spec, modifierMagnitude);
            
            if (spec.Stacking.stackingType == StackingType.None) return attrMagnitude;
            
            var stackMagnitude = spec.StackCount * sK + sB;

            return stackMagnitudeOperation switch
            {
                StackMagnitudeOperation.Add => attrMagnitude + stackMagnitude,
                StackMagnitudeOperation.Multiply => attrMagnitude * stackMagnitude,
                _ => attrMagnitude + stackMagnitude
            };
        }
        
    }
}