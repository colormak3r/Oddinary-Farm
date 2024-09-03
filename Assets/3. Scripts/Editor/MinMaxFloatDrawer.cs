using UnityEngine;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(MinMaxFloat))]
public class MinMaxFloatDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        SerializedProperty minProp = property.FindPropertyRelative("min");
        SerializedProperty maxProp = property.FindPropertyRelative("max");

        float minValue = minProp.floatValue;
        float maxValue = maxProp.floatValue;

        // Set the width for the label
        var width = position.width;
        position.width = EditorGUIUtility.labelWidth;
        EditorGUI.LabelField(position, label);

        // Adjust position for the slider
        position.x += position.width;
        var valueWidth = width - position.width;
        var inputWidth = valueWidth / 8;
        var sliderWidth = valueWidth * 6 / 8;

        // Create min input field
        position.width = inputWidth;
        minValue = EditorGUI.FloatField(position, minValue);
        minValue = (float)Math.Round(minValue,2);
        if(minValue > maxValue)
            minValue = maxValue;

        // Create the MinMaxSlider
        position.x += inputWidth + 2;
        position.width = sliderWidth - 4;       
        EditorGUI.MinMaxSlider(position, ref minValue, ref maxValue, 0f, 1f);

        // Create max input field
        position.x += sliderWidth;
        position.width = inputWidth;
        maxValue = EditorGUI.FloatField(position, maxValue);
        maxValue = (float)Math.Round(maxValue,2);
        if(maxValue < minValue)
            maxValue = minValue;

        // Update the property values
        minProp.floatValue = minValue;
        maxProp.floatValue = maxValue;

        EditorGUI.EndProperty();
    }
}
