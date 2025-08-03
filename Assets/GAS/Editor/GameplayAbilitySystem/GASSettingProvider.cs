#if UNITY_EDITOR
namespace GAS.Editor
{
    using GAS;
    using UnityEngine.UIElements;
    using UnityEditor;
    using UnityEngine;
    
    public class GASSettingProvider: SettingsProvider
    {
        private  GASSettingAsset _asset; // GAS设置资源
        private  Editor _editor; // 编辑器实例
        
        public GASSettingProvider() : base("Project/EX Gameplay Ability System", SettingsScope.Project)
        {
        }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var asset = GASSettingAsset.LoadOrCreate();
            _asset = asset;
            _editor = Editor.CreateEditor(asset);
            GASSettingStatusWatcher.OnEditorFocused += OnEditorFocused;
        }
        
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            GASSettingStatusWatcher.OnEditorFocused -= OnEditorFocused;
            GASSettingAsset.UpdateAsset(_asset);
            GASSettingAsset.Save();
        }
        
        private void OnEditorFocused()
        {
            Repaint();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            if (_editor == null) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.Space();
            _editor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        static GASSettingProvider provider; // 设置提供器实例
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (GASSettingAsset.Instance && provider == null)
            {
                provider = new GASSettingProvider();
                using (var so = new SerializedObject(GASSettingAsset.Instance))
                {
                    provider.keywords = GetSearchKeywordsFromSerializedObject(so);
                }
            }
            return provider;
        }
    }
}
#endif