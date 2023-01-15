// Copyright (C) 2022 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

[DisallowMultipleComponent]
public class HxSteamVRSettings : ScriptableObject {

  //! @brief The name of the asset that stores the serialized singleton.
  public static string serializedSingletonName = "DontModify_HxSteamVRSettings";

  //! @brief Automatically set SteamVR plugin settings that the HaptX plugin requires to function.
  [SerializeField]
  [Tooltip("Automatically set SteamVR plugin settings that the HaptX plugin requires to function.")]
  public bool setRequiredSteamVRSettingsOnStart = true;

  //! @brief Suppress the warning produced when the HaptX plugin changes SteamVR settings.
  [SerializeField]
  [Tooltip("Suppress the warning produced when the HaptX plugin changes SteamVR settings.")]
  public bool suppressChangingSteamVRSettingsWarning = false;

  //! @brief See #Instance.
  private static HxSteamVRSettings _instance = null;

  //! @brief The singleton instance.
  public static HxSteamVRSettings Instance {
    get {
      if (_instance == null) {
        HxSteamVRSettings serialized = Serialized;
        if (serialized != null) {
          _instance = Instantiate(serialized);
        } else {
          _instance = CreateInstance<HxSteamVRSettings>();
        }
      }
      return _instance;
    }
  }

  //! @brief The serialized singleton.
  public static HxSteamVRSettings Serialized {
    get {
      return HxAssetManager.LoadAsset<HxSteamVRSettings>(serializedSingletonName);
    }
  }
}
