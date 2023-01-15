// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEditor;
using UnityEngine;
using System.IO;

//! Contains editor-only functionality for HxHand components.
public class HxHandEditor {

  //! Generate a texture encoding the bone weights of a skinned mesh for use in custom HaptX
  //! shaders.
  //!
  //! @param smr The mesh containing the bone weights to encode.
  //! @param outputPath Where to store the generated file.
  //! @returns True if the texture was successfully generated.
  public static bool GenerateBoneWeightTexture(SkinnedMeshRenderer smr, string outputPath) {
    if (smr == null) {
      return false;
    }

    int numBones = smr.bones.Length;
    int numVertices = smr.sharedMesh.boneWeights.Length;

    // We store bone indices and bone weights in a texture since the amount of vertices exceeds the
    // maximum allowed array length in ShaderLab. Vertices are indexed by column and values of
    // interest are indexed by row. The pixels in the first row store in their RGBA channels the
    // bone indices of the four bones that influence their associated vertices. The second row
    // stores the bone weights in a similar fashion.
    Texture2D boneIndicesAndWeights = new Texture2D(numVertices, 2, TextureFormat.RGBA32, false,
        true);
    for (int i = 0; i < numVertices; i++) {
      // We must compress the range of bone indices between 0 and 1 since we're storing them in
      // Colors.
      boneIndicesAndWeights.SetPixel(i, 0, new Color(
          smr.sharedMesh.boneWeights[i].boneIndex0 / (numBones - 1.0f),
          smr.sharedMesh.boneWeights[i].boneIndex1 / (numBones - 1.0f),
          smr.sharedMesh.boneWeights[i].boneIndex2 / (numBones - 1.0f),
          smr.sharedMesh.boneWeights[i].boneIndex3 / (numBones - 1.0f)));

      boneIndicesAndWeights.SetPixel(i, 1, new Color(
          smr.sharedMesh.boneWeights[i].weight0,
          smr.sharedMesh.boneWeights[i].weight1,
          smr.sharedMesh.boneWeights[i].weight2,
          smr.sharedMesh.boneWeights[i].weight3));
    }
    boneIndicesAndWeights.Apply();

    // Write the texture to file.
    File.Delete(outputPath);
    File.WriteAllBytes(outputPath, boneIndicesAndWeights.EncodeToPNG());

    // Make sure the image doesn't get tampered with by Unity's import
    // settings.
    AssetDatabase.ImportAsset(outputPath);
    TextureImporter ti = AssetImporter.GetAtPath(outputPath) as TextureImporter;
    if (ti != null) {
      ti.sRGBTexture = false;
      ti.maxTextureSize = 8192;
      ti.mipmapEnabled = false;
      ti.textureCompression = TextureImporterCompression.Uncompressed;
      ti.crunchedCompression = false;
      ti.compressionQuality = 100;
      ti.npotScale = TextureImporterNPOTScale.None;
      ti.SaveAndReimport();
      return true;
    } else {
      return false;
    }
  }
}
