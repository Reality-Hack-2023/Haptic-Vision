// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using Mirror;
using UnityEngine;

//! @brief Smoothly syncs the physics state of a Rigidbody by making velocity adjustments toward
//! predicted poses.
public abstract class HxNetworkRigidbodyBase : NetworkBehaviour {

  //! The frequency [Hz] at which the server transmits rigidbody state.
  [Tooltip("The frequency [Hz] at which the server transmits rigidbody state.")]
  public float serverTransmissionFrequencyHz = 10.0f;

  //! The maximum allowed positional error [m] before rigidbody state is teleported.
  [Tooltip("The maximum allowed positional error [m] before rigidbody state is teleported.")]
  public float clientMaxPositionErrorM = 0.3f;

  //! The maximum allowed rotational error [rad] before rigidbody state is teleported.
  [Tooltip("The maximum allowed rotational error [rad] before rigidbody state is teleported.")]
  public float clientMaxOrientationErrorRad = 1.5708f;

  //! Multiplied by the position error to compute a correctional delta velocity each frame [1/s^2].
  [Tooltip("Multiplied by the position error to compute a correctional delta velocity each frame [1/s^2].")]
  public float clientPositionErrorStiffness_1_s2 = 10.0f;

  //! Dampens linear velocity relative to target velocity [1/s]. Increase to reduce oscillation.
  [Tooltip("Dampens linear velocity relative to target velocity [1/s]. Increase to reduce oscillation.")]
  public float clientPositionErrorDamping_1_s = 1.0f;

  //! Multiplied by the orientation error to compute a correctional delta velocity each frame [1/s^2].
  [Tooltip("Multiplied by the orientation error to compute a correctional delta velocity each frame [1/s^2].")]
  public float clientOrientationErrorStiffness_1_s2 = 10.0f;

  //! Dampens angular velocity relative to target velocity [1/s]. Increase to reduce oscillation.
  [Tooltip("Dampens angular velocity relative to target velocity [1/s]. Increase to reduce oscillation.")]
  public float clientOrientationErrorDamping_1_s = 1.0f;

  //! The maximum amount of time [s] to extrapolate for.
  [Tooltip("The maximum amount of time [s] to extrapolate for.")]
  public float clientMaxExtrapolationTimeS = 0.3f;

  //! The last rigidbody frame received from the server.
  RigidbodyFrame _clientLastFrameReceived = null;

  //! The last time [s] the server emitted rigidbody state.
  double _serverLastTransmissionTimeS = 0.0;

  //! The rigidbody to sync.
  protected Rigidbody _rigidbody = null;

  //! The time value at which the last started pausing.
  double _clientTimePausedS = 0.0;

  //! Whether syncing is paused.
  public bool PauseSync {
    get {
      return _pauseSync;
    }
    set {
      if (isClient && !_pauseSync && value) {
        _clientLastFrameReceived = null;
        _clientTimePausedS = NetworkTime.time;
      }
      _pauseSync = value;
    }
  }

  //! @copydoc #PauseSync
  bool _pauseSync = false;

  void FixedUpdate() {
    if (_rigidbody == null || _pauseSync) {
      return;
    }

    if (isServer) {
      // Server broadcasting logic.
      if (!_rigidbody.IsSleeping() &&
          NetworkTime.time - _serverLastTransmissionTimeS > 1.0f / serverTransmissionFrequencyHz) {
        RigidbodyState state = new RigidbodyState();
        if (RigidbodyState.GetRigidbodyState(_rigidbody, ref state)) {
          RpcClientTransmitRigidbodyState(NetworkTime.time, state);
        }
        _serverLastTransmissionTimeS = NetworkTime.time;
      }
    // Else if here because sometimes the server is ALSO a client and we only want this code to run
    // on remote clients.
    } else if (isClient && _clientLastFrameReceived != null) {
      float lagS = (float)(NetworkTime.time - _clientLastFrameReceived.timeS);
      // Only do something if the amount of lag being compensating for is within expected bounds.
      if (lagS < clientMaxExtrapolationTimeS) {
        // Estimate current server pose assuming given velocities remained constant.
        Vector3 targetPosM = _clientLastFrameReceived.state.position +
            lagS * _clientLastFrameReceived.state.linearVelocity;
        Quaternion targetOrient = Quaternion.AngleAxis(
            lagS * _clientLastFrameReceived.state.angularVelocity.magnitude * Mathf.Rad2Deg,
            _clientLastFrameReceived.state.angularVelocity.normalized) *
            _clientLastFrameReceived.state.orientation;

        // Compute the error between current pose and estimated server pose.
        Vector3 positionErrorM = targetPosM - _rigidbody.position;
        Quaternion orientError = targetOrient * Quaternion.Inverse(_rigidbody.rotation);
        orientError.ToAngleAxis(out float orientErrorAngleRad, out Vector3 orientErrorAxis);
        orientErrorAngleRad = Mathf.Deg2Rad * orientErrorAngleRad;

        // If the errors in position or rotation exceed expected bounds snap physics state to the
        // target.
        if (positionErrorM.magnitude > clientMaxPositionErrorM ||
            orientErrorAngleRad > clientMaxOrientationErrorRad) {
          _rigidbody.position = targetPosM;
          _rigidbody.rotation = targetOrient;
          _rigidbody.velocity = _clientLastFrameReceived.state.linearVelocity;
          _rigidbody.angularVelocity = _clientLastFrameReceived.state.angularVelocity;
        // If errors are within expected bounds, make adjustments to velocities to reduce error
        // over time. The adjustments are generated with a spring damper model.
        } else {
          // The linear error reduction "spring" computation.
          _rigidbody.velocity +=
              positionErrorM * clientPositionErrorStiffness_1_s2 * Time.fixedDeltaTime;
          // The linear error reduction "damper" computation.
          Vector3 linearCorrectionVelocity = positionErrorM.normalized * Vector3.Dot(
              _rigidbody.velocity - _clientLastFrameReceived.state.linearVelocity,
              positionErrorM.normalized);
          _rigidbody.velocity -=
              Mathf.Clamp(clientPositionErrorDamping_1_s * Time.fixedDeltaTime, 0.0f, 1.0f) *
              linearCorrectionVelocity;

          // The angular error reduction "spring" computation.
          _rigidbody.angularVelocity += orientErrorAngleRad *
              clientOrientationErrorStiffness_1_s2 * Time.fixedDeltaTime * orientErrorAxis;
          // The angular error reduction "damper" computation.
          Vector3 angularCorrectionVelocity = orientErrorAxis * Vector3.Dot(
              _rigidbody.angularVelocity - _clientLastFrameReceived.state.angularVelocity,
              orientErrorAxis);
          _rigidbody.angularVelocity -=
              Mathf.Clamp(clientOrientationErrorDamping_1_s * Time.fixedDeltaTime, 0.0f, 1.0f) *
              angularCorrectionVelocity;
        }
      }
    }
  }

  [ClientRpc]
  void RpcClientTransmitRigidbodyState(double timeS, RigidbodyState state) {
    // Ignore messages predating the last time we paused to avoid weird race conditions.
    if (timeS < _clientTimePausedS) {
      return;
    }

    if (_clientLastFrameReceived == null) {
      _clientLastFrameReceived = new RigidbodyFrame() {
        timeS = timeS,
        state = state};
    } else {
      _clientLastFrameReceived.timeS = timeS;
      _clientLastFrameReceived.state = state;
    }
  }

  //! A time stamped RigidbodyState.
  class RigidbodyFrame {

    //! The time value associated with this rigidbody state.
    public double timeS;

    //! The rigidbody state.
    public RigidbodyState state;
  }
}
