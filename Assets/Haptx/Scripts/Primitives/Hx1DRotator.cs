// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief Uses features that can be added to @link HxJoint HxJoints @endlink that only need to 
//! operate in one rotational degree of freedom.
//!
//! See the @ref section_unity_hx_1d_components "Unity Haptic Primitive Guide" for a high level 
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class Hx1DRotator : Hx1DJoint {

  //! Which domain this joint operates in.
  public override DofDomain OperatingDomain {
    get {
      return DofDomain.ANGULAR;
    }
  }

  //! Whether we're currently in the low safety sector and using the safety sector 
  //! algorithm.
  bool _inLowSafetySector = false;

  //! Whether we're currently in the high safety sector and using the safety sector 
  //! algorithm.
  bool _inHighSafetySector = false;

  //! Whether the safety sector is currently active.
  bool _safetySectorActive = false;

  //! Reset to default values.
  public void Reset() {
    _lowerLimit = -90.0f;
    _upperLimit = 90.0f;
    HxAngularDof angularDof = (HxAngularDof)GetDof(OperatingDegreeOfFreedom);
    if (angularDof != null) {
      angularDof.trackMultipleRevolutions = true;
    }
    damping = 0.001f;
  }

  //! Called every fixed framerate frame if enabled.
  protected override void FixedUpdate() {
    base.FixedUpdate();

    // If we have dynamic limit checking and handling to do.
    if (limitMotion && ShouldUseSafetySector() && CheckLimits()) {
      UpdateJoint();
    }
  }

  protected override void ConfigureJoint() {
    base.ConfigureJoint();

    // Assume parameters have default values unless otherwise informed.
    _safetySectorActive = false;
    _angularLimitsOffset = Vector3.zero;

    // If we'll be using range of motion limits.
    if (limitMotion) {
      float halfRomRange = (_upperLimit - _lowerLimit) / 2.0f;
      if ((OperatingAxis == DofAxis.Y || OperatingAxis == DofAxis.Z) &&
          halfRomRange > 0.0f && halfRomRange < 3.0f) {
        HxDebug.LogWarning("The underlying ConfigurableJoint doesn't support non-zero angular limits with magnitudes less than 3 degrees on the Y or Z axes.",
            this);
      }

      // If we don't need the safety sector algorithm we can use limited motion.
      if (!ShouldUseSafetySector()) {
        float angleOffset = (_upperLimit + _lowerLimit) / 2.0f;
        _angularLimitsOffset = new Vector3(
            0.0f,
            OperatingAxis == DofAxis.Y ? angleOffset : 0.0f,
            OperatingAxis == DofAxis.Z ? angleOffset : 0.0f);

        _hxJointParameters.lowAngularXLimit.limit = OperatingAxis == DofAxis.X ?
            _lowerLimit : 0.0f;
        _hxJointParameters.highAngularXLimit.limit = OperatingAxis == DofAxis.X ?
            _upperLimit : 0.0f;
        _hxJointParameters.angularYLimit.limit = OperatingAxis == DofAxis.Y ?
            halfRomRange : 0.0f;
        _hxJointParameters.angularZLimit.limit = OperatingAxis == DofAxis.Z ?
            halfRomRange : 0.0f;

        _hxJointParameters.angularXMotion = OperatingAxis == DofAxis.X ?
            ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
        _hxJointParameters.angularYMotion = OperatingAxis == DofAxis.Y ?
            ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
        _hxJointParameters.angularZMotion = OperatingAxis == DofAxis.Z ?
            ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
      } else {
        if (!_inLowSafetySector && !_inHighSafetySector) {
          _safetySectorActive = false;
        } else {
          // Use limited motion when in a safety sector.
          TotalRotation totalRotOffset;
          if (_inLowSafetySector) {
            totalRotOffset = new TotalRotation(_lowerLimit + _SafetySectorWidth);
          } else {  // if (inHighSafetySector) {
            totalRotOffset = new TotalRotation(_upperLimit - _SafetySectorWidth);
          }

          _angularLimitsOffset = new Vector3(
              OperatingAxis == DofAxis.X ? totalRotOffset.partialAngle : 0.0f,
              OperatingAxis == DofAxis.Y ? totalRotOffset.partialAngle : 0.0f,
              OperatingAxis == DofAxis.Z ? totalRotOffset.partialAngle : 0.0f);

          _hxJointParameters.lowAngularXLimit.limit = OperatingAxis == DofAxis.X ?
              -_SafetySectorWidth : 0.0f;
          _hxJointParameters.highAngularXLimit.limit = OperatingAxis == DofAxis.X ?
              _SafetySectorWidth : 0.0f;
          _hxJointParameters.angularYLimit.limit = OperatingAxis == DofAxis.Y ?
              _SafetySectorWidth : 0.0f;
          _hxJointParameters.angularZLimit.limit = OperatingAxis == DofAxis.Z ?
              _SafetySectorWidth : 0.0f;

          _hxJointParameters.angularXMotion = OperatingAxis == DofAxis.X ?
              ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
          _hxJointParameters.angularYMotion = OperatingAxis == DofAxis.Y ?
              ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
          _hxJointParameters.angularZMotion = OperatingAxis == DofAxis.Z ?
              ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;

          _safetySectorActive = true;
        }
      }
    }
  }

  //! Whether limits need to be updated.
  //!
  //! @returns True if limits need to be updated.
  private bool CheckLimits() {
    HxDof dof = GetOperatingDof();
    if (dof != null) {
      float rotation = dof.CurrentPosition;
      _inLowSafetySector = rotation.IsBetween(
          _lowerLimit - _MinDeadZoneWidth,
          _lowerLimit + _SafetySectorWidth);
      _inHighSafetySector = rotation.IsBetween(
          _upperLimit - _SafetySectorWidth,
          _upperLimit + _MinDeadZoneWidth);

      if (!_inLowSafetySector && !_inHighSafetySector) {
        // Verify that the joint hasn't been teleported past its limits, and that its limits
        // haven't been moved past it.
        if (rotation < _lowerLimit) {
          TeleportAnchor1AlongDof(_lowerLimit, OperatingDegreeOfFreedom);
          rotation = _lowerLimit;
          _inLowSafetySector = true;
        } else if (rotation > _upperLimit) {
          TeleportAnchor1AlongDof(_upperLimit, OperatingDegreeOfFreedom);
          rotation = _upperLimit;
          _inHighSafetySector = true;
        }
      }

      bool inSafetySector = _inLowSafetySector || _inHighSafetySector;
      if (_safetySectorActive) {
        // Check for movement out from the safety sector.
        if (!inSafetySector) {
          return true;
        }
      } else {
        // Check for movement into any safety sector.
        if (inSafetySector) {
          return true;
        }
      }
    }

    return false;
  }

  //! Whether the current configuration demands the safety sector algorithm. 
  //!
  //! @returns True if the joint has more than 360 - MinDeadZoneWidth degrees of allowed 
  //! motion.
  private bool ShouldUseSafetySector() {
    return _upperLimit - _lowerLimit >= HxShared.RevToDeg - _MinDeadZoneWidth;
  }

  //! @brief The smallest width of the "dead zone" in the dial's range of motion where we don't 
  //! need the safety net approach. 
  //!
  //! If the dead zone is smaller than this or is nonexistent (> 360 range of motion) then we 
  //! should use the safety net approach when we get close to our range of motion limits.
  private static float _MinDeadZoneWidth = 20.0f;

  //! @brief The angle (degrees) of the safety sector of the circular range of motion of this 
  //! joint.
  //!
  //! When we are within SafetySectorWidth degrees of a range of motion limit, we reform the
  //! underlying joint with new symmetrical limits of size SafetySectorWidth around that threshold
  //! position. When that is not the case, the joint has no limits. We track revolutions and
  //! angles manually to detect this change in state.
  private static float _SafetySectorWidth = 30.0f;
}
