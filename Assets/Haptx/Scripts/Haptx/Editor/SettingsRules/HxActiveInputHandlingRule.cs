using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

class HxActiveInputHandlingRule : IHxRecommendedSettingsRule {
  private enum ActiveInputHandlerValue {
    Old = 0,
    New = 1,
    Both = 2,
  }

  //! See IHxRecommendedSettingsRule.IgnoreId
  public string IgnoreId => "haptx.playersettings.activeInputHandler";

  //! See IHxRecommendedSettingsRule.RecommendsEditorRestartOnApply
  public bool RecommendsEditorRestartOnApply => true;

  //! See IHxRecommendedSettingsRule.DisplayDescription
  public string DisplayDescription =>
    "The active input handler is not set to use both old and new input systems." +
    " HaptX debug input may not behave as expected.";

  //! See IHxRecommendedSettingsRule.SetButtonDisplayText
  public string SetButtonDisplayText => "Set to use both (requires restart)";

  //! See IHxRecommendedSettingsRule.SetRecommendedSetting
  public void SetRecommendedSetting() {
    SerializedProperty activeInputHandlerProperty = GetActiveInputHandlerProperty(
        out UnityEngine.Object playerSettings, out SerializedObject serializedSettings);

    activeInputHandlerProperty.intValue = (int)ActiveInputHandlerValue.Both;

    serializedSettings.ApplyModifiedProperties();
    EditorUtility.SetDirty(playerSettings);
  }

  //! See IHxRecommendedSettingsRule.SettingMatchesRecommended
  public bool SettingMatchesRecommended() {
    SerializedProperty activeInputHandlerProperty = GetActiveInputHandlerProperty(out _, out _);

    return activeInputHandlerProperty != null && 
        activeInputHandlerProperty.intValue == (int)ActiveInputHandlerValue.Both;
  }

  private const string PROJECT_SETTINGS_ASSET_PATH = "ProjectSettings/ProjectSettings.asset";
  private const string ACTIVE_INPUT_HANDLER_PROPERTY_PATH = "activeInputHandler";
  //! @brief Gets the serialized active input handler property from the project settings.
  //!
  //! @param [out] playerSettings The player settings as a Unity Object.
  //! @param [out] serializedSettings The player settings as a SeralizedObject.
  //! @returns The serialized active input handler property.
  private static SerializedProperty GetActiveInputHandlerProperty(out UnityEngine.Object playerSettings,
      out SerializedObject serializedSettings) {

    playerSettings = AssetDatabase.LoadMainAssetAtPath(PROJECT_SETTINGS_ASSET_PATH);
    serializedSettings = new SerializedObject(playerSettings);

    return serializedSettings.FindProperty(ACTIVE_INPUT_HANDLER_PROPERTY_PATH);
  }
}
