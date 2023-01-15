// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using UnityEngine;

//! @brief An enumeration of all types of HxPhysicalModel.
//! 
//! Primarily for GUIs.
public enum HxPhysicalModelType {
  Constant,  //!< Constant model.
  Spring,    //!< Spring model.
  Curve      //!< Curve model. Based on AnimationCurve.
}

//! Extension methods for HxPhysicalModelType.
public static class HxPhysicalModelTypeExtensions {

  //! Inverse of #enumFromType.
  public static Dictionary<HxPhysicalModelType, Type> typeFromEnum =
      new Dictionary<HxPhysicalModelType, Type>() {
        { HxPhysicalModelType.Constant, typeof(HxConstantModel) },
        { HxPhysicalModelType.Spring, typeof(HxSpringModel) },
        { HxPhysicalModelType.Curve, typeof(HxCurveModel) }
      };

  //! @brief Inverse of #typeFromEnum. 
  //! 
  //! Gets populated in static constructor.
  public static Dictionary<Type, HxPhysicalModelType> enumFromType =
      new Dictionary<Type, HxPhysicalModelType>();

  //! Static constructor.
  static HxPhysicalModelTypeExtensions() {
    // Mirror typeFromEnum into enumFromType.
    foreach (KeyValuePair<HxPhysicalModelType, Type> keyValue in typeFromEnum) {
      enumFromType.Add(keyValue.Value, keyValue.Key);
    }
  }

  //! Create an HxPhysicalModel of the class matching the given HxPhysicalModelType.
  //!
  //! @param typeEnum The type of physical behavior to instantiate.
  //! @returns An instance of type @p typeEnum.
  public static HxPhysicalModel CreateByTypeEnum(HxPhysicalModelType typeEnum) {
    Type type = typeFromEnum[typeEnum];
    return (HxPhysicalModel)Activator.CreateInstance(type);
  }
}

//! @brief The base class for all custom physical models used in 
//! @link HxDofBehavior HxDofBehaviors @endlink.
//!
//! See the @ref section_unity_hx_physical_model "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public abstract class HxPhysicalModel : INode<HxPhysicalModelSerialized> {

  //! Get the signed magnitude of the output corresponding to @p input.
  //!
  //! @param input The input to the model.
  //! @returns The signed magnitude of the output.
  public abstract float GetOutput(float input);

  //! See INode.Serialize().
  public abstract HxPhysicalModelSerialized Serialize();
}

//! @brief A model that provides a constant restoring force toward the "0" position.
//!
//! This model is useful for providing a constant source of tension against a limit or another 
//! object, but otherwise tends to be unstable.
//!
//! See the @ref section_unity_hx_physical_model "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxConstantModel : HxPhysicalModel {

  //! @brief The magnitude of the value that gets returned by #GetOutput().
  //!
  //! Units of Unity force (or acceleration) on @link HxLinearDof HxLinearDofs @endlink and 
  //! Unity torque (or angular acceleration) on @link HxAngularDof HxAngularDofs @endlink.
  [Tooltip("The magnitude of the value that gets returned by GetOutput().")]
  public float constant = 10.0f;

  //! Returns a value with a magnitude of #constant and sign opposite of @p input.
  //!
  //! @param input The input to the model.
  //! @returns A value with a magnitude of #constant and sign opposite of @p input.
  public override float GetOutput(float input) {
    return -1.0f * Mathf.Sign(input) * constant;
  }

  public override HxPhysicalModelSerialized Serialize() {
    return new HxConstantModelSerialized {
      constant = constant
    };
  }
}

//! @brief A model that provides a linearly scaled force toward the "0" position.
//!
//! This is a good general-purpose model as it tends to be stable with reasonable damping. It may
//! be thought of as the "default" model.
//!
//! See the @ref section_unity_hx_physical_model "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxSpringModel : HxPhysicalModel {

  //! @brief The stiffness of the spring.
  //!
  //! Units of Unity force (or acceleration) per [m] on @link HxLinearDof HxLinearDofs @endlink
  //! and Unity torque (or angular acceleration) per [deg] on 
  //! @link HxAngularDof HxAngularDofs @endlink.
  [Tooltip("The stiffness of the spring.")]
  public float stiffness = 1.0f;

  //! Returns a value with magnitude that scales linearly with @p input and #stiffness, and
  //! sign opposite of input.
  //!
  //! @param input The input to the model.
  //! @returns A value with magnitude that scales linearly with @p input and #stiffness, and
  //! sign opposite of input.
  public override float GetOutput(float input) {
    return -stiffness * input;
  }

  public override HxPhysicalModelSerialized Serialize() {
    return new HxSpringModelSerialized {
      stiffness = stiffness
    };
  }
}

//! @brief A model that maps outputs (scaled and offset) to inputs (scaled and offset) as defined 
//! by an arbitrary AnimationCurve.
//!
//! This is a very flexible model that can be used to describe almost any continuous function 
//! (in theory). In practice its usefulness is constrained by the editor tool used to create 
//! AnimationCurves.
//!
//! When designing curves that you intend to re-use, it is highly recommended that the functional
//! region be described across -1 and 1 on both axes. This makes scale parameters very intuitive to
//! use.
//!
//! See the @ref section_unity_hx_physical_model "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxCurveModel : HxPhysicalModel {

  //! See #Curve.
  [Tooltip("The curve that defines this behavior.")]
  public CurveAsset curveAsset = null;

  //! @brief The curve that defines this model.
  //!
  //! It is 'f(x)' in: y = a * f(bx + c) + d. The output is in units of Unity force 
  //! (or acceleration) on @link HxLinearDof HxLinearDofs @endlink and Unity torque 
  //! (or angular acceleration) on @link HxAngularDof HxAngularDofs @endlink.
  public AnimationCurve Curve {
    get {
      if (curveAsset != null) {
        return curveAsset.curve;
      } else {
        return null;
      }
    }
  }

  //! @brief The amount by which to scale the input to the curve.
  //!
  //! It is 'b' in: y = a * f(bx + c) + d. The units of 'x' are [m] on 
  //! @link HxLinearDof HxLinearDofs @endlink and [deg] on 
  //! @link HxAngularDof HxAngularDofs @endlink.
  [Tooltip("The amount by which to scale the input to the curve.")]
  public float inputScale = 1.0f;

  //! @brief The amount by which to offset the input to the curve. 
  //!
  //! It is 'c' in: y = a * f(bx + c) + d. Units of [m] on 
  //! @link HxLinearDof HxLinearDofs @endlink and [deg] on 
  //! @link HxAngularDof HxAngularDofs @endlink.
  [Tooltip("The amount by which to offset the input to the curve.")]
  public float inputOffset = 0.0f;

  //! @brief The amount by which to scale the output of the curve.
  //!
  //! It is 'a' in: y = a * f(bx + c) + d.
  [Tooltip("The amount by which to scale the output of the curve.")]
  public float outputScale = 1.0f;

  //! @brief The amount by which to offset the output of the curve.
  //!
  //! It is 'd' in: y = a * f(bx + c) + d. Units of Unity force (or acceleration) on 
  //! @link HxLinearDof HxLinearDofs @endlink and Unity torque (or angular acceleration) on 
  //! @link HxAngularDof HxAngularDofs @endlink. 
  [Tooltip("The amount by which to offset the output of the curve, in units of Unity force (or acceleration) on HxLinearDofs and Unity torque (or angular acceleration) on HxAngularDofs. It is 'd' in: y = a * f(bx + c) + d.")]
  public float outputOffset = 0.0f;

  //! @brief Returns the output value of #Curve (scaled and offset) matching @p input
  //! (scaled and offset).
  //!
  //! It is 'y' in: y = a * f(bx + c) + d. Units of Unity force (or acceleration) on 
  //! @link HxLinearDof HxLinearDofs @endlink and Unity torque (or angular acceleration) on 
  //! @link HxAngularDof HxAngularDofs @endlink.
  //!
  //! @param input The input to the model.
  //! @returns The output value of #Curve (scaled and offset) matching @p input 
  //! (scaled and offset).
  public override float GetOutput(float input) {
    if (Curve != null) {
      return outputOffset + outputScale * Curve.Evaluate(inputOffset + inputScale * input);
    } else {
      return 0.0f;
    }
  }

  public override HxPhysicalModelSerialized Serialize() {
    return new HxCurveModelSerialized {
      curveAsset = curveAsset,
      inputScale = inputScale,
      inputOffset = inputOffset,
      outputScale = outputScale,
      outputOffset = outputOffset
    };
  }
}
