using System;
using System.Collections.Generic;
using System.Linq;
using GAS.General;
using UnityEngine;

namespace GAS.Runtime
{
    [Serializable]
    public class OngoingTaskData : AbilityTaskData
    {
        public OngoingTaskData()
        {
            TaskData = new JsonData()
            {
                Type = typeof(DefaultOngoingAbilityTask).FullName,
            };
        }
        
        public OngoingAbilityTask CreateTask(AbilitySpec abilitySpec)
        {
            var task = base.Create(abilitySpec);
            var ongoingAbilityTask = task as OngoingAbilityTask;
            return ongoingAbilityTask;
        }

        public override AbilityTaskBase Load()
        {
            OngoingAbilityTask task = null;
            var jsonData = TaskData.Data;
            var dataType = string.IsNullOrEmpty(TaskData.Type) ? typeof(DefaultOngoingAbilityTask).FullName : TaskData.Type;

            var type = OngoingTaskSonTypes.FirstOrDefault(sonType => sonType.FullName == dataType);
            if (type == null)
            {
                Debug.LogError("[EX] OngoingAbilityTask SonType not found: " + dataType);
            }
            else
            {
                if (string.IsNullOrEmpty(jsonData))
                    task = Activator.CreateInstance(type) as OngoingAbilityTask;
                else
                    task = JsonUtility.FromJson(jsonData, type) as OngoingAbilityTask;
            }

            return task;
        }

        #region SonTypes

        /// <summary>
        /// 缓存的持续性任务子类型数组
        /// </summary>
        private static Type[] _ongoingTaskSonTypes;

        /// <summary>
        /// 获取所有持续性任务的子类型数组
        /// </summary>
        public static Type[] OngoingTaskSonTypes =>
            _ongoingTaskSonTypes ??= TypeUtil.GetAllSonTypesOf(typeof(OngoingAbilityTask));

        /// <summary>
        /// 获取持续性任务子类型的全名称列表，用于编辑器下拉选择
        /// </summary>
        public static List<string> OngoingTaskSonTypeChoices
        {
            get
            {
                return OngoingTaskSonTypes.Select(sonType => sonType.FullName).ToList();
            }
        }
        #endregion
    }
}