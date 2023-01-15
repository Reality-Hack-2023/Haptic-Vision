// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief Allows for the association of haptic properties on a per-collider basis.
//!
//! See the @ref section_unity_physical_material "Unity Plugin Guide" for a high level overview.
//!
//! @note Note that while most of these parameters can be changed at runtime, some of them will
//! not be reflected on previously registered objects unless HxCore.RegisterCollider() is called 
//! again with the registerAgain flag set to true. This is something we plan to streamline in a
//! future release.
//!
//! @ingroup group_unity_plugin
[DisallowMultipleComponent]
public class HxPhysicalMaterial : MonoBehaviour {
  //! Whether these HxPhysicalMaterial settings propagate to child Colliders. Propagation
  //! stops at Rigidbodies or other HxPhysicalMaterials.
  [Tooltip("Whether these HxPhysicalMaterial settings propagate to child Colliders. Propagation stops at Rigidbodies or other HxPhysicalMaterials.")]
  public bool propagateToChildren = true;

  //! Disable Colliders' abilities to produce tactile feedback.
  [Tooltip("Disable Colliders' abilities to produce tactile feedback.")]
  public bool disableTactileFeedback = false;

  //! Override default settings for #forceFeedbackEnabled.
  [Tooltip("Override default settings for forceFeedbackEnabled.")]
  public bool overrideForceFeedbackEnabled = false;

  //! Control this object's ability to produce force feedback.
  [Tooltip("Controls Colliders' abilities to produce force feedback.")]
  public bool forceFeedbackEnabled = true;

  //! Override the default settings for #baseContactToleranceM.
  [Tooltip("Override default settings for base contact tolerance [m].")]
  public bool overrideBaseContactTolerance = false;

  //! Maximum distance to cause haptic actuation [m].
  [Tooltip("Maximum distance to cause haptic actuation [m]."), Range(-0.01f, 0.01f)]
  public float baseContactToleranceM = 0.0f;

  //! Override the default settings for #complianceM_N.
  [Tooltip("Override default settings for compliance [m/N].")]
  public bool overrideCompliance = false;

  //! Contact tolerance increase per unit force [m/N].
  [Tooltip("Contact tolerance increase per unit force [m/N]."), Range(-0.01f, 0.01f)]
  public float complianceM_N = 0.0f;

  //! Called when the script is being loaded.
  void Awake() {
    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxPhysicalMaterial.Awake(): Failed to get handle to core.", this);
      return;
    }

    Collider[] childColliders = GetComponentsInChildren<Collider>();
    foreach (Collider collider in childColliders) {
      long ciObjectId;
      core.TryRegisterCollider(collider, false, out ciObjectId);
    }
  }
}
