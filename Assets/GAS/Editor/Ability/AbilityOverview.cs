using System.Collections.Generic;
using System.Linq;
using GAS.General.Validation;
using GAS.Runtime;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GAS.Editor
{
    public class AbilityOverview
    {
        /// <summary>
        /// 能力唯一名称为空时显示的警告消息
        /// </summary>
        [BoxGroup("Warning", order: -1)]
        [HideLabel]
        [ShowIf("ExistAbilityWithEmptyUniqueName")]
        [DisplayAsString(TextAlignment.Left, true)]
        public string Warning_AbilityUniqueNameIsNull =
            "<size=13><color=yellow>The <color=orange>Unique Name</color> of the ability must not be <color=red><b>EMPTY</b></color>! " +
            "Please check!</color></size>";

        /// <summary>
        /// 能力唯一名称重复时显示的警告消息
        /// </summary>
        [BoxGroup("Warning", order: -1)]
        [HideLabel]
        [ShowIf("ExistAbilityWithDuplicatedUniqueName")]
        [DisplayAsString(TextAlignment.Left, true)]
        public string Warning_AbilityUniqueNameRepeat =
            "<size=13><color=yellow>The <color=orange>Unique Name</color> of the ability must not be <color=red><b>DUPLICATED</b></color>! " +
            "The duplicated abilities are as follows:<color=white> Move,Attack </color>.</color></size>";

        /// <summary>
        /// 存储所有能力信息的列表
        /// </summary>
        [VerticalGroup("Abilities", order: 1)]
        [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = false, ShowItemCount = true, IsReadOnly = true)]
        [DisplayAsString]
        public List<string> Abilities = new List<string>();

        public AbilityOverview()
        {
            Refresh();
        }

        [HorizontalGroup("Buttons", order: 0, MarginRight = 0.2f)]
        [GUIColor(0, 0.9f, 0.1f, 1)]
        [Button("Generate Ability Collection", ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
        void GenerateAbilityCollection()
        {
            if (ExistAbilityWithEmptyUniqueName() || ExistAbilityWithDuplicatedUniqueName())
            {
                EditorUtility.DisplayDialog("Warning", "Please check the warning message!\n" +
                                                       "Fix the Unique Name Error!\n" +
                                                       "(If you have fixed all and the warning still exist," +
                                                       " try to refresh the abilities with the REFRESH button.)", "OK");
                return;
            }

            AbilityCollectionGenerator.Gen();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 是否按唯一名称排序，true为按唯一名称排序，false为按资产名称排序
        /// </summary>
        private bool _orderByUniqueName = true;

        [HorizontalGroup("Buttons", width: 180)]
        [Button(SdfIconType.SortAlphaDown, "@_orderByUniqueName?\"Sort By AssetName\":\"Sort By UniqueName\"",
            ButtonHeight = 30)]
        public void ToggleOrderByUniqueName()
        {
            _orderByUniqueName = !_orderByUniqueName;
            Refresh();
        }

        /// <summary>
        /// 是否显示详细信息
        /// </summary>
        private bool _showDetail = false;

        [HorizontalGroup("Buttons", width: 120)]
        [Button(SdfIconType.TicketDetailed, "@_showDetail?\"Hide Detail\":\"Show Detail\"", ButtonHeight = 30)]
        public void ToggleShowDetail()
        {
            _showDetail = !_showDetail;
            Refresh();
        }

        [HorizontalGroup("Buttons", width: 50)]
        [GUIColor(1, 1f, 0)]
        [Button(SdfIconType.ArrowRepeat, "", ButtonHeight = 30)]
        [HideLabel]
        public void Refresh()
        {
            Abilities.Clear();
            var abilityAssets = EditorUtil.FindAssetsByType<AbilityAsset>(GASSettingAsset.GameplayAbilityLibPath);
            var orderedAbilityAssets = _orderByUniqueName
                ? abilityAssets
                    .OrderBy(x => x.UniqueName)
                    .ThenBy(x => x.name)
                : abilityAssets.OrderBy(x => x.name);

            Abilities = orderedAbilityAssets.Select(ability =>
            {
                var text = Validations.ValidateVariableName(ability.UniqueName).IsValid
                    ? ability.UniqueName
                    : $"{ability.UniqueName}(非法UniqueName)";

                if (_showDetail)
                {
                    text += $" - asset: {ability.name}, type: {ability.GetType().FullName}";
                }

                return text;
            }).ToList();
        }

        bool ExistAbilityWithEmptyUniqueName()
        {
            bool existEmpty = Abilities.Exists(string.IsNullOrEmpty);
            return existEmpty;
        }

        bool ExistAbilityWithDuplicatedUniqueName()
        {
            var duplicateStrings = FindDuplicateStrings(Abilities);
            bool existDuplicated = duplicateStrings.Length > 0;
            if (existDuplicated)
            {
                string duplicatedUniqueName = duplicateStrings.Aggregate("", (current, d) => current + (d + ","));
                duplicatedUniqueName = duplicatedUniqueName.Remove(duplicatedUniqueName.Length - 1, 1);
                Warning_AbilityUniqueNameRepeat =
                    "<size=13><color=yellow>The <color=orange>Unique Name</color> of the ability must not be <color=red><b>DUPLICATED</b></color>! " +
                    $"The duplicated abilities are as follows: \n <size=15><b><color=white> {duplicatedUniqueName} </color></b></size>.</color></size>";
            }

            return existDuplicated;
        }

        static string[] FindDuplicateStrings(IEnumerable<string> names)
        {
            var duplicates = names
                .Where(name => !string.IsNullOrEmpty(name))
                .GroupBy(name => name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            return duplicates;
        }
    }
}