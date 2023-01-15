// Copyright (C) 2022 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

//! A window for displaying project settings changes recommended by the HaptX plugin.
[InitializeOnLoad]
class HxUnitySettingsWindow : EditorWindow {

  //! Static constructor.
  static HxUnitySettingsWindow() {
    EditorApplication.update += () => {
      EditorApplication.update -= TryOpenWindowOnStartup;
      TryOpenWindowOnStartup();
    };
  }

  //! Opens the instance of this window.
  public static void OpenWindow() {
    HxUnitySettingsWindow window = (HxUnitySettingsWindow)GetWindow(typeof(HxUnitySettingsWindow), 
        utility: true, "HaptX Unity Settings Window");
    window.Show();
  }

  //! Returns true if any rules should be shown in the window.
  public static bool HasRelevantRules() {
    return recommendedSettingsRules_.Any(x =>
        !IgnoredPreferenceRecommendations.instance.HasIgnore(x.IgnoreId) &&
        !x.SettingMatchesRecommended());
  }

  //! Function that will open the window if needed the first time it's called in an editor session.
  private static void TryOpenWindowOnStartup() {
    string HAS_DONE_LAUNCH_CHECK_KEY = $"{nameof(HxUnitySettingsWindow)}DidLaunchCheck";
    if (!SessionState.GetBool(HAS_DONE_LAUNCH_CHECK_KEY, false)) {
      if (HasRelevantRules()) {
        OpenWindow();
      }

      SessionState.SetBool(HAS_DONE_LAUNCH_CHECK_KEY, true);
    }
  }

  //! The list of rules to check when making our recommendations.
  private static List<IHxRecommendedSettingsRule> recommendedSettingsRules_ = InitializeSettingsRules();
  //! @brief Creates a list of all the rules we should check when making our recommendations.
  //! @returns The list of rules.
  private static List<IHxRecommendedSettingsRule> InitializeSettingsRules() {
    List<IHxRecommendedSettingsRule> rules = new List<IHxRecommendedSettingsRule>();

    rules.Add(new HxInputSettingsRule());

    rules.AddRange(HxTimeSettingsRules.GetRules());

    rules.AddRange(HxPhysicsSettingsRules.GetRules());

    rules.Add(new HxActiveInputHandlingRule());

    rules.Add(new HxRelayRecommendedSettingsRule(
        () => Valve.VR.SteamVR_Settings.instance.lockPhysicsUpdateRateToRenderFrequency == false,
        () => {
          Valve.VR.SteamVR_Settings.instance.lockPhysicsUpdateRateToRenderFrequency = false;
          EditorUtility.SetDirty(Valve.VR.SteamVR_Settings.instance);
          },
        "\"Lock Physics Update Rate To Render Frequency\" in SteamVR settings is true." +
        " This will cause issues with HaptX hands.", 
        "Set to false", "haptx.steamvr.lockphysicsupdateratetorenderfrequency"));

    rules.Add(new HxRelayRecommendedSettingsRule(
        () => {
          return Unity.XR.OpenVR.OpenVRSettings.GetSettings(create: false)?.GetStereoRenderingMode() ==
              (ushort?)Unity.XR.OpenVR.OpenVRSettings.StereoRenderingModes.MultiPass;
        },
        () => {
          var settings = Unity.XR.OpenVR.OpenVRSettings.GetSettings(create: true);
          settings.StereoRenderingMode =
              Unity.XR.OpenVR.OpenVRSettings.StereoRenderingModes.MultiPass;
          EditorUtility.SetDirty(settings);
        },
        "OpenVR settings are not set to use multi-pass stereo rendering." +
        " Hand displacement visualization may not behave as expected.",
        "Set stereo rendering to multi-pass", "haptx.openvr.stereorenderingmode"));

    return rules;
  }

  //! Position of our scroll area.
  private Vector2 scrollPosition_ = Vector2.zero;

  //! Runs on editor GUI update.
  private void OnGUI() {
    if (!HasRelevantRules()) {
      EditorGUILayout.HelpBox("All HaptX-relevant Unity settings are either at " +
          "expected values or have been ignored.", MessageType.Info);
    } else {
      EditorGUILayout.HelpBox(
          "Some Unity settings are not set to values expected by the HaptX plugin. " +
          "Please accept or ignore all issues listed below.", MessageType.Warning);

      using (var scrollViewScope = new GUILayout.ScrollViewScope(scrollPosition_)) {
        scrollPosition_ = scrollViewScope.scrollPosition;

        foreach (IHxRecommendedSettingsRule rule in recommendedSettingsRules_) {
          if (IgnoredPreferenceRecommendations.instance.HasIgnore(rule.IgnoreId)) {
            continue;
          }

          if (!rule.SettingMatchesRecommended()) {
            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            GUILayout.Label(rule.DisplayDescription, textStyle);
            using (var horizontalScope =
              new EditorGUILayout.HorizontalScope()) {
              const float MAX_APPLY_BUTTON_WIDTH = 400;
              if (GUILayout.Button(rule.SetButtonDisplayText,
                  GUILayout.MaxWidth(MAX_APPLY_BUTTON_WIDTH))) {
                rule.SetRecommendedSetting();
                if (rule.RecommendsEditorRestartOnApply) {
                  PromptEditorRestart();
                }
              }
              GUILayout.FlexibleSpace();
              if (GUILayout.Button("Ignore", GUILayout.ExpandWidth(false))) {
                IgnoredPreferenceRecommendations.instance.AddIgnore(rule.IgnoreId);
              }
            }
          }
        }
      }
      if (GUILayout.Button("Apply all")) {
        bool shouldPromptForRestart = false;
        foreach(IHxRecommendedSettingsRule rule in recommendedSettingsRules_) {
          if (!rule.SettingMatchesRecommended() &&
              !IgnoredPreferenceRecommendations.instance.HasIgnore(rule.IgnoreId)) {
            rule.SetRecommendedSetting();
            shouldPromptForRestart = shouldPromptForRestart || rule.RecommendsEditorRestartOnApply;
          }
        }
        if (shouldPromptForRestart) {
          PromptEditorRestart();
        }
      }
    }

    EditorGUILayout.Separator();
    if (GUILayout.Button("Reset Ignores")) {
      IgnoredPreferenceRecommendations.instance.ClearIgnores();
    }
  }

  //! Opens a dialog prompting the user to restart the editor, and does so with their consent.
  private void PromptEditorRestart() {
    bool shouldRestart = EditorUtility.DisplayDialog("Restart Editor?", 
        "One or more settings that were applied require an editor restart to work properly." +
        " Restart now?", 
        "Yes", "No");

    if (shouldRestart) {
      EditorApplication.OpenProject(Directory.GetCurrentDirectory());
    }
  }
}

//! Stores which settings rules the user has ignored in this project.
[FilePath("Haptx/IgnoredPreferenceRecommendations.yaml", FilePathAttribute.Location.ProjectFolder)]
internal class IgnoredPreferenceRecommendations : ScriptableSingleton<IgnoredPreferenceRecommendations> {
  //! The list of string ids for settings rules we're ignoring
  [SerializeField]
  private List<string> ignores_ = new List<string>();

  //! @brief Checks if we've ignored a given settings rule string id.
  //!
  //! @param ignoreId The string id to check.
  //! @returns True if we're ignoring that string id.
  public bool HasIgnore(string ignoreId) {
    return ignores_.Contains(ignoreId);
  }

  //! @brief Adds a settings rule string id to the list of ones we're ignoring.
  //!
  //! @param ignoreId The settings rule string id to ignore.
  public void AddIgnore(string ignoreId) {
    if (!ignores_.Contains(ignoreId)) {
      ignores_.Add(ignoreId);
      Save(saveAsText: true);
    }
  }

  //! Clears our ignores so we're no longer ignoring any rules.
  public void ClearIgnores() {
    ignores_.Clear();
    Save(saveAsText: true);
  }
}

//! Interface for a rule that can check if a setting is set properly and fix it if not.
interface IHxRecommendedSettingsRule {
  //! @brief Checks if the current project settings match those desired by the rule.
  //!
  //! @returns True if the project settings are as desired.
  public bool SettingMatchesRecommended();

  //! Sets the project settings to those desired by the rule.
  public void SetRecommendedSetting();

  //! String name to track if the user has ignored this rule. Must be unique per rule.
  public string IgnoreId { get; }

  //! Does this rule recommend a restart on being applied.
  public bool RecommendsEditorRestartOnApply { get; }

  //! The description of the rule displayed to the user.
  public string DisplayDescription { get; }

  //! Text displayed to the user on the button to set the preferences. ie "Add InputManager axes"
  public string SetButtonDisplayText { get; }
}
