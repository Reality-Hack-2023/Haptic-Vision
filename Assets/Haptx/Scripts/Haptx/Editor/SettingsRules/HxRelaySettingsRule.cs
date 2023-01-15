// Copyright (C) 2022 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;

//! An implementation of IHxRecommendedSettingsRule using provided Func<T>s and Actions.
class HxRelayRecommendedSettingsRule : IHxRecommendedSettingsRule {

  //! Constructor
  public HxRelayRecommendedSettingsRule(Func<bool> matchFunction, Action setFunction, 
      string displayDescription, string setButtonDisplayText, string ignorePreferencePath,
      bool recommendsEditorRestartOnApply = false) {
    settingMatchesRecommended_ = matchFunction;
    setRecommendedSetting_ = setFunction;

    IgnoreId = ignorePreferencePath;
    DisplayDescription = displayDescription;
    SetButtonDisplayText = setButtonDisplayText;
    RecommendsEditorRestartOnApply = recommendsEditorRestartOnApply;
  }

  //! See IHxRecommendedSettingsRule.IgnoreId
  public string IgnoreId { get; }

  //! See IHxRecommendedSettingsRule.RecommendsEditorRestartOnApply
  public bool RecommendsEditorRestartOnApply { get; }

  //! See IHxRecommendedSettingsRule.DisplayDescription
  public string DisplayDescription { get; }

  //! See IHxRecommendedSettingsRule.SetButtonDisplayText
  public string SetButtonDisplayText { get; }

  //! The stored SettingMatchesRecommended function.
  private readonly Func<bool> settingMatchesRecommended_;

  //! The stored SetRecommendedSetting function.
  private readonly Action setRecommendedSetting_;

  //! See IHxRecommendedSettingsRule.SetRecommendedSetting
  void IHxRecommendedSettingsRule.SetRecommendedSetting() {
    setRecommendedSetting_();
  }

  //! See IHxRecommendedSettingsRule.SettingMatchesRecommended
  bool IHxRecommendedSettingsRule.SettingMatchesRecommended() {
    return settingMatchesRecommended_();
  }
}
