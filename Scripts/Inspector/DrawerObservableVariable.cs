#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace G3D
{
    // This attribute tells Unity to use this drawer for ObservableVariable and all its subclasses
    // This makes the UI draw ObservableVariable<T> as if it were the underlying value itself
    [CustomPropertyDrawer(typeof(ObservableVariable<>), true)]
    public class DrawerObservableVariable : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");

            if (valueProp != null)
            {
                // 2. Draw the element field natively
                EditorGUI.PropertyField(position, valueProp, label, true);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            return valueProp != null
                ? EditorGUI.GetPropertyHeight(valueProp, label, true)
                : EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif
