// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief Holds rendering information relevant to @link HaptxApi::Tactor Tactors @endlink.
//!
//! Wraps HaptxApi::ContactInterpreter::TactorParameters.
[System.Serializable]
public class TactorParameters {

  //! How much the target Tactor height should vary as a ratio of the height suggested by the
  //! physical model [m/m].
  [Range(0.0f, 1.0f), Tooltip(
      "How much the target Tactor height should vary as a ratio of the height suggested by the physical model [m/m].")]
  public float dynamicScaling = 0.0f;

  //! Maximum height target the Tactor will be allowed to attain [m].
  //!
  //! The actual maximum is dependent on the particular Tactor and is limited in hardware.
  [Range(0.0f, 0.003f), Tooltip(
      "Maximum height target the Tactor will be allowed to attain [m]. The actual maximum is dependent on the particular Tactor and is limited in hardware.")]
  public float maxHeightTargetM = 0.0f;

  //! Unwraps this instance.
  //!
  //! @returns An unwrapped instance.
  public HaptxApi.ContactInterpreter.TactorParameters Unwrap() {
    HaptxApi.ContactInterpreter.TactorParameters parameters =
        new HaptxApi.ContactInterpreter.TactorParameters();
    parameters.dynamic_scaling = dynamicScaling;
    parameters.max_height_target_m = maxHeightTargetM;
    return parameters;
  }
}

//! Retractuator-specific rendering settings.
//!
//! Wraps HaptxApi::ContactInterpreter::RetractuatorParameters
[System.Serializable]
public class RetractuatorParameters {

  //! The minimum sum of scalar projections of contact forces [N] along actuation directions
  //! required to engage a Retractuator.
  [Range(0.0f, float.MaxValue), Tooltip(
      "The minimum sum of scalar projections of contact forces [N] along actuation directions required to engage a Retractuator.")]
  public float actuationThresholdN = 0.0f;

  //! How aggressively nominal Retractuator forces will be filtered when comparing against the
  //! release threshold (similar to time constant) [s].
  //!
  //! Smaller values lead to faster responses but more jitter; larger values lead to slower but
  //! smoother responses.
  [Range(0.0f, 5.0f), Tooltip(
      "How aggressively nominal Retractuator forces will be filtered when comparing against the release threshold (similar to time constant) [s]. Smaller values lead to faster responses but more jitter; larger values lead to slower but smoother responses.")]
  public float filterStrengthS = 0.0f;

  //! The force rate required to release a Retractuator when a contact is still present [N/s].
  //!
  //! Smaller values lead to more aggressive release behavior but more jitter; larger values
  //! lead to more conservative and slower responses, potentially increasing the perception of
  //! stickiness in the force feedback system.
  [Range(0.0f, float.MaxValue), Tooltip(
      "The force rate required to release a Retractuator when a contact is still present [N/s]. Smaller values lead to more aggressive release behavior but more jitter; larger values lead to more conservative and slower responses, potentially increasing the perception of stickiness in the force feedback system.")]
  public float releaseThresholdN_S = 0.0f;

  //! Unwraps this instance.
  //!
  //! @returns An unwrapped instance.
  public HaptxApi.ContactInterpreter.RetractuatorParameters Unwrap() {
    HaptxApi.ContactInterpreter.RetractuatorParameters parameters =
        new HaptxApi.ContactInterpreter.RetractuatorParameters();
    parameters.actuation_threshold_n = actuationThresholdN;
    parameters.filter_strength_s = filterStrengthS;
    parameters.release_threshold_n_s = releaseThresholdN_S;
    return parameters;
  }
}

//! Parameters for haptically enabled bodies controlled by a user.
//!
//! Wraps HaptxApi::ContactInterpreter::BodyParameters
[System.Serializable]
public class BodyParameters {

  //! Base contact distance tolerance [m].
  //!
  //! The maximum separation [m] measured along the ray trace vector that is sufficient to
  //! consider two objects as inter-penetrating in the absence of contact force.  Larger contact
  //! tolerances will cause more haptic rendering when two rigid bodies are close but not
  //! visually in contact.
  [Range(-0.01f, 0.01f), Tooltip(
      "Base contact distance tolerance [m]. The maximum separation [m] measured along the ray trace vector that is sufficient to consider two objects as inter-penetrating in the absence of contact force.  Larger contact tolerances will cause more haptic rendering when two rigid bodies are close but not visually in contact.")]
  public float baseContactToleranceM = 0.0f;

  //! Compliance [m/N].
  //!
  //! Contact distance tolerance increase per unit force [m/N]. Larger values imply softer
  //! haptically enabled bodies and more distributed contact regions.
  [Range(-0.01f, 0.01f), Tooltip(
      "Compliance [m/N]. Contact distance tolerance increase per unit force [m/N]. Larger values imply softer haptically enabled bodies and more distributed contact regions.")]
  public float complianceM_N = 0.0f;

  //! Unwraps this instance.
  //!
  //! @returns An unwrapped instance.
  public HaptxApi.ContactInterpreter.BodyParameters Unwrap() {
    HaptxApi.ContactInterpreter.BodyParameters parameters =
        new HaptxApi.ContactInterpreter.BodyParameters();
    parameters.base_contact_tolerance_m = baseContactToleranceM;
    parameters.compliance_m_n = complianceM_N;
    return parameters;
  }
}
