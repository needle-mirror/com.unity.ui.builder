using System.Linq;
using UnityEngine.UIElements;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements.StyleSheets;
#endif
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class StylePropertyExtensions
    {

        internal static StyleValueHandle AddValue(
            this StyleSheet styleSheet, StyleProperty property, float value, string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = "Change UI Style Value";
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValues = property.values.ToList();
            var newValue = new StyleValueHandle(index, StyleValueType.Float);
            newValues.Add(newValue);
            property.values = newValues.ToArray();

            return newValue;
        }

#if UNITY_2019_3_OR_NEWER
        internal static StyleValueHandle AddValue(
            this StyleSheet styleSheet, StyleProperty property, Dimension value, string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = "Change UI Style Value";
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValues = property.values.ToList();
            var newValue = new StyleValueHandle(index, StyleValueType.Dimension);
            newValues.Add(newValue);
            property.values = newValues.ToArray();

            return newValue;
        }
#endif

        internal static StyleValueHandle AddValue(this StyleSheet styleSheet, StyleProperty property, Color value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, "Change UI Style Value");

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValues = property.values.ToList();
            var newValue = new StyleValueHandle(index, StyleValueType.Color);
            newValues.Add(newValue);
            property.values = newValues.ToArray();

            return newValue;
        }

        internal static StyleValueHandle AddValue(this StyleSheet styleSheet, StyleProperty property, string value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, "Change UI Style Value");

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValues = property.values.ToList();
            var newValue = new StyleValueHandle(index, StyleValueType.String);
            newValues.Add(newValue);
            property.values = newValues.ToArray();

            return newValue;
        }

        internal static StyleValueHandle AddValue(this StyleSheet styleSheet, StyleProperty property, Object value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, "Change UI Style Value");

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValues = property.values.ToList();
            var newValue = new StyleValueHandle(index, StyleValueType.AssetReference);
            newValues.Add(newValue);
            property.values = newValues.ToArray();

            return newValue;
        }

        internal static StyleValueHandle AddValue(this StyleSheet styleSheet, StyleProperty property, Enum value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, "Change UI Style Value");

            // Add value data to data array.
            var index = styleSheet.AddValueToArray(value);

            // Add value object to property.
            var newValues = property.values.ToList();
            var newValue = new StyleValueHandle(index, StyleValueType.Enum);
            newValues.Add(newValue);
            property.values = newValues.ToArray();

            return newValue;
        }

        internal static void RemoveValue(this StyleSheet styleSheet, StyleProperty property, StyleValueHandle valueHandle)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(styleSheet, "Change UI Style Value");

            // We just leave the values in their data array. If we really wanted to remove them
            // we would have to the indicies of all values.

            var valuesList = property.values.ToList();
            valuesList.Remove(valueHandle);
            property.values = valuesList.ToArray();
        }
    }
}