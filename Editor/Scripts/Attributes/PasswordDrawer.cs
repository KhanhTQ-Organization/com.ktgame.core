using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace com.ktgame.core.editor
{
    public class PasswordDrawer : OdinAttributeDrawer<PasswordAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueEntry.SmartValue = EditorGUILayout.PasswordField(label, ValueEntry.SmartValue);
        }
    }
}