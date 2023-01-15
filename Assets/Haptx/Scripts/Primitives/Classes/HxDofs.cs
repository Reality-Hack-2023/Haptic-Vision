// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//! Storage for each HxDof comprising full 6-degree-of-freedom motion.
public class HxDofs : IRootNode<HxDofSerialized>, IRootNode<HxStateFunctionSerialized>,
    IRootNode<HxDofBehaviorSerialized>, IRootNode<HxPhysicalModelSerialized>, IEnumerable<HxDof> {

  //! The X axis linear degree of freedom and its settings.
  public HxLinearDof LinearDofX {
    get {
      return _linearDofX;
    }
  }

  //! See #LinearDofX.
  private HxLinearDof _linearDofX = new HxLinearDof();

  //! The Y axis linear degree of freedom and its settings.
  public HxLinearDof LinearDofY {
    get {
      return _linearDofY;
    }
  }

  //! See #LinearDofY.
  private HxLinearDof _linearDofY = new HxLinearDof();

  //! The Z axis linear degree of freedom and its settings.
  public HxLinearDof LinearDofZ {
    get {
      return _linearDofZ;
    }
  }

  //! See #LinearDofZ.
  private HxLinearDof _linearDofZ = new HxLinearDof();

  //! The X axis (twist) angular degree of freedom and its settings.
  public HxAngularDof AngularDofX {
    get {
      return _angularDofX;
    }
  }

  //! See #AngularDofX.
  private HxAngularDof _angularDofX = new HxAngularDof();

  //! The Y axis (swing1) angular degree of freedom and its settings.
  public HxAngularDof AngularDofY {
    get {
      return _angularDofY;
    }
  }

  //! See #AngularDofY.
  private HxAngularDof _angularDofY = new HxAngularDof();

  //! The Z axis (swing2) angular degree of freedom and its settings.
  public HxAngularDof AngularDofZ {
    get {
      return _angularDofZ;
    }
  }

  //! See #AngularDofZ.
  private HxAngularDof _angularDofZ = new HxAngularDof();

  //! Get an enumerator for all HxDofs.
  //!
  //! @returns An enumerator for all HxDofs.
  public IEnumerator<HxDof> GetEnumerator() {
    yield return _linearDofX;
    yield return _linearDofY;
    yield return _linearDofZ;
    yield return _angularDofX;
    yield return _angularDofY;
    yield return _angularDofZ;
  }

  //! Get an enumerator for all HxDofs.
  //!
  //! @returns An enumerator for all HxDofs.
  IEnumerator IEnumerable.GetEnumerator() {
    return GetEnumerator();
  }

  //! Get the HxDof matching a DegreeOfFreedom.
  //!
  //! @param degreeOfFreedom The degree of freedom of interest.
  //! @returns The matching HxDof.
  public HxDof GetDof(DegreeOfFreedom degreeOfFreedom) {
    switch (degreeOfFreedom) {
      case DegreeOfFreedom.X_LIN:
        return _linearDofX;
      case DegreeOfFreedom.Y_LIN:
        return _linearDofY;
      case DegreeOfFreedom.Z_LIN:
        return _linearDofZ;
      case DegreeOfFreedom.X_ANG:
        return _angularDofX;
      case DegreeOfFreedom.Y_ANG:
        return _angularDofY;
      case DegreeOfFreedom.Z_ANG:
        return _angularDofZ;
      default:
        return null;
    }
  }

  //! Get the HxLinearDof matching a DofAxis.
  //!
  //! @param axis The axis of interest.
  //! @returns The matching HxLinearDof.
  public HxLinearDof GetLinearDof(DofAxis axis) {
    switch (axis) {
      case DofAxis.X:
        return LinearDofX;
      case DofAxis.Y:
        return LinearDofY;
      case DofAxis.Z:
        return LinearDofZ;
      default:
        return null;
    }
  }

  //! Get the HxAngularDof matching a DofAxis.
  //!
  //! @param axis The axis of interest.
  //! @returns The matching HxAngularDof.
  public HxAngularDof GetAngularDof(DofAxis axis) {
    switch (axis) {
      case DofAxis.X:
        return AngularDofX;
      case DofAxis.Y:
        return AngularDofY;
      case DofAxis.Z:
        return AngularDofZ;
      default:
        return null;
    }
  }

  //! Set the HxDof matching a DegreeOfFreedom.
  //!
  //! @param dof The HxDof to set.
  //! @param degreeOfFreedom The degree of freedom of interest.
  public void SetDof(HxDof dof, DegreeOfFreedom degreeOfFreedom) {
    if (degreeOfFreedom.Domain() == DofDomain.LINEAR) {
      HxLinearDof linearDof = (HxLinearDof)dof;
      if (dof != null && linearDof == null) {
        Debug.LogError(string.Format("HxDof {0} cannot operate on {1}. It isn't a {2}.",
            dof, degreeOfFreedom, typeof(HxLinearDof)));
      }
      SetLinearDof(linearDof, degreeOfFreedom.Axis());
    } else {
      HxAngularDof angularDof = (HxAngularDof)dof;
      if (dof != null && angularDof == null) {
        Debug.LogError(string.Format("HxDof {0} cannot operate on {1}. It isn't a {2}.",
            dof, degreeOfFreedom, typeof(HxAngularDof)));
      }
      SetAngularDof(angularDof, degreeOfFreedom.Axis());
    }
  }

  //! Set the HxLinearDof matching a DofAxis.
  //!
  //! @param dof The HxLinearDof to set.
  //! @param axis The axis of interest.
  public void SetLinearDof(HxLinearDof dof, DofAxis axis) {
    switch (axis) {
      case DofAxis.X:
        _linearDofX = dof;
        break;
      case DofAxis.Y:
        _linearDofY = dof;
        break;
      case DofAxis.Z:
        _linearDofZ = dof;
        break;
    }
  }

  //! Set the HxAngularDof matching a DofAxis.
  //!
  //! @param dof The HxAngularDof to set.
  //! @param axis The axis of interest.
  public void SetAngularDof(HxAngularDof dof, DofAxis axis) {
    switch (axis) {
      case DofAxis.X:
        _angularDofX = dof;
        break;
      case DofAxis.Y:
        _angularDofY = dof;
        break;
      case DofAxis.Z:
        _angularDofZ = dof;
        break;
    }
  }

  //! See IRootNode.AddNode().
  public void AddNode(HxDofSerialized dofSerialized) {
    SetDof(dofSerialized.Deserialize(), dofSerialized.GetDegreeOfFreedom());
  }

  //! See IRootNode.GetNodes().
  public void GetNodes(out IEnumerable<HxDofSerialized> nodes) {
    List<HxDofSerialized> serializedDofs = new List<HxDofSerialized>();
    foreach (DegreeOfFreedom degreeOfFreedom in Enum.GetValues(typeof(DegreeOfFreedom))) {
      HxDof dof = GetDof(degreeOfFreedom);
      if (dof == null) continue;

      HxDofSerialized dofSerialized = dof.Serialize();
      dofSerialized.SetDegreeOfFreedom(degreeOfFreedom);
      serializedDofs.Add(dofSerialized);
    }
    nodes = serializedDofs;
  }

  //! See IRootNode.AddNode().
  public void AddNode(HxStateFunctionSerialized stateFunctionSerialized) {
    GetDof(stateFunctionSerialized.GetDegreeOfFreedom()).RegisterStateFunction(
        stateFunctionSerialized.Deserialize());
  }

  //! See IRootNode.GetNodes().
  public void GetNodes(out IEnumerable<HxStateFunctionSerialized> nodes) {
    List<HxStateFunctionSerialized> serializedStateFunctions =
        new List<HxStateFunctionSerialized>();
    foreach (DegreeOfFreedom degreeOfFreedom in Enum.GetValues(typeof(DegreeOfFreedom))) {
      HxDof dof = GetDof(degreeOfFreedom);
      if (dof == null) continue;

      foreach (HxStateFunction stateFunction in dof.StateFunctions) {
        HxStateFunctionSerialized stateFunctionSerialized = stateFunction.Serialize();
        stateFunctionSerialized.SetDegreeOfFreedom(degreeOfFreedom);
        serializedStateFunctions.Add(stateFunctionSerialized);
      }
    }
    nodes = serializedStateFunctions;
  }

  //! See IRootNode.AddNode().
  public void AddNode(HxDofBehaviorSerialized behaviorSerialized) {
    GetDof(behaviorSerialized.GetDegreeOfFreedom()).RegisterBehavior(
        behaviorSerialized.Deserialize());
  }

  //! See IRootNode.GetNodes().
  public void GetNodes(out IEnumerable<HxDofBehaviorSerialized> nodes) {
    List<HxDofBehaviorSerialized> serializedBehaviors =
        new List<HxDofBehaviorSerialized>();
    foreach (DegreeOfFreedom degreeOfFreedom in Enum.GetValues(typeof(DegreeOfFreedom))) {
      HxDof dof = GetDof(degreeOfFreedom);
      if (dof == null) continue;

      foreach (HxDofBehavior behavior in dof.Behaviors) {
        HxDofBehaviorSerialized behaviorSerialized = behavior.Serialize();
        behaviorSerialized.SetDegreeOfFreedom(degreeOfFreedom);
        serializedBehaviors.Add(behaviorSerialized);
      }
    }
    nodes = serializedBehaviors;
  }

  //! See IRootNode.AddNode().
  public void AddNode(HxPhysicalModelSerialized physicalModelSerialized) {
    HxDof dof = GetDof(physicalModelSerialized.GetDegreeOfFreedom());
    HxDofBehavior behavior;
    if (!dof.TryGetBehaviorByName(physicalModelSerialized.GetOwningBehaviorName(),
        out behavior)) {
      Debug.LogError(string.Format(
          "Failed to add HxPhysicalModelSerialized {0}. HxDof {1} does not contain an HxDofBehavior named {2}.",
          physicalModelSerialized, dof, physicalModelSerialized.GetOwningBehaviorName()));
    } else {
      behavior.model = physicalModelSerialized.Deserialize();
    }
  }

  //! See IRootNode.GetNodes().
  public void GetNodes(out IEnumerable<HxPhysicalModelSerialized> nodes) {
    List<HxPhysicalModelSerialized> serializedPhysicalModels =
        new List<HxPhysicalModelSerialized>();
    foreach (DegreeOfFreedom degreeOfFreedom in Enum.GetValues(typeof(DegreeOfFreedom))) {
      HxDof dof = GetDof(degreeOfFreedom);
      if (dof == null) continue;

      foreach (HxDofBehavior behavior in dof.Behaviors) {
        HxPhysicalModelSerialized physicalModelSerialized = behavior.model.Serialize();
        physicalModelSerialized.SetDegreeOfFreedom(degreeOfFreedom);
        physicalModelSerialized.SetOwningBehaviorName(behavior.name);
        serializedPhysicalModels.Add(physicalModelSerialized);
      }
    }
    nodes = serializedPhysicalModels;
  }
}
