// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using UnityEngine;

//! @brief This class displays helpful debugging information about all 
//! @link HxStateFunction HxStateFunctions @endlink found in all 
//! @link HxJoint HxJoints @endlink on a selected GameObject.
//!
//! Note: text get displayed on GameObject's TextMesh. Any existing text will be overwritten.
//!
//! See the @ref section_unity_haptx_primitive_debugger "Unity Plugin Guide" for a high level 
//! overview.
//! 
//! @ingroup group_unity_haptic_primitives
[ExecuteInEditMode]
[RequireComponent(typeof(TextMesh))]
public class HxPrimitiveDebugger : MonoBehaviour {

  //! The HxJoint we'll look for HxStateFunctions on.
  [Tooltip("The HxJoint we'll look for HxStateFunctions on.")]
  public HxJoint targetGameObject = null;

  //! @brief The TextMesh to display debugging information on.
  //!
  //! Note: any existing text will be overwritten.
  private TextMesh _textMesh = null;

  //! Called every frame if enabled.
  void Update() {
    if (_textMesh == null) {
      _textMesh = GetComponent<TextMesh>();
    }

    if (targetGameObject != null) {
      string text = string.Empty;

      HxJoint[] joints = targetGameObject.gameObject.GetComponents<HxJoint>();
      foreach (HxJoint joint in joints) {
        if (joint != null) {
          int indentLevel = 0;  // HxJoint.
          text += string.Format(new string(_IndentChar, indentLevel) + "{0} {1}\n",
              joint.gameObject.name, joint.GetType());

          foreach (DegreeOfFreedom degreeOfFreedom in Enum.GetValues(typeof(DegreeOfFreedom))) {
            HxDof dof = joint.GetDof(degreeOfFreedom);
            if (dof != null) {
              indentLevel += 2;  // HxDof.
              text += string.Format(new string(_IndentChar, indentLevel) + "{0}\n",
                  degreeOfFreedom.ToFriendlyString());

              indentLevel += 2;  // HxDof info.
              text += string.Format(new string(_IndentChar, indentLevel) + "position: {0}\n",
                  degreeOfFreedom.Domain() == DofDomain.LINEAR ?
                  dof.CurrentPosition.ToString("0.000") :
                  dof.CurrentPosition.ToString("0.0"));

              foreach (HxStateFunction stateFunction in dof.StateFunctions) {
                if (stateFunction != null) {
                  text += string.Format(new string(_IndentChar, indentLevel) +
                    "{0} \"{1}\": {2}\n", stateFunction.name, stateFunction.GetType(),
                    stateFunction.CurrentState);
                }
              }

              indentLevel -= 2;  // HxDof info.
              indentLevel -= 2;  // HxDof.
            }
          }
        }
        text += "\n";  // HxJoint.
      }

      _textMesh.text = text;
    } else {
      _textMesh.text = _DefaultText;
    }
  }

  //! The character to use to indent lines.
  private static readonly char _IndentChar = ' ';

  //! Text to display when not functionally configured.
  private static readonly string _DefaultText =
      "Set \"Target Game Object\" to debug HxStateFunctions.";
}
