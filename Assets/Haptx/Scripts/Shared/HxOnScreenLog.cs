// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//! @brief Responsible for creating and managing on-screen log messages.
[DisallowMultipleComponent]
public class HxOnScreenLog : MonoBehaviour {

  //! @brief The severity level of an on-screen log message.
  //!
  //! Must be enumerated in increasing order of severity.
  public enum OnScreenMessageType : uint {
    LOG = 0,  //!< General messages.
    WARNING,   //!< Warning messages.
    ERROR      //!< Error messages.
  }

  //! @brief The information associated with a single on-screen message.
  private class OnScreenMessage {

    //! @brief The message text.
    public string message = string.Empty;

    //! @brief The text color.
    public Color color = Color.black;

    //! @brief The severity of the message being logged.
    public OnScreenMessageType severity = OnScreenMessageType.LOG;

    //! @brief The time [s] at which the message was generated.
    public float startTime = 0.0f;

    //! @brief The frame at which the message was generated.
    public int startRenderedFrame = 0;

    //! @brief The duration [s] the message is to be displayed.
    public float duration = 0.0f;

    //! @brief The component displaying the message.
    public TextMesh textMesh = null;

    //! @brief The key/ID for this message. Only one message with a given key can display at a time
    //! and will replace existing messages with the same key. If negative then a unique key will be
    //! chosen automatically.
    public int key = -1;
  }

  //! @brief The name of the asset that stores the serialized singleton.
  public static string serializedSingletonName = "DontModify_HxOnScreenLog";

  //! @brief True to enable on-screen logging.
  //! 
  //! Determines whether messages get rendered on-screen.

  [SerializeField]
  [Tooltip("True to enable on-screen logging.")]
  bool _displayOnScreenMessages = true;

  //! @brief The minimum level of severity an on-screen message must have to be displayed.
  [SerializeField]
  [Tooltip("The minimum level of severity an on-screen message must have to be displayed.")]
  OnScreenMessageType _minSeverity = OnScreenMessageType.LOG;

  //! @brief The number of spaces to indent with when an on-screen message contains more than one
  //! line.
  [SerializeField]
  [Tooltip("The number of spaces to indent with when an on-screen message contains more than one line.")]
  private uint _messageIndent = 2;

  //! @brief The size of each character in on-screen messages.
  [SerializeField]
  [Tooltip("The size of each character in on-screen messages.")]
  private float _characterSize = 0.002f;

  //! @brief The size of the font used in on-screen messages. Increase this value and decrease 
  //! Character Size to increase resolution.
  [SerializeField]
  [Tooltip("The size of the font used in on-screen messages. Increase this value and decrease Character Size to increase resolution.")]
  private int _fontSize = 140;

  //! @brief The distance from the camera that the on-screen log renders (in world space).
  [SerializeField]
  [Tooltip("The distance from the camera that the on-screen log renders (in world space).")]
  private float _textDistance = 1.0f;

  //! @brief The fraction of vertical screen space reserved for the top margin.
  [SerializeField]
  [Range(0.0f, 1.0f)]
  [Tooltip("The fraction of vertical screen space reserved for the top margin.")]
  private float _topMargin = 0.33f;

  //! @brief The fraction of horizontal screen space reserved for the left margin.
  [SerializeField]
  [Range(0.0f, 1.0f)]
  [Tooltip("The fraction of horizontal screen space reserved for the left margin.")]
  private float _leftMargin = 0.33f;

  //! @brief The max number of characters in a line, including Message Indent. Only used when Wrap
  //! Messages is True.
  [SerializeField]
  [Tooltip("The max number of characters in a line, including Message Indent. Only used when Wrap Messages is True.")]
  private uint _maxLineLength = 100;

  //! @brief See #Instance.
  private static HxOnScreenLog _instance = null;

  //! @brief All of the messages currently being displayed on-screen.
  private static Dictionary<int, OnScreenMessage> onScreenMessages =
      new Dictionary<int, OnScreenMessage>();

  //! @brief The singleton instance.
  public static HxOnScreenLog Instance {
    get {
      if (_instance == null) {
        // Try to load the serialized singleton instance.
        HxOnScreenLog serializedInstance = Serialized;
        if (serializedInstance != null) {
          _instance = Instantiate(serializedInstance);
        } else {
          GameObject gameObject = new GameObject();
          _instance = gameObject.AddComponent<HxOnScreenLog>();
        }

        _instance.gameObject.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
      }
      return _instance;
    }
  }

  //! @brief The serialized instance.
  public static HxOnScreenLog Serialized {
    get {
      GameObject gameObjectSerialized =
          HxAssetManager.LoadAsset<GameObject>(serializedSingletonName);
      if (gameObjectSerialized != null) {
        HxOnScreenLog serialized =
          gameObjectSerialized.GetComponent<HxOnScreenLog>();
        if (serialized != null) {
          return serialized;
        }
      }
      return null;
    }
  }

  //! @brief Called when the script is being loaded.
  private void Awake() {
    if (_instance != null && _instance != this) {
      Destroy(gameObject);
    } else {
      _instance = this;
    }
  }

  //! @brief Called every frame if enabled.
  public void Update() {
    // Attempt to create TextMesh components.
    foreach (KeyValuePair<int, OnScreenMessage> pair in onScreenMessages) {
      if (pair.Value.textMesh == null) {
        pair.Value.textMesh = CreateTextMesh(pair.Value);
      }
    }

    // Hide messages when not in-game and using the editor, lest messages appear in scene view.
    if (Application.isEditor && !Application.isFocused) {
      foreach (KeyValuePair<int, OnScreenMessage> pair in onScreenMessages) {
        if (pair.Value.textMesh != null) {
          pair.Value.textMesh.gameObject.SetActive(false);
        }
      }
    } else {
      foreach (KeyValuePair<int, OnScreenMessage> pair in onScreenMessages) {
        if (pair.Value.textMesh != null && (int)_minSeverity <= (int)pair.Value.severity) {
          pair.Value.textMesh.gameObject.SetActive(true);
        }
      }
    }

    int numLines = 0;
    List<int> keysToRemove = new List<int>();
    foreach (KeyValuePair<int, OnScreenMessage> pair in onScreenMessages) {
      // Remove the message if it has expired.
      if (pair.Value.duration >= 0.0f &&
          Time.renderedFrameCount != pair.Value.startRenderedFrame &&  // Render at least once
          Time.time - pair.Value.startTime > pair.Value.duration) {
        keysToRemove.Add(pair.Key);
        if (pair.Value.textMesh != null) {
          Destroy(pair.Value.textMesh.gameObject);
        }
        continue;
      }

      // Skip this message if it doesn't have a TextMesh yet.
      if (pair.Value.textMesh == null) {
        continue;
      }

      // Draw the message at the correct line in the log. Skip the message if its severity is too 
      // low.
      if (_displayOnScreenMessages && (int)_minSeverity <= (int)pair.Value.severity) {
        pair.Value.textMesh.text = new string('\n', numLines) + pair.Value.message;
        numLines += pair.Value.message.Count(f => f == '\n') + 1;
      } else {
        pair.Value.textMesh.text = string.Empty;
      }
    }

    // Remove all messages destined to be removed.
    foreach (int key in keysToRemove) {
      onScreenMessages.Remove(key);
    }
  }

  //! @brief Called when the scene ends or when manually destroyed.
  public void OnDestroy() {
    // Clear messages still on-screen.
    if (_instance == this) {
      foreach (KeyValuePair<int, OnScreenMessage> pair in onScreenMessages) {
        Destroy(pair.Value.textMesh.gameObject);
      }
      onScreenMessages.Clear();
      _instance = null;
    }
  }

  //! @brief Attempts to create a TextMesh for a message.
  //!
  //! @param onScreenMessage The message to create a TextMesh for.
  //! @returns The TextMesh if created, null otherwise.
  private TextMesh CreateTextMesh(OnScreenMessage onScreenMessage) {
    if (Camera.main == null) {
      return null;
    }

    // Spawn and configure a GameObject for this message.
    GameObject messageObject = new GameObject();
    messageObject.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
    messageObject.transform.parent = Camera.main.transform;

    // Spawn and configure a TextMesh component for this message.
    TextMesh messageTextMesh = messageObject.AddComponent<TextMesh>();
    messageTextMesh.characterSize = _characterSize;
    messageTextMesh.fontSize = _fontSize;
    messageTextMesh.color = onScreenMessage.color;

    // Position the text so it's visible to the camera.
    float halfFovTan = Mathf.Tan(0.5f * Mathf.Deg2Rad * Camera.main.fieldOfView);
    float lWidthM = 2.0f * _textDistance * halfFovTan;
    float inverseAspect = 1.0f;
    if (Camera.main.aspect > 0.0f) {
      inverseAspect = 1.0f /
          ((UnityEngine.XR.XRSettings.enabled ? 2.0f : 1.0f) * Camera.main.aspect);
    }
    float lHeightM = inverseAspect * lWidthM;
    messageObject.transform.localPosition = new Vector3(
        (_leftMargin - 0.5f) * lWidthM,
        (0.5f - _topMargin) * lHeightM,
        _textDistance);
    messageObject.transform.localRotation = Quaternion.identity;
    return messageTextMesh;
  }

  //! @brief Adds a message to the on-screen log.
  //!
  //! Not static so serialized member variables may be accessed.
  //!
  //! @param message The message text.
  //! @param severity The severity of the message.
  //! @param color The color to display this message on-screen.
  //! @param duration The duration the message is to be displayed on-screen.
  //! @param messageKey The key/ID for this message. Only one message with a given key can display
  //!     at a time and will replace existing messages with the same key. If negative then a unique
  //!     key will be chosen automatically.
  //! @returns The key the message was logged with, or a negative number if @p addToScreen is false.
  private int LogToScreenInternal(string message, OnScreenMessageType severity,
      Color color, float duration, int messageKey = -1) {
    // Indent any new lines in the message. Optionally apply wrapping.
    List<string> lines = new List<string>(message.Split('\n'));
    for (int i = 0; i < lines.Count; i++) {
      // Remove the line being wrapped.
      string[] words = lines[i].Split(' ');
      lines.RemoveAt(i);

      // Add the line back, wrapping when necessary.
      string wrappedLine = string.Empty;
      foreach (string word in words) {
        // If, when added, word causes the line to exceed its maximum length start a new line.
        // But only if the line is non-empty; otherwise, the word is solely responsible for the
        // breach.
        if (wrappedLine.Count() + word.Count() >
            (i == 0 ? _maxLineLength : (int)_maxLineLength - _messageIndent) &&
            wrappedLine.Count() > 0) {
          lines.Insert(i, wrappedLine);
          wrappedLine = string.Empty;
          i++;
        }

        // Add word back to the line.
        wrappedLine += (wrappedLine.Count() > 0 ? " " : string.Empty) + word;
      }

      // Make sure we don't miss the last line.
      if (wrappedLine.Count() > 0) {
        lines.Insert(i, wrappedLine);
        i++;
      }
    }

    // Recombine lines, adding space for indents.
    string formattedMessage =
        string.Join('\n' + new string(' ', (int)_messageIndent), lines.ToArray());

    if (messageKey < 0) {
      messageKey = NextOnScreenMessageKey;
    }

    // Add this message to the log.
    OnScreenMessage newMessage = new OnScreenMessage() {
        message = formattedMessage,
        color = color,
        severity = severity,
        startTime = Time.time,
        startRenderedFrame = Time.renderedFrameCount,
        duration = duration,
        textMesh = null,  // Will spawn in Update() when able.
        key = messageKey
    };
    OnScreenMessage existingMessage;
    if (onScreenMessages.TryGetValue(messageKey, out existingMessage)) {
      if (existingMessage.textMesh != null) {
        Destroy(existingMessage.textMesh.gameObject);
      }
      onScreenMessages[messageKey] = newMessage;
    } else {
      onScreenMessages.Add(messageKey, newMessage);
    }
    return messageKey;
  }

  //! @brief Logs an on-screen message.
  //!
  //! @param severity The severity of the message.
  //! @param message The message text.
  //! @param duration The duration the message is to be displayed on-screen. Durations of less than
  //! zero are treated as infinite.
  //! @param messageKey The key/ID for this message. Only one message with a given key can display
  //!     at a time and will replace existing messages with the same key. If negative then a unique
  //!     key will be chosen automatically.
  //! @returns The key the message was logged with, or a negative number if something went wrong.
  public static int LogToScreen(OnScreenMessageType severity, string message, 
      float duration = 5.0f, int messageKey = -1) {
    return LogToScreen(message, severity, Color.white, duration, messageKey);
  }

  //! @brief Logs an on-screen message.
  //!
  //! @param message The message text.
  //! @param severity The severity of the message.
  //! @param color The color to display this message on-screen.
  //! @param duration The duration the message is to be displayed on-screen. Durations of less than
  //! zero are treated as infinite.
  //! @param messageKey The key/ID for this message. Only one message with a given key can display
  //!     at a time and will replace existing messages with the same key. If negative then a unique
  //!     key will be chosen automatically.
  //! @returns The key the message was logged with, or a negative number if something went wrong.
  public static int LogToScreen(string message, OnScreenMessageType severity, Color color,
      float duration = 5.0f, int messageKey = -1) {
    HxOnScreenLog instance = Instance;
    return instance.LogToScreenInternal(message, severity, color, duration, messageKey);
  }

  //! @brief The last on screen message key that was handed out.
  private static int _lastOnScreenMessageKeyDontUse = 0;
  //! @brief A getter for the next on screen message key to hand out.
  private static int NextOnScreenMessageKey {
    get {
      return --_lastOnScreenMessageKeyDontUse;
    }
  }

  //! @brief Clears an on-screen message by key.
  //!
  //! @param key The key of the message to clear.
  public static void ClearFromScreen(int key) {
    if (!onScreenMessages.TryGetValue(key, out OnScreenMessage message)) {
      return;
    }
    onScreenMessages.Remove(key);

    if (message.textMesh != null) {
      Destroy(message.textMesh.gameObject);
    }

    int numLines = 0;
    foreach (KeyValuePair<int, OnScreenMessage> pair in onScreenMessages) {
      if (pair.Value.textMesh == null) {
        continue;
      }

      // Draw the message at the correct line in the log.
      pair.Value.textMesh.text = new string('\n', numLines) + pair.Value.message;
      numLines += pair.Value.message.Count(f => f == '\n') + 1;
    }
  }
}
