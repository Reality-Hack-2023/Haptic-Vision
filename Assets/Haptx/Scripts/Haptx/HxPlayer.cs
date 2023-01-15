// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using Mirror;
using UnityEngine;
using System.Collections;

//! @brief The HaptX defined player.
//! 
//! Complements the HaptxPlayer prefab, which is meant to be the starting point for interfacing
//! with the HaptX SDK via the Unity plugin.
public class HxPlayer : NetworkBehaviour {

  //! @brief Select your left hand prefab here.
  //!
  //! This prefab must also be registered with your scene's network manager.
  [SerializeField]
  [Tooltip("Select your left hand prefab here. This prefab must also be registered with your scene's network manager.")]
  private GameObject _leftHandPrefab = null;

  //! @brief Select your right hand prefab here.
  //!
  //! This prefab must also be registered with your scene's network manager.
  [SerializeField]
  [Tooltip("Select your right hand prefab here. This prefab must also be registered with your scene's network manager.")]
  private GameObject _rightHandPrefab = null;

  //! The player's camera. Only enabled if this is a local player.
  [SerializeField]
  [Tooltip("The player's camera. Only enabled if this is a local player.")]
  private Camera _camera = null;

  //! The player's audio listener. Only enabled if this is a local player.
  [SerializeField]
  [Tooltip("The player's audio listener. Only enabled if this is a local player.")]
  private AudioListener _audioListener = null;

  //! The mesh of the player's HMD. Only enabled if this is NOT a local player.
  [SerializeField]
  [Tooltip("The mesh of the player's HMD. Only enabled if this is NOT a local player.")]
  private MeshRenderer _hmdMesh = null;

  //! The RTT from the client to the server.
  public float RttS {
    get {
      return _rttS;
    }
  }

  //! @copydoc #RttS
  [SyncVar]
  private float _rttS;

  //! The frequency at which RTT is sent to the server.
  private float _rttTransmissionFrequencyHz = 1.0f;

  //! The coroutine that sends RTT to the server.
  IEnumerator _rttTransmissionCoroutine = null;

  void Reset() {
#if UNITY_EDITOR
    _leftHandPrefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand.prefab", typeof(GameObject));
    _rightHandPrefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand.prefab", typeof(GameObject));
    _camera = GetComponentInChildren<Camera>();
    _audioListener = GetComponentInChildren<AudioListener>();
    _hmdMesh = GetComponentInChildren<MeshRenderer>();
#endif
  }

  void Awake() {
    if (_leftHandPrefab != null) {
      ClientScene.RegisterPrefab(_leftHandPrefab);
    }
    if (_rightHandPrefab != null) {
      ClientScene.RegisterPrefab(_rightHandPrefab);
    }
  }

  public override void OnStartClient() {
    if (_audioListener != null) {
      _camera.enabled = hasAuthority;
    }
    if (_audioListener != null) {
      _audioListener.enabled = hasAuthority;
    }
    if (_hmdMesh != null) {
      _hmdMesh.enabled = !hasAuthority;
    }
  }

  public override void OnStartServer() {
    if (_leftHandPrefab != null) {
      GameObject leftHand = Instantiate(_leftHandPrefab);
      HxHand hand = leftHand.GetComponentInChildren<HxHand>();
      hand.player = gameObject;
      hand.hand = RelDir.LEFT;
      hand.mocapOrigin = transform;
      NetworkServer.Spawn(leftHand, connectionToClient);
    }
    if (_rightHandPrefab != null) {
      GameObject rightHand = Instantiate(_rightHandPrefab);
      HxHand hand = rightHand.GetComponentInChildren<HxHand>();
      hand.player = gameObject;
      hand.hand = RelDir.RIGHT;
      hand.mocapOrigin = transform;
      NetworkServer.Spawn(rightHand, connectionToClient);
    }
  }

  public override void OnStartAuthority() {
    if (!isServer) {
      _rttTransmissionCoroutine = TransmitRttS(1.0f / _rttTransmissionFrequencyHz);
      StartCoroutine(_rttTransmissionCoroutine);
    }
  }

  //! Coroutine function for updating #_rttS.
  //!
  //! @param periodS How long to wait in-between RTT updates.
  //! @returns Coroutine iterator.
  private IEnumerator TransmitRttS(float periodS) {
    while (NetworkClient.isConnected) {
      yield return new WaitForSeconds(periodS);
      CmdUpdateRttS((float)NetworkTime.rtt);
    }
  }

  //! Updates server with RTT as known by client.
  //!
  //! @param rttS The RTT from the client.
  [Command]
  private void CmdUpdateRttS(float rttS) {
    _rttS = rttS;
  }
}
