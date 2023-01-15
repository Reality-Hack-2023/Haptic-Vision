// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;

//! @brief The abstract, serializable form of HxDof.
//!
//! Automatically serialized by HxNodeSerializer.
public abstract class HxDofSerialized : INodeSerialized<HxDof> {
  public abstract DegreeOfFreedom GetDegreeOfFreedom();
  public abstract void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom);

  //! See INodeSerialized.Deserialize().
  public abstract HxDof Deserialize();
}

[System.Serializable]
public class HxLinearDofSerialized : HxDofSerialized {
  public DegreeOfFreedom dof;
  public bool forceUpdate;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override HxDof Deserialize() {
    return new HxLinearDof() {
      forceUpdate = forceUpdate
    };
  }
}

[System.Serializable]
public class HxAngularDofSerialized : HxDofSerialized {
  public DegreeOfFreedom dof;
  public bool forceUpdate;
  public bool trackMultipleRevolutions;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override HxDof Deserialize() {
    return new HxAngularDof() {
      forceUpdate = forceUpdate,
      trackMultipleRevolutions = trackMultipleRevolutions
    };
  }
}

//! @brief Holds all serializable @link HxDof HxDofs @endlink for a given HxJoint.
//!
//! Automatically manages the serialization of HxDofs.
[System.Serializable]
public class HxDofSerializedContainer :
    HxNodeSerializer<HxDof, HxDofSerialized, HxDofs> {
  // One list for each implementation of HxDof. They are automatically parsed via
  // HxNodeSerializer.
  public List<HxLinearDofSerialized> dofsSerialized = new List<HxLinearDofSerialized>();
  public List<HxAngularDofSerialized> angularDofsSerialized =
      new List<HxAngularDofSerialized>();
}
