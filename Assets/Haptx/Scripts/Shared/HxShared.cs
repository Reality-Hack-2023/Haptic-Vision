// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using UnityEngine;

//! Left or right?
public enum RelDir : uint {
  LEFT = 0,  //!< Left.
  RIGHT      //!< Right.
}

//! Associates a key input with possible modifiers (alt, shift, and ctrl).
[System.Serializable]
public class KeyWithModifiers {

  //! The base key. Set to None to disable.
  public KeyCode key = KeyCode.None;

  //! Whether alt also needs to be held down.
  public bool alt = false;

  //! Whether shift also needs to be held down.
  public bool shift = false;

  //! Whether control also needs to be held down.
  public bool control = false;

  //! Default constructor.
  public KeyWithModifiers() { }

  //! Construct using given values.
  //!
  //! @param key See #key.
  //! @param alt See #alt.
  //! @param shift See #shift.
  //! @param control See #control.
  public KeyWithModifiers(KeyCode key, bool alt, bool shift, bool control) {
    this.key = key;
    this.alt = alt;
    this.shift = shift;
    this.control = control;
  }

  //! Whether Input.GetKey() is true and the correct set of modifiers are met.
  //!
  //! @returns True if Input.GetKey() is true and the correct set of modifiers are met.
  public bool GetKey() {
    return Input.GetKey(key) && AreModifiersMet();
  }

  //! Whether Input.GetKeyDown() is true and the correct set of modifiers are met.
  //!
  //! @returns True if Input.GetKeyDown() is true and the correct set of modifiers are met.
  public bool GetKeyDown() {
    return Input.GetKeyDown(key) && AreModifiersMet();
  }

  //! Whether Input.GetKeyUp() is true and all modifiers are met.
  //!
  //! @returns True if Input.GetKeyUp() is true and all modifiers are met.
  public bool GetKeyUp() {
    return Input.GetKeyUp(key) && AreModifiersMet();
  }

  //! Whether the configured set of modifiers is being pressed.
  //!
  //! @returns True if the configured set of modifiers is being pressed.
  bool AreModifiersMet() {
    if (alt) {
      if (!(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) return false;
    }
    if (shift) {
      if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) return false;
    }
    if (control) {
      if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) return false;
    }

    return true;
  }
}

//! Functionality accessible to multiple HaptX classes.
public static class HxShared {

  //! The name of the default HaptX home variable.
  public static string HomeEnvVar = "HAPTX_SDK_HOME";
  //! A fall-back directory if the home directly is not defined in environment.
  public static string FallbackHome = "C:/Program Files/HaptX/SDK";
  //! HaptX brand palette.
  public static Color HxCharcoal = new Color(68.0f / 255, 68.0f / 255, 68.0f / 255);
  //! HaptX brand palette.
  public static Color HxOrange = new Color(1.0f, 121.0f / 255, 0.0f);
  //! HaptX brand palette.
  public static Color HxGreen = new Color(166.0f / 255, 183.0f / 255, 0.0f);
  //! HaptX brand palette.
  public static Color HxTeal = new Color(0.0f, 170.0f / 255, 188.0f / 255);
  //! HaptX brand palette.
  public static Color HxYellow = new Color(242.0f / 255, 161.0f / 255, 0.0f);
  //! @brief Debug color palette. 
  //!
  //! Designed to be color blind friendly. For colors that include an "or" the palette is valid if
  //! the value matches either of the colors.
  public static Color DebugBlack = new Color(0.1f, 0.1f, 0.1f);
  //! @brief Debug color palette. 
  //!
  //! Designed to be color blind friendly. For colors that include an "or" the palette is valid if
  //! the value matches either of the colors.
  public static Color DebugWhite = new Color(0.9f, 0.9f, 0.9f);
  //! @brief Debug color palette. 
  //!
  //! Designed to be color blind friendly. For colors that include an "or" the palette is valid if
  //! the value matches either of the colors.
  public static Color DebugGray = new Color(0.5f, 0.5f, 0.5f);
  //! @brief Debug color palette. 
  //!
  //! Designed to be color blind friendly. For colors that include an "or" the palette is valid if
  //! the value matches either of the colors.
  public static Color DebugPurpleOrTeal = new Color(0.0f, 1.0f, 1.0f);
  //! @brief Debug color palette. 
  //!
  //! Designed to be color blind friendly. For colors that include an "or" the palette is valid if
  //! the value matches either of the colors.
  public static Color DebugRedOrGreen = new Color(1.0f, 0.0f, 0.0f);
  //! @brief Debug color palette. 
  //!
  //! Designed to be color blind friendly. For colors that include an "or" the palette is valid if
  //! the value matches either of the colors.
  public static Color DebugBlueOrYellow = new Color(0.0f, 0.0f, 1.0f);

  //! Multiply to convert radians to degrees.
  public static float RadToDeg = 57.2957795131f;
  //! Multiply to convert revolutions to degrees.
  public static float RevToDeg = 360.0f;
  //! Multiply to convert radians to revolutions.
  public static float RevToRad = 2.0f * Mathf.PI;
  //! Multiply to convert degrees to revolutions.
  public static float DegToRad = 0.0174533f;

  //! Relative direction substitution pattern.
  public static string RelDirPattern = "{relDir}";

  //! Convert a HaptxApi::Vector2D to Unity Vector2.
  //!
  //! @param v2d The vector to convert.
  //! @returns The converted vector.
  public static Vector2 UnityFromHx(HaptxApi.Vector2D v2d) {
    return new Vector2(v2d.x_, v2d.y_);
  }

  //! Convert a Unity Vector2 to HaptxApi::Vector2D.
  //!
  //! @param v2 The vector to convert.
  //! @param [out] v2dOut The converted vector.
  public static void HxFromUnity(Vector2 v2, HaptxApi.Vector2D v2dOut) {
    if (v2dOut == null) {
      return;
    }
    v2dOut.setAll(v2.x, v2.y);
  }

  //! @brief Convert a HaptxApi::Vector3D to Unity Vector3.
  //!
  //! @warning Do not use with angular velocities. Please use UnityFromHxAngularVelocity() instead.
  //! @warning Do not use with scales. Please use UnityFromHxScale() instead.
  //!
  //! The HaptX vector's X component becomes the Unity vector's Z component, aligning their forward
  //! directions.
  //! The HaptX vector's Z component becomes the Unity vector's Y component, aligning their up
  //! directions.
  //! The negative HaptX vector's Y component becomes the Unity vector's X component, 
  //! aligning their right directions and handedness.
  //!
  //! @param v3d The vector to convert.
  //! @returns The converted vector.
  public static Vector3 UnityFromHx(HaptxApi.Vector3D v3d) {
    return new Vector3(-v3d.y_, v3d.z_, v3d.x_);
  }

  //! @brief Convert a Unity Vector3 to HaptxApi::Vector3D.
  //!
  //! The negative Unity vector's X component becomes the HaptX vector's Y component, 
  //! aligning their right directions and handedness.
  //! The Unity vector's Y component becomes the HaptX vector's Z component, aligning their up
  //! directions.
  //! The Unity vector's Z component becomes the HaptX vector's X component, aligning their forward
  //! directions.
  //!
  //! @param v3 The vector to convert.
  //! @param [out] v3dOut The converted vector.
  public static void HxFromUnity(Vector3 v3, HaptxApi.Vector3D v3dOut) {
    if (v3dOut == null) {
      return;
    }
    v3dOut.setAll(v3.z, -v3.x, v3.y);
  }

  //! @brief Convert a HaptxApi::Vector3D scale to Unity Vector3 scale.
  //!
  //! The HaptX scale's X component becomes the Unity scale's Z component, aligning their forward 
  //! directions.
  //! The HaptX scale's Z component becomes the Unity scale's Y component, aligning their up 
  //! directions.
  //! The negative HaptX scale's Y component becomes the Unity scale's X component aligning their 
  //! right directions.
  //!
  //! @param hx_scale The scale to convert.
  //! @returns The converted scale.
  public static Vector3 UnityFromHxScale(HaptxApi.Vector3D hx_scale) {
    return new Vector3(hx_scale.y_, hx_scale.z_, hx_scale.x_);
  }

  //! @brief Convert a Unity Vector3 scale to HaptxApi::Vector3D scale.
  //!
  //! The negative Unity scale's X component becomes the HaptX scale's Y component, aligning their 
  //! right directions.
  //! The Unity scale's Y component becomes the HaptX scale's Z component, aligning their up 
  //! directions.
  //! The Unity scale's Z component becomes the HaptX scale's X component, aligning their forward 
  //! directions.
  //!
  //! @param unity_scale The scale to convert.
  //! @param [out] v3dOut The converted vector.
  public static void HxFromUnityScale(Vector3 unity_scale, HaptxApi.Vector3D v3dOut) {
    if (v3dOut == null) {
      return;
    }
    v3dOut.setAll(unity_scale.z, unity_scale.x, unity_scale.y);
  }

  //! Convert a HaptxApi::Quaternion to a Unity quaternion.
  //!
  //! The HaptX axis's X component becomes the Unity axis's Z component, aligning their forward
  //! directions.
  //! The HaptX axis's Z component becomes the Unity axis's Y component, aligning their up
  //! directions.
  //! The negative HaptX axis's Y component becomes the Unity axis's X component, 
  //! aligning their right directions and handedness.
  //! The angle is negated to reflect the new handedness.
  //!
  //! @param hxq The quaternion to convert.
  //! @returns The converted quaternion.
  public static Quaternion UnityFromHx(HaptxApi.Quaternion hxq) {
    return new Quaternion(-hxq.j_, hxq.k_, hxq.i_, -hxq.r_);
  }

  //! @brief Convert a Unity quaternion to a HaptxApi::Quaternion.
  //!
  //! The negative Unity axis's X component becomes the HaptX axis's Y component, 
  //! aligning their right directions and handedness.
  //! The Unity axis's Y component becomes the HaptX axis's Z component, aligning their up
  //! directions.
  //! The Unity axis's Z component becomes the HaptX axis's X component, aligning their forward
  //! directions.
  //! The angle is negated to reflect the new handedness.
  //!
  //! @param uq The quaternion to convert.
  //! @param [out] qOut The converted quaternion.
  public static void HxFromUnity(Quaternion uq, HaptxApi.Quaternion qOut) {
    if (qOut == null) {
      return;
    }
    qOut.setAll(-uq.w, uq.z, -uq.x, uq.y);
  }

  //! Convert a Unity Matrix4x4 to a HaptxApi.Transform.
  //!
  //! @param um The unity matrix to convert.
  //! @param [out] mOut The converted transform.
  public static void HxFromUnity(Matrix4x4 um, HaptxApi.Transform mOut) {
    if (mOut == null) {
      return;
    }
    mOut.setAllColumnMajor(um[10], -um[8], um[9], -um[2], um[0], -um[1], um[6], -um[4], um[5], um[14],
        -um[12], um[13]);
  }

  //! Convert a Transform to a HaptxApi.Transform.
  //!
  //! @param unityTransform The Transfrom to convert.
  //! @param [out] mOut The converted transform.
  public static void HxFromUnity(Transform unityTransform, HaptxApi.Transform mOut) {
    HxFromUnity(unityTransform.localToWorldMatrix, mOut);
  }

  //! Convert a HaptxApi::Transform to an Matrix4x4.
  //!
  //! @param hx_transform The transform to convert.
  //! @returns The converted Matrix4x4.
  public static Matrix4x4 UnityFromHx(HaptxApi.Transform hx_transform) {
    if (hx_transform == null) {
      return Matrix4x4.identity;
    }

    Matrix4x4 m = Matrix4x4.identity;
    float value = 0.0f;
    hx_transform.getRowColumn(1, 1, ref value);
    m.m00 = value;
    hx_transform.getRowColumn(2, 1, ref value);
    m.m10 = -value;
    hx_transform.getRowColumn(0, 1, ref value);
    m.m20 = -value;
    hx_transform.getRowColumn(1, 2, ref value);
    m.m01 = -value;
    hx_transform.getRowColumn(2, 2, ref value);
    m.m11 = value;
    hx_transform.getRowColumn(0, 2, ref value);
    m.m21 = value;
    hx_transform.getRowColumn(1, 0, ref value);
    m.m02 = -value;
    hx_transform.getRowColumn(2, 0, ref value);
    m.m12 = value;
    hx_transform.getRowColumn(0, 0, ref value);
    m.m22 = value;
    hx_transform.getRowColumn(1, 3, ref value);
    m.m03 = -value;
    hx_transform.getRowColumn(2, 3, ref value);
    m.m13 = value;
    hx_transform.getRowColumn(0, 3, ref value);
    m.m23 = value;
    return m;
  }

  //! Convert a right-handed angular velocity HaptxApi.Vector3D (rotation axis scaled by
  //! angular speed in rad/s) to a left-handed angular velocity Vector3 (rotation axis scaled by
  //! angular speed in rad/s).
  //!
  //! @param hxAngularVelocity The right-handed angular velocity vector to convert.
  //!
  //! @returns The converted left-handed angular velocity vector.
  public static Vector3 UnityFromHxAngularVelocity(HaptxApi.Vector3D hxAngularVelocity) {
    // The negation on the following line is very necessary for angular velocities!
    return -UnityFromHx(hxAngularVelocity);
  }

  //! Convert a left-handed angular velocity Vector3 (rotation axis scaled by angular speed
  //! in rad/s) to a right-handed angular velocity HaptxApi.Vector3D (rotation axis scaled by
  //! angular speed in rad/s).
  //!
  //! @param unityAngularVelocity The left-handed angular velocity vector to convert.
  //! @param [out] haptxAngularVelocityOut The converted left-handed angular velocity vector.
  public static void HxFromUnityAngularVelocity(Vector3 unityAngularVelocity,
      HaptxApi.Vector3D haptxAngularVelocityOut) {
    // The negation on the following line is very necessary for angular velocities!
    HxFromUnity(-unityAngularVelocity, haptxAngularVelocityOut);
  }

  //! Substitutes all instances of RelDirPattern with the given relative direction.
  //!
  //! @param input The string to perform substitutions on.
  //! @param relDir The relative direction to substitute.
  //! @returns A copy of @p input with substitutions in place.
  public static string SubstituteRelDir(string input, RelDir relDir) {
    return input.Replace(RelDirPattern, relDir == RelDir.LEFT ? "LEFT" : "RIGHT");
  }

  //! Recursively looks for a child object in a given transform.
  //!
  //! @param transform The transform to start searching on.
  //! @param name The name of the child object to look for.
  //! @returns The child object (if found).
  public static Transform GetChildGameObjectByName(Transform transform,
      string name) {
    // First check if we have the named GameObject.
    Transform foundTransform = transform.Find(name);
    if (foundTransform) {
      return foundTransform;
    }

    // Then check if one of our children has the named GameObject.
    foreach (Transform child in transform) {
      foundTransform = GetChildGameObjectByName(child, name);

      if (foundTransform) {
        return foundTransform;
      }
    }

    return null;
  }

  // Returns a list of all components found in an object and the entirety of 
  // the hierarchy beneath it.
  public static void AddComponentsInChildrenRecursive<T>(GameObject gameObject,
      ref List<T> components) where T : Component {
    // Add all of the components found on me.
    components.AddRange(gameObject.GetComponents<T>());

    // Add all of the components found in my children recursively.
    foreach (Transform child in gameObject.transform) {
      AddComponentsInChildrenRecursive(child.gameObject, ref components);
    }
  }

  //! @brief Get a list of any components found on the first child to contain at least one 
  //! component from each of @p gameObject's child branches. 
  //!
  //! If multiple descendants at the same scene contain components, then all of those components 
  //! will be added.
  //!
  //! @param gameObject The GameObject whose child branches to search.
  //! @param components The list to populate with discovered components.
  public static void AddNearestChildComponentsRecursive<T>(GameObject gameObject,
      ref List<T> components) where T : Component {
    // Check each child for components, and keep digging if not found.
    foreach (Transform child in gameObject.transform) {
      T[] childComponents = child.GetComponents<T>();
      if (childComponents.Length > 0) {
        components.AddRange(childComponents);
      } else {
        AddNearestChildComponentsRecursive<T>(child.gameObject, ref components);
      }
    }
  }

  //! @brief Populate a list with neighbors of the nearest component.
  //! 
  //! Starting at @p gameObject, find the nearest component in its parent hierarchy. That component
  //! becomes the central node. From the central node, traverse the hierarchy looking for 
  //! the nearest parent and children GameObjects that also contain components.
  //!
  //! @param gameObject The object to start searching on.
  //! @param components The list to populated with discovered neighbors.
  public static void AddNeighboringComponents<T>(GameObject gameObject,
      ref List<T> components) where T : Component {
    // The central node.
    T centralComponent = gameObject.GetComponentInParent<T>();
    if (centralComponent != null) {
      components.Add(centralComponent);

      // The parent node.
      Transform parent = centralComponent.transform.parent;
      if (parent != null) {
        T parentComponent = parent.GetComponentInParent<T>();
        if (parentComponent != null) {
          components.Add(parentComponent);
        }
      }

      // Any children nodes.
      AddNearestChildComponentsRecursive(centralComponent.gameObject, ref components);
    }
  }

  //! @brief Get whether the given layer is in the given mask.
  //!
  //! For an explanation of this logic, see 
  //! http://answers.unity3d.com/questions/50279/check-if-layer-is-in-layermask.html.
  //!
  //! returns True if the given layer is in the given mask. 
  public static bool IsLayerInMask(int layer, LayerMask mask) {
    return mask == (mask | (1 << layer));
  }

  //! Creates a new child game object centered about a transform.
  //!
  //! @param transform The parent transform.
  //! @param name The name of the new object.
  //! @returns The new object.
  public static GameObject CreateChildGameObject(Transform transform, string name = "") {
    GameObject gameObject = new GameObject("");
    gameObject.transform.parent = transform;
    gameObject.transform.localPosition = Vector3.zero;
    gameObject.transform.localRotation = Quaternion.identity;
    gameObject.name = name;

    return gameObject;
  }

  //! @brief Returns the bounds of the given GameObject.
  //!
  //! If @p gameObject has a Renderer its bounds will be used. Otherwise if it has a Collider its
  //! bounds will be used.
  //!
  //! @returns The most relevant bounds of the given object.
  public static Bounds GetGameObjectBounds(GameObject gameObject) {
    if (gameObject != null) {
      Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
      if (renderer != null) {
        return renderer.bounds;
      }

      Collider collider = gameObject.GetComponentInChildren<Collider>();
      if (collider != null) {
        return collider.bounds;
      }
    }

    return new Bounds();
  }

  //! Whether x is between a and b.
  //!
  //! @param x The value of interest.
  //! @param a The lower bound.
  //! @param b The upper bound.
  //! @param inclusive Whether the bounds are inclusive.
  //! @returns Whether x is between a and b.
  public static bool IsBetween(this float x, float a, float b, bool inclusive = true) {
    if (inclusive) {
      return x >= a && x <= b;
    } else {
      return x > a && x < b;
    }
  }

  //! Get the HaptxApi::RelativeDirection matching the RelDir.
  //!
  //! @param inDir The direction of interest.
  //! @returns The matching direction.
  public static HaptxApi.RelativeDirection RelativeDirectionFromRelDir(RelDir inDir) {
    switch (inDir) {
      case RelDir.LEFT: {
          return HaptxApi.RelativeDirection.RD_LEFT;
        }
      case RelDir.RIGHT: {
          return HaptxApi.RelativeDirection.RD_RIGHT;
        }
      default: {
          return HaptxApi.RelativeDirection.RD_LAST;
        }
    }
  }

  //! Sets the layer of @p go and all of its children.
  //!
  //! @param go The game object whose layer to set.
  //! @param layerNumber The layer to set.
  public static void SetLayerRecursively(GameObject go, int layerNumber) {
    if (go == null) return;
    foreach (
        Transform xform in go.GetComponentsInChildren<Transform>(true)) {
      xform.gameObject.layer = layerNumber;
    }
  }

  //! @brief Calculates the mass-normalized kinetic energy of a rigidbody.
  //!
  //! From https://forum.unity.com/threads/how-to-calculate-a-rigidbodys-mass-normalized-energy-for-sleepthreshold.311941/.
  //!
  //! @param rigidbody The rigidbody to use.
  //! @returns The mass-normalized kinetic energy.
  public static float GetMassNormalizedKineticEnergy(Rigidbody rigidbody) {
    if (rigidbody == null || rigidbody.mass == 0.0f) return 0.0f;

    // Linear KE.
    float ke = 0.5f * rigidbody.mass * rigidbody.velocity.sqrMagnitude;

    // Angular KE.
    ke += 0.5f * rigidbody.inertiaTensor.x * Mathf.Pow(rigidbody.angularVelocity.x, 2.0f);
    ke += 0.5f * rigidbody.inertiaTensor.y * Mathf.Pow(rigidbody.angularVelocity.y, 2.0f);
    ke += 0.5f * rigidbody.inertiaTensor.z * Mathf.Pow(rigidbody.angularVelocity.z, 2.0f);

    return ke /= rigidbody.mass;
  }

  //! Produces output from a sine wave given the below parameters and an amplitude of 1.
  //!
  //! @param timeS The time that the wave has been playing for [s].
  //! @param frequencyHz The frequency of the wave [Hz].
  //! @param inputPhaseOffsetDeg The phase offset to give the wave [deg].
  public static float EvaluateSineWave(float timeS, float frequencyHz, float inputPhaseOffsetDeg) {
    /// DON'T LET THIS FUNCTIONALLY DIFFER FROM THE EQUIVALENT UNREAL PLUGIN FUNCTION ///
    float wave_input = timeS * frequencyHz * RevToRad + inputPhaseOffsetDeg * DegToRad;
    // A value between -1 and 1 fitting the timing criteria for the sine wave
    return Mathf.Sin(wave_input);
  }

  //! @brief Disables a given joint.
  //!
  //! ConfigurableJoints don't have a native notion of "enabled" so we that they are disabled when
  //! they are rendered physically inert. This means re-enabling them is tricky because you need
  //! their full state from before they were rendered inert. The ConfigurableJointParameters class
  //! may be helpful here.
  //!
  //! @param joint The joint to disable.
  public static void DisableJoint(ConfigurableJoint joint) {
    if (joint == null) {
      return;
    }
    joint.xMotion = ConfigurableJointMotion.Free;
    joint.yMotion = ConfigurableJointMotion.Free;
    joint.zMotion = ConfigurableJointMotion.Free;
    joint.angularXMotion = ConfigurableJointMotion.Free;
    joint.angularYMotion = ConfigurableJointMotion.Free;
    joint.angularZMotion = ConfigurableJointMotion.Free;
    joint.xDrive = new JointDrive();
    joint.yDrive = new JointDrive();
    joint.zDrive = new JointDrive();
    joint.angularXDrive = new JointDrive();
    joint.angularYZDrive = new JointDrive();
    joint.slerpDrive = new JointDrive();
  }

  //! Returns true if the given joint is enabled.
  //!
  //! @param joint The joint to check.
  //! @returns True if the joint is enabled.
  public static bool IsJointEnabled(ConfigurableJoint joint) {
    if (joint == null) {
      return false;
    } else return 
      joint.xMotion != ConfigurableJointMotion.Free ||
      joint.yMotion != ConfigurableJointMotion.Free ||
      joint.zMotion != ConfigurableJointMotion.Free ||
      joint.angularXMotion != ConfigurableJointMotion.Free ||
      joint.angularYMotion != ConfigurableJointMotion.Free ||
      joint.angularZMotion != ConfigurableJointMotion.Free ||
      joint.xDrive.positionSpring > 0.0f ||
      joint.xDrive.positionDamper > 0.0f ||
      joint.yDrive.positionSpring > 0.0f ||
      joint.yDrive.positionDamper > 0.0f ||
      joint.zDrive.positionSpring > 0.0f ||
      joint.zDrive.positionDamper > 0.0f ||
      joint.angularXDrive.positionSpring > 0.0f ||
      joint.angularXDrive.positionDamper > 0.0f ||
      joint.angularYZDrive.positionSpring > 0.0f ||
      joint.angularYZDrive.positionDamper > 0.0f ||
      joint.slerpDrive.positionSpring > 0.0f ||
      joint.slerpDrive.positionDamper > 0.0f;
  }
}

//! Common math constants.
public static class HxMath {

  //! The square root of two.
  public static float RootTwo = 1.41421356237f;

  //! One over the square root of two.
  public static float OneOverRootTwo = 0.70710678118f;

}
