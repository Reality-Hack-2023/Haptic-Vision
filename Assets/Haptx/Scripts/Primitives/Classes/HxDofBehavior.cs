// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using UnityEngine;

//! @brief An enumeration of all types of HxDofBehavior.
//! 
//! Primarily for GUIs.
public enum HxDofBehaviorType {
  Default,         //!< Default behavior.
  TargetPosition,  //!< Target position behavior.
  Detent           //!< Detent behavior.
}

//! Extension methods for HxDofBehaviorType.
public static class HxDofBehaviorTypeExtensions {

  //! Inverse of #enumFromType.
  public static Dictionary<HxDofBehaviorType, Type> typeFromEnum =
      new Dictionary<HxDofBehaviorType, Type>() {
        { HxDofBehaviorType.Default, typeof(HxDofDefaultBehavior) },
        { HxDofBehaviorType.TargetPosition, typeof(HxDofTargetPositionBehavior) },
        { HxDofBehaviorType.Detent, typeof(HxDofDetentBehavior) }
      };

  //! @brief Inverse of #typeFromEnum.
  //!
  //! Gets populated in static constructor.
  public static Dictionary<Type, HxDofBehaviorType> enumFromType =
      new Dictionary<Type, HxDofBehaviorType>();

  //! Static constructor.
  static HxDofBehaviorTypeExtensions() {
    // Mirror typeFromEnum into enumFromType.
    foreach (KeyValuePair<HxDofBehaviorType, Type> keyValue in typeFromEnum) {
      enumFromType.Add(keyValue.Value, keyValue.Key);
    }
  }

  //! Create an HxDofBehavior of the class matching the given HxDofBehaviorType.
  //!
  //! @param typeEnum The type of physical behavior to instantiate.
  //! @param name The name to give this physical behavior.
  //! @returns An instance of type @p typeEnum with name @p @name.
  public static HxDofBehavior CreateByTypeEnum(HxDofBehaviorType typeEnum, string name) {
    Type type = typeFromEnum[typeEnum];
    return (HxDofBehavior)Activator.CreateInstance(type, name);
  }
}

//! @brief The base class for all custom physical behaviors used in 
//! @link HxJoint HxJoints @endlink.
//!
//! See the @ref section_unity_hx_dof_behavior "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public abstract class HxDofBehavior : INode<HxDofBehaviorSerialized> {

  //! Whether forces or torques output by this behavior should be considered 
  //! accelerations.
  [Tooltip("Whether forces or torques output by this behavior should be considered accelerations")]
  public bool acceleration = false;

  //! Whether this behavior is allowed to execute.
  [Tooltip("Whether this behavior is allowed to execute")]
  public bool enabled = true;

  //! @brief The underlying model used to drive this behavior.
  //!
  //! This can be any child class of HxPhysicalModel.
  [Tooltip("The HxPhysicalModel being used to drive this behavior")]
  public HxPhysicalModel model = new HxSpringModel();

  //! @brief The name of this behavior.
  //!
  //! Must be unique for a given HxDof. See HxDof.RegisterBehavior().
  [Tooltip("The name of this behavior.")]
  public readonly string name = string.Empty;

  //! Whether to visualize the forces and torques being applied by this behavior.
  [Tooltip("Whether to visualize the forces and torques being applied by this behavior")]
  public bool visualize = false;

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public HxDofBehavior(string name) {
    this.name = name;
  }

  //! @brief Get the signed magnitude of the force or torque that is associated with @p position.
  //! 
  //! Defined in anchor2's frame.
  //!
  //! @param position The position of interest.
  //! @returns The signed magnitude of the force or torque.
  public abstract float GetForceTorque(float position);

  //! Get this behaviors current target location (if it has one).
  //!
  //! @param[out] outTarget Populated with the target location (if it exists).
  //!
  //! @returns Whether a target location exists.
  public abstract bool TryGetTarget(out float outTarget);

  //! See INode.Serialize().
  public abstract HxDofBehaviorSerialized Serialize();
}
