using UnityEngine;
using UnityEditor;
using System;
using ColorMak3r.Utility;

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
        var inputWidth = EditorGUIUtility.fieldWidth;
        var sliderWidth = valueWidth - inputWidth * 2 - 3;

        // Create min input field
        position.width = inputWidth;
        minValue = EditorGUI.FloatField(position, minValue);
        minValue = (float)Math.Round(minValue, 2);
        if (minValue > maxValue)
            minValue = maxValue;

        // Create the MinMaxSlider
        position.x += inputWidth + 2;
        position.width = sliderWidth - 4;
        var maxSliderValue = maxValue.RoundToNextPowerOf10();
        maxSliderValue = maxValue > maxSliderValue - maxSliderValue * 0.1f ? (maxValue * 10).RoundToNextPowerOf10() : maxSliderValue;
        EditorGUI.MinMaxSlider(position, ref minValue, ref maxValue, 0f, maxSliderValue);

        // Create max input field
        position.x += sliderWidth;
        position.width = inputWidth;
        maxValue = EditorGUI.FloatField(position, maxValue);
        maxValue = (float)Math.Round(maxValue, 2);
        if (maxValue < minValue)
            maxValue = minValue;

        // Update the property values
        minProp.floatValue = minValue;
        maxProp.floatValue = maxValue;

        EditorGUI.EndProperty();
    }
}
