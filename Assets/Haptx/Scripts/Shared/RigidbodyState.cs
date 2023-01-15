// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;
using System;

//! The state of a Rigidbody.
[Serializable]
public struct RigidbodyState {
   
  //! The position.
  public Vector3 position;

  //! The orientation.
  public Quaternion orientation;

  //! The linear velocity.
  public Vector3 linearVelocity;

  //! The angular velocity.
  public Vector3 angularVelocity;

  //! @brief Interpolate between two rigid body states.
  //!
  //! Vectors are linearly interpolated and Quaternions are spherically interpolated.
  //!
  //! @param a Physics state for @p alpha = 0.
  //! @param b Physics state for @p alpha = 1.
  //! @param alpha Interpolation alpha.
  public static RigidbodyState Interpolate(RigidbodyState a, RigidbodyState b, float alpha) {
    return new RigidbodyState {
      position = Vector3.Lerp(a.position, b.position, alpha),
      orientation = Quaternion.Slerp(a.orientation, b.orientation, alpha).normalized,
      linearVelocity = Vector3.Lerp(a.linearVelocity, b.linearVelocity, alpha),
      angularVelocity = Vector3.Lerp(a.angularVelocity, b.angularVelocity, alpha)};
  }

  //! Gets the states from all rigid body children of a game object.
  //!
  //! @param gameObject The GameObject to get rigid body states from.
  //! @param states [out] Populated with rigid body states.
  //! @returns True if rigid body states were successfully populated.
  public static bool GetRigidbodyStates(GameObject gameObject, ref RigidbodyState[] states) {
    if (gameObject == null) {
      return false;
    }

    Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
    if (states == null || states.Length != rigidbodies.Length) {
      states = new RigidbodyState[rigidbodies.Length];
    }

    bool somethingWentWrong = false;
    for (int i = 0; i < rigidbodies.Length; i++) {
      somethingWentWrong = !GetRigidbodyState(rigidbodies[i], ref states[i]) || somethingWentWrong;
    }
    return !somethingWentWrong;
  }

  //! Get the state from a single rigid body.
  //!
  //! @param rigidbody The rigid body to get state from.
  //! @param state [out] Populated with rigid body state.
  //! @returns True if rigid body state was successfully populated.
  public static bool GetRigidbodyState(Rigidbody rigidbody, ref RigidbodyState state) {
    if (rigidbody == null) {
      return false;
    }

    state.position = rigidbody.position;
    state.orientation = rigidbody.rotation;
    state.linearVelocity = rigidbody.velocity;
    state.angularVelocity = rigidbody.angularVelocity;
    return true;
  }

  //! Set the states of all rigid body children of a game object.
  //!
  //! @param gameObject The GameObject to set rigid body states on.
  //! @param states The rigid body states to apply.
  //! @returns True if rigid body states were successfully applied.
  public static bool SetRigidbodyStates(GameObject gameObject, RigidbodyState[] states) {
    if (gameObject == null || states == null) {
      return false;
    }

    Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
    if (states.Length != rigidbodies.Length) {
      return false;
    }

    bool somethingWentWrong = false;
    for (int i = 0; i < rigidbodies.Length; i++) {
      somethingWentWrong = !SetRigidbodyState(rigidbodies[i], states[i]) || somethingWentWrong;
    }
    return !somethingWentWrong;
  }

  //! Set the state of a single rigid body.
  //!
  //! @param rigidbody The rigid body to set the state of.
  //! @param state The rigid body state to set.
  //! @returns True if rigid body state was successfully applied.
  public static bool SetRigidbodyState(Rigidbody rigidbody, RigidbodyState state) {
    if (rigidbody == null) {
      return false;
    }

    rigidbody.position = state.position;
    rigidbody.rotation = state.orientation;
    rigidbody.velocity = state.linearVelocity;
    rigidbody.angularVelocity = state.angularVelocity;
    return true;
  }
}
