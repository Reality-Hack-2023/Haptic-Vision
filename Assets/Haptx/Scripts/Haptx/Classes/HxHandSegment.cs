// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using UnityEngine;

//! This class is responsible for passing collision information to an HxHand somewhere in the
//! parent hierarchy.
[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
public class HxHandSegment : MonoBehaviour {

  //! The HxHand component in our parent hierarchy to relay collision information to.
  HxHand hand;

  void Awake() {
    hand = GetComponentInParent<HxHand>();
    if (hand == null) {
      Debug.LogError("Failed to find an HxHand in parent hierarchy.", this);
    }

    // Don't allow parts of the hand to sleep. That seems to allow contacts with errant impulse
    // magnitudes of zero, which is not good.
    Rigidbody rb = GetComponent<Rigidbody>();
    if (rb != null) {
      rb.sleepThreshold = 0.0f;
    }
  }

  //! Cached to save on GC allocations.
  private List<Collider> _fixedUpdateKeysToRemove = new List<Collider>();

  void OnCollisionStay(Collision collision) {
    // Send the contact to the hand.
    hand.ReceiveOnCollisionStay(collision, this);
  }

  void OnTriggerEnter(Collider collider) {
    hand.ReceiveOnTriggerEnter(collider);
  }

  void OnTriggerExit(Collider collider) {
    hand.ReceiveOnTriggerExit(collider);
  }
}
