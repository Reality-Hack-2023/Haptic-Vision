// Copyright (C) 2022 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//! Deals with settings rules related to Unity's time settings.
static internal class HxTimeSettingsRules {

  private const string TIME_PREFERENCES_ASSET_PATH = "ProjectSettings/TimeManager.asset";

  //! Gets the rules dealing with Time Settings.
  internal static IEnumerable<IHxRecommendedSettingsRule> GetRules() {
    // Fixed Timestep
    const float RECOMMENDED_FIXED_DT_SECONDS = 0.00222f;
    yield return new HxRelayRecommendedSettingsRule(
        () => Mathf.Approximately(Time.fixedDeltaTime, RECOMMENDED_FIXED_DT_SECONDS),
        () => {
          UnityEngine.Object timeManager =
              AssetDatabase.LoadMainAssetAtPath(TIME_PREFERENCES_ASSET_PATH);
          Undo.RecordObject(timeManager, "Changed Fixed Timestep");
          Time.fixedDeltaTime = RECOMMENDED_FIXED_DT_SECONDS;
          EditorUtility.SetDirty(timeManager);
        },
        "Fixed Timestep is not set at the HaptX recommended value of " +
            $"{RECOMMENDED_FIXED_DT_SECONDS} seconds. HaptX hands may behave unexpectedly.",
        $"Set Fixed Timestep to {RECOMMENDED_FIXED_DT_SECONDS}",
        "haptx.time.fixedtimestep");

    // Maximum Allowed Timestep
    const float RECOMMENDED_MAXIMUM_TIMESTEP = 0.0222f;
    yield return new HxRelayRecommendedSettingsRule(
        () => Mathf.Approximately(Time.maximumDeltaTime, RECOMMENDED_MAXIMUM_TIMESTEP),
        () => {
          UnityEngine.Object timeManager =
              AssetDatabase.LoadMainAssetAtPath(TIME_PREFERENCES_ASSET_PATH);
          Undo.RecordObject(timeManager, "Changed Fixed Timestep");
          Time.maximumDeltaTime = RECOMMENDED_MAXIMUM_TIMESTEP;
          EditorUtility.SetDirty(timeManager);
        },
        "Maximum Allowed Timestep is not set at the HaptX recommended value of " +
            $"{RECOMMENDED_MAXIMUM_TIMESTEP} seconds. HaptX hands may behave unexpectedly.",
        $"Set Maximum Allowed Timestep to {RECOMMENDED_MAXIMUM_TIMESTEP}",
        "haptx.time.maximumtimestep");
  }
}
