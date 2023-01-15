// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;
using UnityEngine;

//! Makes HxLayer appear in the inspector as a layer selection drop down menu.
[CustomPropertyDrawer(typeof(HxLayer))]
public class HxLayerDrawer : PropertyDrawer {

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.BeginProperty(position, label, property);
    label.tooltip = HxGUIShared.GetTooltip(fieldInfo, true);
    SerializedProperty layerProperty = property.FindPropertyRelative("value");
    layerProperty.intValue = EditorGUI.LayerField(position, label, layerProperty.intValue);
    EditorGUI.EndProperty();
  }
}
