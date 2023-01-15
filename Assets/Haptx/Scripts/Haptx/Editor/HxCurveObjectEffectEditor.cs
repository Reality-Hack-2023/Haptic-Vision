// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;

//! A custom inspector for HxCurveObjectEffect.
[CustomEditor(typeof(HxCurveObjectEffect)), CanEditMultipleObjects]
public class HxCurveObjectEffectEditor : Editor {

  //! Draw custom inspector.
  public override void OnInspectorGUI() {
    HxCurveObjectEffect hxCurveObjectEffect = (HxCurveObjectEffect)target;

    HxGUILayout.SerializedFieldLayout(serializedObject, "_playOnAwake");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_isLooping");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_propagateToChildren");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_inputTimeScale");
    HxGUILayout.UndoableCurveFieldLayout(target, typeof(HxCurveObjectEffect),
        () => hxCurveObjectEffect.forceCurveAsset, ref hxCurveObjectEffect.forceCurveAsset);
    HxGUILayout.SerializedFieldLayout(serializedObject, "_outputForceScale");
  }
}
