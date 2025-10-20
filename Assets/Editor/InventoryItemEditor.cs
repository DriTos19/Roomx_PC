#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryItem))]
public class InventoryItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InventoryItem item = (InventoryItem)target;

        EditorGUILayout.Space();
        if (string.IsNullOrEmpty(item.itemID))
        {
            EditorGUILayout.HelpBox("No itemID set. Generate one for saving/networking.", MessageType.Warning);
            if (GUILayout.Button("Generate ID"))
            {
                item.itemID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(item);
            }
        }

        if (item.prefab == null)
        {
            EditorGUILayout.HelpBox("Prefab is missing. Assign a placeable prefab so the placement system can instantiate it.", MessageType.Error);
        }

        if (item.icon == null)
        {
            EditorGUILayout.HelpBox("Icon missing — add a sprite for the UI menu.", MessageType.Info);
        }
    }
}
#endif
