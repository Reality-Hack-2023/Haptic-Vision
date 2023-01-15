// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @copydoc HxNetworkRigidbodyBase
[RequireComponent(typeof(Rigidbody))]
public class HxNetworkRigidbody : HxNetworkRigidbodyBase {
  void Start() {
    _rigidbody = GetComponent<Rigidbody>();
  }
}
