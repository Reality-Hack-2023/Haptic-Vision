// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;
using UnityEditor.AnimatedValues;

//! A custom inspector for HxRigidbodyProperties.
[CustomEditor(typeof(HxRigidbodyProperties))]
[CanEditMultipleObjects]
public class HxRigidbodyPropertiesEditor : Editor {

  //! Animates HxRigidbodyProperties.graspingEnabled.
  AnimBool _egAnim = null;

  //! Animates HxRigidbodyProperties.graspThreshold.
  AnimBool _gtAnim = null;

  //! Animates HxRigidbodyProperties.releaseHysteresis.
  AnimBool _rhAnim = null;

  //! Animates HxRigidbodyProperties.graspLinearLimits.
  AnimBool _gllAnim = null;

  //! Animates HxRigidbodyProperties.graspAngularLimits.
  AnimBool _galAnim = null;

  //! Animates HxRigidbodyProperties.contactDampingEnabled.
  AnimBool _cdeAnim = null;

  //! Animates HxRigidbodyProperties.maxContactDampingSeparation.
  AnimBool _mcdsAnim = null;

  //! Animates HxRigidbodyProperties.linearContactDamping.
  AnimBool _lcdAnim = null;

  //! Animates HxRigidbodyProperties.angularContactDamping.
  AnimBool _acdAnim = null;

  //! Animates HxRigidbodyProperties.sleepThreshold.
  AnimBool _stAnim = null;

  //! Draw custom inspector.
  public override void OnInspectorGUI() {
    HxRigidbodyProperties inner = (HxRigidbodyProperties)target;

    EditorGUILayout.LabelField("Grasping", HxStyles.BoldLabel);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () => inner.graspingEnabled,
        () => inner.overrideGraspingEnabled, ref _egAnim, this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () => inner.graspThreshold,
        () => inner.overrideGraspThreshold, ref _gtAnim, this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () => inner.releaseHysteresis,
        () => inner.overrideReleaseHysteresis, ref _rhAnim, this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () => inner.graspLinearLimits,
        () => inner.overrideGraspLinearLimits, ref _gllAnim, this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () => inner.graspAngularLimits,
        () => inner.overrideGraspAngularLimits, ref _galAnim, this);

    EditorGUILayout.LabelField("Contact Damping", HxStyles.BoldLabel);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () =>
        inner.contactDampingEnabled, () => inner.overrideContactDampingEnabled, ref _cdeAnim,
        this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject,
        () => inner.maxContactDampingSeparation,
        () => inner.overrideMaxContactDampingSeparation, ref _mcdsAnim, this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () => inner.linearContactDamping,
        () => inner.overrideLinearContactDamping, ref _lcdAnim, this);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () =>
        inner.angularContactDamping, () => inner.overrideAngularContactDamping, ref _acdAnim,
        this);

    EditorGUILayout.LabelField("Rigidbody", HxStyles.BoldLabel);
    HxGUILayout.SerializedFieldLayoutWithToggle(serializedObject, () => inner.sleepThreshold,
        () => inner.overrideSleepThreshold, ref _stAnim, this);
  }
}
