// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using UnityEngine;

//! A class that simplifies how HaptX manages visualizers.
[System.Serializable]
public class HxVisualizer {

  //! Whether this visualizer is actively visualizing.
  [Tooltip("Whether this visualizer is actively visualizing.")]
  public bool visualize = false;

  //! Toggles the active state of this visualizer.
  [Tooltip("Toggles the active state of this visualizer.")]
  public KeyWithModifiers toggle = new KeyWithModifiers();

  //! Default constructor.
  public HxVisualizer() { }

  //! Construct using given values.
  //!
  //! @param key See #toggle.
  //! @param alt See #toggle.
  //! @param shift See #toggle.
  //! @param control See #toggle.
  public HxVisualizer(KeyCode key, bool alt, bool shift, bool control) : base() {
    toggle = new KeyWithModifiers(key, alt, shift, control);
  }

  //! Update the state of this visualizer.
  //!
  //! @returns True if the state changed.
  public bool Update() {
    if (toggle.GetKeyDown()) {
      visualize = !visualize;
      return true;
    }

    return false;
  }
}
