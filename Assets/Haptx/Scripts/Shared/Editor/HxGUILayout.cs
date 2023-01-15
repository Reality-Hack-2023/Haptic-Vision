// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

//! Helper functions that loosely follow the same paradigm as GUILayout.
public static class HxGUILayout {

  //! Creates a small "add" button.
  //!
  //! @param type The type of object that this button may add.
  //! @returns True when pressed.
  public static bool AddButton(Type type) {
    return GUILayout.Button(
        new GUIContent("+", HxGUIShared.GetAddText(type, true)),
        HxStyles.MiniButton);
  }

  //! Creates a small "remove" button.
  //!
  //! @param type The type of object that this button may remove.
  //! @returns True when pressed.
  public static bool RemoveButton(Type type) {
    return GUILayout.Button(
        new GUIContent("-", HxGUIShared.GetRemoveText(type, true)),
        HxStyles.MiniButton);
  }

  //! Creates a thin line that separates content vertically.
  public static void VerticalSeparatorLayout() {
    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
  }

  //! Signifies unimplemented sections of views.
  public static void NotImplementedLayout() {
    EditorGUILayout.LabelField("Not implemented", HxStyles.Label);
  }

  //! Creates a name field and an enum dropdown menu for instantiating objects.
  //!
  //! @param type The parent type.
  //! @param currentName The name typed in the name field.
  //! @param currentEnum The value currently selected for the child type.
  //! @param[out] outName The new name typed in the name field.
  //! @param[out] outEnum The new value selected for the child type.
  public static void CreationFieldLayout(Type type, string currentName, Enum currentEnum, out string outName, out Enum outEnum) {
    outName = EditorGUILayout.TextField(
        new GUIContent("Name",
            string.Format("Enter a name for the new {0} and select its type", type.ToString())),
        currentName, HxStyles.TextField);
    outEnum = EditorGUILayout.EnumPopup(currentEnum, HxStyles.Popup);
  }

  //! Creates a standard Unity serialized field.
  //!
  //! @param serializedObject The object that contains the serialized field.
  //! @param expression An expression that implies which field to display. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //! @param includeChildren True to include serialized sub-fields.
  public static void SerializedFieldLayout<T>(SerializedObject serializedObject,
      Expression<Func<T>> expression, bool includeChildren = true) {
    string propertyName = HxGUIShared.GetFieldName(expression);
    SerializedFieldLayout(serializedObject, propertyName, includeChildren);
  }

  //! Creates a standard Unity serialized field with a built-in toggle property.
  //!
  //! @param serializedObject The object that contains the serialized fields.
  //! @param field An expression that implies which field to display. E.g. 
  //! @code () => inner.graspingEnabled @endcode
  //! @param toggleField An expression that implies which field is the toggle. E.g. 
  //! @code () => inner.overrideGraspingEnabled @endcode
  //! @param anim An animator for toggled field.
  //! @param editor The Unity Editor instance representing the serialized object.
  //! @param includeChildren True to include serialized sub-fields.
  public static void SerializedFieldLayoutWithToggle<T>(SerializedObject serializedObject,
      Expression<Func<T>> field, Expression<Func<bool>> toggleField,
      ref AnimBool anim, Editor editor, bool includeChildren = true) {
    string fieldName = HxGUIShared.GetFieldName(field);
    string toggleFieldName = HxGUIShared.GetFieldName(toggleField);

    SerializedFieldLayout(serializedObject, toggleFieldName, false);
    EditorGUI.indentLevel++;
    if (anim == null) {
      anim = new AnimBool(toggleField.Compile()());
      anim.valueChanged.AddListener(editor.Repaint);
    } else {
      anim.target = toggleField.Compile()();
    }
    if (EditorGUILayout.BeginFadeGroup(anim.faded)) {
      SerializedFieldLayout(serializedObject, fieldName, includeChildren);
    }
    EditorGUILayout.EndFadeGroup();
    EditorGUI.indentLevel--;
  }

  //! Creates a standard Unity serialized field.
  //!
  //! @param serializedObject The object that contains the serialized field.
  //! @param propertyName The name of the field to display.
  //! @param includeChildren True to include serialized sub-fields.
  public static void SerializedFieldLayout(SerializedObject serializedObject,
      string propertyName, bool includeChildren = true) {
    SerializedProperty serializedProperty =
        serializedObject.FindProperty(propertyName);
    if (serializedProperty != null) {
      EditorGUILayout.PropertyField(serializedProperty, includeChildren);
      serializedObject.ApplyModifiedProperties();
    }
  }

  //! Creates a standard Unity serialized field.
  //!
  //! @param serializedObject The object that contains the serialized field.
  //! @param propertyName The name of the field to display.
  //! @param type The type of the field being displayed.
  //! @param labelText Text to display as the field's label.
  //! @param includeChildren True to include serialized sub-fields.
  public static void SerializedFieldLayout(SerializedObject serializedObject,
      string propertyName, Type type, string labelText, bool includeChildren = true) {
    SerializedProperty serializedProperty =
        serializedObject.FindProperty(propertyName);
    if (serializedProperty != null) {
      EditorGUILayout.PropertyField(serializedProperty,
          HxGUIShared.GetGUIContent(type, labelText, propertyName), includeChildren);
      serializedObject.ApplyModifiedProperties();
    }
  }

  //! Creates a standard Unity serialized float field that tracks changes in Unity's Undo 
  //! system.
  //!
  //! @param target The object whose state gets recorded in Unity's Undo system.
  //! @param owningType The type of the object that owns the field being displayed.
  //! @param expression An expression that implies which field to display. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //! @param valueRef A reference to the field being displayed.
  public static void UndoableFloatFieldLayout(UnityEngine.Object target, Type owningType,
      Expression<Func<float>> expression, ref float valueRef) {
    EditorGUI.BeginChangeCheck();
    float guiValue = EditorGUILayout.FloatField(
        HxGUIShared.GetGUIContent(owningType, expression), valueRef);
    if (EditorGUI.EndChangeCheck()) {
      Undo.RecordObject(target, HxGUIShared.GetChangeText(expression));
      valueRef = guiValue;
    }
  }

  //! Creates a standard Unity serialized bool field that tracks changes in Unity's Undo 
  //! system.
  //!
  //! @param target The object whose state gets recorded in Unity's Undo system.
  //! @param owningType The type of the object that owns the field being displayed.
  //! @param expression An expression that implies which field to display. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //! @param valueRef A reference to the field being displayed.
  public static void UndoableBoolFieldLayout(UnityEngine.Object target, Type owningType,
      Expression<Func<bool>> expression, ref bool valueRef) {
    EditorGUI.BeginChangeCheck();
    bool guiValue = EditorGUILayout.ToggleLeft(
          HxGUIShared.GetGUIContent(owningType, expression), valueRef);
    if (EditorGUI.EndChangeCheck()) {
      Undo.RecordObject(target, HxGUIShared.GetChangeText(expression));
      valueRef = guiValue;
    }
  }

  //! Creates a standard Unity serialized object field that tracks changes in Unity's Undo 
  //! system.
  //!
  //! @param target The object whose state gets recorded in Unity's Undo system.
  //! @param owningType The type of the object that owns the field being displayed.
  //! @param expression An expression that implies which field to display. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //! @param valueRef A reference to the field being displayed.
  public static void UndoableObjectFieldLayout<T>(UnityEngine.Object target, Type owningType,
      Expression<Func<T>> expression, ref T valueRef)
      where T : UnityEngine.Object {
    EditorGUI.BeginChangeCheck();
    T guiValue = (T)EditorGUILayout.ObjectField(
          HxGUIShared.GetGUIContent(owningType, expression), valueRef, typeof(T), false);
    if (EditorGUI.EndChangeCheck()) {
      Undo.RecordObject(target, HxGUIShared.GetChangeText(expression));
      valueRef = guiValue;
    }
  }

  //! Creates a standard Unity serialized curve field that tracks changes in Unity's Undo 
  //! system.
  //!
  //! @param target The object whose state gets recorded in Unity's Undo system.
  //! @param owningType The type of the object that owns the field being displayed.
  //! @param expression An expression that implies which field to display. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //! @param valueRef A reference to the field being displayed.
  public static void UndoableCurveFieldLayout(UnityEngine.Object target, Type owningType,
      Expression<Func<CurveAsset>> expression, ref CurveAsset valueRef) {
    EditorGUILayout.BeginHorizontal();
    UndoableObjectFieldLayout(target, owningType, expression, ref valueRef);
    if (valueRef == null) {
      EditorGUI.BeginChangeCheck();
      if (AddButton(typeof(CurveAsset)) && EditorGUI.EndChangeCheck()) {
        CurveAsset newCurveAsset = HxGUIShared.CreateCurveAsset();
        Undo.RecordObject(target, HxGUIShared.GetAddText(typeof(CurveAsset)));
        valueRef = newCurveAsset;
      }
      EditorGUILayout.EndHorizontal();
    } else {
      EditorGUILayout.EndHorizontal();
      EditorGUI.indentLevel++;  // Curve
      SerializedObject serializedObject = new SerializedObject(valueRef);
      CurveAsset value = valueRef;  // Can't use ref parameters in lambda functions.
      SerializedFieldLayout(serializedObject, () => value.curve);
      EditorGUI.indentLevel--;  // Curve
    }
  }

  //! Creates a float list field that tracks changes in Unity's Undo system.
  //!
  //! @param inList The list of floats being displayed.
  //! @param labelGuiContent The label to use on the field.
  //! @param target The object whose state gets recorded in Unity's Undo system.
  //! @param changeText The text to use in the Undo step.
  public static List<float> UndoableFloatListFieldLayout(List<float> inList,
      GUIContent labelGuiContent, UnityEngine.Object target, string changeText) {
    List<float> outList = new List<float>(inList);

    EditorGUILayout.BeginHorizontal();

    EditorGUILayout.LabelField(labelGuiContent, HxStyles.Label, GUILayout.MinWidth(0.0f));

    EditorGUI.BeginChangeCheck();
    if (AddButton(typeof(float)) &&
        EditorGUI.EndChangeCheck()) {
      Undo.RecordObject(target, changeText);
      outList.Add(0.0f);
    }

    EditorGUILayout.EndHorizontal();

    EditorGUI.indentLevel++;  // List.
    List<int> indicesToRemove = new List<int>();
    for (int i = 0; i < outList.Count; i++) {
      EditorGUILayout.BeginHorizontal();

      EditorGUI.BeginChangeCheck();
      float guiValue = EditorGUILayout.DelayedFloatField(string.Empty, outList[i]);
      if (EditorGUI.EndChangeCheck()) {
        Undo.RecordObject(target, changeText);
        outList[i] = guiValue;
      }

      EditorGUI.BeginChangeCheck();
      if (RemoveButton(typeof(float)) &&
          EditorGUI.EndChangeCheck()) {
        Undo.RecordObject(target, changeText);
        indicesToRemove.Add(i);
      }

      EditorGUILayout.EndHorizontal();
    }

    indicesToRemove.Reverse();  // Sort greatest to least.
    foreach (int i in indicesToRemove) {
      outList.RemoveAt(i);
    }

    EditorGUI.indentLevel--;  // List.

    return outList;
  }

  //! Creates a label indicating that a certain class may not be multi-edited.
  public static void CannotMultiEditLabel<T>() {
    EditorGUILayout.LabelField(string.Format(
        "Instances of {0} cannot be multi-edited.", typeof(T).ToString()), EditorStyles.helpBox);
  }

}
