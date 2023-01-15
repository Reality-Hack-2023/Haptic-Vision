// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief HaptX Debug wrapper.
[DisallowMultipleComponent]
public class HxDebug : ScriptableObject {

  //! @brief Determines whether HaptX warnings and errors get displayed on-screen.
  [Tooltip("Determines whether HaptX warnings and errors get displayed on-screen.")]
  public bool _displayOnScreenWarningAndErrorMessages = true;

  //! @brief The duration for which HaptX warning messages get drawn on-screen.
  [Tooltip("The duration for which HaptX warning messages get drawn on-screen.")]
  public float _warningMessageDuration = 10.0f;

  //! @brief The duration for which HaptX error messages get drawn on-screen.
  [Tooltip("The duration for which HaptX error messages get drawn on-screen.")]
  public float _errorMessageDuration = 15.0f;

  //! @brief Whether Rigidbodies joined by an HxJoint will indicate if they're awake.
  [Tooltip("Whether Rigidbodies joined by an HxJoint will indicate if they're awake.")]
  public bool _indicateRigidbodiesAwake = false;

  //! @brief The name of the asset that stores the serialized singleton.
  public static string serializedSingletonName = "DontModify_HxDebug";

  //! @brief See #Instance.
  private static HxDebug _instance = null;

  //! @brief Whether or not we've displayed a restart message yet (we only print one per session).
  private bool hasPrintedRestartMessage = false;

  //! @brief The singleton instance.
  //!
  //! Will attempt to load the serialized singleton asset. Spawns an instance with default values
  //! if unsuccessful.
  public static HxDebug Instance {
    get {
      if (_instance == null) {
        HxDebug serialized = Serialized;
        if (serialized != null) {
          _instance = Instantiate(serialized);
        } else {
          _instance = CreateInstance<HxDebug>();
        }
      }
      return _instance;
    }
  }

  //! @brief The serialized singleton.
  public static HxDebug Serialized {
    get {
      return HxAssetManager.LoadAsset<HxDebug>(serializedSingletonName);
    }
  }

  //! @brief Logs a message to the Unity log.
  //!
  //! @param message The message to log.
  //! @param context Associate an object with the message.
  public static void Log(string message, Object context = null) {
    Debug.Log(message, context);
  }

  //! @brief Logs a warning message to the Unity log.
  //!
  //! @param message The message to log.
  //! @param context Associate an object with the message.
  //! @param addToScreen Whether to also add this message to the on-screen log.
  //! @param messageKey The key/ID for this message. Only one message with a given key can display
  //!     at a time and will replace existing messages with the same key. If negative then a unique
  //!     key will be chosen automatically.
  //! @returns The key the message was logged with, or a negative number if @p addToScreen is false.
  public static int LogWarning(string message, Object context = null, bool addToScreen = false,
      int messageKey = -1)
  {
    Debug.LogWarning(message, context);
    if (addToScreen && Instance._displayOnScreenWarningAndErrorMessages) {
      return HxOnScreenLog.LogToScreen(message, HxOnScreenLog.OnScreenMessageType.WARNING, 
          Color.yellow, Instance._warningMessageDuration, messageKey);
    }
    return -1;
  }

  //! @brief Logs an error message to the Unity log.
  //!
  //! @param message The message to log.
  //! @param context Associate an object with the message.
  //! @param addToScreen Whether to also add this message to the on-screen log.
  //! @param messageKey The key/ID for this message. Only one message with a given key can display
  //!     at a time and will replace existing messages with the same key. If negative then a unique
  //!     key will be chosen automatically.
  //! @returns The key the message was logged with, or a negative number if @p addToScreen is false.
  public static int LogError(string message, Object context = null, bool addToScreen = false,
      int messageKey = -1)
  {
    Debug.LogError(message, context);
    if (addToScreen && Instance._displayOnScreenWarningAndErrorMessages) {
      return HxOnScreenLog.LogToScreen(message, HxOnScreenLog.OnScreenMessageType.ERROR, 
          Color.red, Instance._errorMessageDuration, messageKey);
    }
    return -1;
  }

  //! @brief Logs a message asking the user to restart their game to the output log and the screen.
  public static void LogRestartMessage()
  {
    // Only print once per session
    if (Instance.hasPrintedRestartMessage)
    {
      return;
    }
    Instance.hasPrintedRestartMessage = true;

    const string message =
        "There were errors in the HaptX SDK. You must fix them, then restart your game before the HaptX SDK will function correctly again. Check the log for more information.";
    Debug.LogError(message);
    if (Instance._displayOnScreenWarningAndErrorMessages)
    {
      HxOnScreenLog.LogToScreen(message, HxOnScreenLog.OnScreenMessageType.ERROR, Color.white,
          float.MaxValue);
    }
  }
}
