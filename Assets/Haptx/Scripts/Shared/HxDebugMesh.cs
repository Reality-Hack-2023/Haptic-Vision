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
    SPHERE_CENTERED  //!< A sphere with its pivot at its center.
  }

  //! The types of debug material that are supported.
  public enum MaterialType : int {
    OPAQUE,    //!< An opaque material.
    WIREFRAME  //!< A wireframe material.
  }

  //! An instance of the material used to draw opaque debug meshes.
  private static Material _MatUnlitSolidColorOpaque = null;

  //! An instance of the material used to draw wireframe debug meshes.
  private static Material _MatUnlitSolidColorWireframe = null;

  //! The material property block for all opaque debug draw calls. Gets copied internally within 
  //! each call.
  private static MaterialPropertyBlock _MaterialPropertyBlockOpaque = new MaterialPropertyBlock();

  //! The material property block for all wireframe debug draw calls. Gets copied internally within 
  //! each call.
  private static MaterialPropertyBlock _MaterialPropertyBlockWireframe = new MaterialPropertyBlock();

  //! The path to the shader used to draw opaque debug meshes.
  private static string _ShaderPathOpaque = "HaptX/DontModify_HxDebugMesh_UnlitSolidColor";

  //! The path to the shader used to draw wireframe debug meshes.
  private static string _ShaderPathWireframe = "SuperSystems/Wireframe-Transparent";

  //! Returns the material used for debug draw calls.
  //!
  //! @param type Which type of material to get.
  //! @returns The material used for debug draw calls.
  private static Material GetDebugMaterial(MaterialType type) {
    switch (type) {
      case MaterialType.OPAQUE:
        if (_MatUnlitSolidColorOpaque == null) {
          Shader shader = Shader.Find(_ShaderPathOpaque);
          if (shader != null) {
            _MatUnlitSolidColorOpaque = new Material(shader);
          } else {
            HxDebug.LogError(string.Format("Failed to find debug shader: {0}", _ShaderPathOpaque));
          }
        }
        return _MatUnlitSolidColorOpaque;
      case MaterialType.WIREFRAME:
        if (_MatUnlitSolidColorWireframe == null) {
          Shader shader = Shader.Find(_ShaderPathWireframe);
          if (shader != null) {
            _MatUnlitSolidColorWireframe = new Material(shader);
          } else {
            HxDebug.LogError(string.Format("Failed to find debug shader: {0}", _ShaderPathWireframe));
          }
        }
        return _MatUnlitSolidColorWireframe;
      default:
        return null;
    }
  }

  //! Get the material property block but with the given color.
  //!
  //! @param material Which material type to get the property block for.
  //! @param color The color of interest.
  //! @returns The material property block but with the given color.
  private static MaterialPropertyBlock GetMaterialPropertyBlock(MaterialType material, Color color) {
    switch (material) {
      case MaterialType.OPAQUE:
        _MaterialPropertyBlockOpaque.SetColor("_Color", color);
        return _MaterialPropertyBlockOpaque;
      case MaterialType.WIREFRAME:
        _MaterialPropertyBlockWireframe.SetColor("_WireColor", color);
        return _MaterialPropertyBlockWireframe;
      default:
        return null;
    }
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

  //! Wraps Graphics.DrawMesh().
  //!
  //! @param mesh The mesh to draw.
  //! @param trs The transform to draw with.
  //! @param color The color to draw with.
  //! @param material Which material to draw the mesh with.
  private static void DrawMesh(Mesh mesh, Matrix4x4 trs, Color color, MaterialType material) {
    Graphics.DrawMesh(mesh, trs, GetDebugMaterial(material), 0, null, 0,
        GetMaterialPropertyBlock(material, color));
  }

  //! Draws a mesh for at least one frame.
  //!
  //! @param mesh The mesh to draw.
  //! @param trs The transform to draw with.
  //! @param color The color to draw with.
  //! @param persistent Whether the draw persists for more than one frame.
  //! @returns If @p persistent is true, returns an action to stop drawing. Otherwise, returns 
  //! null.
  //! @param material Which material to draw the mesh with.
  private static Action DrawMesh(Mesh mesh, Matrix4x4 trs, Color color, bool persistent,
      MaterialType material) {
    if (persistent && Application.isPlaying) {
      PersistentMeshDrawer.Notify();
      Action drawMesh = () => DrawMesh(mesh, trs, color, material);
      _PersistentDrawMeshActions.Add(drawMesh);
      return () => _PersistentDrawMeshActions.Remove(drawMesh);
    } else {
      DrawMesh(mesh, trs, color, material);
      return null;
    }
  }

  //! Gets a debug mesh (included with the HaptX plugin). 
  //!
  //! @param meshType Which mesh to get.
  //! @returns The requested debug mesh.
  public static Mesh GetDebugMeshAsset(DebugMeshType meshType) {
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

    if (importedGameObject == null) {
      Debug.LogError(string.Format(
          "Failed to find debug mesh {0}. It has likely been modified or removed. Any dependent debug visualizers will not work.",
          meshType.ToString()));
      return null;
    }
    return importedGameObject.GetComponent<MeshFilter>().sharedMesh;
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
