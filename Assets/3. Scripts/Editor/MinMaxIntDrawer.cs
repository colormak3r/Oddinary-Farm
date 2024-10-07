using UnityEngine;
using UnityEditor;
using System;
using ColorMak3r.Utility;

[CustomPropertyDrawer(typeof(MinMaxInt))]
public class MinMaxIntDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Retrieve the min and max properties
        SerializedProperty minProp = property.FindPropertyRelative("min");
        SerializedProperty maxProp = property.FindPropertyRelative("max");

        int minValue = minProp.intValue;
        int maxValue = maxProp.intValue;

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
        minValue = EditorGUI.IntField(position, minValue);

        // Ensure minValue does not exceed maxValue
        if (minValue > maxValue)
            minValue = maxValue;

        // Create the MinMaxSlider
        position.x += inputWidth + 2;
        position.width = sliderWidth - 4;

        // Determine the slider's maximum value, rounded to the next power of 10
        int sliderMax = maxValue.RoundToNextPowerOf10();
        // Handle cases where maxValue is very close to sliderMax
        if (maxValue > sliderMax - sliderMax * 0.2f)
            sliderMax = (int)Math.Round(maxValue * 10.0).RoundToNextPowerOf10();

        // Convert int values to floats for the slider
        float sliderMin = minValue;
        float sliderMaxValue = maxValue;

        EditorGUI.MinMaxSlider(position, ref sliderMin, ref sliderMaxValue, 0f, sliderMax);

        // Update min and max values based on the slider
        minValue = Mathf.RoundToInt(sliderMin);
        maxValue = Mathf.RoundToInt(sliderMaxValue);

        // Position for the max input field
        position.x += sliderWidth;
        position.width = inputWidth;
        maxValue = EditorGUI.IntField(position, maxValue);

        // Ensure maxValue is not less than minValue
        if (maxValue < minValue)
            maxValue = minValue;

        // Assign the updated values back to the serialized properties
        minProp.intValue = minValue;
        maxProp.intValue = maxValue;

        EditorGUI.EndProperty();
    }
}


