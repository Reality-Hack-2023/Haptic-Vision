// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! Wraps HaptxApi::BoxBoundingVolume.
public class HxBoxBoundingVolume : HxBoundingVolume {

  //! The minima [m] of the box volume.
  public Vector3 MinimaM {
    get {
      return _minimaM;
    }
  }

  //! See #_minimaM.
  [Tooltip("The minima [m] of the box volume.")]
  [SerializeField]
  private Vector3 _minimaM = -0.1f * Vector3.one;

  //! The maxima [m] of the box volume.
  public Vector3 MaximaM {
    get {
      return _maximaM;
    }
  }

  //! See #_maximaM.
  [Tooltip("The maxima [m] of the box volume.")]
  [SerializeField]
  private Vector3 _maximaM = 0.1f * Vector3.one;

  //! The underlying bounding volume.
  private HaptxApi.BoundingVolume _boundingVolume = null;

  //! Draw a gizmo if the object is selected.
  private void OnDrawGizmosSelected() {
    Gizmos.color = HxShared.HxTeal;
    Gizmos.matrix = transform.localToWorldMatrix;
    Vector3 lCenterPositionM = 0.5f * (_minimaM + _maximaM);
    Vector3 lSizeM = _maximaM - _minimaM;
    Gizmos.DrawWireCube(lCenterPositionM, lSizeM);
  }

  //! Awake is called when the script instance is being loaded.
  private void Awake() {
    HaptxApi.BoxBoundingVolume boxVolume = GetBoundingVolume() as HaptxApi.BoxBoundingVolume;
    if (boxVolume == null) {
      HxDebug.LogError(
          "HxBoxBoundingVolume.Awake(): Underlying bounding volume is not a HaptxApi.BoxBoundingVolume.");
      return;
    }

    HaptxApi.Vector3D hxMinimaM = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(_minimaM, ref hxMinimaM);
    HaptxApi.Vector3D hxMaximaM = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(_maximaM, ref hxMaximaM);
    boxVolume.setExtrema(hxMinimaM.x_, hxMaximaM.x_, hxMinimaM.y_, hxMaximaM.y_,
        hxMinimaM.z_, hxMaximaM.z_);
    HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxMinimaM);
    HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxMaximaM);
  }

  //! Get the underlying bounding volume.
  //!
  //! @returns The underlying bounding volume.
  public override HaptxApi.BoundingVolume GetBoundingVolume() {
    if (_boundingVolume == null) {
      _boundingVolume = new HaptxApi.BoxBoundingVolume();
    }
    return _boundingVolume;
  }

  //! Set the extrema [m] to use.
  //!
  //! @param minimaM The minima [m] to use.
  //! @param maximaM The maxima [m] to use.
  public void SetExtremaM(Vector3 minimaM, Vector3 maximaM) {
    _minimaM = minimaM;
    _maximaM = maximaM;

    HaptxApi.BoxBoundingVolume boxVolume = GetBoundingVolume() as HaptxApi.BoxBoundingVolume;
    if (boxVolume == null) {
      HxDebug.LogError(
          "HxBoxBoundingVolume.SetExtremaM(): Underlying bounding volume is not a HaptxApi.BoxBoundingVolume.");
      return;
    }

    HaptxApi.Vector3D hxMinimaM = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(_minimaM, ref hxMinimaM);
    HaptxApi.Vector3D hxMaximaM = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(_maximaM, ref hxMaximaM);
    boxVolume.setExtrema(hxMinimaM.x_, hxMaximaM.x_, hxMinimaM.y_,
        hxMaximaM.y_, hxMinimaM.z_, hxMaximaM.z_);
    HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxMinimaM);
    HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxMaximaM);
  }
};
