namespace GAS
{
    /// <summary>
    /// GAS系统的常量定义类
    /// 包含所有系统级别的常量，如版本号、文件名、文件夹名等
    /// </summary>
    public static class GasDefine
    {
        /// <summary>
        /// GAS系统版本号
        /// </summary>
        public const string GAS_VERSION = "1.1.8";

        /// <summary>
        /// 游戏标签库生成的C#脚本文件名
        /// </summary>
        public const string GAS_TAG_LIB_CSHARP_SCRIPT_NAME = "GTagLib.gen.cs";

        /// <summary>
        /// 属性库生成的C#脚本文件名
        /// </summary>
        public const string GAS_ATTRIBUTE_LIB_CSHARP_SCRIPT_NAME = "GAttrLib.gen.cs";

        /// <summary>
        /// 属性集库生成的C#脚本文件名
        /// </summary>
        public const string GAS_ATTRIBUTESET_LIB_CSHARP_SCRIPT_NAME = "GAttrSetLib.gen.cs";

        /// <summary>
        /// 技能库生成的C#脚本文件名
        /// </summary>
        public const string GAS_ABILITY_LIB_CSHARP_SCRIPT_NAME = "GAbilityLib.gen.cs";

        /// <summary>
        /// 技能系统组件工具类生成的C#脚本文件名
        /// </summary>
        public const string GAS_ASCUTIL_CSHARP_SCRIPT_NAME = "AbilitySystemComponentExtension.gen.cs";

        /// <summary>
        /// TODO
        /// I will try to make an Ability-Script-Generator in the future. 
        /// </summary>
        public const string GAS_GAMEPLAYABILITY_CLASS_CSHARP_SCRIPT_NAME = "GameplayAbilityClass.gen.cs";

        /// <summary>
        /// 技能系统组件库文件夹名称
        /// </summary>
        public const string GAS_ASC_LIBRARY_FOLDER = "AbilitySystemComponentLib";

        /// <summary>
        /// 游戏效果库文件夹名称
        /// </summary>
        public const string GAS_EFFECT_LIBRARY_FOLDER = "GameplayEffectLib";

        /// <summary>
        /// 游戏技能库文件夹名称
        /// </summary>
        public const string GAS_ABILITY_LIBRARY_FOLDER = "GameplayAbilityLib";

        /// <summary>
        /// 游戏Cue库文件夹名称
        /// </summary>
        public const string GAS_CUE_LIBRARY_FOLDER = "GameplayCueLib";

        /// <summary>
        /// 修饰符计算库文件夹名称
        /// </summary>
        public const string GAS_MMC_LIBRARY_FOLDER = "ModMagnitudeCalculationLib";

        /// <summary>
        /// 技能任务库文件夹名称
        /// </summary>
        public const string GAS_ABILITY_TASK_LIBRARY_FOLDER = "AbilityTaskLib";

        /// <summary>
        /// 属性集类型前缀
        /// </summary>
        public const string GAS_ATTRIBUTESET_CLASS_TYPE_PREFIX = "GAS.Runtime.AttributeSet.AS_";

#if UNITY_EDITOR
        /// <summary>
        /// GAS基础设置资产路径
        /// </summary>
        public const string GAS_BASE_SETTING_PATH = "ProjectSettings/GASSettingAsset.asset";
        
        /// <summary>
        /// 游戏标签管理器资产路径
        /// </summary>
        public const string GAS_TAGS_MANAGER_ASSET_PATH = "ProjectSettings/GameplayTagsAsset.asset";
        
        /// <summary>
        /// 属性资产路径
        /// </summary>
        public const string GAS_ATTRIBUTE_ASSET_PATH = "ProjectSettings/AttributeAsset.asset";
        
        /// <summary>
        /// 属性集资产路径
        /// </summary>
        public const string GAS_ATTRIBUTE_SET_ASSET_PATH = "ProjectSettings/AttributeSetAsset.asset";
#endif
    }
}