// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;
using UnityEngine;

//! An editor window for global HaptX settings.
public class HxGlobalSettingsWindow : EditorWindow {

  //! A reference to serialized HxDebug settings.
  private static SerializedObject _serializedHxDebug = null;

  //! A reference to serialized HxOnScreenLog settings.
  private static SerializedObject _serializedOnScreenLog = null;

  //! The fallback directory to store serialized settings in.
  private static string _fallbackResourceDirectory = "Assets/Haptx/Resources";

  //! Called when the window is opened.
  [MenuItem("Window/HaptX")]
  private static void Init() {
    HxGlobalSettingsWindow window = (HxGlobalSettingsWindow)GetWindow(
        typeof(HxGlobalSettingsWindow), false, "HaptX");
    window.Show();
  }

  //! Draw custom inspector.
  private void OnGUI() {
    if (_serializedHxDebug == null) {
      if (HxDebug.Serialized == null) {
        HxDebug.Log(string.Format("Creating a serialized HxDebug at {0}.",
            _fallbackResourceDirectory));
        HxDebug asset = CreateInstance<HxDebug>();
        AssetDatabase.CreateAsset(asset, string.Format("{0}/{1}.asset", _fallbackResourceDirectory,
            HxDebug.serializedSingletonName));
        AssetDatabase.SaveAssets();
      }
      _serializedHxDebug = new SerializedObject(HxDebug.Serialized);
    }

    if (_serializedHxDebug != null) {
      EditorGUILayout.LabelField("Errors and Warnings", EditorStyles.boldLabel);
      HxGUILayout.SerializedFieldLayout(_serializedHxDebug,
          "_displayOnScreenWarningAndErrorMessages");
      HxGUILayout.SerializedFieldLayout(_serializedHxDebug, "_warningMessageDuration");
      HxGUILayout.SerializedFieldLayout(_serializedHxDebug, "_errorMessageDuration");
      EditorGUILayout.Space();

      EditorGUILayout.LabelField("Haptic Primitives", EditorStyles.boldLabel);
      HxGUILayout.SerializedFieldLayout(_serializedHxDebug, "_indicateRigidbodiesAwake");
      EditorGUILayout.Space();
    } else {
      EditorGUILayout.LabelField(
          "Failed to load serialized HxDebug resource. Please let HaptX support know that this has occurred.");
    }

    if (_serializedOnScreenLog == null) {
      if (HxOnScreenLog.Serialized == null) {
        HxDebug.Log(string.Format("Creating a serialized HxOnScreenLog at {0}.",
            _fallbackResourceDirectory));
        GameObject gameObject = new GameObject();
        gameObject.AddComponent<HxOnScreenLog>();
        PrefabUtility.SaveAsPrefabAsset(gameObject,
            string.Format("{0}/{1}.prefab", _fallbackResourceDirectory,
            HxOnScreenLog.serializedSingletonName));
        DestroyImmediate(gameObject);
        AssetDatabase.SaveAssets();
      }
      _serializedOnScreenLog = new SerializedObject(HxOnScreenLog.Serialized);
    }

    if (_serializedOnScreenLog != null) {
      EditorGUILayout.LabelField("On-Screen Log", EditorStyles.boldLabel);
      HxGUILayout.SerializedFieldLayout(_serializedOnScreenLog, "_displayOnScreenMessages");
      HxGUILayout.SerializedFieldLayout(_serializedOnScreenLog, "_minSeverity");
      HxGUILayout.SerializedFieldLayout(_serializedOnScreenLog, "_characterSize");
      HxGUILayout.SerializedFieldLayout(_serializedOnScreenLog, "_fontSize");
      HxGUILayout.SerializedFieldLayout(_serializedOnScreenLog, "_maxLineLength");
      HxGUILayout.SerializedFieldLayout(_serializedOnScreenLog, "_topMargin");
      HxGUILayout.SerializedFieldLayout(_serializedOnScreenLog, "_leftMargin");
    } else {
      EditorGUILayout.LabelField(
          "Failed to load serialized HxOnScreenLog resource. Please let HaptX support know that this has occurred.");
    }

    EditorGUILayout.LabelField("Displacement Visualizer", EditorStyles.boldLabel);
    if (GUILayout.Button("Generate Bone Weight Texture")) {
      string meshPath = EditorUtility.OpenFilePanel("Select the mesh",
          "Assets/Haptx/HaptxHand/Meshes", string.Empty);
      if (meshPath == string.Empty) {
        return;
      } else if (!meshPath.StartsWith(Application.dataPath)) {
        Debug.LogError("The mesh must be inside your Assets folder.", this);
        return;
      } else {
        meshPath = "Assets" + meshPath.Substring(Application.dataPath.Length);
      }

      GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
      if (gameObject != null) {
        SkinnedMeshRenderer smr = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null) {
          Debug.LogError(string.Format("{0} does not have a SkinnedMeshRenderer.", meshPath),
              this);
          return;
        }

        string texturePath = string.Format(
            "Assets/Haptx/Resources/{0}{1}.png", smr.sharedMesh.name,
            HxHand.BoneWeightsFileSuffix);
        if (HxHandEditor.GenerateBoneWeightTexture(smr, texturePath)) {
          Debug.Log(string.Format("Created {0}.", texturePath), this);
        } else {
          Debug.LogError(string.Format("Failed to create {0}.", texturePath), this);
          return;
        }
      } else {
        Debug.LogError(string.Format("Failed to load {0} from AssetDatabase.", meshPath), this);
        return;
      }
    }
  }

  //! Called when the window is closed.
  private void OnDestroy() {
    _serializedHxDebug = null;
    _serializedOnScreenLog = null;
  }
}
