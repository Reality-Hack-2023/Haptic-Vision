// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;

//! A custom inspector for Hx1DTranslator.
[CustomEditor(typeof(Hx1DTranslator)), CanEditMultipleObjects]
public class Hx1DTranslatorEditor : Hx1DJointEditor { }

//! A custom inspector for Hx1DRotator.
[CustomEditor(typeof(Hx1DRotator)), CanEditMultipleObjects]
public class Hx1DRotatorEditor : Hx1DJointEditor { }

//! A custom inspector for Hx1DJoint.
public class Hx1DJointEditor : HxJointEditor {

  //! Persistent view settings for Hx1DJointEditor.
  private static class ViewSettings {

    //! @brief Whether to expand operating dof.
    //!
    //! This setting applies to all instances by design. It makes editing the same value on 
    //! multiple instances in sequence more convenient.
    public static bool ShowOperatingDof {
      get {
        return EditorPrefs.GetBool(_ShowOperatingDof, false);
      }
      set {
        EditorPrefs.SetBool(_ShowOperatingDof, value);
      }
    }

    //! Key prefix.
    private static string _ShowOperatingDof = "showOperatingDof";
  }

  //! Draw custom inspector.
  public override void OnInspectorGUI() {
    Hx1DJoint hx1DJoint = (Hx1DJoint)target;

    // Draw fields that would automatically serialize.
    HxGUILayout.SerializedFieldLayout(serializedObject,
        "_supportedConfigurableJointParametersHaptx1DJoint",
        typeof(Hx1DJoint), "Configurable Joint Parameters");
    HxGUILayout.SerializedFieldLayout(serializedObject, () => hx1DJoint.visualizeAnchors);
    HxGUILayout.SerializedFieldLayout(serializedObject, () => hx1DJoint.initialPosition);
    HxGUILayout.SerializedFieldLayout(serializedObject, () => hx1DJoint.limitMotion);

    EditorGUI.BeginChangeCheck();
    HxGUILayout.SerializedFieldLayout(serializedObject,
        "_lowerLimit",
        typeof(Hx1DJoint), "Lower Limit");
    if (EditorGUI.EndChangeCheck()) {
      if (hx1DJoint.LowerLimit > hx1DJoint.UpperLimit) {
        hx1DJoint.SetLimits(hx1DJoint.UpperLimit, hx1DJoint.UpperLimit);
      }
    }
    EditorGUI.BeginChangeCheck();
    HxGUILayout.SerializedFieldLayout(serializedObject,
        "_upperLimit",
        typeof(Hx1DJoint), "Upper Limit");
    if (EditorGUI.EndChangeCheck()) {
      if (hx1DJoint.LowerLimit > hx1DJoint.UpperLimit) {
        hx1DJoint.SetLimits(hx1DJoint.LowerLimit, hx1DJoint.LowerLimit);
      }
    }

    HxGUILayout.SerializedFieldLayout(serializedObject, "_lockOtherDomain");
    HxGUILayout.SerializedFieldLayout(serializedObject, "damping");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_startAsleep");
    HxGUILayout.VerticalSeparatorLayout();

    if (HxGUIShared.AreMultipleComponentsSelected<Hx1DJoint>()) {
      HxGUILayout.CannotMultiEditLabel<HxDof>();
    } else {
      // Move HxStateFunctions and HxDofBehaviors when the operating axis changes.
      DofAxis priorDofAxis = hx1DJoint.OperatingAxis;
      SerializedProperty serializedProperty = serializedObject.FindProperty("_operatingAxis");
      EditorGUI.BeginChangeCheck();
      EditorGUILayout.PropertyField(serializedProperty);
      if (EditorGUI.EndChangeCheck()) {
        serializedObject.ApplyModifiedProperties();
        DofAxis newDofAxis = hx1DJoint.OperatingAxis;
        if (priorDofAxis != newDofAxis) {
          HxDof priorOperatingDof = hx1DJoint.dofs.GetDof(
              DegreeOfFreedomExtensions.FromDomainAndAxis(hx1DJoint.OperatingDomain,
              priorDofAxis));

          if (hx1DJoint.OperatingDomain == DofDomain.LINEAR) {
            hx1DJoint.dofs.SetLinearDof((HxLinearDof)priorOperatingDof, newDofAxis);
            hx1DJoint.dofs.SetLinearDof(new HxLinearDof(), priorDofAxis);
          } else {
            hx1DJoint.dofs.SetAngularDof((HxAngularDof)priorOperatingDof, newDofAxis);
            hx1DJoint.dofs.SetAngularDof(new HxAngularDof(), priorDofAxis);
          }
        }
      }

      // Draw the operating HxDof.
      HxDof operatingDof = hx1DJoint.GetOperatingDof();
      bool showOperatingDof = ViewSettings.ShowOperatingDof;
      DrawDof(target, operatingDof, hx1DJoint.OperatingDegreeOfFreedom, ref showOperatingDof);
      ViewSettings.ShowOperatingDof = showOperatingDof;
    }
  }
}
