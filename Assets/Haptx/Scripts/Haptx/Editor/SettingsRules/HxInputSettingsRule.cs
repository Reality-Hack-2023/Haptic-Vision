// Copyright (C) 2022 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

//! Settings rule to make sure the debug input axes are working.
class HxInputSettingsRule : IHxRecommendedSettingsRule {
  //! See IHxRecommendedSettingsRule.IgnoreId
  public string IgnoreId => "haptx.input.debugaxes";

  //! See IHxRecommendedSettingsRule.RecommendsEditorRestartOnApply
  public bool RecommendsEditorRestartOnApply => false;

  //! See IHxRecommendedSettingsRule.DisplayDescription
  public string DisplayDescription => "InputManager axes for some HaptX debug functions are missing.";

  //! See IHxRecommendedSettingsRule.SetButtonDisplayText
  public string SetButtonDisplayText => "Add InputManager axes";

  //! Class to hold data about the input axes we want to set.
  private class InputDefault {
    public string axisName;
    public string positiveButton;
    public string negativeButton;
  }

  //! Array of input axes we want to set.
  private static readonly InputDefault[] inputDefaults_ = GetInputDefaults().ToArray();

  //! Gets the input axes we want to set.
  private static IEnumerable<InputDefault> GetInputDefaults() {

    yield return new InputDefault() {
      axisName = "HxOffsetForward",
      positiveButton = "w",
      negativeButton = "s"
    };

    yield return new InputDefault() {
      axisName = "HxOffsetRight",
      positiveButton = "d",
      negativeButton = "a"
    };

    yield return new InputDefault() {
      axisName = "HxOffsetUp",
      positiveButton = "q",
      negativeButton = "z"
    };

    yield return new InputDefault() {
      axisName = "HxOffsetPitch",
      positiveButton = "[8]",
      negativeButton = "[2]"
    };

    yield return new InputDefault() {
      axisName = "HxOffsetYaw",
      positiveButton = "[6]",
      negativeButton = "[4]"
    };

    yield return new InputDefault() {
      axisName = "HxOffsetRoll",
      positiveButton = "[.]",
      negativeButton = "[0]"
    };
  }

  //! See IHxRecommendedSettingsRule.SetRecommendedSetting
  public void SetRecommendedSetting() {
    SerializedProperty axes = GetInputAxesProperty(
        out UnityEngine.Object inputManager, out SerializedObject serializedManager);

    HashSet<string> existingAxisNames = new HashSet<string>();
    for (int i = 0; i < axes.arraySize; ++i) {
      SerializedProperty inputAxis = axes.GetArrayElementAtIndex(i);
      existingAxisNames.Add(inputAxis.FindPropertyRelative("m_Name").stringValue);
    }

    foreach (InputDefault value in inputDefaults_) {
      if (!existingAxisNames.Contains(value.axisName)) {
        axes.InsertArrayElementAtIndex(axes.arraySize);
        SerializedProperty newAxis = axes.GetArrayElementAtIndex(axes.arraySize - 1);

        newAxis.FindPropertyRelative("m_Name").stringValue = value.axisName;
        newAxis.FindPropertyRelative("descriptiveName").stringValue = null;
        newAxis.FindPropertyRelative("descriptiveNegativeName").stringValue = null;
        newAxis.FindPropertyRelative("negativeButton").stringValue = value.negativeButton;
        newAxis.FindPropertyRelative("positiveButton").stringValue = value.positiveButton;
        newAxis.FindPropertyRelative("altNegativeButton").stringValue = null;
        newAxis.FindPropertyRelative("altPositiveButton").stringValue = null;
        newAxis.FindPropertyRelative("gravity").floatValue = 1000;
        newAxis.FindPropertyRelative("dead").floatValue = 0.001f;
        newAxis.FindPropertyRelative("sensitivity").floatValue = 2;
        newAxis.FindPropertyRelative("snap").boolValue = false;
        newAxis.FindPropertyRelative("invert").boolValue = false;
        newAxis.FindPropertyRelative("type").intValue = 0; // "Key or Mouse Button"
        newAxis.FindPropertyRelative("axis").intValue = 0; // "X axis"
        newAxis.FindPropertyRelative("joyNum").intValue = 0; // "Get Motion from all Joysticks"
      }
    }

    Undo.RecordObject(inputManager, "Added HaptX debug input axes");
    serializedManager.ApplyModifiedProperties();
    EditorUtility.SetDirty(inputManager);
  }

  //! See IHxRecommendedSettingsRule.SettingMatchesRecommended
  public bool SettingMatchesRecommended() {
    SerializedProperty axes = GetInputAxesProperty(out _, out _);

    HashSet<string> existingAxisNames = new HashSet<string>();
    for (int i = 0; i < axes.arraySize; ++i) {
      SerializedProperty inputAxis = axes.GetArrayElementAtIndex(i);
      existingAxisNames.Add(inputAxis.FindPropertyRelative("m_Name").stringValue);
    }

    foreach (InputDefault value in inputDefaults_) {
      if (!existingAxisNames.Contains(value.axisName)) {
        return false;
      }
    }

    return true;
  }

  private const string INPUT_MANAGER_ASSET_PATH = "ProjectSettings/InputManager.asset";
  private const string AXES_PROPERTY_PATH = "m_Axes";
  //! @brief Gets the serialized input axes property from the input manager.
  //!
  //! @param [out] inputManager The input manager as a Unity Object.
  //! @param [out] serializedManager The input manager as a SeralizedObject.
  //! @returns The serialized input axes property.
  private static SerializedProperty GetInputAxesProperty(out UnityEngine.Object inputManager,
      out SerializedObject serializedManager) {

    inputManager = AssetDatabase.LoadMainAssetAtPath(INPUT_MANAGER_ASSET_PATH);
    serializedManager = new SerializedObject(inputManager);

    return serializedManager.FindProperty(AXES_PROPERTY_PATH);
  }
}
