#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirstGearGames.Mirror.FlexNetworkTransform
{

    [CustomEditor(typeof(FlexNetworkTransformChild))]
    public class FlexNetworkTransformChildEditor : FlexNetworkTransformBaseEditor
    {
        //private MonoScript _script;
        private SerializedProperty _target;

        protected override void OnEnable()
        {
            base.OnEnable();
            //_script = MonoScript.FromMonoBehaviour((FlexNetworkTransformChild)target);
            _target = serializedObject.FindProperty("Target");
        }

        public override void OnInspectorGUI()
        {            
            serializedObject.Update();

            //EditorGUI.BeginDisabledGroup(true);
            //_script = EditorGUILayout.ObjectField("Script:", _script, typeof(MonoScript), false) as MonoScript;
            //EditorGUI.EndDisabledGroup();

            //Transform.
            EditorGUILayout.LabelField("Transform");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_target, new GUIContent("Target", "Transform to synchronize."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();


        }

    }
}
#endif