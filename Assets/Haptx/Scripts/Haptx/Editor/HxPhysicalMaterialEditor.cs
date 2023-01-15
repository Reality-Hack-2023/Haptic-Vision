// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;
using UnityEditor.AnimatedValues;

//! A custom inspector for HxPhysicalMaterial.
[CustomEditor(typeof(HxPhysicalMaterial))]
[CanEditMultipleObjects]
public class HxPhysicalMaterialEditor : Editor {

  //! Animates HxPhysicalMaterial.forceFeedbackEnabled.
  AnimBool _ffeAnim = null;

  //! Animates HxPhysicalMaterial.baseContactToleranceM.
  AnimBool _bctAnim = null;

  //! Animates HxPhysicalMaterial.complianceM_N.
  AnimBool _cAnim = null;

  //! Draw custom inspector.
  public override void OnInspectorGUI() {
    HxPhysicalMaterial inner = (HxPhysicalMaterial)target;

    HxGUILayout.SerializedFieldLayout(serializedObject, () => inner.propagateToChildren);
    HxGUILayout.SerializedFieldLayout(serializedObject, () => inner.disableTactileFeedback);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject,
        () => inner.forceFeedbackEnabled, () => inner.overrideForceFeedbackEnabled,
        ref _ffeAnim, this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject,
        () => inner.baseContactToleranceM, () => inner.overrideBaseContactTolerance,
        ref _bctAnim, this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () => inner.complianceM_N,
        () => inner.overrideCompliance,
        ref _cAnim, this);
  }
}
