using UnityEngine;

//! A class that demonstrates connecting an HxJoint to an HxHapticEffect to render detents.
public class HxDetentLogic : MonoBehaviour {

  //! The Haptic Effect representing the detent.
  [Tooltip("The Haptic Effect representing the detent.")]
  public HxHapticEffect effect = null;

  //! The joint that owns the state function whose state changes trigger the detent.
  [Tooltip("The joint that owns the state function whose state changes trigger the detent.")]
  public Hx1DJoint joint = null;

  //! The name of the state function whose state changes trigger the detent.
  [Tooltip("The name of the state function whose state changes trigger the detent.")]
  public string stateFunctionName = "Function0";

  //! Whether to reset the effect if it's already playing when state changes.
  [Tooltip("Whether to reset the effect if it's already playing when state changes.")]
  public bool restartIfAlreadyPlaying = true;

  // Use this for initialization
  void Start() {
    if (joint != null) {
      HxStateFunction stateFunction = null;
      if (joint.GetOperatingDof() != null && joint.GetOperatingDof().TryGetStateFunctionByName(
          stateFunctionName, out stateFunction)) {
        stateFunction.OnStateChange += OnStateChange;
      }
    }
  }

  //! Called when state #stateFunctionName's changes.
  //!
  //! @param newState The new state.
  void OnStateChange(int newState) {
    if (effect != null) {
      if (effect.IsPlaying()) {
        if (restartIfAlreadyPlaying) {
          effect.Restart();
        }
      } else {
        effect.Play();
      }
    }
  }
}
