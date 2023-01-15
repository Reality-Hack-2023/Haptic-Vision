// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;

//! A custom inspector for HxCurveSpatialEffect.
[CustomEditor(typeof(HxCurveSpatialEffect)), CanEditMultipleObjects]
public class HxCurveSpatialEffectEditor : Editor {

  //! Draw custom inspector.
  public override void OnInspectorGUI() {
    HxCurveSpatialEffect hxCurveSpatialEffect = (HxCurveSpatialEffect)target;

    HxGUILayout.SerializedFieldLayout(serializedObject, "_playOnAwake");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_isLooping");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_boundingVolume");
    HxGUILayout.SerializedFieldLayout(serializedObject, "_inputTimeScale");
    HxGUILayout.UndoableCurveFieldLayout(target, typeof(HxCurveObjectEffect),
        () => hxCurveSpatialEffect.forceCurveAsset, ref hxCurveSpatialEffect.forceCurveAsset);
    HxGUILayout.SerializedFieldLayout(serializedObject, "_outputForceScale");
  }
}
