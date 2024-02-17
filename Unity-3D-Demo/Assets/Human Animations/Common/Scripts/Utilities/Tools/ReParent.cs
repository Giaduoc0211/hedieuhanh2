using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif
namespace MalbersAnimations.Utilities
{
    /// <summary>
    /// Simple script to reparent a bone on enable
    /// </summary>
    [AddComponentMenu("Malbers/Utilities/Tools/Parent")]
    public class ReParent : MonoBehaviour
    {
        [RequiredField] public Transform newParent;

        private void OnEnable()
        {
            transform.SetParent(newParent, true);

        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(ReParent)), CanEditMultipleObjects]
    public class ReParentEditor : Editor
    {
        SerializedProperty newParent;
        private void OnEnable()
        {
            newParent = serializedObject.FindProperty("newParent");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(newParent);
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}