// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;

//! Wraps an integer layer so it may be paired with HxLayerDrawer to appear in the inspector as a
//! layer selection drop down menu.
[Serializable]
public struct HxLayer {

  //! The integer value of the layer.
  public int value;
}
