// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief The abstract Haptic Effect class to override when creating effects that are
//! generated in free space.
//!
//! Extend this class and override the GetForceN() function to create your own spatial haptic
//! effects. Position your effect and call Play() when ready. Optionally define its bounding volume
//! using a child of HxBoundingVolume.
public abstract class HxSpatialEffect : HxHapticEffect {
  //! The underlying Haptic Effect.
  protected override HaptxApi.HapticEffect EffectInternal {
    get {
      return spatialEffect;
    }
  }

  //! The underlying spatial effect.
  protected HaptxApi.SpatialEffect spatialEffect = null;

  //! Callbacks for getting simulation information about this object.
  private HxTransformCallbacks _callbacks = null;

  //! @brief The bounding volume this effect is operating in.
  //!
  //! A value of null indicates an unbounded effect.
  public HxBoundingVolume BoundingVolume {
    get {
      return _boundingVolume;
    }
    set {
      _boundingVolume = value;
      if (spatialEffect != null) {
        if (_boundingVolume != null) {
          _callbacks = new HxTransformCallbacks(_boundingVolume.transform, Matrix4x4.identity);
          spatialEffect.setBoundingVolume(_boundingVolume.GetBoundingVolume());
        } else {
          _callbacks = new HxTransformCallbacks(transform, Matrix4x4.identity);
          spatialEffect.setBoundingVolume(null);
        }
        spatialEffect.setCallbacks(_callbacks);
      }
    }
  }

  //! See #BoundingVolume.
  [Tooltip("The bounding volume this effect is operating in. A value of null indicates an unbounded effect.")]
  [SerializeField]
  private HxBoundingVolume _boundingVolume = null;

  new protected void Awake() {
    spatialEffect = new HxUnitySpatialEffect(this);
    BoundingVolume = _boundingVolume;

    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core != null) {
      core.ContactInterpreter.registerSpatialEffect(spatialEffect);
    } else {
      HxDebug.LogError("HxSpatialEffect.Awake(): Failed to get handle to core.", this);
    }

    base.Awake();
  }

  private void OnApplicationQuit() {
    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core != null) {
      if (spatialEffect != null) {
        core.ContactInterpreter.unregisterSpatialEffect(spatialEffect.getId());
      } else {
        HxDebug.LogError("HxSpatialEffect.OnDestroy(): Null internal effect.", this);
      }
    } else {
      HxDebug.LogError("HxSpatialEffect.OnDestroy(): Failed to get handle to core.", this);
    }
  }

  //! Override to define your Haptic Effect.
  //!
  //! @param spatialInfo Information about a tactor in range of the effect.
  //! @returns The force [N] applied by the effect.
  protected abstract float GetForceN(
      HaptxApi.SpatialEffect.SpatialInfo spatialInfo);

  //! Wraps HaptxApi.HapticEffectInterface.SpatialEffect and associates it with an HxSpatialEffect.
  private class HxUnitySpatialEffect : HaptxApi.SpatialEffect {
    //! The HxSpatialEffect component associated with this effect.
    private HxSpatialEffect _spatialEffect = null;

    //! Construct by association with a HxSpatialEffect component.
    //!
    //! @param spatialEffect The HxSpatialEffect component associated with this effect.
    public HxUnitySpatialEffect(HxSpatialEffect spatialEffect) {
      _spatialEffect = spatialEffect;
    }

    public override float getForceN(SpatialInfo spatialInfo) {
      if (_spatialEffect != null) {
        return _spatialEffect.GetForceN(spatialInfo);
      } else {
        return 0.0f;
      }
    }
  }
}
