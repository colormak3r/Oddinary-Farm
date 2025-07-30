using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ControlSetting))]
public class ControlSettingDrawer : PropertyDrawer
{
    const float kSpacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Grab sub-properties
        var action = property.FindPropertyRelative("ActionName");
        var defKb = property.FindPropertyRelative("DefaultKey_Keyboard");
        var curKb = property.FindPropertyRelative("CurrentKey_Keyboard");
        var defCtl = property.FindPropertyRelative("DefaultKey_Controller");
        var curCtl = property.FindPropertyRelative("CurrentKey_Controller");

        // Replace element label with action name (fallback to default label if empty)
        string elementLabel = string.IsNullOrEmpty(action.stringValue) ? label.text : action.stringValue;
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded, elementLabel, true);

        float y = position.y + EditorGUIUtility.singleLineHeight + kSpacing;

        if (property.isExpanded)
        {
            // Editable ActionName
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                action, new GUIContent("Action Name"));
            y += EditorGUIUtility.singleLineHeight + kSpacing;

            // Row widths
            float labelWidth = EditorGUIUtility.labelWidth + 2f;                                     // “Keyboard” / “Controller”
            float fieldWidth = (position.width - labelWidth - kSpacing) * 0.5f;

            // Keyboard row
            DrawPairRow(position.x, ref y, labelWidth, fieldWidth,
                        "Keyboard", defKb, curKb);

            // Controller row
            DrawPairRow(position.x, ref y, labelWidth, fieldWidth,
                        "Controller", defCtl, curCtl);
        }

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    void DrawPairRow(float x, ref float y, float labelW, float fieldW,
                     string rowLabel, SerializedProperty left, SerializedProperty right)
    {
        float h = EditorGUIUtility.singleLineHeight;
        EditorGUI.LabelField(new Rect(x, y, labelW, h), rowLabel);

        float fx = x + labelW;
        EditorGUI.PropertyField(new Rect(fx, y, fieldW, h), left, GUIContent.none);
        EditorGUI.PropertyField(new Rect(fx + fieldW + kSpacing, y, fieldW, h), right, GUIContent.none);

        y += h + kSpacing;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
        // foldout + action + two data rows + spacing between them
        int lines = 4;
        return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * kSpacing;
    }
}
