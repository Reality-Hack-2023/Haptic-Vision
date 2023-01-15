// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! Wraps HaptxApi::SphereBoundingVolume.
public class HxSphereBoundingVolume : HxBoundingVolume {
  //! The radius [m] of the sphere volume.  
  public float RadiusM {
    get {
      return _radiusM;
    }
    set {
      _radiusM = Mathf.Max(0.0f, value);

      HaptxApi.SphereBoundingVolume sphereVolume = GetBoundingVolume()
          as HaptxApi.SphereBoundingVolume;
      if (sphereVolume == null) {
        HxDebug.LogError(
            "HxSphereBoundingVolume.RadiusM: Underlying bounding volume is not a HaptxApi.SphereBoundingVolume.");
        return;
      }

      sphereVolume.setRadiusM(_radiusM);
    }
  }

  //! See #_radiusM.
  [Tooltip("The radius [m] of the sphere volume.")]
  [SerializeField, Range(0, float.MaxValue)]
  private float _radiusM = 0.1f;

  //! The center position [m] of the sphere volume.
  public Vector3 CenterPositionM {
    get {
      return _centerPositionM;
    }
    set {
      _centerPositionM = value;

      HaptxApi.SphereBoundingVolume sphereVolume = GetBoundingVolume()
          as HaptxApi.SphereBoundingVolume;
      if (sphereVolume == null) {
        HxDebug.LogError(
            "HxSphereBoundingVolume.CenterPositionM: Underlying bounding volume is not a HaptxApi.SphereBoundingVolume.");
        return;
      }

      HaptxApi.Vector3D centerPositionM = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
      HxShared.HxFromUnity(_centerPositionM, centerPositionM);
      sphereVolume.setCenterPositionM(centerPositionM);
      HxReusableObjectPool<HaptxApi.Vector3D>.Release(centerPositionM);
    }
  }

  //! See #_centerPositionM.
  [Tooltip("The center position [m] of the sphere volume.")]
  [SerializeField]
  private Vector3 _centerPositionM = Vector3.zero;

  //! The underlying bounding volume.
  private HaptxApi.BoundingVolume _boundingVolume = null;

  //! Draw a gizmo if the object is selected.
  private void OnDrawGizmosSelected() {
    Gizmos.color = HxShared.HxTeal;
    Gizmos.matrix = transform.localToWorldMatrix;
    Gizmos.DrawWireSphere(_centerPositionM, _radiusM);
  }

  //! Awake is called when the script instance is being loaded.
  private void Awake() {
    HaptxApi.SphereBoundingVolume sphereVolume = GetBoundingVolume()
        as HaptxApi.SphereBoundingVolume;
    if (sphereVolume == null) {
      HxDebug.LogError(
          "HxSphereBoundingVolume.Awake(): Underlying bounding volume is not a HaptxApi.SphereBoundingVolume.");
      return;
    }

    sphereVolume.setRadiusM(_radiusM);
    HaptxApi.Vector3D centerPositionM = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(_centerPositionM, centerPositionM);
    sphereVolume.setCenterPositionM(centerPositionM);
    HxReusableObjectPool<HaptxApi.Vector3D>.Release(centerPositionM);
  }

  //! Get the underlying bounding volume.
  //!
  //! @returns The underlying bounding volume.
  public override HaptxApi.BoundingVolume GetBoundingVolume() {
    if (_boundingVolume == null) {
      _boundingVolume = new HaptxApi.SphereBoundingVolume();
    }
    return _boundingVolume;
  }
}
