using UnityEditor;
using UnityEngine;

namespace Spellplague.Utility
{
    [CustomEditor(typeof(MeshCombiner))]
    public class MeshCombinerEditor : Editor
    {
        SerializedProperty serializedObjectToCombine;
        SerializedProperty serializedMaterialToUse;
        SerializedProperty serializedOldObjectSetting;
        SerializedProperty serializedIncludeParent;
        SerializedProperty serializedMergeSubMeshes;
        SerializedProperty serializedIncludeTransformMatrices;
        SerializedProperty serializedIncludeLightMapData;
        SerializedProperty serializedColliderToAdd;

        private void OnEnable()
        {
            serializedObjectToCombine = serializedObject.FindProperty("objectToCombine");
            serializedMaterialToUse = serializedObject.FindProperty("materialToUse");
            serializedOldObjectSetting = serializedObject.FindProperty("whatToDoToOldObjects");
            serializedIncludeParent = serializedObject.FindProperty("includeParentMesh");
            serializedMergeSubMeshes = serializedObject.FindProperty("mergeSubMeshes");
            serializedIncludeTransformMatrices = serializedObject.FindProperty("includeTransformMatrices");
            serializedIncludeLightMapData = serializedObject.FindProperty("includeLightMapData");
            serializedColliderToAdd = serializedObject.FindProperty("colliderToAdd");
        }

        public override void OnInspectorGUI()
        {
            MeshCombiner targetObject = (MeshCombiner)target;
            serializedObject.Update();
            InspectorCombine(targetObject);
            serializedObject.ApplyModifiedProperties();
        }

        private void InspectorCombine(MeshCombiner targetObject)
        {
            EditorGUILayout.PropertyField(serializedObjectToCombine);
            EditorGUILayout.PropertyField(serializedMaterialToUse);
            EditorGUILayout.PropertyField(serializedOldObjectSetting);
            EditorGUILayout.PropertyField(serializedIncludeParent);
            EditorGUILayout.PropertyField(serializedMergeSubMeshes);
            EditorGUILayout.PropertyField(serializedIncludeTransformMatrices);
            EditorGUILayout.PropertyField(serializedIncludeLightMapData);
            EditorGUILayout.PropertyField(serializedColliderToAdd);
            if (GUILayout.Button("Combine Current Object"))
            {
                if (serializedObjectToCombine == null)
                {
                    Debug.LogError("Combine object is null");
                    return;
                }

                targetObject.Combine();
            }
        }
    }
}