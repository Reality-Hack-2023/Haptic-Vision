// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;

//! @brief The abstract, serializable form of HxDofBehavior.
//!
//! Automatically serialized by HxNodeSerializer.
public abstract class HxDofBehaviorSerialized : INodeSerialized<HxDofBehavior> {
  public abstract DegreeOfFreedom GetDegreeOfFreedom();
  public abstract void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom);
  public abstract string GetName();

  //! See INodeSerialized.Deserialize().
  public abstract HxDofBehavior Deserialize();
}

[System.Serializable]
public class HxDofDefaultBehaviorSerialized : HxDofBehaviorSerialized {
  public bool acceleration;
  public DegreeOfFreedom dof;
  public bool enabled;
  public string name;
  public bool visualize;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetName() {
    return name;
  }

  public override HxDofBehavior Deserialize() {
    return new HxDofDefaultBehavior(name) {
      acceleration = acceleration,
      enabled = enabled,
      visualize = visualize,
    };
  }
}

[System.Serializable]
public class HxDofTargetPositionBehaviorSerialized : HxDofBehaviorSerialized {
  public bool acceleration;
  public DegreeOfFreedom dof;
  public bool enabled;
  public string name;
  public bool visualize;
  public float targetPosition;

  public override DegreeOfFreedom GetDegreeOfFreedom() {
    return dof;
  }

  public override void SetDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    this.dof = degreeOfFreedom;
  }

  public override string GetName() {
    return name;
  }

  public override HxDofBehavior Deserialize() {
    return new HxDofTargetPositionBehavior(name) {
      acceleration = acceleration,
      enabled = enabled,
      visualize = visualize,
      targetPosition = targetPosition
    };
  }
}

[System.Serializable]
public class HxDofDetentBehaviorSerialized : HxDofBehaviorSerialized {
  public bool acceleration;
  public string detentsString;
  public DegreeOfFreedom dof;
  public bool enabled;
  public string name;
  public bool visualize;

  public HxDofDetentBehaviorSerialized(List<float> detents) {
    detentsString = ListUtilities.ToString(detents);
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

  public override HxDofBehavior Deserialize() {
    return new HxDofDetentBehavior(name) {
      acceleration = acceleration,
      Detents = ListUtilities.FromString(detentsString),
      enabled = enabled,
      visualize = visualize,
    };
  }
}

//! @brief Holds all serializable @link HxDofBehavior HxDofBehaviors @endlink for a given HxJoint.
//!
//! Automatically manages the serialization of HxDofBehaviors.
[System.Serializable]
public class HxDofBehaviorSerializedContainer :
    HxNodeSerializer<HxDofBehavior, HxDofBehaviorSerialized, HxDofs> {
  // One list for each implementation of HxDofBehavior. They are automatically parsed via
  // HxNodeSerializer.
  public List<HxDofDefaultBehaviorSerialized> defaultBehaviorsSerialized =
      new List<HxDofDefaultBehaviorSerialized>();
  public List<HxDofTargetPositionBehaviorSerialized> targetPositionBehaviorsSerialized =
      new List<HxDofTargetPositionBehaviorSerialized>();
  public List<HxDofDetentBehaviorSerialized> detentBehaviorsSerialized =
      new List<HxDofDetentBehaviorSerialized>();
}
