// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using UnityEngine;

//! @brief A physical behavior that always drives toward the nearest detent.
//!
//! See the @ref section_unity_hx_dof_detent_behavior "Unity Haptic Primitive Guide" for a high
//! level overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxDofDetentBehavior : HxDofBehavior {

  //! The internal list of detent positions (sorted).
  public List<float> Detents {
    get {
      return new List<float>(_detents);
    }
    set {
      _detents = new List<float>(value);
      _detents.Sort();
    }
  }

  //! See #Detents.
  [Tooltip("The list of detent positions.")]
  private List<float> _detents = new List<float>() { 0.0f };

  //! @brief The index of the last targeted detent.
  //!
  //! Invalid targets represented with #InvalidDetentIndex.
  public int DetentIndex {
    get {
      return _detentIndex;
    }
  }

  //! See #DetentIndex.
  private int _detentIndex = InvalidDetentIndex;

  //! @brief The value of the last targeted detent.
  //!
  //! Will return 0 if the last target was invalid.
  public float Detent {
    get {
      if (_detentIndex > -1 && _detentIndex < Detents.Count) {
        return Detents[_detentIndex];
      } else {
        return 0.0f;
      }
    }
  }

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public HxDofDetentBehavior(string name) : base(name) { }

  //! @brief Get the signed magnitude of the force or torque that is associated with @p position
  //! relative to the nearest detent.
  //! 
  //! Defined in anchor2's frame.
  //!
  //! @param position The position of interest.
  //! @returns The signed magnitude of the force or torque.
  public override float GetForceTorque(float position) {
    if (_detents.Count > 0 && model != null) {
      _detentIndex = GetNearestDetentIndex(position, _detentIndex);
      return model.GetOutput(position - _detents[_detentIndex]);
    } else {
      return 0.0f;
    }
  }

  //! Gets the value of nearest detent.
  //!
  //! @param[out] outTarget Populated with the value of the nearest detent.
  //! @returns True.
  public override bool TryGetTarget(out float outTarget) {
    if (_detentIndex > -1) {
      outTarget = _detents[_detentIndex];
      return true;
    } else {
      outTarget = 0.0f;
      return false;
    }
  }

  //! @brief Get the index of the nearest detent to @p value. 
  //!
  //! Starting at @p startingIndex and work outward.
  //!
  //! @returns The index of the nearest detent to @p value, or #InvalidDetentIndex if no valid index exists.
  private int GetNearestDetentIndex(float value, int startingIndex) {
    // Find a valid place to start searching for our nearest state.
    if (_detents.Count < 1) {
      return InvalidDetentIndex;
    } else {
      if (startingIndex < 0 || Mathf.Abs(startingIndex) > _detents.Count - 1) {
        startingIndex = 0;
      }

      // Initialize search variables.
      int currentIndex = startingIndex;
      int closestIndex = startingIndex;
      float smallestDistanceSoFar = Mathf.Abs(_detents[startingIndex] - value);

      // Choose which direction to search.
      bool goLeft = false;
      if (startingIndex - 1 > -1 &&
          Mathf.Abs(_detents[startingIndex - 1] - value) < smallestDistanceSoFar) {
        goLeft = true;
      }

      if (goLeft) {
        // Check to the left, stopping if the beginning is reached, or if the next index is
        // further then the current index.
        while (currentIndex > 0) {
          float nextDistance = Mathf.Abs(_detents[currentIndex - 1] - value);
          if (nextDistance < smallestDistanceSoFar) {
            smallestDistanceSoFar = nextDistance;
            closestIndex = currentIndex - 1;
            currentIndex = closestIndex;
          } else {
            break;
          }
        }
      } else {
        // Check to the right, stopping as soon as a value exceeds smallest distance.
        currentIndex = startingIndex;
        while (currentIndex < _detents.Count - 1) {
          float nextDistance = Mathf.Abs(_detents[currentIndex + 1] - value);
          if (nextDistance < smallestDistanceSoFar) {
            smallestDistanceSoFar = nextDistance;
            closestIndex = currentIndex + 1;
            currentIndex += 1;
          } else {
            break;
          }
        }
      }

      return closestIndex;
    }
  }

  //! The index representing an invalid detent.
  public static readonly int InvalidDetentIndex = -1;

  public override HxDofBehaviorSerialized Serialize() {
    return new HxDofDetentBehaviorSerialized(_detents) {
      acceleration = acceleration,
      enabled = enabled,
      name = name,
      visualize = visualize
    };
  }
}
