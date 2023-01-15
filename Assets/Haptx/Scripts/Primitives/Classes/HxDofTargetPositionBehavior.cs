// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief A physical behavior that always drives toward a target position.
//!
//! See the @ref section_unity_hx_dof_behavior "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxDofTargetPositionBehavior : HxDofBehavior {

  //! The position along the degree of freedom where the behavior is driving to.
  [Tooltip("The position along the degree of freedom where the behavior is driving to.")]
  public float targetPosition = 0.0f;

  //! @brief Get the signed magnitude of the force or torque that is associated with @p position
  //! relative to #targetPosition.
  //! 
  //! Defined in anchor2's frame.
  //!
  //! @param position The position of interest.
  //! @returns The signed magnitude of the force or torque.
  public override float GetForceTorque(float position) {
    if (model == null) {
      return 0.0f;
    } else {
      return model.GetOutput(position - targetPosition);
    }
  }

  //! Gets the current value of #targetPosition.
  //!
  //! @param[out] outTarget Populated with the current value of #targetPosition.
  //!
  //! @returns True.
  public override bool TryGetTarget(out float outTarget) {
    outTarget = targetPosition;
    return true;
  }

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public HxDofTargetPositionBehavior(string name) : base(name) {
  }

  public override HxDofBehaviorSerialized Serialize() {
    return new HxDofTargetPositionBehaviorSerialized {
      acceleration = acceleration,
      enabled = enabled,
      name = name,
      visualize = visualize,
      targetPosition = targetPosition
    };
  }
}
