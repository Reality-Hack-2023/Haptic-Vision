// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;
using UnityEngine;

//! Makes HxName appear in the inspector as a string.
[CustomPropertyDrawer(typeof(HxName))]
public class HxNameDrawer : PropertyDrawer {

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.BeginProperty(position, label, property);
    label.tooltip = HxGUIShared.GetTooltip(fieldInfo, true);
    SerializedProperty layerProperty = property.FindPropertyRelative("_string");
    layerProperty.stringValue = EditorGUI.TextField(position, label, layerProperty.stringValue);
    EditorGUI.EndProperty();
  }
}
