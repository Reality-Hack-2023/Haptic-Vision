// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using UnityEngine;

//! A utility class for drawing debug meshes in the scene.
public class HxDebugMesh {

  //! The types of debug mesh that are supported.
  public enum DebugMeshType : uint {
    CUBE_CENTERED,   //!< A cube with its pivot at its center.
    CUBE_GROUNDED,   //!< A cube with its pivot at the center of its -Z face.
    PLANE_CENTERED,  //!< A plane with its pivot at its center.
    PLANE_GROUNDED,  //!< A plane with its pivot at the center of its -Z line.
    SPHERE_CENTERED,  //!< A sphere with its pivot at its center.

    FIRST_VALUE = CUBE_CENTERED, //!< Value of the first DebugMeshType
    LAST_VALUE = SPHERE_CENTERED //!< Value of the last DebugMeshType
  }

  //! The types of debug material that are supported.
  public enum MaterialType : int {
    OPAQUE,    //!< An opaque material.
    WIREFRAME  //!< A wireframe material.
  }

  //! The path to the material used to draw opaque debug meshes.
  private const string MATERIAL_PATH_OPAQUE = "DontModify_HxDebugMaterialOpaque";
  //! The path to the material used to draw wireframe debug meshes.
  private const string MATERIAL_PATH_WIREFRAME = "DontModify_HxDebugMaterialWireframe";

  // Comes from Graphics.DrawMeshInstanced
  // See https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html
  private const int MAX_INSTANCES_PER_BATCH = 1023;

  //! The data for a batch of instances of a mesh to be drawn.
  private class DrawBatchData {
    public Mesh Mesh { get; }
    public MaterialType MaterialType { get; }
    public DrawBatchData(Mesh mesh, MaterialType materialType) {
      Mesh = mesh;
      MaterialType = materialType;
    }

    public List<Matrix4x4> matrices = new List<Matrix4x4>();
    public Vector4[] colors = new Vector4[MAX_INSTANCES_PER_BATCH];
  }

  //! Contains data needed to render using a particular material.
  private class MaterialData {
    //! The material.
    public Material Material { get; }
    //! The MaterialPropertyBlock used to render the material.
    public MaterialPropertyBlock PropertyBlock { get; }

    //! Constructor.
    public MaterialData(Material material) {
      Material = material;
      PropertyBlock = new MaterialPropertyBlock();
    }
  }

  //! The list of batches to draw.
  private static List<DrawBatchData> _DrawBatches = new List<DrawBatchData>();

  //! Material data to draw using our opaque material.
  private static MaterialData _MaterialDataOpaque;
  //! Material data to draw using our wireframe material.
  private static MaterialData _MaterialDataWireframe;

  //! The shader id for the opaque color property.
  private static readonly int _OpaqueShaderColorProperty = Shader.PropertyToID("_Color");
  //! The shader id for the wireframe color property.
  private static readonly int _WireframeShaderColorProperty = Shader.PropertyToID("_WireColor");

  //! Cached Mesh for each mesh type.
  private static readonly Mesh[] _CachedDebugMeshes = new Mesh[
      Enum.GetValues(typeof(DebugMeshType)).Length];

  //! Returns the material data used for debug draw calls.
  //!
  //! @param type Which type of material to get.
  //! @returns The material data used for debug draw calls.
  private static MaterialData GetDebugMaterialData(MaterialType type) {
    switch (type) {
      case MaterialType.OPAQUE:
        if (_MaterialDataOpaque == null) {
          Material mat = Resources.Load<Material>(MATERIAL_PATH_OPAQUE);
          if (mat != null) {
            _MaterialDataOpaque = new MaterialData(mat);
          } else {
            HxDebug.LogError($"Failed to find debug material: {MATERIAL_PATH_OPAQUE}");
          }
        }
        return _MaterialDataOpaque;
      case MaterialType.WIREFRAME:
        if (_MaterialDataWireframe == null) {
          Material mat = Resources.Load<Material>(MATERIAL_PATH_WIREFRAME);
          if (mat != null) {
            _MaterialDataWireframe = new MaterialData(mat);
          } else {
            HxDebug.LogError($"Failed to find debug material: {MATERIAL_PATH_WIREFRAME}");
          }
        }
        return _MaterialDataWireframe;
      default:
        return null;
    }
  }

  //! Gets the shader color property id for a type of material.
  //!
  //! @param material The MaterialType to get the property id for.
  //! @returns The shader property id of the color property for the material.
  private static int GetShaderColorPropertyId(MaterialType material) {
    switch (material) {
      case MaterialType.OPAQUE:
        return _OpaqueShaderColorProperty;
      case MaterialType.WIREFRAME:
        return _WireframeShaderColorProperty;
    }

    return -1;
  }

  //! Draws a coordinate frame.
  //!
  //! @param pos The position to draw at.
  //! @param rot The rotation to draw at.
  //! @param length The lengths of the axes.
  //! @param thickness The thicknesses of the axes.
  //! @param persistent Whether the draw persists for more than one frame.
  //! @param material Which material to draw the mesh with.
  //! @returns If @p persistent is true, returns an action to stop drawing. Otherwise, returns 
  //! null.
  public static Action DrawCoordinateFrame(
      Vector3 pos, Quaternion rot, float length, float thickness, bool persistent = false,
      MaterialType material = MaterialType.OPAQUE) {
    Mesh mesh = GetDebugMeshAsset(DebugMeshType.CUBE_GROUNDED);
    Vector3 scale = new Vector3(thickness, thickness, length);
    Matrix4x4 xTransform = Matrix4x4.TRS(
        pos,
        (Quaternion.AngleAxis(90.0f, rot * Vector3.up) * rot),
        scale);
    Matrix4x4 yTransform = Matrix4x4.TRS(
        pos,
        (Quaternion.AngleAxis(-90.0f, rot * Vector3.right) * rot),
        scale);
    Matrix4x4 zTransform = Matrix4x4.TRS(pos, rot, scale);

    Action stopDrawingActionX = DrawMesh(mesh, xTransform, Color.red, persistent, material);
    Action stopDrawingActionY = DrawMesh(mesh, yTransform, Color.green, persistent, material);
    Action stopDrawingActionZ = DrawMesh(mesh, zTransform, Color.blue, persistent, material);
    return () => {
      if (stopDrawingActionX != null) {
        stopDrawingActionX();
      }
      if (stopDrawingActionY != null) {
        stopDrawingActionY();
      }
      if (stopDrawingActionZ != null) {
        stopDrawingActionZ();
      }
    };
  }

  //! @brief Draws a line.
  //!
  //! Rendered as a stretched out cube.
  //!
  //! @param startPos One end of the line.
  //! @param endPos The other end of the line.
  //! @param thickness The thickness of the line.
  //! @param color The color to draw with.
  //! @param persistent Whether the draw persists for more than one frame.
  //! @param material Which material to draw the mesh with.
  //! @returns If @p persistent is true, returns an action to stop drawing. Otherwise, returns 
  //! null.
  public static Action DrawLine(Vector3 startPos, Vector3 endPos, float thickness, Color color,
    bool persistent = false, MaterialType material = MaterialType.OPAQUE) {
    Mesh mesh = GetDebugMeshAsset(DebugMeshType.CUBE_GROUNDED);

    Vector3 distance = endPos - startPos;
    if (distance.magnitude > 0.001f && thickness > 0.0f) {
      Vector3 scale = new Vector3(thickness, thickness, distance.magnitude);
      Matrix4x4 transform = Matrix4x4.TRS(startPos, Quaternion.LookRotation(distance), scale);

      return DrawMesh(mesh, transform, color, persistent, material);
    }

    return null;
  }

  //! Draws a cube.
  //!
  //! @param pos The position to draw at.
  //! @param rot The rotation to draw at.
  //! @param scale The scale to draw with.
  //! @param color The color to draw with.
  //! @param grounded If true, the center of the -Z face of the cube will be considered the pivot.
  //! @param persistent Whether the draw persists for more than one frame.
  //! @param material Which material to draw the mesh with.
  //! @returns If @p persistent is true, returns an action to stop drawing. Otherwise, returns 
  //! null.
  public static Action DrawCube(Vector3 pos, Quaternion rot, Vector3 scale, Color color,
      bool grounded = false, bool persistent = false,
      MaterialType material = MaterialType.OPAQUE) {
    Mesh cubeMesh;
    if (grounded) {
      cubeMesh = GetDebugMeshAsset(DebugMeshType.CUBE_GROUNDED);
    } else {
      cubeMesh = GetDebugMeshAsset(DebugMeshType.CUBE_CENTERED);
    }

    return DrawMesh(cubeMesh, Matrix4x4.TRS(pos, rot, scale), color,
        persistent, material);
  }

  //! Draws a sphere.
  //!
  //! @param pos The position to draw at.
  //! @param rot The rotation to draw at.
  //! @param scale The scale to draw with.
  //! @param color The color to draw with.
  //! @param persistent Whether the draw persists for more than one frame.
  //! @param material Which material to draw the mesh with.
  //! @returns If @p persistent is true, returns an action to stop drawing. Otherwise, returns 
  //! null.
  public static Action DrawSphere(Vector3 pos, Quaternion rot, Vector3 scale, Color color,
      bool persistent = false, MaterialType material = MaterialType.OPAQUE) {
    Mesh sphereMesh = GetDebugMeshAsset(DebugMeshType.SPHERE_CENTERED);

    return DrawMesh(sphereMesh, Matrix4x4.TRS(pos, rot, scale), color,
        persistent, material);
  }

  //! Draws an arrow.
  //!
  //! @param tailPos The precise location of the arrow's tail.
  //! @param headPos The precise location of the arrow's head.
  //! @param normal The cross product of the arrow's wings gets oriented in this direction.
  //! @param thickness The thickness of the arrow.
  //! @param color The color to draw with.
  //! @param persistent Whether the draw persists for more than one frame.
  //! @param material Which material to draw the mesh with.
  //! @returns If @p persistent is true, returns an action to stop drawing. Otherwise, returns 
  //! null.
  public static Action DrawArrow(Vector3 tailPos, Vector3 headPos, Vector3 normal, float thickness,
      Color color, bool persistent = false, MaterialType material = MaterialType.OPAQUE) {
    float arrowLength = (tailPos - headPos).magnitude;
    if (arrowLength < Mathf.Epsilon) {
      return null;
    }
    normal.Normalize();

    Vector3 forward = (headPos - tailPos).normalized;
    Quaternion q = Quaternion.LookRotation(forward, normal);
    Vector3 right = q * Vector3.right;
    Vector3 up = q * Vector3.up;

    // Draw the body.
    Vector3 tailEndPos = headPos - (HxMath.RootTwo - 0.5f) * thickness * forward;
    float tailLength = (tailEndPos - tailPos).magnitude;
    Action stopDrawingBodyAction = DrawCube(
        0.5f * (tailPos + tailEndPos), q,
        new Vector3(thickness, thickness, tailLength),
        color, false, persistent, material);

    // Draw the wings.
    float wingLength = 0.33f * arrowLength;
    Vector3 leftWingForward = (forward + right).normalized;
    Vector3 rightWingForward = (forward - right).normalized;
    Vector3 leftWingEndPos = headPos - 0.5f * thickness * rightWingForward;
    Vector3 leftWingStartPos = leftWingEndPos - wingLength * leftWingForward;
    Quaternion leftWingRot = Quaternion.LookRotation(leftWingForward, up);
    Vector3 rightWingEndPos = headPos - 0.5f * thickness * leftWingForward;
    Vector3 rightWingStartPos = rightWingEndPos - wingLength * rightWingForward;
    Quaternion rightWingRot = Quaternion.LookRotation(rightWingForward, up);
    Action stopDrawingLeftWingAction = DrawCube(
        0.5f * (leftWingStartPos + leftWingEndPos), leftWingRot,
        new Vector3(thickness, thickness, wingLength),
        color, false, persistent, material);
    Action stopDrawingRightWingAction = DrawCube(
        0.5f * (rightWingStartPos + rightWingEndPos), rightWingRot,
        new Vector3(thickness, thickness, wingLength),
        color, false, persistent, material);

    return () => {
      if (stopDrawingBodyAction != null) {
        stopDrawingBodyAction();
      }
      if (stopDrawingLeftWingAction != null) {
        stopDrawingLeftWingAction();
      }
      if (stopDrawingRightWingAction != null) {
        stopDrawingRightWingAction();
      }
    };
  }

  //! Renders draws queued by calls to Draw[Shape] functions.
  public static void RenderQueuedDraws() {
    foreach (DrawBatchData drawBatchData in _DrawBatches) {

      if (drawBatchData.matrices.Count == 0) {
        continue;
      }

      MaterialData materialData = GetDebugMaterialData(drawBatchData.MaterialType);
      if (materialData.Material.enableInstancing) {
        RenderInstancedBatch(drawBatchData, materialData);
      } else {
        RenderNonInstancedBatch(drawBatchData, materialData);
      }

      drawBatchData.matrices.Clear();
    }
  }

  //! Renders a batch using instanced drawing.
  //!
  //! @param drawBatchData The data for the batch to render.
  //! @param materialData The data for the material used to render.
  private static void RenderInstancedBatch(DrawBatchData drawBatchData, MaterialData materialData) {
    materialData.PropertyBlock.SetVectorArray(
        GetShaderColorPropertyId(drawBatchData.MaterialType), drawBatchData.colors);

    Graphics.DrawMeshInstanced(drawBatchData.Mesh, submeshIndex: 0, materialData.Material, 
        drawBatchData.matrices, materialData.PropertyBlock);
  }

  //! Renders a batch by making an individual draw call for each instance.
  //!
  //! @param drawBatchData The data for the batch to render.
  //! @param materialData The data for the material used to render.
  private static void RenderNonInstancedBatch(DrawBatchData drawBatchData, MaterialData materialData) {
    for (int i = 0; i < drawBatchData.matrices.Count; ++i) {
      materialData.PropertyBlock.SetColor(
          GetShaderColorPropertyId(drawBatchData.MaterialType), drawBatchData.colors[i]);

      Graphics.DrawMesh(drawBatchData.Mesh, drawBatchData.matrices[i], materialData.Material, 
          layer: 0, camera: null, submeshIndex: 0, materialData.PropertyBlock);
    }
  }
  
  //! Wraps Graphics.DrawMesh().
  //!
  //! @param mesh The mesh to draw.
  //! @param trs The transform to draw with.
  //! @param color The color to draw with.
  //! @param material Which material to draw the mesh with.
  private static void DrawMesh(Mesh mesh, Matrix4x4 trs, Color color, MaterialType material) {
    DrawBatchData drawData = null;
    for (int i = 0; i < _DrawBatches.Count; ++i) {
      DrawBatchData data = _DrawBatches[i];

      if (data.Mesh == mesh &&
          data.MaterialType == material &&
          data.matrices.Count < MAX_INSTANCES_PER_BATCH) {
        drawData = data;

        // Optimization: swap this one to the front of the list.
        // (We're likely to check for it again soon)
        DrawBatchData temp = _DrawBatches[0];
        _DrawBatches[0] = _DrawBatches[i];
        _DrawBatches[i] = temp;
        break;
      }
    }

    if (drawData == null) {
      drawData = new DrawBatchData(mesh, material);
      _DrawBatches.Add(drawData);
    }

    drawData.matrices.Add(trs);
    Vector4 vColor = QualitySettings.activeColorSpace == ColorSpace.Linear ? 
        color.linear : color.gamma;
    drawData.colors[drawData.matrices.Count - 1] = vColor;
  }

  //! Draws a mesh for at least one frame.
  //!
  //! @param mesh The mesh to draw.
  //! @param trs The transform to draw with.
  //! @param color The color to draw with.
  //! @param persistent Whether the draw persists for more than one frame.
  //! @param material Which material to draw the mesh with.
  //! @returns If @p persistent is true, returns an action to stop drawing. Otherwise, returns 
  //! null.
  private static Action DrawMesh(Mesh mesh, Matrix4x4 trs, Color color, bool persistent,
      MaterialType material) {
    if (persistent && Application.isPlaying) {
      // Subtlety: This is split into its own function because making the required lambdas
      // inline here would cause lambda garbage to be generated even if we don't make it into
      // this if block. See this sharplab example:
      // https://sharplab.io/#v2:CYLg1APgAgTAjAWAFBQAwAIpwCwG5nJQDMmM6AwugN7q3rJ2YlYBsm26A8gC4AWApgCcoAVgAUASwB23dBICUVBozpYAdFgCck+fiSMAvstrGmmOGyxkAMgEMAtgCNgtgAq3BD/tyEBnAJJSnACuPoIAygDGAPYADvySMnIANOiO0dEANuj2tgDW/IqmjBIAZuhiuQXy1MUqtFbotpHcEtFS6AC8FTWdAHxcfEKiOnr19VAA7E0tbVJj4wb8mb78SvrjjFPoUsGZmQv1Rhu0x4ymxOaWcDYOzm4eXmGBgVJCUXEJ0rISqelZOXyhXWmzKFSqhVqJ02ciSACsunJDjDGs1Wu1EWJegMeAJhOI4bo6hNpmi5sjDMtViCYQ1prt9hS6GdmaYLswLOZbk4XABZWzAfiBACiAA9uJ5ROipIkfn8MtkIUVoSVypUgTUabTtmKJbYpXNRsT0EsVmtjVt6XsDsaWac2dDYoIJAA3Ww+K5c9C6yUiaWyuSa42o2YY7pYro4ob4o0q1Sk0PzUzHAxAA===
      return DrawMeshPersistent(mesh, trs, color, material);
    } else {
      DrawMesh(mesh, trs, color, material);
      return null;
    }
  }

  //! Draws a mesh until the returned action is called.
  //!
  //! @param mesh The mesh to draw.
  //! @param trs The transform to draw with.
  //! @param color The color to draw with.
  //! @param material Which material to draw the mesh with.
  //! @returns an action to stop drawing.
  private static Action DrawMeshPersistent(Mesh mesh, Matrix4x4 trs, Color color, 
      MaterialType material) {
    PersistentMeshDrawer.Notify();
    Action drawMesh = () => DrawMesh(mesh, trs, color, material);
    _PersistentDrawMeshActions.Add(drawMesh);
    return () => _PersistentDrawMeshActions.Remove(drawMesh);
  }

  //! Gets a debug mesh (included with the HaptX plugin). 
  //!
  //! @param meshType Which mesh to get.
  //! @returns The requested debug mesh.
  public static Mesh GetDebugMeshAsset(DebugMeshType meshType) {
    Debug.Assert(meshType >= DebugMeshType.FIRST_VALUE && meshType <= DebugMeshType.LAST_VALUE);
    Mesh importedMesh = _CachedDebugMeshes[(int)meshType];
    if (importedMesh == null) {
      GameObject importedGameObject = null;
      switch (meshType) {
        case DebugMeshType.CUBE_CENTERED:
          importedGameObject =
              HxAssetManager.LoadAsset<GameObject>("DontModify_HxDebugMesh_CubeCentered");
          break;
        case DebugMeshType.CUBE_GROUNDED:
          importedGameObject =
              HxAssetManager.LoadAsset<GameObject>("DontModify_HxDebugMesh_CubeGrounded");
          break;
        case DebugMeshType.PLANE_CENTERED:
          importedGameObject =
              HxAssetManager.LoadAsset<GameObject>("DontModify_HxDebugMesh_PlaneCentered");
          break;
        case DebugMeshType.PLANE_GROUNDED:
          importedGameObject =
              HxAssetManager.LoadAsset<GameObject>("DontModify_HxDebugMesh_PlaneGrounded");
          break;
        case DebugMeshType.SPHERE_CENTERED:
          importedGameObject =
              HxAssetManager.LoadAsset<GameObject>("DontModify_HxDebugMesh_SphereCentered");
          break;
      }

      importedMesh = importedGameObject?.GetComponent<MeshFilter>()?.sharedMesh;
      _CachedDebugMeshes[(int)meshType] = importedMesh;
    }

    if (importedMesh == null) {
      Debug.LogError(string.Format(
          "Failed to find debug mesh {0}. It has likely been modified or removed. Any dependent debug visualizers will not work.",
          meshType.ToString()));
      return null;
    }
    return importedMesh;
  }

  //! A list of all DrawMesh() calls that should be made every visual frame.
  private static List<Action> _PersistentDrawMeshActions = new List<Action>();

  //! A singleton class that gets automatically created whenever an HxDebugMesh draw call is
  //! made with the persistent flag.
  private class PersistentMeshDrawer : MonoBehaviour {

    //! See #Instance.
    private static PersistentMeshDrawer _Instance = null;

    //! @brief Get the singleton instance. 
    //!
    //! Creates the instance if it doesn't already exist.
    public static PersistentMeshDrawer Instance {
      get {
        if (_Instance == null && Application.isPlaying) {
          GameObject gameObject = new GameObject("HxDebugMesh.PersistentMeshDrawer") {
            hideFlags = HideFlags.HideAndDontSave
          };
          _Instance = gameObject.AddComponent<PersistentMeshDrawer>();
        }
        return _Instance;
      }
    }

    //! @brief Called when the script is being loaded.
    //!
    //! Enforces singleton-like behavior.
    private void Awake() {
      if (_Instance != null && _Instance != this) {
        Destroy(gameObject);
      } else {
        _Instance = this;
      }
    }

    //! @brief Called every frame if enabled.
    //!
    //! Performs all persistent HxDebugMesh draw calls.
    public void Update() {
      foreach (Action drawMeshAction in _PersistentDrawMeshActions) {
        drawMeshAction();
      }
    }

    //! Notify that a new persistent mesh call has been made.
    public static void Notify() {
      PersistentMeshDrawer instance = Instance;
    }
  }
}
