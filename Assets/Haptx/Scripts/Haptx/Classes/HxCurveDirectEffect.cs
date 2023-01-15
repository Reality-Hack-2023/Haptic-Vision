// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! An implementation of HxDirectEffect that defines the Haptic Effect using a provided
//! AnimationCurve.
//!
//! @ingroup group_unity_plugin
public class HxCurveDirectEffect : HxDirectEffect {

  //! The displacement curve [m] that defines this effect. Accepts time [s] as an input.
  //!
  //! Sets #DisplacementCurve in Awake().
  [Tooltip("The displacement curve [m] that defines this effect. Accepts time [s] as an input.")]
  public CurveAsset displacementCurveAsset;

  //! Time values get multiplied by this value before they're evaluated in the curve.
  public float InputTimeScale {
    get {
      return _inputTimeScale;
    }
    set {
      _inputTimeScale = value;
      UpdateDuration();
    }
  }

  //! See #InputTimeScale.
  [Tooltip("Time values get multiplied by this value before they're evaluated in the curve.")]
  [SerializeField]
  private float _inputTimeScale = 1.0f;

  //! Curve output values get multiplied by this value before they're applied as
  //! displacements.
  public float OutputDisplacementScale {
    get {
      return _outputDisplacementScale;
    }
    set {
      _outputDisplacementScale = value;
    }
  }

  //! See #OutputDisplacementScale.
  [Tooltip("Curve output values get multiplied by this value before they're applied as displacements.")]
  [SerializeField]
  private float _outputDisplacementScale = 1.0f;

  //! The displacement curve [m] that defines this effect. Accepts time [s] as an input.
  public AnimationCurve DisplacementCurve {
    get {
      return _displacementCurve;
    }
    set {
      _displacementCurve = value;
      UpdateCurveExtrema();
    }
  }

  //! See #DisplacementCurve.
  private AnimationCurve _displacementCurve = null;

  //! The smallest time value present in the curve.
  private float _minTimeS = 0.0f;

  //! The largest time value present in the curve.
  private float _maxTimeS = 0.0f;

  //! Awake is called when the script instance is being loaded.
  new protected void Awake() {
    base.Awake();
    DisplacementCurve = displacementCurveAsset != null ? displacementCurveAsset.curve : null;
  }

  //! Returns #OutputDisplacementScale * #DisplacementCurve(#InputTimeScale * t).
  protected override float GetDisplacementM(HaptxApi.DirectEffect.DirectInfo directInfo) {
    if (_displacementCurve == null) {
      return 0.0f;
    } else {
      float curveOutputN = _displacementCurve.Evaluate(
          (_inputTimeScale > 0.0f ? _minTimeS : _maxTimeS) + _inputTimeScale * directInfo.time_s);

      return _outputDisplacementScale * curveOutputN;
    }
  }

  //! Updates values internally so that scaling time works as expected.
  private void UpdateDuration() {
    if (EffectInternal != null) {
      EffectInternal.setDurationS(
          _inputTimeScale != 0.0f ? (_maxTimeS - _minTimeS) / Mathf.Abs(_inputTimeScale) : 0.0f);
    }
  }

  //! Updates private curve extrema.
  private void UpdateCurveExtrema() {
    if (_displacementCurve == null || _displacementCurve.keys.Length == 0) {
      _minTimeS = 0.0f;
      _maxTimeS = 0.0f;
    } else {
      _minTimeS = _displacementCurve.keys[0].time;
      _maxTimeS = _displacementCurve.keys[0].time;
      foreach (Keyframe frame in _displacementCurve.keys) {
        if (frame.time < _minTimeS) {
          _minTimeS = frame.time;
        } else if (frame.time > _maxTimeS) {
          _maxTimeS = frame.time;
        }
      }
    }
    UpdateDuration();
  }
};
