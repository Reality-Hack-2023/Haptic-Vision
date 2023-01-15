// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using UnityEngine;

//! @brief The abstract, serializable form of HxPhysicalModel.
//!
//! Automatically serialized by HxNodeSerializer.
public abstract class HxPhysicalModelSerialized : INodeSerialized<HxPhysicalModel> {
  public abstract DegreeOfFreedom GetDegreeOfFreedom();
  public abstract void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom);
  public abstract string GetOwningBehaviorName();
  public abstract void SetOwningBehaviorName(string owningBehaviorName);

  //! See INodeSerialized.Deserialize().
  public abstract HxPhysicalModel Deserialize();
}

[System.Serializable]
public class HxConstantModelSerialized : HxPhysicalModelSerialized {
  public DegreeOfFreedom dof;
  public string owningBehaviorName;
  public float constant;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetOwningBehaviorName() {
    return owningBehaviorName;
  }

  public override void SetOwningBehaviorName(string owningBehaviorName) {
    this.owningBehaviorName = owningBehaviorName;
  }

  public override HxPhysicalModel Deserialize() {
    return new HxConstantModel() {
      constant = constant
    };
  }
}

[System.Serializable]
public class HxSpringModelSerialized : HxPhysicalModelSerialized {
  public DegreeOfFreedom dof;
  public string owningBehaviorName;
  public float stiffness;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetOwningBehaviorName() {
    return owningBehaviorName;
  }

  public override void SetOwningBehaviorName(string owningBehaviorName) {
    this.owningBehaviorName = owningBehaviorName;
  }

  public override HxPhysicalModel Deserialize() {
    return new HxSpringModel() {
      stiffness = stiffness
    };
  }
}

[System.Serializable]
public class HxCurveModelSerialized : HxPhysicalModelSerialized {
  public DegreeOfFreedom dof;
  public string owningBehaviorName;
  public CurveAsset curveAsset;
  public float inputScale;
  public float inputOffset;
  public float outputScale;
  public float outputOffset;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetOwningBehaviorName() {
    return owningBehaviorName;
  }

  public override void SetOwningBehaviorName(string owningBehaviorName) {
    this.owningBehaviorName = owningBehaviorName;
  }

  public override HxPhysicalModel Deserialize() {
    return new HxCurveModel() {
      curveAsset = curveAsset,
      inputScale = inputScale,
      inputOffset = inputOffset,
      outputScale = outputScale,
      outputOffset = outputOffset
    };
  }
}

//! @brief Holds all serializable @link HxPhysicalModel HxPhysicalModels @endlink for a given 
//! HxJoint.
//!
//! Automatically manages the serialization of HxPhysicalModels.
[System.Serializable]
public class HxPhysicalModelSerializedContainer :
    HxNodeSerializer<HxPhysicalModel, HxPhysicalModelSerialized, HxDofs> {
  // One list for each implementation of HxPhysicalModel. They are automatically parsed via
  // HxNodeSerializer.
  public List<HxConstantModelSerialized> constantModelsSerialized =
      new List<HxConstantModelSerialized>();
  public List<HxSpringModelSerialized> springModelsSerialized =
      new List<HxSpringModelSerialized>();
  public List<HxCurveModelSerialized> curveModelSerialized =
      new List<HxCurveModelSerialized>();
}
