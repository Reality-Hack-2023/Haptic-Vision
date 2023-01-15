// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//! @brief Responsible for finely tuning the player's starting location and rotation in the game 
//! world.
//!
//! @ingroup group_unity_plugin
public class HxOffsetTuner : NetworkBehaviour {
  //! An enumeration for the coordinate frame that this player translates with respect to.
  public enum TranslationFrame {
    GLOBAL,    //!< World space
    LOCAL,     //!< Local space
    CAMERA     //!< Camera space
  }

  //! An enumeration for the coordinate frame that this player rotates with respect to.
  public enum RotationFrame {
    GLOBAL,    //!< World space
    LOCAL      //!< Local space
  }

  //! The coordinate frame the player translates in.
  [Tooltip("The coordinate frame the player translates in.")]
  public TranslationFrame serverTranslationFrame = TranslationFrame.LOCAL;

  //! The speed [m/s] at which the player translates in each independent direction.
  [Tooltip("The speed (m/s) at which the player translates in each independent direction.")]
  [Range(0.0f, 10.0f)]
  [SerializeField()]
  float serverTranslationSpeedM_S = 0.15f;

  //! The coordinate frame the player rotates in.
  [Tooltip("The coordinate frame the player rotates in.")]
  public RotationFrame serverRotationFrame = RotationFrame.LOCAL;

  //! The speed [deg/s] at which the player rotates in each independent direction.
  [Tooltip("The speed (deg/s) at which the player rotates in each independent direction.")]
  [Range(0.0f, 360.0f)]
  [SerializeField()]
  float serverRotationSpeedDeg_S = 30.0f;

  //! The name of the input axis that controls forward motion.
  [Tooltip("The name of the input axis that controls forward motion.")]
  [SerializeField()]
  string clientForwardAxis = "HxOffsetForward";

  //! The name of the input axis that controls rightward motion.
  [Tooltip("The name of the input axis that controls rightward motion.")]
  [SerializeField()]
  string clientRightAxis = "HxOffsetRight";

  //! The name of the input axis that controls upward motion.
  [Tooltip("The name of the input axis that controls upward motion.")]
  [SerializeField()]
  string clientUpAxis = "HxOffsetUp";

  //! The name of the input axis that controls pitch motion.
  [Tooltip("The name of the input axis that controls pitch motion.")]
  [SerializeField()]
  string clientPitchAxis = "HxOffsetPitch";

  //! The name of the input axis that controls yaw motion.
  [Tooltip("The name of the input axis that controls yaw motion.")]
  [SerializeField()]
  string clientYawAxis = "HxOffsetYaw";

  //! The name of the input axis that controls roll motion.
  [Tooltip("The name of the input axis that controls roll motion.")]
  [SerializeField()]
  string clientRollAxis = "HxOffsetRoll";

  //! The name of the input action that resets the offset.
  [Tooltip("The name of the input action that resets the offset.")]
  [SerializeField()]
  KeyCode clientResetKey = KeyCode.KeypadMinus;

  //! Save a different offset for each scene.
  [Tooltip("Save a different offset for each scene.")]
  [SerializeField()]
  bool clientEnableSceneSpecificOffsets = true;

  //! The world transform of our owner.
  Matrix4x4 serverInitialTransform = Matrix4x4.identity;

  //! The local offset (as known by the client).
  Matrix4x4 clientLocalOffset = Matrix4x4.identity;

  //! The local offset (as known by the server).
  Matrix4x4 serverLocalOffset = Matrix4x4.identity;

  //! The input state for linear velocity (as known by the client).
  Vector3 clientVelocityInput = Vector3.zero;

  //! The input state for linear velocity (as known by the server).
  Vector3 serverVelocityInput;

  //! The input state for angular velocity (as known by the client).
  Vector3 clientAngularVelocityInput;

  //! The input state for angular velocity (as known by the server).
  Vector3 serverAngularVelocityInput;

  //! The player preference key prefix used to save and load our world offsets.
  string clientPpkOffsetPrefix;

  //! Functions that have been successfully bound to input axes.
  List<Action> clientBoundFunctions = null;

  public override void OnStartServer() {
    serverInitialTransform = transform.localToWorldMatrix;
  }

  public override void OnStartAuthority() {
    string defaultPpkOffsetPrefix = GetType().Name;

    if (clientEnableSceneSpecificOffsets) {
      clientPpkOffsetPrefix = defaultPpkOffsetPrefix + "-" + SceneManager.GetActiveScene().name;

      if (ClientDoesSavedOffsetExist(clientPpkOffsetPrefix)) {
        ClientLoadOffset(clientPpkOffsetPrefix, out clientLocalOffset);
      }
    } else {
      if (ClientDoesSavedOffsetExist(clientPpkOffsetPrefix)) {
        ClientLoadOffset(clientPpkOffsetPrefix, out clientLocalOffset);
      }
    }

    ClientApplyLocalOffset();
    ClientBindInput();
  }

  void Update() {
    if (hasAuthority) {
      foreach (Action clientBoundFunction in clientBoundFunctions) {
        clientBoundFunction();
      }

      if (Input.GetKeyDown(clientResetKey)) {
        CmdResetOffset();
      }
    }

    if (isServer) {
      if (serverVelocityInput.sqrMagnitude > 0.0f) {
        ServerTranslate(ServerGetCoordinateFrame(serverTranslationFrame).rotation *
            serverVelocityInput * serverTranslationSpeedM_S * Time.deltaTime);
      }

      if (serverAngularVelocityInput.sqrMagnitude > 0.0f) {
        ServerRotate(Quaternion.AngleAxis(serverRotationSpeedDeg_S * Time.deltaTime,
            ServerGetCoordinateFrame(serverRotationFrame).rotation * serverAngularVelocityInput));
      }
    }
  }

  //! Called when the scene ends or when manually destroyed.
  void OnDestroy() {
    if (hasAuthority) {
      ClientSaveOffset(clientPpkOffsetPrefix, clientLocalOffset);
    }
  }

  //! Applies the offset loaded by the client to the player.
  [Client]
  void ClientApplyLocalOffset() {
    CmdApplyLocalOffset(clientLocalOffset.MultiplyPoint3x4(Vector3.zero),
        clientLocalOffset.rotation);
  }

  //! Applies an offset received from the client.
  //!
  //! @param lTranslationM The translation to apply.
  //! @param lRotation The rotation to apply.
  [Command]
  void CmdApplyLocalOffset(Vector3 lTranslationM, Quaternion lRotation) {
    serverLocalOffset = Matrix4x4.TRS(lTranslationM, lRotation, Vector3.one);

    Matrix4x4 wPlayer = transform.localToWorldMatrix * serverLocalOffset;
    transform.SetPositionAndRotation(wPlayer.MultiplyPoint3x4(Vector3.zero),
        wPlayer.rotation);
  }

  //! Binds functions to input axes.
  //!
  //! @returns True if all input was successfully bound.
  [Client]
  bool ClientBindInput() {
    Dictionary<string, Action> allFunctions = new Dictionary<string, Action>() {
      {clientForwardAxis, ClientTranslateForward},
      {clientRightAxis, ClientTranslateRight},
      {clientUpAxis, ClientTranslateUp},
      {clientPitchAxis, ClientRotatePitch},
      {clientYawAxis, ClientRotateYaw},
      {clientRollAxis, ClientRotateRoll}
      };

    clientBoundFunctions = new List<Action>();
    bool hasSomethingGoneWrong = false;
    foreach (var keyValue in allFunctions) {
      // Alias for convenience.
      string axisName = keyValue.Key;
      Action function = keyValue.Value;

      // See if this axis exists by trying to get a value from it. 
      // An ArgumentException is thrown if the axis does not exist.
      try {
        // The axis exists, so add to our lists to be checked every frame.
        Input.GetAxis(axisName);  // Throws ArgumentException if axis isn't bound.
        clientBoundFunctions.Add(function);
      } catch (ArgumentException) {
        Debug.LogWarning(string.Format("Axis {0} not found in input settings.",
            axisName), this);
        hasSomethingGoneWrong = true;
      }
    }

    return !hasSomethingGoneWrong;
  }

  //! Moves the player forward.
  [Client]
  void ClientTranslateForward() {
    float axis = Input.GetAxis(clientForwardAxis);
    if (clientVelocityInput.z != axis) {
      clientVelocityInput.z = axis;
      CmdTranslateForward(axis);
    }
  }

  //! Moves the player right.
  [Client]
  void ClientTranslateRight() {
    float axis = Input.GetAxis(clientRightAxis);
    if (clientVelocityInput.x != axis) {
      clientVelocityInput.x = axis;
      CmdTranslateRight(axis);
    }
  }

  //! Moves the player up.
  [Client]
  void ClientTranslateUp() {
    float axis = Input.GetAxis(clientUpAxis);
    if (clientVelocityInput.y != axis) {
      clientVelocityInput.y = axis;
      CmdTranslateUp(axis);
    }
  }

  //! Pitches the player.
  [Client]
  void ClientRotatePitch() {
    float axis = Input.GetAxis(clientPitchAxis);
    if (clientAngularVelocityInput.x != axis) {
      clientAngularVelocityInput.x = axis;
      CmdRotatePitch(axis);
    }
  }

  //! Yaws the player.
  [Client]
  void ClientRotateYaw() {
    float axis = Input.GetAxis(clientYawAxis);
    if (clientAngularVelocityInput.y != axis) {
      clientAngularVelocityInput.y = axis;
      CmdRotateYaw(axis);
    }
  }

  //! Rolls the player.
  [Client]
  void ClientRotateRoll() {
    float axis = Input.GetAxis(clientRollAxis);
    if (clientAngularVelocityInput.z != axis) {
      clientAngularVelocityInput.z = axis;
      CmdRotateRoll(axis);
    }
  }

  //! @copydoc #ClientTranslateForward()
  //!
  //! @param axis The client value of the forward axis.
  [Command]
  void CmdTranslateForward(float axis) {
    serverVelocityInput.z = axis;
  }

  //! @copydoc #ClientTranslateRight()
  //!
  //! @param axis The client value of the right axis.
  [Command]
  void CmdTranslateRight(float axis) {
    serverVelocityInput.x = axis;
  }

  //! @copydoc #ClientTranslateUp()
  //!
  //! @param axis The client value of the up axis.
  [Command]
  void CmdTranslateUp(float axis) {
    serverVelocityInput.y = axis;
  }

  //! @copydoc #ClientRotatePitch()
  //!
  //! @param axis The client value of the pitch axis.
  [Command]
  void CmdRotatePitch(float axis) {
    serverAngularVelocityInput.x = axis;
  }

  //! @copydoc #ClientRotateYaw()
  //!
  //! @param axis The client value of the yaw axis.
  [Command]
  void CmdRotateYaw(float axis) {
    serverAngularVelocityInput.y = axis;
  }

  //! @copydoc #ClientRotateRoll()
  //!
  //! @param axis The client value of the roll axis.
  [Command]
  void CmdRotateRoll(float axis) {
    // Sign flip aligns axis to Unity's weird convention for roll.
    serverAngularVelocityInput.z = -1.0f * axis;
  }

  //! Reset our player to its initial transform.
  [Command]
  void CmdResetOffset() {
    transform.position = serverInitialTransform.MultiplyPoint3x4(Vector3.zero);
    transform.rotation = serverInitialTransform.rotation;
    
    serverLocalOffset = Matrix4x4.identity;
    RpcClientUpdateLocalTranslation(Vector3.zero);
    RpcClientUpdateLocalRotation(Quaternion.identity);
  }

  //! Gets the transform of a given coordinate frame.
  //!
  //! @param frame The frame.
  //! @returns The transform.
  [Server]
  Matrix4x4 ServerGetCoordinateFrame(TranslationFrame frame) {
    switch (frame) {
      case (TranslationFrame.CAMERA):
        if (Camera.main != null) {
          return Camera.main.transform.localToWorldMatrix;
        }
        break;
      case (TranslationFrame.GLOBAL):
        break;
      case (TranslationFrame.LOCAL):
        return transform.localToWorldMatrix;
    }

    return Matrix4x4.identity;
  }

  //! @copydoc #ServerGetCoordinateFrame(TranslationFrame)
  [Server]
  Matrix4x4 ServerGetCoordinateFrame(RotationFrame frame) {
    // Use the value returned by the translation frame version of getCoordinateFrameDirection as
    // the logic is the same.
    switch (frame) {
      case RotationFrame.GLOBAL:
        return ServerGetCoordinateFrame(TranslationFrame.GLOBAL);
      case RotationFrame.LOCAL:
        return ServerGetCoordinateFrame(TranslationFrame.LOCAL);
    }

    return ServerGetCoordinateFrame(TranslationFrame.GLOBAL);
  }

  //! Translates our player by a given delta.
  //!
  //! @param wDeltaM The world space position delta.
  [Server]
  void ServerTranslate(Vector3 wDeltaM) {
    if (wDeltaM.sqrMagnitude > 0.0f) {
      transform.position += wDeltaM;

      // Save the delta as if it were local to our initial transform.
      Vector3 lDeltaM = serverInitialTransform.inverse.MultiplyVector(wDeltaM);
      serverLocalOffset = Matrix4x4.TRS(serverLocalOffset.MultiplyPoint3x4(Vector3.zero) + lDeltaM,
          serverLocalOffset.rotation, Vector3.one);
      RpcClientUpdateLocalTranslation(serverLocalOffset.MultiplyPoint3x4(Vector3.zero));
    }
  }

  //! Sets the offset that gets saved on-disk on the client's side.
  //!
  //! @param lTranslationM The offset that gets saved.
  [ClientRpc]
  void RpcClientUpdateLocalTranslation(Vector3 lTranslationM) {
    clientLocalOffset = Matrix4x4.TRS(lTranslationM, clientLocalOffset.rotation, Vector3.one);
  }

  //! Rotates our player by a given delta.
  //!
  //! @param wDelta The world space rotation delta.
  [Server]
  void ServerRotate(Quaternion wDelta) {
    if (Math.Pow(wDelta.w, 2.0f) < 1.0f) {
      Matrix4x4 wPlayer = transform.localToWorldMatrix;
      transform.rotation = wDelta * transform.rotation;

      // Save the delta rotation as if it were local to our initial transform.
      Quaternion lOffset = serverLocalOffset.rotation * Quaternion.Inverse(wPlayer.rotation) *
          wDelta * wPlayer.rotation;
      serverLocalOffset = Matrix4x4.TRS(serverLocalOffset.MultiplyPoint3x4(Vector3.zero), lOffset,
          Vector3.one);
      RpcClientUpdateLocalRotation(serverLocalOffset.rotation);
    }
  }

  //! Sets the offset that gets saved on-disk on the client's side.
  //!
  //! @param lRotation The offset that gets saved.
  [ClientRpc]
  void RpcClientUpdateLocalRotation(Quaternion lRotation) {
    clientLocalOffset = Matrix4x4.TRS(clientLocalOffset.MultiplyPoint3x4(Vector3.zero), lRotation,
        Vector3.one);
  }

  //! Player preference key suffix for position-X.
  static string XPosPpkSuffix = "PosX";

  //! Player preference key suffix for position-Y.
  static string YPosPpkSuffix = "PosY";

  //! Player preference key suffix for position-Z.
  static string ZPosPpkSuffix = "PosZ";

  //! Player preference key suffix for rotation-X.
  static string XRotPpkSuffix = "RotX";

  //! Player preference key suffix for rotation-Y.
  static string YRotPpkSuffix = "RotY";

  //! Player preference key suffix for rotation-Z.
  static string ZRotPpkSuffix = "RotZ";

  //! Player preference key suffix for rotation-W.
  static string WRotPpkSuffix = "RotW";

  //! Returns true if values corresponding to the given prefix exist in player preferences.
  //!
  //! @param ppkPrefix The given player preference key prefix.
  //! @returns Whether or not player preference entries for @p ppkPrefix exist.
  static bool ClientDoesSavedOffsetExist(string ppkPrefix) {
    if (
      PlayerPrefs.HasKey(ppkPrefix + XPosPpkSuffix) &&
      PlayerPrefs.HasKey(ppkPrefix + YPosPpkSuffix) &&
      PlayerPrefs.HasKey(ppkPrefix + ZPosPpkSuffix) &&
      PlayerPrefs.HasKey(ppkPrefix + XRotPpkSuffix) &&
      PlayerPrefs.HasKey(ppkPrefix + YRotPpkSuffix) &&
      PlayerPrefs.HasKey(ppkPrefix + ZRotPpkSuffix) &&
      PlayerPrefs.HasKey(ppkPrefix + WRotPpkSuffix)) {
      return true;
    }

    return false;
  }

  //! Loads an offset saved in player preferences.
  //!
  //! @param ppkPrefix The player preference key prefix to use.
  //! @param[out] offset Populated with the offset.
  static void ClientLoadOffset(string ppkPrefix, out Matrix4x4 offset) {
    Vector3 positionM = new Vector3(
      PlayerPrefs.GetFloat(ppkPrefix + XPosPpkSuffix),
      PlayerPrefs.GetFloat(ppkPrefix + YPosPpkSuffix),
      PlayerPrefs.GetFloat(ppkPrefix + ZPosPpkSuffix));
    Quaternion orientation = new Quaternion(
      PlayerPrefs.GetFloat(ppkPrefix + XRotPpkSuffix),
      PlayerPrefs.GetFloat(ppkPrefix + YRotPpkSuffix),
      PlayerPrefs.GetFloat(ppkPrefix + ZRotPpkSuffix),
      PlayerPrefs.GetFloat(ppkPrefix + WRotPpkSuffix));
    offset = Matrix4x4.TRS(positionM, orientation, Vector3.one);
  }

  //! Saves an offset in player preferences.
  //!
  //! @param ppkPrefix The player preference key prefix to use.
  //! @param offset The offset to save.
  static void ClientSaveOffset(string ppkPrefix, Matrix4x4 offset) {
    Vector3 position = offset.MultiplyPoint3x4(Vector3.zero);
    Quaternion orienation = offset.rotation;
    PlayerPrefs.SetFloat(ppkPrefix + XPosPpkSuffix, position.x);
    PlayerPrefs.SetFloat(ppkPrefix + YPosPpkSuffix, position.y);
    PlayerPrefs.SetFloat(ppkPrefix + ZPosPpkSuffix, position.z);
    PlayerPrefs.SetFloat(ppkPrefix + XRotPpkSuffix, orienation.x);
    PlayerPrefs.SetFloat(ppkPrefix + YRotPpkSuffix, orienation.y);
    PlayerPrefs.SetFloat(ppkPrefix + ZRotPpkSuffix, orienation.z);
    PlayerPrefs.SetFloat(ppkPrefix + WRotPpkSuffix, orienation.w);
  }
}
