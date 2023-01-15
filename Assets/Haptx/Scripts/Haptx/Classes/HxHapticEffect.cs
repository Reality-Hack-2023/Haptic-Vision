// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief Defines the basic interface for Haptic Effects.
//!
//! See HaptxApi::HapticEffectInterface::Effect.
public abstract class HxHapticEffect : MonoBehaviour {

  //! Whether this effect awakens already playing.
  [Tooltip("Whether this effect awakens already playing.")]
  [SerializeField]
  protected bool _playOnAwake = true;

  //! Whether this effect stops once its duration is exceeded, or begins anew.
  [Tooltip("Whether this effect stops once its duration is exceeded, or begins anew.")]
  [SerializeField]
  protected bool _isLooping = false;

  //! @brief The underlying Haptic Effect.
  //!
  //! Defined by child classes.
  protected abstract HaptxApi.HapticEffect EffectInternal {
    get;
  }

  //! Awake is called when the script instance is being loaded.
  protected void Awake() {
    if (EffectInternal != null) {
      EffectInternal.setIsLooping(_isLooping);
      if (_playOnAwake) {
        EffectInternal.play();
      }
    } else {
      HxDebug.LogError("HxHapticEffect.Awake(): Underlying effect is null.", this);
    }
  }

  //! This function is called when the object becomes enabled and active.
  private void OnEnable() {
    if (EffectInternal != null) {
      EffectInternal.unpause();
    } else {
      HxDebug.LogError("HxHapticEffect.OnEnable(): Underlying effect is null.", this);
    }
  }

  //! This function is called when the behaviour becomes disabled.
  private void OnDisable() {
    if (EffectInternal != null) {
      EffectInternal.pause();
    } else {
      HxDebug.LogError("HxHapticEffect.OnDisable(): Underlying effect is null.", this);
    }
  }

  //! Play the Haptic Effect.
  public void Play() {
    if (EffectInternal != null) {
      EffectInternal.play();
    } else {
      HxDebug.LogError("HxHapticEffect.Play(): Underlying effect is null.", this);
    }
  }

  //! Get whether the effect is playing and not paused.
  //!
  //! @returns Whether the effect is playing and not paused.
  public bool IsPlaying() {
    if (EffectInternal != null) {
      return EffectInternal.isPlaying();
    } else {
      HxDebug.LogError("HxHapticEffect.IsPlaying(): Underlying effect is null.", this);
    }

    return false;
  }

  //! Pause the Haptic Effect.
  public void Pause() {
    enabled = false;
  }

  //! Get whether the effect is paused.
  //!
  //! @returns Whether the effect is paused.
  public bool IsPaused() {
    if (EffectInternal != null) {
      return EffectInternal.isPaused();
    } else {
      HxDebug.LogError("HxHapticEffect.IsPaused(): Underlying effect is null.", this);
    }

    return enabled;
  }

  //! Unpause the effect.
  public void Unpause() {
    enabled = true;
  }

  //! Whether the effect is looping.
  public bool IsLooping() {
    if (EffectInternal != null) {
      return EffectInternal.isLooping();
    } else {
      HxDebug.LogError("HxHapticEffect.IsLooping(): Underlying effect is null.", this);
    }

    return _isLooping;
  }

  //! Set whether the effect is looping.
  //!
  //! @param isLooping Whether the effect is looping.
  public void SetIsLooping(bool isLooping) {
    this._isLooping = isLooping;
    if (EffectInternal != null) {
      EffectInternal.setIsLooping(isLooping);
    } else {
      HxDebug.LogError("HxHapticEffect.SetIsLooping(): Underlying effect is null.", this);
    }
  }

  //! Stop the Haptic Effect.
  public void Stop() {
    if (EffectInternal != null) {
      EffectInternal.stop();
    }
  }

  //! Restart the effect back to the beginning.
  public void Restart() {
    if (EffectInternal != null) {
      EffectInternal.restart();
    } else {
      HxDebug.LogError("HxHapticEffect.Restart(): Underlying effect is null.", this);
    }
  }

  //! Advance the effect by some delta time.
  //!
  //! @param deltaTimeS The amount of time by which to advance the effect.
  public void Advance(float deltaTimeS) {
    if (EffectInternal != null) {
      EffectInternal.advance(deltaTimeS);
    } else {
      HxDebug.LogError("HxHapticEffect.Advance(): Underlying effect is null.", this);
    }
  }

  //! @brief Get the amount of time [s] the effect has spent playing.
  //!
  //! If looping this will cycle between 0 and duration.
  //!
  //! @returns The amount of time [s] the effect has spent playing.
  public float GetPlayTimeS() {
    if (EffectInternal != null) {
      return EffectInternal.getPlayTimeS();
    } else {
      HxDebug.LogError("HxHapticEffect.GetPlayTime(): Underlying effect is null.", this);
    }

    return 0.0f;
  }

  //! @brief Get the amount of play time after which the effect automatically stops [s].
  //!
  //! Values less than 0 represent infinite duration.
  //!
  //! If the effect is set to loop it will instead reset back to the beginning.
  //!
  //! @returns The amount of play time after which the effect automatically stops [s].
  public float GetDurationS() {
    if (EffectInternal != null) {
      return EffectInternal.getDurationS();
    } else {
      HxDebug.LogError("HxHapticEffect.GetDuration(): Underlying effect is null.", this);
    }

    return 0.0f;
  }

  //! @brief Set the amount of play time after which the effect automatically stops [s].
  //!
  //! Values less than 0 represent infinite duration.
  //!
  //! If the effect is set to loop it will instead reset back to the beginning.
  //!
  //! @param durationS The new amount of play time after which the effect automatically
  //! stops [s].
  public void SetDurationS(float durationS) {
    if (EffectInternal != null) {
      EffectInternal.setDurationS(durationS);
    } else {
      HxDebug.LogError("HxHapticEffect.SetDuration(): Underlying effect is null.", this);
    }
  }
}
