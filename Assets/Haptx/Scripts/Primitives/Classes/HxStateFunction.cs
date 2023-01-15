// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using UnityEngine;

//! Delegate for state change events.
public delegate void OnStateChangeEvent(int newState);

//! @brief An enumeration of all types of HxStateFunction.
//! 
//! Primarily for GUIs.
public enum HxStateFunctionType {
  TwoState,
  ThreeState,
  NState,
  Curve
}

//! Extension methods for HxStateFunctionType.
public static class HxStateFunctionTypeExtensions {

  //! Inverse of #enumFromType.
  public static Dictionary<HxStateFunctionType, Type> typeFromEnum =
      new Dictionary<HxStateFunctionType, Type>() {
        { HxStateFunctionType.TwoState, typeof(Hx2StateFunction) },
        { HxStateFunctionType.ThreeState, typeof(Hx3StateFunction) },
        { HxStateFunctionType.NState, typeof(HxNStateFunction) },
        { HxStateFunctionType.Curve, typeof(HxCurveStateFunction) }
      };

  //! @brief Inverse of #typeFromEnum. 
  //!
  //! Gets populated in static constructor.
  public static Dictionary<Type, HxStateFunctionType> enumFromType =
      new Dictionary<Type, HxStateFunctionType>();

  //! Static constructor.
  static HxStateFunctionTypeExtensions() {
    // Mirror typeFromEnum into enumFromType.
    foreach (KeyValuePair<HxStateFunctionType, Type> keyValue in typeFromEnum) {
      enumFromType.Add(keyValue.Value, keyValue.Key);
    }
  }

  //! Create an HxStateFunction of the class matching the given HxStateFunctionType.
  //!
  //! @param typeEnum The type of state function to instantiate.
  //! @param name The name to give this state function.
  //! @returns An instance of type @p typeEnum with name @p @name.
  public static HxStateFunction CreateByTypeEnum(HxStateFunctionType typeEnum, string name) {
    Type type = typeFromEnum[typeEnum];
    return (HxStateFunction)Activator.CreateInstance(type, name);
  }
}

//! @brief The base class for all custom state functions used in 
//! @link HxJoint HxJoints @endlink. 
//!
//! See the @ref section_unity_hx_state_function "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public abstract class HxStateFunction : INode<HxStateFunctionSerialized> {

  //! @brief The name of this state function.
  //!
  //! Unique for a given HxDof. See HxDof.RegisterStateFunction().
  [Tooltip("The name of this HxStateFunction.")]
  public readonly string name = string.Empty;

  //! Represents an invalid state.
  public static readonly int InvalidState = -100;

  //! By default, state values increase as position increases. Set this to true to invert
  //! that ordering.
  [Tooltip("By default, state values increase as position increases. Set this to true to invert that ordering.")]
  public bool invertStateOrder = false;

  //! Get the state of this function as of the last call to #Update().
  public int CurrentState {
    // Get the current state of this function (from the last call to update()).
    get {
      return _currentState;
    }
    // Changes the current state. Fires an event if this state differs from the current one.
    protected set {
      if (_currentState != value) {
        _currentState = value;
        if (OnStateChange != null) {
          OnStateChange(_currentState);
        }
      }
    }
  }

  //! See #CurrentState.
  private int _currentState = 0;

  //! @brief Event that fires when state changes.
  //!
  //! See @ref section_unity_hx_state_function for an example of how to bind this event.
  public OnStateChangeEvent OnStateChange;

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public HxStateFunction(string name) {
    this.name = name;
  }

  //! Updates the underlying state machine with a new position.
  //!
  //! @param inputPosition The new position.
  //! @returns The state at the provided position.
  public abstract int Update(float inputPosition);

  //! See INode.Serialize().
  public abstract HxStateFunctionSerialized Serialize();
}

//! @brief A state function with two states that change about a transition position.
//!
//! See the @ref section_unity_hx_state_function "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class Hx2StateFunction : HxStateFunction {

  //! The position along the degree of freedom where the transition from 0 to 1 happens.
  [Tooltip("The position along the degree of freedom where the transition from 0 to 1 happens")]
  public float transitionPosition = 0.0f;

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public Hx2StateFunction(string name) : base(name) {
  }

  //! @brief Updates the state.
  //!
  //! 0 is the most negative portion of positions unless #invertStateOrder is true.
  //!
  //! @param inputPosition The new position.
  //! @returns 0 or 1. 
  public override int Update(float inputPosition) {
    CurrentState = invertStateOrder == (inputPosition < transitionPosition) ? 1 : 0;
    return CurrentState;
  }

  public override HxStateFunctionSerialized Serialize() {
    return new Hx2StateFunctionSerialized {
      name = name,
      invertStateOrder = invertStateOrder,
      transitionPosition = transitionPosition
    };
  }
}

//! @brief A state function with three states that change about high and low transition positions.
//!
//! See the @ref section_unity_hx_state_function "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class Hx3StateFunction : HxStateFunction {

  //! @brief The position along the degree of freedom where the transition from low to mid happens.
  //!
  //! Will output InvalidState if lowTransitionPosition >= highTransitionPosition.
  [Tooltip("The position along the degree of freedom where the transition from low to mid happens.")]
  public float lowTransitionPosition = 0.0f;

  //! @brief The position along the degree of freedom where the transition from mid to high happens.
  //!
  //! Will output InvalidState if lowTransitionPosition >= highTransitionPosition.
  [Tooltip("The position along the degree of freedom where the transition from mid to high happens.")]
  public float highTransitionPosition = 0.0f;

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public Hx3StateFunction(string name) : base(name) {
  }

  // Normally returns 0-2. 0 is the most negative portion of positions unless invertStateOrder is true.
  // Will always output InvalidState if lowTransitionPosition >= highTransitionPosition.
  public override int Update(float inputPosition) {
    int newState = InvalidState;
    if (lowTransitionPosition >= highTransitionPosition) {
      HxDebug.LogWarning(string.Format(
          "Low transition position higher then high transition position in Hx3StateFunction {0}: low {1} > high {2}",
          name, lowTransitionPosition, highTransitionPosition));
    } else if (inputPosition < lowTransitionPosition) {
      newState = invertStateOrder ? 2 : 0;
    } else if (inputPosition < highTransitionPosition) {
      newState = 1;
    } else {
      newState = invertStateOrder ? 0 : 2;
    }
    CurrentState = newState;
    return CurrentState;
  }

  // Create an Hx3StateFunctionSerialized instance matching this instance.
  public override HxStateFunctionSerialized Serialize() {
    return new Hx3StateFunctionSerialized {
      name = name,
      invertStateOrder = invertStateOrder,
      lowTransitionPosition = lowTransitionPosition,
      highTransitionPosition = highTransitionPosition
    };
  }
}

//! @brief A state function whose state matches the index of the nearest of N positions.
//!
//! See the @ref section_unity_hx_state_function "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxNStateFunction : HxStateFunction {

  //! The list of state positions (sorted).
  public List<float> StatePositions {
    get {
      return new List<float>(_statePositions);
    }
    set {
      _statePositions = new List<float>(value);
      _statePositions.Sort();
      if (invertStateOrder) {
        _statePositions.Reverse();
      }
    }
  }

  //! See #StatePositions.
  [Tooltip("The list of state positions (sorted).")]
  private List<float> _statePositions = new List<float>() { 0.0f };

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public HxNStateFunction(string name) : base(name) {
  }

  // Returns the index of the state position that is nearest the input position. State positions 
  // are sorted, so 0 always corresponds to positions lower than the smallest position, N-1 
  // corresponds to positions higher than the largest position, and the rest of the states are 
  // sorted in-between. If invertStateOrder is true, that is flipped. Will always output
  // InvalidState if the array is empty.
  public override int Update(float inputPosition) {
    if (_statePositions.Count < 1) {
      CurrentState = InvalidState;
      return InvalidState;
    } else {
      // Find a valid place to start searching for our nearest state.
      int startingIndex = CurrentState - 1;
      if (startingIndex < 0) {
        startingIndex = 0;
      } else if (startingIndex > _statePositions.Count - 1) {
        startingIndex = _statePositions.Count - 1;
      }

      // Initialize search variables.
      int currentIndex = startingIndex;
      int closestIndex = startingIndex;
      float smallestDistanceSoFar = Mathf.Abs(_statePositions[currentIndex] - inputPosition);

      // Choose which direction to search.
      bool goLeft = false;
      if (startingIndex - 1 > -1 &&
          Mathf.Abs(_statePositions[startingIndex - 1] - inputPosition) < smallestDistanceSoFar) {
        goLeft = true;
      }

      if (goLeft) {
        // Check to the left, stopping if the beginning is reached, or if the next index is
        // further then the current index.
        while (currentIndex > 0) {
          float nextDistance = Mathf.Abs(_statePositions[currentIndex - 1] - inputPosition);
          if (smallestDistanceSoFar > nextDistance) {
            smallestDistanceSoFar = nextDistance;
            closestIndex = currentIndex - 1;
            currentIndex = closestIndex;
          } else {
            break;
          }
        }
      } else {
        // Now check to the right, stopping as soon as a value exceeds smallest distance.
        currentIndex = startingIndex;
        while (currentIndex < _statePositions.Count - 1) {
          float nextDistance = Mathf.Abs(_statePositions[currentIndex + 1] - inputPosition);
          if (smallestDistanceSoFar > nextDistance) {
            smallestDistanceSoFar = nextDistance;
            closestIndex = currentIndex + 1;
            currentIndex += 1;
          } else {
            break;
          }
        }
      }

      CurrentState = closestIndex;
      return CurrentState;
    }
  }

  // Create an HxNStateFunctionSerialized instance matching this instance.
  public override HxStateFunctionSerialized Serialize() {
    return new HxNStateFunctionSerialized(_statePositions) {
      name = name,
      invertStateOrder = invertStateOrder
    };
  }
}

//! @brief A state function whose states correspond to the output of a #Curve.
//!
//! See the @ref section_unity_hx_state_function "Unity Haptic Primitive Guide" for a high level
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public class HxCurveStateFunction : HxStateFunction {

  //! See #Curve.
  [Tooltip("The curve asset that defines this state function.")]
  public CurveAsset curveAsset = null;

  //! @brief The amount by which to scale the input to the curve.
  //!
  //! It is 'a' in: y = f(ax + b). The units of 'x' are [m] on 
  //! @link HxLinearDof HxLinearDofs @endlink and [deg] on 
  //! @link HxAngularDof HxAngularDofs @endlink.
  [Tooltip("The amount by which to scale the input to the curve.")]
  public float inputScale = 1.0f;

  //! @brief The amount by which to offset the input to the curve. 
  //!
  //! It is 'b' in: y = f(ax + b). The units of 'x' are [m] on 
  //! @link HxLinearDof HxLinearDofs @endlink and [deg] on 
  //! @link HxAngularDof HxAngularDofs @endlink.
  [Tooltip("The amount by which to offset the input to the curve.")]
  public float inputOffset = 0.0f;

  //! The curve that defines this state function.
  public AnimationCurve Curve {
    get {
      return curveAsset == null ? null : curveAsset.curve;
    }
  }

  //! Constructs using the given name.
  //!
  //! @param name The given name.
  public HxCurveStateFunction(string name) : base(name) {
  }

  //! Updates the state.
  //!
  //! @param inputPosition The new position.
  //! @returns A state extracted from the output of #Curve rounded to the nearest integer. If 
  //! the curve is not set, returns InvalidState.
  public override int Update(float inputPosition) {
    if (Curve == null) {
      CurrentState = InvalidState;
    } else {
      CurrentState = (int)Mathf.Round(Curve.Evaluate(inputPosition *
          (invertStateOrder ? -1.0f : 1.0f) * inputScale + inputOffset));
    }
    return CurrentState;
  }

  public override HxStateFunctionSerialized Serialize() {
    return new HxCurveStateFunctionSerialized {
      name = name,
      invertStateOrder = invertStateOrder,
      curveAsset = curveAsset,
      inputScale = inputScale,
      inputOffset = inputOffset
    };
  }
}
