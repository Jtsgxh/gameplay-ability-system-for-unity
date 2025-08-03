using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    /// <summary>
    /// 属性捕获来源
    /// </summary>
    public enum AttributeCaptureSource
    {
        Source,  // 来自效果源
        Target   // 来自效果目标
    }
    
    /// <summary>
    /// 属性捕获定义 - 对应UE中的FGameplayEffectAttributeCaptureDefinition
    /// </summary>
    [Serializable]
    public struct GameplayEffectAttributeCaptureDefinition
    {
        /// <summary>
        /// 属性集名称
        /// </summary>
        public string AttributeSetName;
        
        /// <summary>
        /// 属性名称
        /// </summary>
        public string AttributeName;
        
        /// <summary>
        /// 捕获来源（Source或Target）
        /// </summary>
        public AttributeCaptureSource CaptureSource;
        
        /// <summary>
        /// 是否快照 - 如果为true，在GameplayEffectSpec创建时捕获；如果为false，在执行时捕获
        /// </summary>
        public bool bSnapshot;
        
        public GameplayEffectAttributeCaptureDefinition(string attributeSetName, string attributeName, 
            AttributeCaptureSource captureSource, bool snapshot = true)
        {
            AttributeSetName = attributeSetName;
            AttributeName = attributeName;
            CaptureSource = captureSource;
            bSnapshot = snapshot;
        }
        
        /// <summary>
        /// 获取捕获键，用于存储和查找捕获的属性值
        /// </summary>
        public string GetCaptureKey()
        {
            return $"{CaptureSource}.{AttributeSetName}.{AttributeName}";
        }
    }
    
    /// <summary>
    /// 捕获的属性值信息
    /// </summary>
    public struct CapturedAttributeValue
    {
        public float Value;
        public bool IsSnapshot;
        public string AttributeSetName;
        public string AttributeName;
        public AttributeCaptureSource CaptureSource;
        
        public CapturedAttributeValue(float value, bool isSnapshot, string attributeSetName, 
            string attributeName, AttributeCaptureSource captureSource)
        {
            Value = value;
            IsSnapshot = isSnapshot;
            AttributeSetName = attributeSetName;
            AttributeName = attributeName;
            CaptureSource = captureSource;
        }
    }
    
    /// <summary>
    /// 属性捕获管理器 - 负责根据定义捕获和存储属性值
    /// </summary>
    public class GameplayEffectAttributeCaptureManager
    {
        /// <summary>
        /// 存储捕获的属性值，键为捕获键，值为捕获的属性信息
        /// </summary>
        private readonly Dictionary<string, CapturedAttributeValue> _capturedAttributes = new();
        
        /// <summary>
        /// 根据捕获定义捕获属性
        /// </summary>
        public void CaptureAttributes(GameplayEffectAttributeCaptureDefinition[] captureDefinitions, 
            AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (captureDefinitions == null) return;
            
            foreach (var captureDef in captureDefinitions)
            {
                var captureComponent = captureDef.CaptureSource == AttributeCaptureSource.Source ? source : target;
                if (captureComponent == null) continue;
                
                var currentValue = captureComponent.GetAttributeCurrentValue(
                    captureDef.AttributeSetName, captureDef.AttributeName) ?? 0f;
                    
                var capturedValue = new CapturedAttributeValue(
                    currentValue, 
                    captureDef.bSnapshot, 
                    captureDef.AttributeSetName, 
                    captureDef.AttributeName, 
                    captureDef.CaptureSource);
                    
                _capturedAttributes[captureDef.GetCaptureKey()] = capturedValue;
            }
        }
        
        /// <summary>
        /// 获取捕获的属性值
        /// </summary>
        public float GetCapturedAttributeValue(string attributeSetName, string attributeName, 
            AttributeCaptureSource captureSource, AbilitySystemComponent liveSource, AbilitySystemComponent liveTarget)
        {
            var key = $"{captureSource}.{attributeSetName}.{attributeName}";
            
            if (_capturedAttributes.TryGetValue(key, out var capturedValue))
            {
                // 如果是快照，直接返回捕获的值
                if (capturedValue.IsSnapshot)
                {
                    return capturedValue.Value;
                }
                
                // 如果不是快照，返回实时值
                var liveComponent = captureSource == AttributeCaptureSource.Source ? liveSource : liveTarget;
                if (liveComponent != null)
                {
                    return liveComponent.GetAttributeCurrentValue(attributeSetName, attributeName) ?? capturedValue.Value;
                }
                
                return capturedValue.Value;
            }
            
            // 如果没有找到捕获的值，尝试获取实时值
            var component = captureSource == AttributeCaptureSource.Source ? liveSource : liveTarget;
            return component?.GetAttributeCurrentValue(attributeSetName, attributeName) ?? 0f;
        }
        
        /// <summary>
        /// 检查是否有捕获指定属性
        /// </summary>
        public bool HasCapturedAttribute(string attributeSetName, string attributeName, AttributeCaptureSource captureSource)
        {
            var key = $"{captureSource}.{attributeSetName}.{attributeName}";
            return _capturedAttributes.ContainsKey(key);
        }
        
        /// <summary>
        /// 获取所有捕获的属性
        /// </summary>
        public Dictionary<string, CapturedAttributeValue> GetAllCapturedAttributes()
        {
            return new Dictionary<string, CapturedAttributeValue>(_capturedAttributes);
        }
    }
}