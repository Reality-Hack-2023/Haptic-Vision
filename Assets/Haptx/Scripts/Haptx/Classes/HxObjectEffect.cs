// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using UnityEngine;

//! @brief The Haptic Effect component to override when creating effects that are generated via
//! contact with objects.
//!
//! Extend this class and override the GetForceN() function to create your own object-based haptic
//! effects. Add your child component to your GameObject and call play() when ready.
abstract public class HxObjectEffect : HxHapticEffect {
  //! The underlying Haptic Effect.
  protected override HaptxApi.HapticEffect EffectInternal {
    get {
      return objectEffect;
    }
  }

  //! The underlying object effect.
  protected HaptxApi.ObjectEffect objectEffect = null;

  //! Whether this Haptic Effect applies to Colliders found on child GameObjects as well.
  [Tooltip("Whether this Haptic Effect applies to Colliders found on child GameObjects as well.")]
  [SerializeField]
  private bool _propagateToChildren = false;

  //! Adds this effect to an object.
  //!
  //! @param collider The collider representing the object.
  //! @param includeChildren Whether to also add this effect to child colliders.
  //! @returns Whether the effect was successfully added to all objects that were capable of
  //! registration with the HaptX SDK.
  public bool AddToObject(Collider collider, bool includeChildren = false) {
    if (collider == null) {
      HxDebug.LogWarning("HxObjectEffect.AddToObject(): Null collider provided.", this);
      return false;
    }

    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxObjectEffect.AddToObject(): Failed to get handle to core.", this);
      return false;
    }

    long[] objectIds = GetObjectIds(collider, includeChildren);
    if (objectIds.Length == 0) {
      HxDebug.LogError(string.Format(
          "HxObjectEffect.AddToObject(): Collider {0} could not be registered with the HaptX SDK.",
          collider.name), this);
      return false;
    }

    bool failed = false;
    foreach (long objectId in objectIds) {
      if (!core.ContactInterpreter.addEffectToObject(objectId, objectEffect)) {
        HxDebug.LogError(string.Format(
            "HxObjectEffect.AddToObject(): Failed to add effect to object {0}.", objectId),
            this);
        failed = true;
      }
    }
    return !failed;
  }

  //! Removes this effect from an object.
  //!
  //! @param collider The collider representing the object.
  //! @param includeChildren Whether to also remove this effect from child colliders.
  //! @returns Whether the effect was successfully removed from all objects that were capable of
  //! registration with the HaptX SDK.
  public bool RemoveFromObject(Collider collider, bool includeChildren = false) {
    if (collider == null) {
      HxDebug.LogWarning("HxObjectEffect.RemoveFromObject(): Null collider provided.", this);
      return false;
    }

    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxObjectEffect.RemoveFromObject(): Failed to get handle to core.", this);
      return false;
    }

    if (objectEffect == null) {
      HxDebug.LogError("HxObjectEffect.RemoveFromObject(): Null internal effect.", this);
      return false;
    }

    long[] objectIds = GetObjectIds(collider, includeChildren);
    if (objectIds.Length == 0) {
      HxDebug.LogError(string.Format(
          "HxObjectEffect.RemoveFromObject(): Collider {0} could not be registered with the HaptX SDK.",
          collider.name), this);
      return false;
    }

    bool failed = false;
    foreach (long objectId in objectIds) {
      if (!core.ContactInterpreter.removeEffectFromObject(objectId, objectEffect.getId())) {
        HxDebug.LogError(string.Format(
            "HxObjectEffect.RemoveFromObject(): Failed to remove effect from object {0}.",
            objectId), this);
        failed = true;
      }
    }
    return !failed;
  }

  //! Whether this effect is on the object.
  //!
  //! @param collider The collider representing the object.
  //! @returns True if the effect is on the object.
  public bool IsOnObject(Collider collider) {
    if (collider == null) {
      HxDebug.LogWarning("HxObjectEffect.IsOnObject(): Null collider provided.", this);
      return false;
    }

    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxObjectEffect.IsOnObject(): Failed to get handle to core.", this);
      return false;
    }

    long objectId;
    if (!core.TryRegisterCollider(collider, false, out objectId)) {
      HxDebug.LogWarning(string.Format(
          "HxObjectEffect.IsOnObject(): Collider {0} could not be registered with the HaptX SDK.",
          collider.name), this);
      return false;
    }

    if (objectEffect == null) {
      HxDebug.LogError("HxObjectEffect.IsOnObject(): Null internal effect.", this);
      return false;
    }

    return core.ContactInterpreter.isEffectOnObject(objectId, objectEffect.getId());
  }

  //! Get the objects this effect is attached to.
  //!
  //! @returns The objects this effect is attached to.
  public long[] GetAttachedObjects() {
    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("GetAttachedObjects.GetAttachedObjects(): Failed to get handle to core.",
          this);
      return new long[0];
    }

    if (objectEffect == null) {
      HxDebug.LogError("GetAttachedObjects.GetAttachedObjects(): Null internal effect.", this);
      return new long[0];
    }

    UnorderedSetOfInt64_t attachedObjects =
        core.ContactInterpreter.getEffectAttachedObjects(objectEffect.getId());
    long[] array = new long[attachedObjects.Count];
    attachedObjects.CopyTo(array);
    return array;
  }

  //! Awake is called when the script instance is being loaded.
  new protected void Awake() {
    objectEffect = new HxUnityObjectEffect(this);

    Collider[] colliders = _propagateToChildren ? GetComponentsInChildren<Collider>() :
        GetComponents<Collider>();
    foreach (Collider collider in colliders) {
      AddToObject(collider, false);
    }

    base.Awake();
  }

  //! Override to define your Haptic Effect.
  //!
  //! @param contactInfo Information about a contact generating the effect.
  //! @returns The force [N] applied by the effect.
  abstract protected float GetForceN(HaptxApi.ObjectEffect.ContactInfo contactInfo);

  //! Get the ID's of an object and (optionally) all of its children.
  //!
  //! @param collider The collider representing the object.
  //! @param includeChildren Whether to also get the ID's for child colliders.
  //! @returns The ID's of the object and (optionally) its children.
  static private long[] GetObjectIds(Collider collider, bool includeChildren) {
    if (collider == null) {
      return new long[0];
    }

    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxObjectEffect.GetObjectIds(): Failed to get handle to core.");
      return new long[0];
    }

    Collider[] colliders = includeChildren ?
        collider.GetComponentsInChildren<Collider>() : new Collider[1] { collider };
    List<long> objectIds = new List<long>();
    foreach (Collider colliderIt in colliders) {
      long objectId;
      if (core.TryRegisterCollider(colliderIt, false, out objectId)) {
        objectIds.Add(objectId);
      }
    }

    return objectIds.ToArray();
  }

  //! Wraps HaptxApi.HapticEffectInterface.ObjectEffect and associates it with an HxObjectEffect.
  private class HxUnityObjectEffect : HaptxApi.ObjectEffect {

    //! The MonoBehaviour associated with this effect.
    private HxObjectEffect _objectEffect = null;

    //! Construct by association with a MonoBehaviour.
    //!
    //! @param objectEffect The MonoBehaviour associated with this effect.
    public HxUnityObjectEffect(HxObjectEffect objectEffect) {
      _objectEffect = objectEffect;
    }

    public override float getForceN(ContactInfo contactInfo) {
      if (_objectEffect == null) {
        return 0.0f;
      } else {
        return _objectEffect.GetForceN(contactInfo);
      }
    }
  }
}
