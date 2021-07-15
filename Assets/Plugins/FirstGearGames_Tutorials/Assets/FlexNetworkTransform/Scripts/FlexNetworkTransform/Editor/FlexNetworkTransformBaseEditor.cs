#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirstGearGames.Mirror.FlexNetworkTransform
{

    [CustomEditor(typeof(FlexNetworkTransformBase), true)]
    public class FlexNetworkTransformBaseEditor : Editor
    {
        private SerializedProperty _synchronizeInterval;

        private SerializedProperty _automaticInterpolation;
        private SerializedProperty _interpolationStrength;

        private SerializedProperty _enforceResults;
        private SerializedProperty _clientAuthoritative;
        private SerializedProperty _synchronizeToOwner;

        private SerializedProperty _preciseSynchronization;

        private SerializedProperty _synchronizePosition;
        private SerializedProperty _snapPosition;

        private SerializedProperty _synchronizeRotation;
        private SerializedProperty _snapRotation;

        private SerializedProperty _synchronizeScale;
        private SerializedProperty _snapScale;

        protected virtual void OnEnable()
        {
            _synchronizeInterval = serializedObject.FindProperty("_synchronizeInterval");

            _enforceResults = serializedObject.FindProperty("_enforceResults");
            _automaticInterpolation = serializedObject.FindProperty("_automaticInterpolation");
            _interpolationStrength = serializedObject.FindProperty("_interpolationStrength");

            _clientAuthoritative = serializedObject.FindProperty("_clientAuthoritative");
            _synchronizeToOwner = serializedObject.FindProperty("_synchronizeToOwner");

            _preciseSynchronization = serializedObject.FindProperty("_preciseSynchronization");

            _synchronizePosition = serializedObject.FindProperty("_synchronizePosition");
            _snapPosition = serializedObject.FindProperty("_snapPosition");

            _synchronizeRotation = serializedObject.FindProperty("_synchronizeRotation");
            _snapRotation = serializedObject.FindProperty("_snapRotation");

            _synchronizeScale = serializedObject.FindProperty("_synchronizeScale");
            _snapScale = serializedObject.FindProperty("_snapScale");
        }

        public override void OnInspectorGUI()
        {
            FlexNetworkTransformBase data = (FlexNetworkTransformBase)target;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            //Timing.
            EditorGUILayout.LabelField("Timing");
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_synchronizeInterval, new GUIContent("Synchronize Interval", "How often to synchronize this transform."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();



            //Synchronization Processing.
            EditorGUILayout.LabelField("Synchronization Processing");
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_preciseSynchronization, new GUIContent("Precise Synchronization", "True to synchronize data anytime it has changed. False to allow greater differences before synchronizing."));
            EditorGUILayout.PropertyField(_enforceResults, new GUIContent("Enforce Results", "True to force transform to results. False to stop checking for synchronization once at results. Enforcing ensures clients cannot move around after transform is set to the proper values at a minor performance cost."));
            EditorGUILayout.PropertyField(_automaticInterpolation, new GUIContent("Automatic Interpolation", "True to automatically determine interpolation strength. False to specify your own value."));
            if (_automaticInterpolation.boolValue == false)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_interpolationStrength, new GUIContent("Interpolation Strength", "How strongly to interpolate to server results. Higher values will result in more real-time results but may result in occasional stutter during network instability."));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();



            //Authority.
            EditorGUILayout.LabelField("Authority");
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_clientAuthoritative, new GUIContent("Client Authoritative", "True if using client authoritative movement."));
            if (_clientAuthoritative.boolValue == false)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_synchronizeToOwner, new GUIContent("Synchronize To Owner", "True to synchronize server results back to owner."));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();



            //Synchronize Properties.
            EditorGUILayout.LabelField("Synchronized Properties");
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_synchronizePosition, new GUIContent("Position", "Synchronize options for position."));
            if (_synchronizePosition.intValue == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapPosition, new GUIContent("Snap Position", "Euler axes on the position to snap into place rather than move towards over time."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(_synchronizeRotation, new GUIContent("Rotation", "Synchronize options for position."));
            if (_synchronizeRotation.intValue == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapRotation, new GUIContent("Snap Rotation", "Euler axes on the rotation to snap into place rather than move towards over time."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(_synchronizeScale, new GUIContent("Scale", "Synchronize options for scale."));
            if (_synchronizeScale.intValue == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapScale, new GUIContent("Snap Scale", "Euler axes on the scale to snap into place rather than move towards over time."));
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                data.SetSnapPosition((Axes)_snapPosition.intValue);
                data.SetSnapRotation((Axes)_snapRotation.intValue);
                data.SetSnapScale((Axes)_snapScale.intValue);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif