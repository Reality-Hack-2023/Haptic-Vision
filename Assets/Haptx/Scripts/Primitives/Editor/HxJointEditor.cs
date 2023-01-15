// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//! A custom inspector for HxJoint.
[CustomEditor(typeof(HxJoint)), CanEditMultipleObjects]
public class HxJointEditor : Editor {

  //! Persistent view settings for an HxDof.
  private class DofViewSettings {

    //! Associates these view settings with a degree of freedom.
    //!
    //! @param degreeOfFreedom The degree of freedom to associate with.
    public DofViewSettings(DegreeOfFreedom degreeOfFreedom) {
      this._degreeOfFreedom = degreeOfFreedom;
    }

    //! @brief Whether to expand.
    //!
    //! This setting applies to all instances by design. It makes editing the same value on 
    //! multiple instances in sequence more convenient.
    public bool ShowDof {
      get {
        return EditorPrefs.GetBool(GetKey(_ShowDofSuffix), false);
      }
      set {
        EditorPrefs.SetBool(GetKey(_ShowDofSuffix), value);
      }
    }

    //! Key prefix.
    private static string _ShowDofSuffix = "showDof";

    //! @brief Whether to expand state functions.
    //!
    //! This setting applies to all instances by design. It makes editing the same value on 
    //! multiple instances in sequence more convenient.
    public bool ShowStateFunctions {
      get {
        return EditorPrefs.GetBool(GetKey(_ShowStateFunctions), true);
      }
      set {
        EditorPrefs.SetBool(GetKey(_ShowStateFunctions), value);
      }
    }

    //! Key prefix.
    private static string _ShowStateFunctions = "showStateFunctions";



    //! The name entered for creating a new state function.
    public string stateFunctionName = string.Empty;

    //! The type selected for creating a new state function.
    public Enum stateFunctionType = HxStateFunctionType.TwoState;

    //! @brief Whether to expand behaviors.
    //!
    //! This setting applies to all instances by design. It makes editing the same value on 
    //! multiple instances in sequence more convenient.
    public bool ShowDofBehaviors {
      get {
        return EditorPrefs.GetBool(GetKey(_ShowDofBehaviorsSuffix), true);
      }
      set {
        EditorPrefs.SetBool(GetKey(_ShowDofBehaviorsSuffix), value);
      }
    }

    //! Key prefix.
    private static string _ShowDofBehaviorsSuffix = "showDofBehaviors";

    //! The name entered for create a new behavior.
    public string dofBehaviorName = string.Empty;

    //! The type selected for creating a new behavior.
    public Enum dofBehaviorType = HxDofBehaviorType.Default;

    //! Which degree of freedom these variables apply to.
    private DegreeOfFreedom _degreeOfFreedom;

    //! Gets the key for a particular variable.
    //!
    //! @param prefix The prefix of the key.
    //! @returns The full key (prefix and suffix).
    private string GetKey(string prefix) {
      return prefix + ((int)_degreeOfFreedom).ToString();
    }
  }

  //! View settings for each HxDof.
  private Dictionary<DegreeOfFreedom, DofViewSettings> _settings =
      new Dictionary<DegreeOfFreedom, DofViewSettings>() {
        { DegreeOfFreedom.X_LIN, new DofViewSettings(DegreeOfFreedom.X_LIN) },
        { DegreeOfFreedom.Y_LIN, new DofViewSettings(DegreeOfFreedom.Y_LIN) },
        { DegreeOfFreedom.Z_LIN, new DofViewSettings(DegreeOfFreedom.Z_LIN) },
        { DegreeOfFreedom.X_ANG, new DofViewSettings(DegreeOfFreedom.X_ANG) },
        { DegreeOfFreedom.Y_ANG, new DofViewSettings(DegreeOfFreedom.Y_ANG) },
        { DegreeOfFreedom.Z_ANG, new DofViewSettings(DegreeOfFreedom.Z_ANG) }
      };

  //! Draw custom inspector.
  public override void OnInspectorGUI() {
    HxJoint hxJoint = (HxJoint)target;

    // Default configurable joint parameters.
    HxGUILayout.SerializedFieldLayout(serializedObject,
        "_supportedConfigurableJointParametersHaptxJoint", typeof(HxJoint),
        "Configurable Joint Parameters");
    HxGUILayout.SerializedFieldLayout(serializedObject, () => hxJoint.visualizeAnchors);
    HxGUILayout.SerializedFieldLayout(serializedObject, "_linearLimitsOffset");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_angularLimitsOffset");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_startAsleep");
    HxGUILayout.VerticalSeparatorLayout();

    if (HxGUIShared.AreMultipleComponentsSelected<HxJoint>()) {
      HxGUILayout.CannotMultiEditLabel<HxDof>();
    } else {
      // All HxJoint parameters.
      foreach (DegreeOfFreedom degreeOfFreedom in Enum.GetValues(typeof(DegreeOfFreedom))) {
        bool showDof = _settings[degreeOfFreedom].ShowDof;
        DrawDof(target, hxJoint.dofs.GetDof(degreeOfFreedom), degreeOfFreedom, ref showDof);
        _settings[degreeOfFreedom].ShowDof = showDof;
      }
    }
  }

  //! Draw GUI for an HxDof.
  //!
  //! @param target The GUI target.
  //! @param dof The HxDof of interest.
  //! @param degreeOfFreedom The degree of freedom of @p dof.
  //! @param expandDof Whether to expand this dof revealing behaviors and state functions.
  protected void DrawDof(UnityEngine.Object target, HxDof dof,
      DegreeOfFreedom degreeOfFreedom, ref bool expandDof) {
    if (dof == null) {
      return;
    }

    expandDof = EditorGUILayout.Foldout(expandDof, degreeOfFreedom.ToFriendlyString());
    if (expandDof) {
      // Fields that don't show up on Hx1DJoints.
      Hx1DJoint hx1DJoint = target as Hx1DJoint;
      if (!hx1DJoint) {
        HxGUILayout.UndoableBoolFieldLayout(target, dof.GetType(), () => dof.forceUpdate,
          ref dof.forceUpdate);
      }

      // HxAngularDof settings.
      if (dof.GetType() == typeof(HxAngularDof)) {
        HxAngularDof angularDof = (HxAngularDof)dof;

        HxGUILayout.UndoableBoolFieldLayout(target, dof.GetType(),
            () => angularDof.trackMultipleRevolutions, ref angularDof.trackMultipleRevolutions);
      }

      // HxStateFunctions.
      EditorGUI.indentLevel++;  // HxStateFunctions foldout.
      bool stateFunctionsExist = dof.StateFunctions.Count > 0;
      _settings[degreeOfFreedom].ShowStateFunctions =
          EditorGUILayout.Foldout(_settings[degreeOfFreedom].ShowStateFunctions,
          "State Functions");
      if (_settings[degreeOfFreedom].ShowStateFunctions) {
        // Create a new HxStateFunction.
        EditorGUI.indentLevel++;  // Individual HxStateFunctions.
        EditorGUILayout.BeginHorizontal();
        HxGUILayout.CreationFieldLayout(typeof(HxStateFunction),
            _settings[degreeOfFreedom].stateFunctionName == string.Empty ?
                RecommendStateFunctionName(dof.StateFunctions) :
                _settings[degreeOfFreedom].stateFunctionName,
            _settings[degreeOfFreedom].stateFunctionType,
            out _settings[degreeOfFreedom].stateFunctionName,
            out _settings[degreeOfFreedom].stateFunctionType);
        EditorGUI.BeginChangeCheck();
        if (HxGUILayout.AddButton(typeof(HxStateFunction)) &&
            HxGUIShared.ValidateName(_settings[degreeOfFreedom].stateFunctionName) &&
            EditorGUI.EndChangeCheck()) {
          Undo.RecordObject(target, HxGUIShared.GetAddText(typeof(HxStateFunction)));
          dof.RegisterStateFunction(HxStateFunctionTypeExtensions.CreateByTypeEnum(
              (HxStateFunctionType)_settings[degreeOfFreedom].stateFunctionType,
              _settings[degreeOfFreedom].stateFunctionName));
          _settings[degreeOfFreedom].stateFunctionName = string.Empty;
        }
        EditorGUILayout.EndHorizontal();
        if (stateFunctionsExist) {
          HxGUILayout.VerticalSeparatorLayout();
        }

        // View existing HxStateFunctions.
        List<HxStateFunction> stateFunctionsToUnreg = new List<HxStateFunction>();
        foreach (HxStateFunction stateFunction in dof.StateFunctions) {
          DrawStateFunction(stateFunction, stateFunctionsToUnreg, target);
        }
        foreach (HxStateFunction stateFunctionToUnreg in stateFunctionsToUnreg) {
          dof.UnregisterStateFunction(stateFunctionToUnreg);
        }
        EditorGUI.indentLevel--;  // Individual HxStateFunctions.
      }
      EditorGUI.indentLevel--;  // HxStateFunctions foldout.

      // HxDofBehaviors.
      EditorGUI.indentLevel++;  // HxDofBehaviors foldout.
      bool dofBehaviorsExist = dof.Behaviors.Count > 0;
      _settings[degreeOfFreedom].ShowDofBehaviors =
          EditorGUILayout.Foldout(_settings[degreeOfFreedom].ShowDofBehaviors,
          "Dof Behaviors");
      if (_settings[degreeOfFreedom].ShowDofBehaviors) {
        // Create a new HxDofBehavior.
        EditorGUI.indentLevel++;  // Individual HxDofBehaviors.
        EditorGUILayout.BeginHorizontal();
        HxGUILayout.CreationFieldLayout(typeof(HxDofBehavior),
            _settings[degreeOfFreedom].dofBehaviorName == string.Empty ?
                RecommendDofBehaviorName(dof.Behaviors) :
                _settings[degreeOfFreedom].dofBehaviorName,
            _settings[degreeOfFreedom].dofBehaviorType,
            out _settings[degreeOfFreedom].dofBehaviorName,
            out _settings[degreeOfFreedom].dofBehaviorType);
        EditorGUI.BeginChangeCheck();
        if (HxGUILayout.AddButton(typeof(HxDofBehavior)) &&
            HxGUIShared.ValidateName(_settings[degreeOfFreedom].dofBehaviorName) &&
            EditorGUI.EndChangeCheck()) {
          Undo.RecordObject(target, HxGUIShared.GetAddText(typeof(HxDofBehavior)));
          dof.RegisterBehavior(HxDofBehaviorTypeExtensions.CreateByTypeEnum(
              (HxDofBehaviorType)_settings[degreeOfFreedom].dofBehaviorType,
              _settings[degreeOfFreedom].dofBehaviorName));
          _settings[degreeOfFreedom].dofBehaviorName = string.Empty;
        }
        EditorGUILayout.EndHorizontal();
        if (dofBehaviorsExist) {
          HxGUILayout.VerticalSeparatorLayout();
        }

        // View existing HxDofBehaviors.
        List<HxDofBehavior> dofBehaviorsToUnreg = new List<HxDofBehavior>();
        foreach (HxDofBehavior dofBehavior in dof.Behaviors) {
          DrawDofBehavior(dofBehavior, dofBehaviorsToUnreg, target);
        }
        foreach (HxDofBehavior dofBehaviorToUnreg in dofBehaviorsToUnreg) {
          dof.UnregisterBehavior(dofBehaviorToUnreg);
        }

        EditorGUI.indentLevel--;  // Individual HxDofBehaviors.
      }
      EditorGUI.indentLevel--;  // HxDofBehaviors foldout.
    }
  }

  //! Draw GUI for an HxStateFunction.
  //!
  //! @param stateFunction The state function of interest.
  //! @param stateFunctionsToUnreg Appended with any state functions that need to be removed.
  //! @param target The GUI target.
  protected static void DrawStateFunction(HxStateFunction stateFunction,
      List<HxStateFunction> stateFunctionsToUnreg, UnityEngine.Object target) {
    if (stateFunction == null) {
      Debug.LogError(string.Format("Asked to draw null {0}", typeof(HxStateFunction)));
      return;
    }

    EditorGUILayout.BeginHorizontal();
    Type stateFunctionType = stateFunction.GetType();
    EditorGUILayout.LabelField(
        new GUIContent(
            string.Format("{0} ({1})", stateFunction.name, stateFunctionType),
            HxGUIShared.GetTooltip(stateFunctionType, () => stateFunction.name)),
        HxStyles.BoldLabel, GUILayout.MinWidth(0.0f));
    EditorGUI.BeginChangeCheck();
    if (HxGUILayout.RemoveButton(typeof(HxStateFunction)) &&
        EditorGUI.EndChangeCheck()) {
      Undo.RecordObject(target, HxGUIShared.GetRemoveText(typeof(HxStateFunction)));
      stateFunctionsToUnreg.Add(stateFunction);
    }
    EditorGUILayout.EndHorizontal();

    EditorGUI.indentLevel++;  // HxStateFunction.

    HxGUILayout.UndoableBoolFieldLayout(target, stateFunctionType,
        () => stateFunction.invertStateOrder, ref stateFunction.invertStateOrder);

    if (stateFunctionType == typeof(Hx2StateFunction)) {
      Hx2StateFunction twoStateFunction = (Hx2StateFunction)stateFunction;

      HxGUILayout.UndoableFloatFieldLayout(target, stateFunctionType,
          () => twoStateFunction.transitionPosition, ref twoStateFunction.transitionPosition);
    } else if (stateFunctionType == typeof(Hx3StateFunction)) {
      Hx3StateFunction threeStateFunction = (Hx3StateFunction)stateFunction;

      HxGUILayout.UndoableFloatFieldLayout(target, stateFunctionType,
          () => threeStateFunction.lowTransitionPosition,
          ref threeStateFunction.lowTransitionPosition);

      HxGUILayout.UndoableFloatFieldLayout(target, stateFunctionType,
          () => threeStateFunction.highTransitionPosition,
          ref threeStateFunction.highTransitionPosition);
    } else if (stateFunctionType == typeof(HxNStateFunction)) {
      HxNStateFunction nStateFunction = (HxNStateFunction)stateFunction;

      nStateFunction.StatePositions = HxGUILayout.UndoableFloatListFieldLayout(
          nStateFunction.StatePositions,
          HxGUIShared.GetGUIContent(stateFunctionType, () =>
          nStateFunction.StatePositions, "_statePositions"), target,
          HxGUIShared.GetChangeText(() => nStateFunction.StatePositions));
    } else if (stateFunctionType == typeof(HxCurveStateFunction)) {
      HxCurveStateFunction curveStateFunction = (HxCurveStateFunction)stateFunction;

      HxGUILayout.UndoableFloatFieldLayout(target, stateFunctionType,
          () => curveStateFunction.inputScale, ref curveStateFunction.inputScale);
      HxGUILayout.UndoableFloatFieldLayout(target, stateFunctionType,
          () => curveStateFunction.inputOffset, ref curveStateFunction.inputOffset);
      HxGUILayout.UndoableCurveFieldLayout(target, stateFunctionType,
          () => curveStateFunction.curveAsset, ref curveStateFunction.curveAsset);
    } else {
      HxGUILayout.NotImplementedLayout();
    }

    EditorGUI.indentLevel--;  // HxStateFunction.
  }

  //! Draw GUI for an HxDofBehavior.
  //!
  //! @param dofBehavior The behavior of interest.
  //! @param dofBehaviorsToUnreg Appended with any behaviors that need to be removed.
  //! @param target The GUI target.
  protected static void DrawDofBehavior(HxDofBehavior dofBehavior,
      List<HxDofBehavior> dofBehaviorsToUnreg, UnityEngine.Object target) {
    if (dofBehavior == null) {
      Debug.LogError(string.Format("Asked to draw null {0}", typeof(HxDofBehavior)));
      return;
    }

    EditorGUILayout.BeginHorizontal();
    Type dofBehaviorType = dofBehavior.GetType();
    EditorGUILayout.LabelField(
        new GUIContent(
            string.Format("{0} ({1})", dofBehavior.name, dofBehaviorType),
            HxGUIShared.GetTooltip(dofBehaviorType, () => dofBehavior.name)),
        HxStyles.BoldLabel, GUILayout.MinWidth(0.0f));
    EditorGUI.BeginChangeCheck();
    if (HxGUILayout.RemoveButton(typeof(HxDofBehavior)) &&
        EditorGUI.EndChangeCheck()) {
      Undo.RecordObject(target, HxGUIShared.GetRemoveText(typeof(HxDofBehavior)));
      dofBehaviorsToUnreg.Add(dofBehavior);
    }
    EditorGUILayout.EndHorizontal();

    EditorGUI.indentLevel++;  // HxDofBehavior.

    HxGUILayout.UndoableBoolFieldLayout(target, dofBehaviorType,
          () => dofBehavior.acceleration, ref dofBehavior.acceleration);

    HxGUILayout.UndoableBoolFieldLayout(target, dofBehaviorType,
          () => dofBehavior.enabled, ref dofBehavior.enabled);

    HxGUILayout.UndoableBoolFieldLayout(target, dofBehaviorType,
          () => dofBehavior.visualize, ref dofBehavior.visualize);

    if (dofBehaviorType == typeof(HxDofTargetPositionBehavior)) {
      HxDofTargetPositionBehavior targetPosBehavior = (HxDofTargetPositionBehavior)dofBehavior;

      HxGUILayout.UndoableFloatFieldLayout(target, dofBehaviorType,
          () => targetPosBehavior.targetPosition, ref targetPosBehavior.targetPosition);
    } else if (dofBehaviorType == typeof(HxDofDetentBehavior)) {
      HxDofDetentBehavior detentBehavior = (HxDofDetentBehavior)dofBehavior;

      detentBehavior.Detents = HxGUILayout.UndoableFloatListFieldLayout(
          detentBehavior.Detents,
          HxGUIShared.GetGUIContent(dofBehaviorType, () => detentBehavior.Detents, "detents"),
          target,
          HxGUIShared.GetChangeText(() => detentBehavior.Detents));
      // This type of behavior has no custom GUI.
    } else if (dofBehaviorType != typeof(HxDofDefaultBehavior)) {
      HxGUILayout.NotImplementedLayout();
    }

    // Match the HxDofBehavior's HxPhysicalModel to the dropdown menu.
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField(
        HxGUIShared.GetGUIContent(dofBehaviorType, () => dofBehavior.model),
        HxStyles.Label, GUILayout.MinWidth(0.0f));
    if (dofBehavior.model == null) {
      dofBehavior.model = new HxConstantModel();
    }
    HxPhysicalModelType modelTypeExisting =
        HxPhysicalModelTypeExtensions.enumFromType[dofBehavior.model.GetType()];
    EditorGUI.BeginChangeCheck();
    HxPhysicalModelType modelTypeGui = (HxPhysicalModelType)EditorGUILayout.EnumPopup(
        modelTypeExisting,
        HxStyles.Popup);
    if (modelTypeExisting != modelTypeGui) {
      HxPhysicalModel model = HxPhysicalModelTypeExtensions.CreateByTypeEnum(modelTypeGui);
      if (EditorGUI.EndChangeCheck()) {
        Undo.RecordObject(target, HxGUIShared.GetChangeText(() => dofBehavior.model));
        dofBehavior.model = model;
      }
    }
    EditorGUILayout.EndHorizontal();

    // Draw fields unique to the current model.
    EditorGUI.indentLevel++;  // HxPhysicalModel.
    Type modelType = dofBehavior.model.GetType();
    if (modelType == typeof(HxConstantModel)) {
      HxConstantModel constantModel = (HxConstantModel)dofBehavior.model;

      HxGUILayout.UndoableFloatFieldLayout(target, modelType, () => constantModel.constant,
          ref constantModel.constant);
    } else if (modelType == typeof(HxSpringModel)) {
      HxSpringModel springModel = (HxSpringModel)dofBehavior.model;

      HxGUILayout.UndoableFloatFieldLayout(target, modelType, () => springModel.stiffness,
          ref springModel.stiffness);
    } else if (modelType == typeof(HxCurveModel)) {
      HxCurveModel curveModel = (HxCurveModel)dofBehavior.model;

      HxGUILayout.UndoableCurveFieldLayout(target, modelType, () => curveModel.curveAsset,
          ref curveModel.curveAsset);

      HxGUILayout.UndoableFloatFieldLayout(target, modelType, () => curveModel.inputScale,
          ref curveModel.inputScale);

      HxGUILayout.UndoableFloatFieldLayout(target, modelType, () => curveModel.inputOffset,
          ref curveModel.inputOffset);

      HxGUILayout.UndoableFloatFieldLayout(target, modelType, () => curveModel.outputScale,
          ref curveModel.outputScale);

      HxGUILayout.UndoableFloatFieldLayout(target, modelType, () => curveModel.outputOffset,
          ref curveModel.outputOffset);
    } else {
      HxGUILayout.NotImplementedLayout();
    }

    EditorGUI.indentLevel--;  // HxPhysicalModel.
    EditorGUI.indentLevel--;  // HxDofBehavior.
  }

  //! Recommends a unique name for an HxStateFunction.
  //!
  //! @param existingStateFunctions The existing set of state functions.
  //! @returns A name not present in @p existingStateFunctions.
  private static string RecommendStateFunctionName(
      ICollection<HxStateFunction> existingStateFunctions) {
    List<string> existingNames = new List<string>();
    foreach (HxStateFunction stateFunction in existingStateFunctions) {
      if (stateFunction != null) {
        existingNames.Add(stateFunction.name);
      }
    }

    return HxGUIShared.RecommendName(existingNames, "Function");
  }

  //! Recommends a unique name for an HxDofBehavior.
  //!
  //! @param existingDofBehaviors The existing set of behaviors.
  //! @returns A name not present in @p existingDofBehaviors.
  private static string RecommendDofBehaviorName(ICollection<HxDofBehavior> existingDofBehaviors) {
    List<string> existingNames = new List<string>();
    foreach (HxDofBehavior dofBehavior in existingDofBehaviors) {
      if (dofBehavior != null) {
        existingNames.Add(dofBehavior.name);
      }
    }

    return HxGUIShared.RecommendName(existingNames, "Behavior");
  }
}
