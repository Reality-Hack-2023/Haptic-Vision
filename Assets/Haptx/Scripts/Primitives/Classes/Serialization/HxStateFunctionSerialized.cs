// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;

//! @brief The abstract, serializable form of HxStateFunction.
//!
//! Automatically serialized by HxNodeSerializer.
public abstract class HxStateFunctionSerialized : INodeSerialized<HxStateFunction> {
  public abstract DegreeOfFreedom GetDegreeOfFreedom();
  public abstract void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom);
  public abstract string GetName();

  //! See INodeSerialized.Deserialize().
  public abstract HxStateFunction Deserialize();
}

[System.Serializable]
public class Hx2StateFunctionSerialized : HxStateFunctionSerialized {
  public DegreeOfFreedom dof;
  public string name;
  public float transitionPosition;
  public bool invertStateOrder;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetName() {
    return name;
  }

  public override HxStateFunction Deserialize() {
    return new Hx2StateFunction(name) {
      invertStateOrder = invertStateOrder,
      transitionPosition = transitionPosition
    };
  }
}

[System.Serializable]
public class Hx3StateFunctionSerialized : HxStateFunctionSerialized {
  public DegreeOfFreedom dof;
  public string name;
  public float lowTransitionPosition;
  public float highTransitionPosition;
  public bool invertStateOrder;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetName() {
    return name;
  }

  public override HxStateFunction Deserialize() {
    return new Hx3StateFunction(name) {
      invertStateOrder = invertStateOrder,
      lowTransitionPosition = lowTransitionPosition,
      highTransitionPosition = highTransitionPosition
    };
  }
}

[System.Serializable]
public class HxNStateFunctionSerialized : HxStateFunctionSerialized {
  public DegreeOfFreedom dof;
  public string name;
  public bool invertStateOrder;
  public string statePositionsString;

  public HxNStateFunctionSerialized(List<float> statePositions) {
    statePositionsString = ListUtilities.ToString(statePositions);
  }

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetName() {
    return name;
  }

  public override HxStateFunction Deserialize() {
    return new HxNStateFunction(name) {
      invertStateOrder = invertStateOrder,
      StatePositions = ListUtilities.FromString(statePositionsString)
    };
  }
}

[System.Serializable]
public class HxCurveStateFunctionSerialized : HxStateFunctionSerialized {
  public DegreeOfFreedom dof;
  public string name;
  public CurveAsset curveAsset;
  public bool invertStateOrder;
  public float inputScale;
  public float inputOffset;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetName() {
    return name;
  }

  public override HxStateFunction Deserialize() {
    return new HxCurveStateFunction(name) {
      invertStateOrder = invertStateOrder,
      curveAsset = curveAsset,
      inputScale = inputScale,
      inputOffset = inputOffset
    };
  }
}

//! @brief Holds all serializable @link HxStateFunction HxStateFunctions @endlink for a given 
//! HxJoint.
//!
//! Automatically manages the serialization of HxDofBehaviors.
[System.Serializable]
public class HxStateFunctionSerializedContainer :
    HxNodeSerializer<HxStateFunction, HxStateFunctionSerialized, HxDofs> {
  // One list for each implementation of HxStateFunction. They are automatically parsed via
  // HxNodeSerializer.
  public List<Hx2StateFunctionSerialized> twoStateFunctionsSerialized =
      new List<Hx2StateFunctionSerialized>();
  public List<Hx3StateFunctionSerialized> threeStateFunctionsSerialized =
      new List<Hx3StateFunctionSerialized>();
  public List<HxNStateFunctionSerialized> nStateFunctionsSerialized =
      new List<HxNStateFunctionSerialized>();
  public List<HxCurveStateFunctionSerialized> curveStateFunctionsSerialized =
      new List<HxCurveStateFunctionSerialized>();
}
