using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class EnsBehaviourCollectionEditor : EditorWindow
{
    // 在菜单中添加一个选项，路径为 Ens/Collect EnsBehaviours
    [MenuItem("Ens/Collect EnsBehaviours", false, 10)]
    public static void ShowWindow()
    {
        // 获取当前所有选中的物体
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", "请先选择一个或多个物体", "确定");
            return;
        }

        int processedCount = 0; // 记录成功处理的物体数量

        // 遍历所有选中的物体
        foreach (GameObject obj in selectedObjects)
        {
            // 查找每个物体上的 EnsBeaviourCollection 组件
            EnsBehaviourCollection collection = obj.GetComponent<EnsBehaviourCollection>();
            if (collection == null)
            {
                // 如果某个物体没有该组件，仅在控制台提示，不中断整个过程
                Debug.LogWarning($"跳过物体 '{obj.name}'，因为它没有 EnsBehaviourCollection 组件。");
                continue;
            }

            // 查找该物体上所有的 EnsBehaviour 组件
            EnsBehaviour[] behaviours = obj.GetComponents<EnsBehaviour>();

            // 将找到的 EnsBehaviour 添加到列表中（即使数量为0也更新，以清空旧列表）
            collection.Behaviors = new List<EnsBehaviour>(behaviours);

            // 标记场景为已修改
            EditorUtility.SetDirty(collection);

            // 如果是预制体，保存预制体
            if (PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                PrefabUtility.SavePrefabAsset(obj);
            }

            processedCount++;
        }

        // 操作完成后，显示一个总结对话框
        if (processedCount > 0)
        {
            Debug.Log($"成功处理了 {processedCount} 个物体。\n\n详细信息请查看控制台日志。");
        }
        else
        {
            Debug.Log("没有找到任何带有 EnsBehaviourCollection 组件的选中物体。");
        }
    }

    // 验证菜单是否可用
    [MenuItem("Ens/Collect EnsBehaviours", true)]
    public static bool ValidateShowWindow()
    {
        // 只要有一个选中的物体带有 EnsBeaviourCollection 组件，菜单就可用
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            return false;
        }

        foreach (GameObject obj in selectedObjects)
        {
            if (obj.GetComponent<EnsBehaviourCollection>() != null)
            {
                return true;
            }
        }

        return false;
    }
}