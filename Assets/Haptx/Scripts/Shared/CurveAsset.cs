// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! A serializable asset for 
//! [AnimationCurves](https://docs.unity3d.com/ScriptReference/AnimationCurve.html).
public class CurveAsset : ScriptableObject {

  //! The underlying curve.
  [Tooltip("The underlying curve.")]
  public AnimationCurve curve = new AnimationCurve();
}
