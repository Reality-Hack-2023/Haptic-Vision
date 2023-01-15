// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using Mirror;

//! @brief Satisfies all the abstract requirements of the Mirror.Transport class without actually
//! doing any network communication.
//!
//! This class allows functionality dependent on Mirror.NetworkManager to function in a single
//! player capacity.
public class DummyTransport : Transport {

  public override void ClientDisconnect() { }

  public override void ClientConnect(string address) { }

  public override void ServerStart() { }

  public override void ServerStop() { }

  public override bool ServerSend(List<int> connectionIds, int channelId, ArraySegment<byte> segment) {
    return true;
  }

  public override bool ServerActive() {
    return true;
  }

  public override void Shutdown() {
    ClientDisconnect();
    ServerStop();
  }

  public override int GetMaxPacketSize(int channelId = Channels.DefaultReliable) {
    return 16384;
  }

  public override Uri ServerUri() {
    return new Uri("localhost");
  }

  public override bool ClientSend(int channelId, ArraySegment<byte> segment) {
    return true;
  }

  public override bool Available() {
    return true;
  }

  public override string ServerGetClientAddress(int connectionId) {
    return "localhost";
  }

  public override bool ClientConnected() {
    return true;
  }

  public override bool ServerDisconnect(int connectionId) {
    return true;
  }
}
