// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using UnityEngine;

//! @brief A class that flags rigidbodies as "sleeping" under the right conditions.
//!
//! Unlike the PhysX sleeping system, this system is not automatically reset on calls to 
//! Rigidbody.AddForce() and Rigidbody.AddTorque() which HxJoints use to exhibit physical 
//! behaviors.
[DisallowMultipleComponent()]
public class HxSleepMonitor : MonoBehaviour {

  //! The rigidbody this instance is watching.
  private Rigidbody _rigidbody = null;

  //! The number of physics frames this rigidbody has been eligible for sleep.
  private uint _sleepCounter = 0u;

  //! The number of frames a rigidbody must be eligible before it sleeps.
  private uint _sleepCounterMax = 10u;

  //! The number of components watching this sleep monitor.
  private int _numComponentsMonitoring = 0;

  //! The length to draw the awake arrow.
  private float _awakeArrowLengthM = 1.0f;

  //! Execute to stop drawing the "awake" arrow from last frame. Can be null.
  Action _stopDrawingAwakeArrow = null;

  //! Whether the rigidbody is sleeping (from the HaptX perspective).
  //!
  //! @returns Whether the rigidbody is sleeping (from the HaptX perspective).
  public bool IsSleeping() {
    return _sleepCounter >= _sleepCounterMax;
  }

  //! Called by a component that has begun monitoring this rigidbody.
  public void NotifyMonitoringBegin() {
    _numComponentsMonitoring++;
  }

  //! Called by a component that has stopped monitoring this rigidbody.
  public void NotifyMonitoringEnd() {
    _numComponentsMonitoring--;
    if (_numComponentsMonitoring < 1) {
      Destroy(this);
    }
  }

  //! Called when the script is being loaded.
  private void Awake() {
    _rigidbody = GetComponent<Rigidbody>();
    if (_rigidbody == null) {
      HxDebug.LogError("HxSleepMonitor may only be used on GameObjects that contain a Rigidbody.");
      enabled = false;
      return;
    }

    _awakeArrowLengthM = HxShared.GetGameObjectBounds(_rigidbody.gameObject).size.magnitude;
  }

  //! Called every fixed framerate frame if enabled.
  private void FixedUpdate() {
    // Manage the sleep counter.
    if (HxShared.GetMassNormalizedKineticEnergy(_rigidbody) < _rigidbody.sleepThreshold) {
      _sleepCounter = Math.Min(_sleepCounter + 1, _sleepCounterMax);
    } else {
      WakeUp();
    }

    // Optionally visualize whether this rigidbody is awake.
    if (_stopDrawingAwakeArrow != null) {
      _stopDrawingAwakeArrow();
    }
    if (HxDebug.Instance._indicateRigidbodiesAwake && !_rigidbody.IsSleeping()) {
      Color arrowColor = IsSleeping() ? HxShared.DebugBlueOrYellow : HxShared.DebugPurpleOrTeal;

      // How much the arrow bobs up and down as a fraction of its length.
      float bobFactor = 0.1f;
      // The frequency that the arrow bobs up and down [Hz].
      float bobFrequency = 1.0f;
      // The frequency that the arrow rotates [Hz].
      float rotationFrequency = 1.0f;
      // The thickness of the arrow as a fraction of its length.
      float thicknessFactor = 0.033f;

      Vector3 pos = _rigidbody.worldCenterOfMass + bobFactor * _awakeArrowLengthM *
          (Mathf.Sin(bobFrequency * 2.0f * Mathf.PI * Time.time) + 1.0f) * Vector3.up;
      _stopDrawingAwakeArrow = HxDebugMesh.DrawArrow(
          pos + _awakeArrowLengthM * Vector3.up, pos,
          Quaternion.AngleAxis(rotationFrequency * 360.0f * Time.time, Vector3.up) *
              Vector3.forward,
          thicknessFactor * _awakeArrowLengthM, arrowColor, true);
    }
  }

  //! Called the first frame that a collider/rigidbody that is touching another 
  //! rigidbody/collider.
  private void OnCollisionEnter() {
    WakeUp();
  }

  //! Called once per frame for every collider/rigidbody that is touching another
  //! rigidbody/collider.
  private void OnCollisionStay() {
    WakeUp();
  }

  //! Put this rigidbody to sleep.
  public void Sleep() {
    _sleepCounter = _sleepCounterMax;
  }

  //! Wake this rigidbody up.
  public void WakeUp() {
    _sleepCounter = 0u;
  }
}
