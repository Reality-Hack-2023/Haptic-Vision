// Copyright (C) 2022 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

public static class HxVersionCompatability {
  public static class MirrorChannels {
#if MIRROR_35_0_OR_NEWER
    public const int Reliable = Mirror.Channels.Reliable;
    public const int Unreliable = Mirror.Channels.Unreliable;
#else
    public const int Reliable = Mirror.Channels.DefaultReliable;
    public const int Unreliable = Mirror.Channels.DefaultUnreliable;
#endif
  }

  public static void MirrorClientRegisterPrefab(UnityEngine.GameObject gameObject) {
#if MIRROR_35_0_OR_NEWER
    Mirror.NetworkClient.RegisterPrefab(gameObject);
#else
    Mirror.ClientScene.RegisterPrefab(gameObject);
#endif
  }
}
