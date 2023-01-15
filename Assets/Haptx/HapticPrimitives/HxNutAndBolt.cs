using UnityEngine;

//! @brief Game logic for the HaptX Nut and Bolt interaction.
//!
//! Swap out the meshes and collision geometry to make this interaction your own!
[RequireComponent(typeof(Hx1DRotator))]
public class HxNutAndBolt : MonoBehaviour {

  //! Delegate for the #OnAttach event.
  public delegate void OnAttachEvent();

  //! @brief Gets fired when the bolt attaches.
  //! 
  //! Bind this event to perform actions when the bolt is attached.
  public event OnAttachEvent OnAttach;

  //! Delegate for the #OnRelease event.
  public delegate void OnReleaseEvent();

  //! @brief Gets fired when the bolt releases.
  //! 
  //! Bind this event to perform actions when the bolt is released.
  public event OnReleaseEvent OnRelease;

  //! The rigidbody constituting the bolt.
  [Tooltip("The rigidbody constituting the bolt.")]
  public Rigidbody bolt = null;

  //! The transform representing the initial transform of the bolt when it attaches.
  [Tooltip("The transform representing the initial transform of the bolt when it attaches.")]
  public Transform initialAttachmentTransform = null;

  //! The total amount the bolt rotates in the nut.
  [Tooltip("The total amount the bolt rotates in the nut.")]
  public float totalTwistDeg = 3600.0f;

  //! The displacement of the bolt at max rotation. Lerped in-between.
  [Tooltip("The displacement of the bolt at max rotation. Lerped in-between.")]
  public float totalDisplacementM = -0.06f;

  //! Whether the bolt may ever break its connection to the nut.
  [Tooltip("Whether the bolt may ever break its connection to the nut.")]
  public bool releaseEnabled = true;

  //! The amount the bolt must be rotated before it can be released.
  [Tooltip("The amount the bolt must be rotated before it can be released."),
      Range(0.0f, float.MaxValue)]
  public float releaseThresholdDeg = 270.0f;

  //! The distance between the bolt and its initial pose must be less than or equal to this before
  //! it may attach.
  [Tooltip("The distance between the bolt and its initial pose must be less than or equal to this before it may attach."),
      Range(0.0f, float.MaxValue)]
  public float linearAttachToleranceM = 0.03f;

  //! The angle between the bolt's up vector and its initial pose's up vector must be smaller than
  //! or equal to this before the bolt may attach.
  [Tooltip("The angle between the bolt's up vector and its initial pose's up vector must be smaller than or equal to this before the bolt may attach."),
      Range(0.0f, float.MaxValue)]
  public float angularAttachToleranceDeg = 60.0f;

  //! Gets multiplied by "Linear Attach Tolerance M" and "Angular Attach Tolerance Deg" to
  //! determine how far the bolt must translate or rotate from its initial pose before it may be
  //! re-attached.
  [Tooltip("Gets multiplied by \"Linear Attach Tolerance M\" and \"Angular Attach Tolerance Deg\" to determine how far the bolt must translate or rotate from its initial pose before it may be re-attached."),
      RangeAttribute(1.0f, float.MaxValue)]
  public float attachHysteresis = 1.25f;

  //! The joint that holds the bolt in place when it's attached.
  private Hx1DRotator _hx1DRotator = null;

  //! Whether the bolt can release if its twist is less than "Release Threshold Deg".
  private bool _ableToRelease = false;

  //! Whether the bolt can attach if it is within "Linear Attach Tolerance M" and 
  //! "Angular Attach Tolerance Deg".
  private bool _ableToAttach = true;

  //! The last bolt displacement value that was propagated to the joint.
  private float _lastBoltDisplacementM = 0.0f;

  //! Called when the script is being loaded.
  private void Awake() {
    _hx1DRotator = GetComponent<Hx1DRotator>();
  }

  //! Called every fixed framerate frame if enabled.
  private void FixedUpdate() {
    if (_hx1DRotator == null) {
      return;
    }

    if (_hx1DRotator.enabled) {
      float boltRotationDeg = GetBoltRotation();

      if (releaseEnabled) {
        // Release the bolt once it has passed a certain threshold, and gone back down to zero.
        if (Mathf.Abs(boltRotationDeg) <= releaseThresholdDeg) {
          if (_ableToRelease && boltRotationDeg * Mathf.Sign(totalTwistDeg) <= 0.0f) {
            _hx1DRotator.enabled = false;
            _ableToAttach = false;
            if (OnRelease != null) {
              OnRelease();
            }
          }
        } else {
          _ableToRelease = true;
        }
      }

      // Update the translational target based on rotation.
      SetBoltTargetDisplacement(boltRotationDeg * totalDisplacementM / totalTwistDeg);
    } else {
      float linearAttachOffsetM, angularAttachOffsetDeg;
      GetBoltAttachmentOffset(out linearAttachOffsetM, out angularAttachOffsetDeg);
      if (_ableToAttach) {
        // Re-connect the bolt once it has satisfied linear/angular tolerances.
        if (linearAttachOffsetM <= linearAttachToleranceM &&
            angularAttachOffsetDeg <= angularAttachToleranceDeg) {
          if (bolt == null || initialAttachmentTransform == null) {
            HxDebug.LogError(
                "Tried to attach bolt, but failed because a dependent object was null.");
            return;
          }

          // Apply the total twist as joint limits.
          if (totalTwistDeg > 0.0f) {
            _hx1DRotator.SetLimits(-3.0f, totalTwistDeg);
          } else {
            _hx1DRotator.SetLimits(totalTwistDeg, 3.0f);
          }

          // Position the bolt and form the joint.
          Vector3 boltForwardVector = bolt.rotation * Vector3.forward;
          Vector3 initialUpVector = initialAttachmentTransform.rotation * Vector3.up;
          Vector3 projectedBoltForwardVector = (boltForwardVector - Vector3.Dot(boltForwardVector,
              initialUpVector) * initialUpVector).normalized;
          Quaternion boltRotation = Quaternion.LookRotation(projectedBoltForwardVector,
              initialUpVector);
          bolt.transform.SetPositionAndRotation(initialAttachmentTransform.position, boltRotation);
          _hx1DRotator.SetConnectedBody(bolt);
          _ableToRelease = false;
          if (OnAttach != null) {
            OnAttach();
          }
        }
      } else {
        // Only allow the bolt to re-connect once it has traveled a certain distance away.
        if (linearAttachOffsetM >= linearAttachToleranceM * attachHysteresis ||
            angularAttachOffsetDeg > angularAttachToleranceDeg * attachHysteresis) {
          _ableToAttach = true;
        }
      }
    }
  }

  //! Get the current "twist" of the bolt.
  //!
  //! @returns The current "twist" of the bolt.
  private float GetBoltRotation() {
    if (_hx1DRotator != null && _hx1DRotator.enabled && _hx1DRotator.GetOperatingDof() != null) {
      return _hx1DRotator.GetOperatingDof().CurrentPosition;
    }
    return 0.0f;
  }

  //! Set the linear position of the bolt.
  //!
  //! @param targetDisplacementM The new linear position of the bolt.
  private void SetBoltTargetDisplacement(float targetDisplacementM) {
    if (_hx1DRotator != null && _hx1DRotator.enabled &&
        Mathf.Abs(_lastBoltDisplacementM - targetDisplacementM) > 0.0001f) {
      _lastBoltDisplacementM = targetDisplacementM;
      _hx1DRotator.LinearLimitsOffset = _lastBoltDisplacementM * Vector3.up;
    }
  }

  //! Get the offset of the bolt from its initial attachment transform.
  //!
  //! @param [out] linearAttachOffsetM The distance of the bolt from its initial attachment 
  //! position.
  //! @param [out] angularAttachOffsetDeg The angle between the bolt's up vector and that of its 
  //! initial attachment rotation.
  private void GetBoltAttachmentOffset(out float linearAttachOffsetM,
      out float angularAttachOffsetDeg) {
    if (bolt != null && initialAttachmentTransform != null) {
      linearAttachOffsetM = (bolt.position - initialAttachmentTransform.position).magnitude;
      angularAttachOffsetDeg = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(bolt.rotation * Vector3.up,
          initialAttachmentTransform.rotation * Vector3.up));
    } else {
      linearAttachOffsetM = 0.0f;
      angularAttachOffsetDeg = 0.0f;
    }
  }
}
