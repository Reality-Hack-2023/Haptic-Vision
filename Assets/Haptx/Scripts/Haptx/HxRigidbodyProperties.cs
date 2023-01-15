// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief Allows for the association of haptic properties on a per-rigidbody basis.
//!
//! See the @ref section_rigidbody_properties_script "Unity Plugin Guide" for a high level overview.
//!
//! @note Note that while most of these parameters can be changed at runtime, some of them will not
//! be reflected on previously registered objects unless HxCore.RegisterRigidbody() is called again
//! with the registerAgain flag set to true. This is something we plan to streamline in a future
//! release.
//!
//! @ingroup group_unity_plugin
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class HxRigidbodyProperties : MonoBehaviour {

  //! Override the default settings for #graspingEnabled.
  [Tooltip("Override the default settings for #graspingEnabled.")]
  public bool overrideGraspingEnabled = false;

  //! Control this object's ability to be grasped.
  [Tooltip("Control this object's ability to be grasped.")]
  public bool graspingEnabled = true;

  //! Override the default settings for #graspThreshold. 
  [Tooltip("Override default settings for this Rigidbody with graspThreshold.")]
  public bool overrideGraspThreshold = false;

  //! The grasp threshold to use if overriding the default.
  [Tooltip("The grasp threshold to use if overriding the default."), Range(0.0f, float.MaxValue)]
  public float graspThreshold = 0;

  //! Override the default settings for #releaseHysteresis. 
  [Tooltip("Override default settings for this Rigidbody with releaseHysteresis.")]
  public bool overrideReleaseHysteresis = false;

  //! The release hysteresis to use if overriding the default.
  [Tooltip("The release hysteresis to use if overriding the default."), Range(0.0f, 1.0f)]
  public float releaseHysteresis = 0;

  //! Override the default settings for #graspLinearLimits. 
  [Tooltip("Override the default settings for graspLinearLimits.")]
  public bool overrideGraspLinearLimits = false;

  //! The grasp linear limits to use if overriding the default.
  [Tooltip("The grasp linear limits to use if overriding the default.")]
  public HxCore.LinearAnchorConstraintSettings graspLinearLimits =
      new HxCore.LinearAnchorConstraintSettings();

  //! Override the default settings for #graspAngularLimits. 
  [Tooltip("Override the default settings for graspAngularLimits.")]
  public bool overrideGraspAngularLimits = false;

  //! The grasp angular limits to use if overriding the default.
  [Tooltip("The grasp angular limits to use if overriding the default.")]
  public HxCore.AngularAnchorConstraintSettings graspAngularLimits =
      new HxCore.AngularAnchorConstraintSettings();

  //! Override the default settings for #sleepThreshold.
  [Tooltip("Override the default settings for sleepThreshold.")]
  public bool overrideSleepThreshold = false;

  //! Override the default settings for #contactDampingEnabled.
  [Tooltip("Override the default settings for contactDampingEnabled.")]
  public bool overrideContactDampingEnabled = false;

  //! Whether this object's motion gets damped when in contact with the palm. This makes it 
  //! considerably easier to hold.
  [Tooltip("Whether this object's motion gets damped when in contact with the palm. This makes it considerably easier to hold.")]
  public bool contactDampingEnabled = true;

  //! Override the default settings for #maxContactDampingSeparation.
  [Tooltip("Override the default settings for maxContactDampingSeparation.")]
  public bool overrideMaxContactDampingSeparation = false;

  //! @brief The maximum separation below which contact damping is triggered.
  //!
  //! Only meaningful if less than Physics.defaultContactOffset.
  [Tooltip("The maximum separation below which contact damping is triggered."),
      Range(0.0f, float.MaxValue)]
  public float maxContactDampingSeparation = 0.01f;

  //! Override the default settings for linearContactDamping.
  [Tooltip("Override the default settings for linearContactDamping.")]
  public bool overrideLinearContactDamping = false;

  //! Linear damping used in the joint that forms between the palm and objects contacting it.
  [Tooltip("Linear damping used in the physics constraint that forms between the palm and objects contacting it.")]
  public float linearContactDamping = 0.0f;

  //! Override the default settings for #angularContactDamping. 
  [Tooltip("Override the default settings for angularContactDamping.")]
  public bool overrideAngularContactDamping = false;

  //! Angular damping used in the joint that forms between the palm and objects contacting 
  //! it.
  [Tooltip("Angular damping used in the joint that forms between the palm and objects contacting it.")]
  public float angularContactDamping = 0.0f;

  //! The sleep threshold to use if overriding the default.
  [Tooltip("The sleep threshold to use if overriding the default."), Range(0.0f, float.MaxValue)]
  public float sleepThreshold = 0.005f;

  //! Called when the script is being loaded.
  void Awake() {
    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxRigidbodyProperties.Awake(): Failed to get handle to core.", this);
      return;
    }

    Rigidbody rigidbody = GetComponent<Rigidbody>();
    long gdObjectId;
    core.TryRegisterRigidbody(rigidbody, false, out gdObjectId);

    if (overrideSleepThreshold) {
      rigidbody.sleepThreshold = sleepThreshold;
    }
  }
}
