using System;
using System.Collections.Generic;
using System.Linq;
using GAS.General;
using UnityEngine;

namespace GAS.Runtime
{
    [Serializable]
    public class InstantTaskData : AbilityTaskData
    {
        public InstantTaskData()
        {
            TaskData = new JsonData()
            {
                Type = typeof(DefaultInstantAbilityTask).FullName,
            };    
        }
        
        public InstantAbilityTask CreateTask(AbilitySpec abilitySpec)
        {
            var task = base.Create(abilitySpec);
            var instantAbilityTask = task as InstantAbilityTask;
            return instantAbilityTask;
        }

        public override AbilityTaskBase Load()
        {
            InstantAbilityTask task = null;
            var jsonData = TaskData.Data;
            var dataType = string.IsNullOrEmpty(TaskData.Type) ? typeof(DefaultInstantAbilityTask).FullName : TaskData.Type;

            var type = InstantTaskSonTypes.FirstOrDefault(sonType => sonType.FullName == dataType);
            if (type == null)
            {
                Debug.LogError("[EX] InstantAbilityTask SonType not found: " + dataType);
            }
            else
            {
                if (string.IsNullOrEmpty(jsonData))
                    task = Activator.CreateInstance(type) as InstantAbilityTask;
                else
                    task = JsonUtility.FromJson(jsonData, type) as InstantAbilityTask;
            }

            return task;
        }

        #region SonTypes

        /// <summary>
        /// 缓存的瞬时任务子类型数组
        /// </summary>
        private static Type[] _instantTaskSonTypes;

        /// <summary>
        /// 获取所有瞬时任务的子类型数组
        /// </summary>
        public static Type[] InstantTaskSonTypes =>
            _instantTaskSonTypes ??= TypeUtil.GetAllSonTypesOf(typeof(InstantAbilityTask));

        /// <summary>
        /// 获取瞬时任务子类型的全名称列表，用于编辑器下拉选择
        /// </summary>
        public static List<string> InstantTaskSonTypeChoices
        {
            get
            {
                return InstantTaskSonTypes.Select(sonType => sonType.FullName).ToList();
            }
        }
        #endregion
    }
}