// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! An implementation of HxSpatialEffect that defines the Haptic Effect using a provided
//! AnimationCurve.
//!
//! @ingroup group_unity_plugin
public class HxCurveSpatialEffect : HxSpatialEffect {
  //! @brief The force curve [N] that defines this effect. Accepts time [s] as an input.
  //!
  //! Sets #ForceCurve in Awake().
  [Tooltip("The force curve [N] that defines this effect. Accepts time [s] as an input.")]
  public CurveAsset forceCurveAsset;

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

  //! Curve output values get multiplied by this value before they're applied as forces.
  public float OutputForceScale {
    get {
      return _outputForceScale;
    }
    set {
      _outputForceScale = value;
    }
  }

  //! See #OutputForceScale.
  [Tooltip("Curve output values get multiplied by this value before they're applied as forces.")]
  [SerializeField]
  private float _outputForceScale = 1.0f;

  //! The force curve [N] that defines this effect. Accepts time [s] as an input.
  public AnimationCurve ForceCurve {
    get {
      return _forceCurve;
    }
    set {
      _forceCurve = value;
      UpdateCurveExtrema();
    }
  }

  //! See #ForceCurve.
  private AnimationCurve _forceCurve = null;

  //! The smallest time value present in the curve.
  private float _minTimeS = 0.0f;

  //! The largest time value present in the curve.
  private float _maxTimeS = 0.0f;

  //! Awake is called when the script instance is being loaded.
  new protected void Awake() {
    base.Awake();
    ForceCurve = forceCurveAsset != null ? forceCurveAsset.curve : null;
  }

  //! Returns #OutputForceScale * #ForceCurve(#InputTimeScale * t).
  protected override float GetForceN(HaptxApi.SpatialEffect.SpatialInfo spatialInfo) {
    if (_forceCurve == null) {
      return 0.0f;
    } else {
      float curveOutputN = _forceCurve.Evaluate(
          (_inputTimeScale > 0.0f ? _minTimeS : _maxTimeS) + _inputTimeScale * spatialInfo.time_s);

      return _outputForceScale * curveOutputN;
    }
  }

  //! Updates values internally so that scaling time works as expected.
  private void UpdateDuration() {
    if (EffectInternal != null) {
      EffectInternal.setDurationS(
          _inputTimeScale != 0.0f ? (_maxTimeS - _minTimeS) / Mathf.Abs(_inputTimeScale) : 0.0f);
    }
  }

  //!  Updates private curve extrema.
  private void UpdateCurveExtrema() {
    if (_forceCurve == null || _forceCurve.keys.Length == 0) {
      _minTimeS = 0.0f;
      _maxTimeS = 0.0f;
    } else {
      _minTimeS = _forceCurve.keys[0].time;
      _maxTimeS = _forceCurve.keys[0].time;
      foreach (Keyframe frame in _forceCurve.keys) {
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
