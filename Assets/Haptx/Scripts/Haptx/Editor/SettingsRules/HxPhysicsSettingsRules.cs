// Copyright (C) 2022 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using UnityEditor;

//! Deals with settings rules related to Unity's physics settings.
static internal class HxPhysicsSettingsRules {

  private const string PHYSICS_PREFERENCES_ASSET_PATH = "ProjectSettings/DynamicsManager.asset";

  //! Gets the rules dealing with Physics Settings.
  internal static IEnumerable<IHxRecommendedSettingsRule> GetRules() {
    // Enable Enhanced Determinism
    const bool RECOMMENDED_ENABLE_ENHANCED_DETERMINISM = true;
    const string ENHANCED_DETERMINISM_PROPERTY_PATH = "m_EnableEnhancedDeterminism";
    yield return new HxRelayRecommendedSettingsRule(
      () => GetSerializedPhysicsProperty(ENHANCED_DETERMINISM_PROPERTY_PATH, out _, out _)
          .boolValue == RECOMMENDED_ENABLE_ENHANCED_DETERMINISM,
      () => {
        SerializedProperty enabledProperty = GetSerializedPhysicsProperty(
            ENHANCED_DETERMINISM_PROPERTY_PATH, out UnityEngine.Object physicsManager, 
            out SerializedObject serialized);

        Undo.RecordObject(physicsManager, "Enabled enhanced physics determinism");

        enabledProperty.boolValue = RECOMMENDED_ENABLE_ENHANCED_DETERMINISM;
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(physicsManager);
      },
      "Enhanced physics determinism is not enabled.",
      "Enable enhanced physics determinism.",
      "haptx.physics.enableenhanceddeterminism");
  }

  //! @brief Gets a serialized property from the physics manager.
  //!
  //! @param propertyPath The path of the property to get.
  //! @param [out] physicsManager The physics manager as a Unity Object.
  //! @param [out] serializedManager The physics manager as a SeralizedObject.
  //! @returns The serialized property.
  private static SerializedProperty GetSerializedPhysicsProperty(
      string propertyPath, out UnityEngine.Object physicsManager, 
      out SerializedObject serializedManager) {

    physicsManager = AssetDatabase.LoadMainAssetAtPath(PHYSICS_PREFERENCES_ASSET_PATH);
    serializedManager = new SerializedObject(physicsManager);

    return serializedManager.FindProperty(propertyPath);
  }
}
