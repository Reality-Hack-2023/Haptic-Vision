// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//! @brief The Haptic Effect component to override when creating effects that aren't convenient via
//! the other base effect classes.
//!
//! Extend this class and override the GetDisplacementM() function to create your own direct haptic
//! effects. Add tactors to the effect using AddToTactor() or AddToCoverageRegion() and call
//! Play() when ready.
public abstract class HxDirectEffect : HxHapticEffect {
  //! The underlying Haptic Effect.
  protected override HaptxApi.HapticEffect EffectInternal {
    get {
      return _directEffect;
    }
  }

  //! This effect operates on all tactors that are associated with these coverage regions.
  protected HashSet<string> coverageRegions = new HashSet<string>(
      System.StringComparer.InvariantCultureIgnoreCase);

  //! The underlying direct effect.
  protected HaptxApi.DirectEffect _directEffect = null;

  //! @brief See #coverageRegions.
  //!
  //! Only for serialization. DO NOT USE IN CODE.
  [Tooltip("This effect operates on all tactors that are associated with these coverage regions.")]
  [SerializeField]
  private string[] _coverageRegionsArray = new string[0];

  //! Adds this effect to a tactor.
  //!
  //! @param peripheralId Which peripheral.
  //! @param tactorId Which tactor.
  //! @returns True if this is the function call that adds the effect to the tactor.
  public bool AddToTactor(HaptxApi.HaptxUuid peripheralId, int tactorId) {
    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxDirectEffect.AddToTactor(): Failed to get handle to core.", this);
      return false;
    }

    return core.ContactInterpreter.addEffectToTactor(peripheralId, tactorId, _directEffect);
  }

  //! Remove this effect from a tactor.
  //!
  //! @param peripheralId Which peripheral.
  //! @param tactorId Which tactor.
  //! @returns True if this is the function call that removes the effect from the tactor.
  public bool RemoveFromTactor(HaptxApi.HaptxUuid peripheralId, int tactorId) {
    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxDirectEffect.RemoveFromTactor(): Failed to get handle to core.", this);
      return false;
    }

    if (_directEffect == null) {
      HxDebug.LogError("HxDirectEffect.RemoveFromTactor(): Null internal effect.", this);
      return false;
    }

    return core.ContactInterpreter.removeEffectFromTactor(peripheralId, tactorId,
        _directEffect.getId());
  }

  //! Checks whether this effect is on the tactor.
  //!
  //! @param peripheralId Which peripheral.
  //! @param tactorId Which tactor.
  //! @returns True if the effect is on the tactor.
  public bool IsOnTactor(HaptxApi.HaptxUuid peripheralId, int tactorId) {
    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxDirectEffect.IsOnTactor(): Failed to get handle to core.", this);
      return false;
    }

    if (_directEffect == null) {
      HxDebug.LogError("HxDirectEffect.IsOnTactor(): Null internal effect.", this);
      return false;
    }

    return core.ContactInterpreter.isEffectOnTactor(peripheralId, tactorId, _directEffect.getId());
  }

  //! Get the tactors this effect is attached to.
  //!
  //! @returns A map from peripheral ID to the set of tactors on that peripheral the effect is
  //! attached too.
  public UnorderedHaptxUuidToUnorderedSetOfInt GetAttachedTactors() {
    HxCore core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HxDebug.LogError("HxDirectEffect.GetAttachedTactors(): Failed to get handle to core.", this);
      return null;
    }

    if (_directEffect == null) {
      HxDebug.LogError("HxDirectEffect.GetAttachedTactors(): Null internal effect.", this);
      return null;
    }

    return core.ContactInterpreter.getEffectAttachedTactors(_directEffect.getId());
  }

  //! Adds this effect to all tactors that are associated with the given coverage region.
  //!
  //! @param coverageRegion Which coverage region.
  //! @returns True unless the effect failed to be added to one of the coverage region's tactors.
  public bool AddToCoverageRegion(string coverageRegion) {
    coverageRegions.Add(coverageRegion);
    HaptxApi.HaptxName coverageRegionName = HxReusableObjectPool<HaptxApi.HaptxName>.Get();
    coverageRegionName.setText(coverageRegion);

    // Add all tactors associated with the given coverage region.
    IEnumerable<PeripheralLink> peripheralLinks =
        FindObjectsOfType<MonoBehaviour>().OfType<PeripheralLink>();
    bool aTactorHasFailed = false;
    foreach (PeripheralLink peripheralLink in peripheralLinks) {
      if (peripheralLink != null && peripheralLink.Peripheral != null) {
        foreach (HaptxApi.Tactor tactor in peripheralLink.Peripheral.tactors) {
          if (tactor.coverage_region.operator_comp_eq(coverageRegionName) &&
              !AddToTactor(peripheralLink.Peripheral.id, tactor.id)) {
            aTactorHasFailed = true;
          }
        }
      }
    }

    HxReusableObjectPool<HaptxApi.HaptxName>.Release(coverageRegionName);
    return !aTactorHasFailed;
  }

  //! Remove this effect from all tactors that are associated with the given coverage region.
  //!
  //! @param coverageRegion Which coverage region.
  //! @returns True if the effect was removed from the coverage region.
  public bool RemoveFromCoverageRegion(string coverageRegion) {
    HaptxApi.HaptxName coverageRegionName = HxReusableObjectPool<HaptxApi.HaptxName>.Get();
    coverageRegionName.setText(coverageRegion);

    bool success = false;
    if (coverageRegions.Remove(coverageRegion)) {
      // Remove all tactors associated with the given coverage region.
      IEnumerable<PeripheralLink> peripheralLinks =
          FindObjectsOfType<MonoBehaviour>().OfType<PeripheralLink>();
      foreach (PeripheralLink peripheralLink in peripheralLinks) {
        if (peripheralLink != null && peripheralLink.Peripheral != null) {
          foreach (HaptxApi.Tactor tactor in peripheralLink.Peripheral.tactors) {
            if (tactor.coverage_region.operator_comp_eq(coverageRegionName)) {
              RemoveFromTactor(peripheralLink.Peripheral.id, tactor.id);
            }
          }
        }
      }

      success = true;
    }

    HxReusableObjectPool<HaptxApi.HaptxName>.Release(coverageRegionName);
    return success;
  }

  //! Whether this effect is on a given coverage region.
  //!
  //! @param coverageRegion Which coverage region to search for.
  public bool IsOnCoverageRegion(string coverageRegion) {
    return coverageRegions.Contains(coverageRegion);
  }

  protected virtual void Reset() {
    _coverageRegionsArray = new string[(int)HaptxApi.CoverageRegion.LAST];
    for (int i = 0; i < (int)HaptxApi.CoverageRegion.LAST; i++) {
      _coverageRegionsArray[i] = HaptxApiSwig.getName((HaptxApi.CoverageRegion)i).getText();
    }
  }

  //! Awake is called when the script instance is being loaded.
  new protected virtual void Awake() {
    _directEffect = new HxUnityDirectEffect(this);
    HxHand.OnLeftHandInitialized += OnHandInitialized;
    HxHand.OnRightHandInitialized += OnHandInitialized;

    // Sync serialized fields with runtime fields.
    foreach (string coverageRegion in _coverageRegionsArray) {
      if (!coverageRegions.Contains(coverageRegion)) {
        coverageRegions.Add(coverageRegion);
      }
    }

    AddToTactors();
    base.Awake();
  }

  //! Override to define your Haptic Effect.
  //!
  //! @param directInfo Information about a tactor the effect is playing on.
  //! @returns The displacement [m] resulting from the effect.
  protected abstract float GetDisplacementM(HaptxApi.DirectEffect.DirectInfo directInfo);

  //! Adds this effect to all tactors.
  private void AddToTactors() {
    // Find all the tactors in the world that are associated with our rigid body parts.
    IEnumerable<PeripheralLink> peripheralLinks =
        FindObjectsOfType<MonoBehaviour>().OfType<PeripheralLink>();
    foreach (PeripheralLink peripheralLink in peripheralLinks) {
      if (peripheralLink != null && peripheralLink.Peripheral != null) {
        foreach (HaptxApi.Tactor tactor in peripheralLink.Peripheral.tactors) {
          if (coverageRegions.Contains(tactor.coverage_region.getText())) {
            AddToTactor(peripheralLink.Peripheral.id, tactor.id);
          }
        }
      }
    }
  }

  //! Gets bound to HxHand.OnRightHandInitialized and HxHand.OnLeftHandInitialized .
  //!
  //! @param hand The hand that was just initialized.
  private void OnHandInitialized(HxHand hand) {
    AddToTactors();
  }

  //! Wraps HaptxApi.HapticEffectInterface.DirectEffect and associates it with an HxDirectEffect.
  private class HxUnityDirectEffect : HaptxApi.DirectEffect {
    //! The HxDirectEffect component associated with this effect.
    private HxDirectEffect _directEffect = null;

    //! Construct by association with a HxDirectEffect component.
    //!
    //! @param directEffect The HxDirectEffect component associated with this effect.
    public HxUnityDirectEffect(HxDirectEffect directEffect) {
      _directEffect = directEffect;
    }

    public override float getDisplacementM(DirectInfo directInfo) {
      if (_directEffect != null) {
        return _directEffect.GetDisplacementM(directInfo);
      } else {
        return 0.0f;
      }
    }
  }
}
