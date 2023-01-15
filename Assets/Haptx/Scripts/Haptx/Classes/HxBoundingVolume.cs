// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! Wraps HaptxApi::BoundingVolume.
public abstract class HxBoundingVolume : MonoBehaviour {

  //! Get the underlying bounding volume.
  //!
  //! @returns The underlying bounding volume.
  public abstract HaptxApi.BoundingVolume GetBoundingVolume();
}
