using UnityEngine;
using UnityEditor;

// Custom property drawers are used only in the Editor mode.
// They help to display things that are normally not visible or are displayed in an inconvenient manner.
[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer
{
    // Property drawers render their contents via an OnGUI method.
    // This method is provided the screen rectangle to draw inside, the serialized data of the property,
    // and the label of the field it belongs to.
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var coordinates = new HexCoordinates(
            property.FindPropertyRelative("_x").intValue,
            property.FindPropertyRelative("_z").intValue
        );

        // Names are usually drawn with the EditorGUI.PrefixLabel method.
        // It returns an adjusted rectangle which matches the space to the right of this label.
        position = EditorGUI.PrefixLabel(position, label);
        GUI.Label(position, coordinates.ToString());
    }
}