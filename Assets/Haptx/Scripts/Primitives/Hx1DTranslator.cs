// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief Uses features that can be added to @link HxJoint HxJoints @endlink that only need to 
//! operate in one translational degree of freedom.
//!
//! See the @ref section_unity_hx_1d_components "Unity Haptic Primitive Guide" for a high level 
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class Hx1DTranslator : Hx1DJoint {

  //! Which domain this joint operates in.
  public override DofDomain OperatingDomain {
    get {
      return DofDomain.LINEAR;
    }
  }

  //! Reset to default values.
  public void Reset() {
    _lowerLimit = 0.0f;
    _upperLimit = 1.0f;
    damping = 0.1f;
  }

  protected override void ConfigureJoint() {
    base.ConfigureJoint();

    if (limitMotion) {
      float midPoint = (_lowerLimit + _upperLimit) / 2.0f;
      _linearLimitsOffset = midPoint * OperatingAxis.GetDirection();

      float halfRomRange = (_upperLimit - _lowerLimit) / 2.0f;
      if (halfRomRange > 0.0f && halfRomRange < 0.001f) {
        HxDebug.LogWarning("The underlying ConfigurableJoint doesn't support non-zero linear limits with magnitudes less than 0.001m.",
            this);
      }

      _hxJointParameters.linearLimit.limit = halfRomRange;
      _hxJointParameters.xMotion = OperatingAxis == DofAxis.X ? ConfigurableJointMotion.Limited :
          ConfigurableJointMotion.Locked;
      _hxJointParameters.yMotion = OperatingAxis == DofAxis.Y ? ConfigurableJointMotion.Limited :
          ConfigurableJointMotion.Locked;
      _hxJointParameters.zMotion = OperatingAxis == DofAxis.Z ? ConfigurableJointMotion.Limited :
          ConfigurableJointMotion.Locked;
    } else {
      _linearLimitsOffset = Vector3.zero;
    }
  }
}
