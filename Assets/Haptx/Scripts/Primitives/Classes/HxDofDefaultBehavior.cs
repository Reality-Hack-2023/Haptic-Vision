// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

//! @brief The default physical behavior.
//!
//! See the @ref section_unity_hx_dof_behavior "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxDofDefaultBehavior : HxDofBehavior {

  //! @brief Get the signed magnitude of the force or torque that is associated with @p position
  //! per the current #model.
  //! 
  //! Defined in anchor2's frame.
  //!
  //! @param position The position of interest.
  //! @returns The signed magnitude of the force or torque.
  public override float GetForceTorque(float position) {
    if (model == null) {
      return 0.0f;
    } else {
      return model.GetOutput(position);
    }
  }

  //! Gets the target position of 0 (if it exists).
  //!
  //! @param[out] outTarget Populated with 0.
  //! @returns False if #model is of type HxCurveModel, and true otherwise.
  public override bool TryGetTarget(out float outTarget) {
    outTarget = 0.0f;
    if (model != null && model.GetType() == typeof(HxCurveModel)) {
      return false;
    }
    return true;
  }

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public HxDofDefaultBehavior(string name) : base(name) {
  }

  public override HxDofBehaviorSerialized Serialize() {
    return new HxDofDefaultBehaviorSerialized {
      acceleration = acceleration,
      enabled = enabled,
      name = name,
      visualize = visualize,
    };
  }
}
