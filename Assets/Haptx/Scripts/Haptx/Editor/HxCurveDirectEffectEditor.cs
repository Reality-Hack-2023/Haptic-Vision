// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;

//! A custom inspector for HxCurveDirectEffect.
[CustomEditor(typeof(HxCurveDirectEffect)), CanEditMultipleObjects]
public class HxCurveDirectEffectEditor : Editor {

  //! Draw custom inspector.
  public override void OnInspectorGUI() {
    HxCurveDirectEffect hxCurveDirectEffect = (HxCurveDirectEffect)target;

    HxGUILayout.SerializedFieldLayout(serializedObject, "_playOnAwake");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_isLooping");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_coverageRegionsArray");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_inputTimeScale");
    HxGUILayout.UndoableCurveFieldLayout(target, typeof(HxCurveObjectEffect),
        () => hxCurveDirectEffect.displacementCurveAsset,
        ref hxCurveDirectEffect.displacementCurveAsset);
    HxGUILayout.SerializedFieldLayout(serializedObject, "_outputDisplacementScale");
  }
}
