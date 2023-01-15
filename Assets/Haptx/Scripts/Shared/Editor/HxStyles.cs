// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;
using UnityEngine;

//! Style presets for HaptX inspectors.
public static class HxStyles {

  //! The default height for things organized horizontally.
  private static float _FixedHeight = 18.0f;

  //! A customized version of EditorStyles.label.
  public static GUIStyle Label = new GUIStyle(EditorStyles.label);

  //! A customized version of EditorStyles.boldLabel.
  public static GUIStyle BoldLabel = new GUIStyle(EditorStyles.boldLabel);

  //! A customized version of EditorStyles.miniButton.
  public static GUIStyle Button = new GUIStyle(EditorStyles.miniButton);

  //! A customized version of EditorStyles.miniButton.
  public static GUIStyle MiniButton = new GUIStyle(EditorStyles.miniButton);

  //! A customized version of EditorStyles.popup.
  public static GUIStyle Popup = new GUIStyle(EditorStyles.popup);

  //! A customized version of EditorStyles.textField.
  public static GUIStyle TextField = new GUIStyle(EditorStyles.textField);

  //! @brief Static constructor.
  //!
  //! Customizes various EditorStyles.
  static HxStyles() {
    Label.fixedHeight = _FixedHeight;

    BoldLabel.fixedHeight = _FixedHeight;

    Button.fixedHeight = _FixedHeight;
    Button.stretchWidth = false;

    MiniButton.fixedWidth = 20.0f;
    MiniButton.fixedHeight = _FixedHeight;

    Popup.fixedHeight = _FixedHeight;

    TextField.fixedHeight = _FixedHeight;
  }
}
