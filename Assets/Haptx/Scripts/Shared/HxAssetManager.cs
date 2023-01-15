// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using UnityEngine;

//! @brief Manages assets loaded by HaptX classes.
public class HxAssetManager {

  //! @brief A dictionary that stores loaded assets.
  static Dictionary<string, Object> loadedAssets = new Dictionary<string, Object>();

  //! @brief Gets the requested asset by name regardless of whether the game is in Editor or built.
  //! Only works with built games if the asset is directly beneath a Resources folder. If multiple
  //! assets match the name only the first is returned. If the asset doesn't exist, returns null.
  //!
  //! @param name The name of the asset to load.
  //!
  //! @returns The loaded asset, or null if it doesn't exist.
  public static T LoadAsset<T>(string name) where T : Object {
    // First check to see if this asset has already been loaded.
    Object genericAsset = null;
    T asset = null;
    if (loadedAssets.TryGetValue(name, out genericAsset)) {
      asset = genericAsset as T;
      if (asset != null) {
        return asset;
      }
    }

#if UNITY_EDITOR
    // If we're in the Unity Editor use the given name as an AssetDatabase filter.
    string[] guids = UnityEditor.AssetDatabase.FindAssets(name);
    foreach (string guid in guids)
    {
      asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
      if (asset != null)
      {
        break;
      }
    }
#else
    // If we're in a built game look in the Resources folder for an asset of the given name.
    // Note: this is very finicky. If the asset isn't LITERALLY in one of your Resources folders
    // (i.e. GameObject, Prefab, Material, Texture, etc.) you're better off loading the owning
    // GameObject and using GetComponent().
    asset = Resources.Load(name) as T;
#endif

    // If we successfully loaded an asset store it so we don't end up loading it twice.
    if (asset != null) {
      // If loadedAssets already contains this key a few things may have happened:
      // 1. The underlying object has been invalidated.
      // 2. An asset of a different type but with the same name has been loaded.
      // Either way, we can remove the old asset to make way for the new.
      if (loadedAssets.ContainsKey(name)) {
        loadedAssets.Remove(name);
      }
      loadedAssets.Add(name, asset);
    }
    return asset;
  }
}
