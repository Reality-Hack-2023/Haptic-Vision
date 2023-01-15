// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using UnityEngine;

//! The linear and angular domains of 3-dimensional space.
public enum DofDomain {
  LINEAR = 0,  //!< Linear domain.
  ANGULAR      //!< Angular domain.
}

//! Basis vectors that describe 3-dimensional space.
public enum DofAxis {
  X = 0,  //!< X basis vector.
  Y,      //!< Y basis vector.
  Z       //!< Z basis vector.
}

//! Extension methods for DofAxis.
public static class DofAxisExtensions {

  //! Get the local space direction of a DofAxis.
  //!
  //! @param axis The axis of interest.
  //! @returns The local space direction of the axis.
  public static Vector3 GetDirection(this DofAxis axis) {
    switch (axis) {
      case DofAxis.X:
        return Vector3.right;
      case DofAxis.Y:
        return Vector3.up;
      case DofAxis.Z:
        return Vector3.forward;
      default:
        return Vector3.one;
    }
  }

  //! @brief Get the orthonormal basis vectors of a DofAxis.
  //!
  //! For example, if provided DofAxis.X @p first and @p second will be populated with the 
  //! directions of DofAxis.Y and DofAxis.Z respectively.
  //!
  //! @param axis The axis of interest.
  //! @param[out] first The first orthonormal basis vector.
  //! @param[out] second The second orthonormal basis vector.
  public static void GetOrthonormalDirections(this DofAxis axis,
      out Vector3 first, out Vector3 second) {
    switch (axis) {
      case DofAxis.X:
        first = GetDirection(DofAxis.Y);
        second = GetDirection(DofAxis.Z);
        break;
      case DofAxis.Y:
        first = GetDirection(DofAxis.Z);
        second = GetDirection(DofAxis.X);
        break;
      case DofAxis.Z:
        first = GetDirection(DofAxis.X);
        second = GetDirection(DofAxis.Y);
        break;
      default:
        first = Vector3.one;
        second = Vector3.one;
        break;
    }
  }

  //! Get the local space direction of a rotation's DofAxis.
  //!
  //! @param axis The axis of interest.
  //! @param rotation The rotation of interest.
  //! @returns The local space direction of the rotation's axis.
  public static Vector3 GetDirection(this DofAxis axis, Quaternion rotation) {
    return rotation * axis.GetDirection();
  }

  //! @brief Get the orthonormal basis vectors of a rotation's DofAxis.
  //!
  //! For example, if provided DofAxis.X @p first and @p second will be populated with the 
  //! rotation's directions of DofAxis.Y and DofAxis.Z respectively.
  //!
  //! @param axis The axis of interest.
  //! @param rotation The rotation of interest.
  //! @param[out] first The first orthonormal basis vector.
  //! @param[out] second The second orthonormal basis vector.
  public static void GetOrthonormalDirections(this DofAxis axis,
      Quaternion rotation, out Vector3 first, out Vector3 second) {
    axis.GetOrthonormalDirections(out first, out second);
    first = rotation * first;
    second = rotation * second;
  }
}

//! The 6 degrees of freedom of 3-dimensional space.
public enum DegreeOfFreedom {
  X_LIN = 0,   //!< X linear degree of freedom.
  Y_LIN,       //!< Y linear degree of freedom.
  Z_LIN,       //!< Z linear degree of freedom.
  X_ANG,       //!< X angular degree of freedom.
  Y_ANG,       //!< Y angular degree of freedom.
  Z_ANG        //!< Z angular degree of freedom.
}

//! Extension methods for DegreeOfFreedom.
public static class DegreeOfFreedomExtensions {

  //! Gets a human-readable string matching a given DegreeOfFreedom.
  //!
  //! @param degreeOfFreedom The degree of freedom of interest.
  //! @returns A human-readable string matching @p degreeOfFreedom.
  public static string ToFriendlyString(this DegreeOfFreedom degreeOfFreedom) {
    switch (degreeOfFreedom) {
      case DegreeOfFreedom.X_LIN:
        return "X Linear";
      case DegreeOfFreedom.Y_LIN:
        return "Y Linear";
      case DegreeOfFreedom.Z_LIN:
        return "Z Linear";
      case DegreeOfFreedom.X_ANG:
        return "X Angular";
      case DegreeOfFreedom.Y_ANG:
        return "Y Angular";
      case DegreeOfFreedom.Z_ANG:
        return "Z Angular";
      default:
        return "Humans may not access this realm";
    }
  }

  //! Make a DegreeOfFreedom from a DofDomain and a DofAxis.
  //!
  //! @param domain The domain of the degree of freedom.
  //! @param axis The axis of the degree of freedom.
  //! @returns The matching degree of freedom.
  public static DegreeOfFreedom FromDomainAndAxis(DofDomain domain, DofAxis axis) {
    switch (axis) {
      case DofAxis.X:
        if (domain == DofDomain.LINEAR) {
          return DegreeOfFreedom.X_LIN;
        } else {
          return DegreeOfFreedom.X_ANG;
        }
      case DofAxis.Y:
        if (domain == DofDomain.LINEAR) {
          return DegreeOfFreedom.Y_LIN;
        } else {
          return DegreeOfFreedom.Y_ANG;
        }
      case DofAxis.Z:
        if (domain == DofDomain.LINEAR) {
          return DegreeOfFreedom.Z_LIN;
        } else {
          return DegreeOfFreedom.Z_ANG;
        }
    }
    return DegreeOfFreedom.Z_LIN;
  }

  //! Get the DofAxis of a DegreeOfFreedom.
  //!
  //! @param degreeOfFreedom The degree of freedom.
  //! @returns The axis of @p degreeOfFreedom.
  public static DofAxis Axis(this DegreeOfFreedom degreeOfFreedom) {
    return (DofAxis)((uint)degreeOfFreedom % 3);
  }

  //! Get the DofDomain of a DegreeOfFreedom.
  //!
  //! @param degreeOfFreedom The degree of freedom.
  //! @returns The domain of @p degreeOfFreedom.
  public static DofDomain Domain(this DegreeOfFreedom degreeOfFreedom) {
    return (DofDomain)((uint)degreeOfFreedom / 3);
  }
}

//! @brief A struct that mirrors Matrix4x4, except about a single cardinal direction.
//!
//! Rotation will be populated with the Matrix4x4's rotation about the chosen direction, scale 
//! will be populated with the Matrix4x4's scale in the chosen direction, and translation will be
//! populated with the Matrix4x4's translation in the chosen direction.
public static class Transform1D {

  //! The translation [m] of a transform along a given DofAxis.
  //!
  //! @param localTransform The transform of interest.
  //! @param axis The axis of interest.
  //! @returns The translation [m] of a transform along a given DofAxis.
  public static float GetTranslation(this Matrix4x4 localTransform, DofAxis axis) {
    Vector3 direction = axis.GetDirection();
    return Vector3.Dot(localTransform.MultiplyPoint3x4(Vector3.zero), direction);
  }

  //! The rotation [deg] of a transform about a given DofAxis.
  //!
  //! @param localTransform The transform of interest.
  //! @param axis The axis of interest.
  //! @returns The rotation [deg] of a transform about a given DofAxis.
  public static float GetRotation(this Matrix4x4 localTransform, DofAxis axis) {
    // Prevents failed assertion.
    if (!localTransform.ValidTRS()) {
      return 0.0f;
    }

    // Determine reference vectors.
    Vector3 direction = axis.GetDirection();
    Vector3 orthoFirst;
    Vector3 orthoSecond;
    axis.GetOrthonormalDirections(out orthoFirst, out orthoSecond);

    // Calculate the first, planar orthographic direction of localTransform.
    Quaternion localRotation = localTransform.rotation;
    Vector3 rotOrthoFirst;
    Vector3 rotOrthoSecond;
    axis.GetOrthonormalDirections(localRotation, out rotOrthoFirst, out rotOrthoSecond);
    Vector3 transformOrthoFirstPlanar =
        rotOrthoFirst - Vector3.Dot(rotOrthoFirst, direction) * direction;
    transformOrthoFirstPlanar.Normalize();

    // The rotation's magnitude is equal to the angle between the component of the transform's
    // first orthogonal vector that is in the plane formed by the axis' first and second orthogonal 
    // vectors, and the axis' first orthogonal vector. It's sign is determined by the sign of the
    // dot product between the transform's planar vector the axis' second orthogonal vector.
    return
        HxShared.RadToDeg * Mathf.Sign(Vector3.Dot(transformOrthoFirstPlanar, orthoSecond)) *
        Mathf.Acos(Vector3.Dot(transformOrthoFirstPlanar, orthoFirst));
  }

  //! The scale of a transform in a given DofAxis.
  //!
  //! @param localTransform The transform of interest.
  //! @param axis The axis of interest.
  //! @returns The scale of a transform in a given DofAxis.
  public static float GetScale(this Matrix4x4 localTransform, DofAxis axis) {
    Vector3 direction = axis.GetDirection();
    return Vector3.Dot(localTransform.lossyScale, direction);
  }
}

//! A rotation in degrees around a single axis relative to an original rotation on 
//! [-inf, inf].
public class TotalRotation {

  //! Constructs a total rotation from a given revolution and partialAngle.
  //!
  //! @param revolution Which revolution this total rotation is on.
  //! @param partialAngle Progress through the current rotation.
  public TotalRotation(int revolution, float partialAngle) {
    this.revolution = revolution;
    this.partialAngle = partialAngle;
  }

  //! Constructs a TotalRotation object from a given value [deg].
  //!
  //! @param totalAngle The given total rotation [deg].
  public TotalRotation(float totalAngle) {
    float offset = totalAngle < 0.0f ? -HxShared.RevToDeg / 2.0f : HxShared.RevToDeg / 2.0f;
    revolution = (int)Math.Truncate((totalAngle + offset) / HxShared.RevToDeg);
    partialAngle = totalAngle - revolution * HxShared.RevToDeg;
  }

  //! Get the value of the total rotation on [-inf, inf].
  //!
  //! @returns The value of the total rotation on [-inf, inf].
  public float GetTotalRotation() {
    return partialAngle + revolution * HxShared.RevToDeg;
  }

  //! Get a human-readable string.
  //!
  //! @returns A human-readable string.
  public new string ToString() {
    return string.Format("Total rotation = {0}, revolution = {1}, partial angle = {2}.",
        GetTotalRotation(), revolution, partialAngle);
  }

  //! @brief Which revolution the total rotation is on.
  //!
  //! The 0th revolution is resting rotation +/- 180 degrees, the 1st revolution is
  //! CW 180 -> CW 540, the -1st revolution is CCW 180 -> CCW 540.
  public int revolution;

  //! @brief The partial rotation expressed as an angle in degrees on [-180,180].
  //!
  //! This + (revolution_ * 360) is the total rotation.
  public float partialAngle;
}

//! @brief A single degree of freedom that an HxJoint can operate upon.
//!
//! See the @ref section_unity_hx_dof "Unity Haptic Primitive Guide" for a high level overview.
//!
//! @ingroup group_unity_haptic_primitives
public abstract class HxDof : INode<HxDofSerialized> {

  //! The collection of state functions on this HxDof.
  public ICollection<HxStateFunction> StateFunctions {
    get {
      return _stateFunctions.Values;
    }
  }

  //! The collection of state functions on this HxDof.
  private Dictionary<string, HxStateFunction> _stateFunctions =
      new Dictionary<string, HxStateFunction>();

  //! The collection of physical behaviors on this HxDof.
  public ICollection<HxDofBehavior> Behaviors {
    get {
      return _behaviors.Values;
    }
  }

  //! The collection of physical behaviors on this HxDof.
  private Dictionary<string, HxDofBehavior> _behaviors =
      new Dictionary<string, HxDofBehavior>();

  //! @brief Get the current position or rotation around this degree of freedom.
  //!
  //! Rotations are returned in degrees. For rotations the absolute value might be > 180 if 
  //! HxAngularDof.trackMultipleRevolutions is true. For rotations, we measure this using a 
  //! vector projection onto the associated plane. This does not use any Euler angles. This means 
  //! this function may not do what you're expecting it to do if the constrained object can rotate
  //! around more than one axis.
  public float CurrentPosition {
    get {
      return _currentPosition;
    }
  }

  //! The position on this HxDof that we were at on the last #Update() ([deg] for 
  //! HxAngularDof, [m] for HxLinearDof).
  protected float _currentPosition;

  //! Update position even with no state functions or behaviors.
  [Tooltip("Update position even with no state functions or behaviors.")]
  public bool forceUpdate = false;

  //! @brief Update this HxDof with a new position. 
  //!
  //! Generally this should only get called by HxJoint or one of its child classes.
  //!
  //! @param position The new position.
  //! @param teleport If false @p position should be on [-180, 180].
  public virtual void Update(float position, bool teleport = false) {
    _currentPosition = position;
  }

  //! Register a new, unique state function.
  //!
  //! @param stateFunction The new state function. Needs to have a name not already present in 
  //! #StateFunctions.
  public void RegisterStateFunction(HxStateFunction stateFunction) {
    if (!_stateFunctions.ContainsKey(stateFunction.name)) {
      if (stateFunction != null) {
        _stateFunctions.Add(stateFunction.name, stateFunction);
      } else {
        Debug.LogError("Attempted to register null HxStateFunction.");
      }
    } else {
      Debug.LogError(string.Format("HxDof already contains an HxStateFunction named \"{0}\".",
          stateFunction.name));
    }
  }

  //! Register a new, unique physical behavior.
  //!
  //! @param behavior The new physical behavior. Needs to have a name not already present in 
  //! #Behaviors.
  public void RegisterBehavior(HxDofBehavior behavior) {
    if (!_behaviors.ContainsKey(behavior.name)) {
      if (behavior != null) {
        _behaviors.Add(behavior.name, behavior);
      } else {
        Debug.LogError("Attempted to register null HxDofBehvaior.");
      }
    } else {
      Debug.LogError(string.Format("HxDof already contains an HxDofBehavior named \"{0}\".",
          behavior.name));
    }
  }

  //! Unregister a state function.
  //!
  //! @param stateFunction The state function to unregister.
  public void UnregisterStateFunction(HxStateFunction stateFunction) {
    if (_stateFunctions.ContainsKey(stateFunction.name)) {
      _stateFunctions.Remove(stateFunction.name);
    } else {
      Debug.LogWarning(string.Format("HxDof does not contain an HxStateFunction named \"{0}\".",
          stateFunction.name));
    }
  }

  //! Unregister a physical behavior.
  //!
  //! @param behavior The behavior to unregister.
  public void UnregisterBehavior(HxDofBehavior behavior) {
    if (_behaviors.ContainsKey(behavior.name)) {
      _behaviors.Remove(behavior.name);
    } else {
      Debug.LogWarning(string.Format("HxDof does not contain an HxDofBehavior named \"{0}\".",
          behavior.name));
    }
  }

  //! Find a registered HxStateFunction by name.
  //!
  //! @param name The name of the state function to look for.
  //! @param[out] stateFunction Populated with the state function, or null if not found.
  //! @returns Whether the state function was found.
  public bool TryGetStateFunctionByName(string name, out HxStateFunction stateFunction) {
    return _stateFunctions.TryGetValue(name, out stateFunction);
  }

  //! Find a registered HxDofBehavior by name.
  //!
  //! @param name The name of the state function to look for.
  //! @param[out] behavior Populated with the physical behavior, or null if not found.
  //! @returns Whether the physical behavior was found.
  public bool TryGetBehaviorByName(string name, out HxDofBehavior behavior) {
    return _behaviors.TryGetValue(name, out behavior);
  }

  //! Whether this HxDof should update.
  //!
  //! @returns True if either #StateFunctions or #Behaviors is not empty or if #forceUpdate is 
  //! true.
  public bool ShouldUpdate() {
    return _stateFunctions.Count > 0 || _behaviors.Count > 0 || forceUpdate;
  }

  //! See INode.Serialize().
  public abstract HxDofSerialized Serialize();
}

//! @brief A single linear degree of freedom that an HxJoint can operate upon.
//!
//! See the @ref section_unity_hx_dof "Unity Haptic Primitive Guide" for a high level overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxLinearDof : HxDof {

  public override HxDofSerialized Serialize() {
    return new HxLinearDofSerialized() {
      forceUpdate = forceUpdate
    };
  }
}

//! @brief A single angular degree of freedom that an HxJoint can operate upon.
//!
//! See the @ref section_unity_hx_dof "Unity Haptic Primitive Guide" for a high level overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxAngularDof : HxDof {

  //! @brief Track the rotation as more than just an angle between -180 and 180 degrees.
  //! 
  //! This changes how attached behaviors and state machines work.
  [Tooltip("Track the rotation as more than just an angle between -180 and 180 degrees. This changes how attached behaviors and state machines work.")]
  public bool trackMultipleRevolutions = true;

  public override void Update(float position, bool teleport = false) {
    if (trackMultipleRevolutions && !teleport) {
      TotalRotation totalRotationPrevious = new TotalRotation(_currentPosition);
      // Current rotation on axis in degrees.
      int revolution = totalRotationPrevious.revolution;

      // Check to see if the rotation has crossed onto another revolution.
      if (Mathf.Abs(position) > 90.0f && Mathf.Abs(totalRotationPrevious.partialAngle) > 90.0f &&
          (position * totalRotationPrevious.partialAngle) < 0) {
        revolution += position < 0 ? 1 : -1;
      }

      // Save rotation so it can be checked against next frame.
      _currentPosition = new TotalRotation(revolution, position).GetTotalRotation();
    } else {
      base.Update(position);
    }
  }

  public override HxDofSerialized Serialize() {
    return new HxAngularDofSerialized() {
      forceUpdate = forceUpdate,
      trackMultipleRevolutions = trackMultipleRevolutions
    };
  }
}
