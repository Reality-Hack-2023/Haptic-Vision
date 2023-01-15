// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using Mirror;
using UnityEngine;

//! A class that automatically jumps through all the necessary hoops to support single player play
//! using the HaptX plugin.
public class HxSinglePlayer : MonoBehaviour {
  
  //! Drag your HaptxPlayer prefab here.
  [SerializeField]
  [Tooltip("Drag your HaptxPlayer prefab here.")]
  private GameObject _hxPlayerPrefab = null;

  void Awake() {
    Transport dummyTransport = gameObject.AddComponent<DummyTransport>();
    NetworkManager networkManager = gameObject.AddComponent<NetworkManager>();
    networkManager.playerPrefab = _hxPlayerPrefab;
    Transport.activeTransport = dummyTransport;
    networkManager.StartHost();
  }
}
