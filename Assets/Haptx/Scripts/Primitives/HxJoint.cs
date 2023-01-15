// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using UnityEngine;

//! @brief An extension of 
//! [ConfigurableJoint](https://docs.unity3d.com/ScriptReference/ConfigurableJoint.html)
//! that adds additional functionality and allows for an arbitrary number of custom @link 
//! HxDofBehavior HxDofBehaviors @endlink and @link HxStateFunction HxStateFunctions @endlink 
//! to be defined in constraint (anchor2) space about the six cardinal degrees of freedom.
//!
//! Rotations are represented in degrees. For rotations the absolute value might be > 180 if 
//! HxAngularDof.trackMultipleRevolutions is true. We measure rotations around a 
//! single axis using a vector projection onto the associated plane. This does not use any
//! Euler angles. This means that functions related to rotations may not do what you're expecting 
//! if the constrained object can rotate around more than one axis.
//!
//! See the @ref section_unity_hx_joint "Unity Haptic Primitive Guide" for a high 
//! level overview.
//!
//! @ingroup group_unity_haptic_primitives
[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class HxJoint : MonoBehaviour, ISerializationCallbackReceiver {

  //! The two anchors involved in a joint.
  public enum Anchor : uint {
    ANCHOR1,  //!< Anchor1 (the child).
    ANCHOR2   //!< Anchor2 (the parent).
  }

  //! @brief A space in which anchor1 has meaning.
  //! 
  //! Gets used by HxJoint.GetAnchor1Transform();
  public enum Anchor1Space : uint {
    WORLD,  //!< Relative to the world.
    BODY1   //!< Relative to body1.
  }

  //! @brief A space in which anchor2 has meaning.
  //! 
  //! Gets used by HxJoint.GetAnchor2Transform();
  public enum Anchor2Space : uint {
    WORLD,  //!< Relative to the world.
    BODY2   //!< Relative to body2.
  }

  //! The spaces in which forces, torques, impulses, and angular impulses may be applied to 
  //! anchors.
  public enum AnchorForceTorqueSpace : uint {
    WORLD,    //!< In world space.
    ANCHOR2   //!< In anchor2 space.
  };

  //! The bodies participating in the joint.
  private enum JointBody : uint {
    BODY1,  //!< Body1 (the child).
    BODY2   //!< Body2 (the parent).
  };

  //! The @link HxDof HxDofs @endlink defining the degrees of freedom of this joint.
  [NonSerialized]
  public HxDofs dofs = new HxDofs();

  //! @brief Whether to visualize the transforms of the anchors where each component is being 
  //! constrained.
  //!
  //! This is helpful for debugging the setup of your joint.
  [Tooltip("Whether to visualize the transforms of the anchors where each body is being constrained.")]
  public bool visualizeAnchors = false;

  //! @brief An offset applied to the HxJoint linear limits of this joint.
  //!
  //! E.G. if the linear limit is 10, and this value is 10 in the forward direction, 
  //! then the acceptable range of motion in the forward direction is 0 to 20 instead of -10 to 10.
  //! Only gets reflected after calls to #UpdateJoint().
  public Vector3 LinearLimitsOffset {
    get {
      return _linearLimitsOffset;
    }

    set {
      _linearLimitsOffset = value;
      UpdateJoint();
    }
  }

  //! See #LinearLimitsOffset
  [SerializeField]
  [Tooltip("An offset applied to the linear limit of this joint.")]
  protected Vector3 _linearLimitsOffset = Vector3.zero;

  //! @brief An offset applied to the HxJoint angular limits of this joint.
  //!
  //! E.G. if the angular limit is 10, and this value is 10 in the forward direction, 
  //! then the acceptable range of motion in the forward direction is 0 to 20 instead of -10 to 10.
  //! Only gets reflected after calls to #UpdateJoint().
  public Vector3 AngularLimitsOffset {
    get {
      return _angularLimitsOffset;
    }

    set {
      _angularLimitsOffset = value;
      UpdateJoint();
    }
  }

  //! See #AngularLimitsOffset.
  [SerializeField]
  [Tooltip("An offset applied to the angular limits of this joint.")]
  protected Vector3 _angularLimitsOffset = Vector3.zero;

  //! @brief The settings used to drive the underlying ConfigurableJoint (when not frozen). 
  //!
  //! May be configured at runtime by child classes.
  //!
  //! Changes to most of these parameters will be reflected after the next call to 
  //! #UpdateJoint(). The parameters that will not be properly reflected include: connectedBody, 
  //! configuredInWorldSpace, anchor, autoConfigureConnectedAnchor, and connectedAnchor. Those 
  //! parameters *will* be reflected if the joint is re-formed via #SetConnectedBody().
  //! 
  //! If #Frozen is true, then the settings in ConfigurableJoint will deviate from 
  //! #_hxJointParameters as necessary.
  [NonSerialized]
  public ConfigurableJointParameters _hxJointParameters =
      new ConfigurableJointParameters();

  //! The underlying ConfigurableJoint.
  private ConfigurableJoint _configurableJoint = null;

  //! See #HxJoint.SupportedConfigurableJointParameters.
  [SerializeField]
  [Tooltip("Supported ConfigurableJoint parameters.")]
  private SupportedConfigurableJointParameters _supportedConfigurableJointParametersHaptxJoint =
      new SupportedConfigurableJointParameters();

  //! @brief A pointer to connectedBody (body1). 
  //!
  //! Will be null if #Body (body2) is constrained to the world.
  public Rigidbody ConnectedBody {
    get {
      return _body1;
    }
    set {
      SetConnectedBody(value);
    }
  }

  //! See #ConnectedBody.
  protected Rigidbody _body1 = null;

  //! @brief A pointer to the rigidbody on this GameObject (body2). 
  //!
  //! Will be null if #ConnectedBody (body1) is constrained to the world.
  public Rigidbody Body {
    get {
      return _body2;
    }
  }

  //! See #Body.
  protected Rigidbody _body2 = null;

  //! Whether this joint is currently frozen by a call to #Freeze().
  public bool Frozen {
    get {
      return _frozen;
    }
    set {
      if (value) {
        Freeze();
      } else {
        Unfreeze();
      }
    }
  }

  //! See #Frozen.
  private bool _frozen = false;

  //! @brief The transform of anchor2 in body2's frame.
  //!
  //! Gets updated when InitJoint() is called.
  private Matrix4x4 _l2Anchor2 = Matrix4x4.identity;

  //! @brief The transform of body2 in anchor2's frame.
  //!
  //! Gets updated when InitJoint() is called.
  private Matrix4x4 _a2Body2 = Matrix4x4.identity;

  //! @brief A GameObject whose transform represents anchor2.
  //!
  //! Gets updated when InitJoint() is called.
  private GameObject _anchor2 = null;

  //! @brief The transform of anchor2 in body2' frame.
  //!
  //! Gets updated when InitJoint() is called.
  private Matrix4x4 _l1Anchor1 = Matrix4x4.identity;

  //! @brief The transform of body1 in anchor1's frame.
  //!
  //! Gets updated when InitJoint() is called.
  private Matrix4x4 _a1Body1 = Matrix4x4.identity;

  //! @brief A GameObject whose transform represents anchor1.
  //!
  //! Gets updated when InitJoint() is called.
  private GameObject _anchor1 = null;

  //! Scales visualizers based on the size of the constrained components and the lengths of
  //! their anchors.
  private float _visScale = 0.0f;

  //! A list of all "stop drawing" actions that need to be performed at the beginning of 
  //! each #FixedUpdate() call.
  private List<Action> _stopDrawingActions = new List<Action>();

  //! The sleep monitor for body1.
  private HxSleepMonitor _body1SleepMonitor = null;

  //! The sleep monitor for body2.
  private HxSleepMonitor _body2SleepMonitor = null;

  //! Called when the script is being loaded.
  protected virtual void Awake() {
    _hxJointParameters = GetInitialJointParameters();
  }

  //! Called when the object becomes enabled and active.
  private void OnEnable() {
    // Initialize the joint in the editor so visualizers can be seen.
    if (Application.isEditor && !Application.isPlaying) {
      _hxJointParameters = GetInitialJointParameters();
    }

    InitJoint();

    // Initialize the joint in the editor so visualizers can be seen.
    if (Application.isEditor && !Application.isPlaying) {
      UpdateInternal();
    }
  }

  //! This function is called when the behaviour becomes disabled.
  private void OnDisable() {
    DestroyHelperGameObjects();

    // This gets created in UpdateJoint().
    if (_configurableJoint != null) {
      Destroy(_configurableJoint);
      _configurableJoint = null;
    }
  }

  //! Called every fixed framerate frame if enabled.
  protected virtual void FixedUpdate() {
    // Stop drawing any meshes from last frame.
    foreach (Action stopDrawingAction in _stopDrawingActions) {
      if (stopDrawingAction != null) {
        stopDrawingAction();
      }
    }
    _stopDrawingActions.Clear();

    // Don't execute if the joint itself is sleeping.
    if (IsSleeping()) {
      return;
    }

    UpdateInternal();
  }

  //! Called every frame if enabled.
  protected virtual void Update() {
    // The relative length to draw coordinate system axes.
    const float CoordSysLengthScale = 1.2f;
    // The relative thickness to draw coordinate system axes.
    const float CoordSysThicknessScale = 0.033f;
    // The color to draw anchor1's coordinate system.
    Color Anchor1Color = Color.red;
    // The color to draw anchor2's coordinate system.
    Color Anchor2Color = Color.blue;
    // The relative size to draw anchor coordinate systems.
    const float AnchorThicknessScale = 0.066f;

    // Refresh the joint since changes may have been made to configurableJointParameters.
    if (Application.isEditor && !Application.isPlaying) {
      _hxJointParameters = GetInitialJointParameters();
      InitJoint();
      UpdateInternal();
    }

    // Visualize anchors.
    if (visualizeAnchors &&
        (_body1 != null || _body2 != null)) {
      float wCoordSysLength = CoordSysLengthScale * _visScale;
      float wCoordSysThickness = CoordSysThicknessScale * _visScale;
      float wAnchorThickness = AnchorThicknessScale * _visScale;

      // Draw anchor1 in the world.
      Matrix4x4 wAnchor1 = GetAnchor1Transform(Anchor1Space.WORLD);
      _stopDrawingActions.Add(HxDebugMesh.DrawCoordinateFrame(
          wAnchor1.MultiplyPoint3x4(Vector3.zero), wAnchor1.rotation,
          wCoordSysLength, wCoordSysThickness, false));

      // Draw a line between body1's COM and its anchor.
      if (_body1 != null) {
        _stopDrawingActions.Add(HxDebugMesh.DrawLine(
            _body1.worldCenterOfMass,
            wAnchor1.MultiplyPoint3x4(Vector3.zero),
            wAnchorThickness,
            Anchor1Color,
            false));
      }

      // Draw anchor2 in the world.
      Matrix4x4 wAnchor2 = GetAnchor2Transform(Anchor2Space.WORLD);
      _stopDrawingActions.Add(HxDebugMesh.DrawCoordinateFrame(
          wAnchor2.MultiplyPoint3x4(Vector3.zero), wAnchor2.rotation, wCoordSysLength,
          wCoordSysThickness, false));

      // Draw a line between component2's COM and its anchor.
      if (_body2 != null) {
        _stopDrawingActions.Add(HxDebugMesh.DrawLine(
            _body2.worldCenterOfMass,
            wAnchor2.MultiplyPoint3x4(Vector3.zero),
            wAnchorThickness,
            Anchor2Color,
            false));
      }
    }
  }

  //! Called every frame if enabled.
  private void UpdateInternal() {
    // Calculate the current position of anchor1 in anchor2's frame. This is the "constraint space"
    // transform that drives behaviors and states.
    Matrix4x4 a2Anchor1 = CalculateJointTransform();

    // Determine whether each body is sleeping (from the HaptX perspective).
    bool body1Sleeping = _body1SleepMonitor != null && _body1SleepMonitor.IsSleeping();
    bool body2Sleeping = _body2SleepMonitor != null && _body2SleepMonitor.IsSleeping();

    // Update each HxDof with new positions, then apply physical behaviors.
    foreach (DegreeOfFreedom degreeOfFreedom in Enum.GetValues(typeof(DegreeOfFreedom))) {
      HxDof dof = dofs.GetDof(degreeOfFreedom);
      if (dof == null || !dof.ShouldUpdate()) {
        continue;
      }
      DofAxis axis = degreeOfFreedom.Axis();
      Vector3 direction = degreeOfFreedom.Axis().GetDirection();
      // If we care about the linear position, not the angular position.
      float inPosition;
      if (degreeOfFreedom.Domain() == DofDomain.LINEAR) {
        inPosition = Transform1D.GetTranslation(a2Anchor1, axis);
      } else {
        inPosition = Transform1D.GetRotation(a2Anchor1, axis);
      }
      dof.Update(inPosition);

      // Apply physical behaviors.
      foreach (HxDofBehavior behavior in dof.Behaviors) {
        if (behavior != null && behavior.enabled) {
          // Calculate the signed magnitude of the force or torque being applied to each body
          // at its anchor.
          float a2ForceTorqueSmag = behavior.GetForceTorque(dof.CurrentPosition);

          // It's a force!
          if (degreeOfFreedom.Domain() == DofDomain.LINEAR) {
            if (!body1Sleeping) {
              AddForceAtAnchor(a2ForceTorqueSmag * direction, Anchor.ANCHOR1,
                AnchorForceTorqueSpace.ANCHOR2, behavior.acceleration ?
                ForceMode.Acceleration : ForceMode.Force, behavior.visualize);
            }
            if (!body2Sleeping) {
              AddForceAtAnchor(-a2ForceTorqueSmag * direction, Anchor.ANCHOR2,
                AnchorForceTorqueSpace.ANCHOR2, behavior.acceleration ?
                ForceMode.Acceleration : ForceMode.Force, behavior.visualize);
            }
          } else {  // It's a torque!
            if (!body1Sleeping) {
              AddTorqueAtAnchor(a2ForceTorqueSmag * direction, Anchor.ANCHOR1,
                AnchorForceTorqueSpace.ANCHOR2, behavior.acceleration ?
                ForceMode.Acceleration : ForceMode.Force, behavior.visualize);
            }
            if (!body2Sleeping) {
              AddTorqueAtAnchor(-a2ForceTorqueSmag * direction, Anchor.ANCHOR2,
                AnchorForceTorqueSpace.ANCHOR2, behavior.acceleration ?
                ForceMode.Acceleration : ForceMode.Force, behavior.visualize);
            }
          }
        }
      }

      // Manage state functions.
      foreach (HxStateFunction stateFunction in dof.StateFunctions) {
        stateFunction.Update(dof.CurrentPosition);
      }
    }
  }

  //! Changes which rigidbody is connected, and re-initializes the joint.
  //!
  //! @param connectedBody The new joined rigidbody.
  public void SetConnectedBody(Rigidbody connectedBody) {
    _hxJointParameters.ConnectedBody = connectedBody;
    if (enabled) {
      InitJoint();
    } else {
      enabled = true;
    }
  }

  //! Updates the joint with any changes made to underlying settings.
  public void UpdateJoint() {
    if (!Application.isPlaying || !enabled) {
      return;
    }

    // Perform any configuration logic.
    ConfigureJoint();

    // If this joint is frozen, override limits offsets.
    Vector3 a2LinearLimitsOffset;
    Matrix4x4 a2AngularLimitsOffset;
    if (Frozen) {
      Matrix4x4 a2Anchor1 = CalculateJointTransform();
      a2LinearLimitsOffset = a2Anchor1.MultiplyPoint3x4(Vector3.zero);
      a2AngularLimitsOffset = Matrix4x4.Rotate(a2Anchor1.rotation);
    } else {
      a2LinearLimitsOffset = _linearLimitsOffset;
      a2AngularLimitsOffset = Matrix4x4.Rotate(Quaternion.Euler(_angularLimitsOffset));
    }

    // To replace the underlying configurable joint without affecting the positions of each HxDof 
    // we first set the relative positions of the anchors to zero, then we reform the joint and
    // return the anchors to their original positions.
    Matrix4x4 wAnchorNoScale2 = _l2Anchor2;
    Vector3 l2ScaleW = Vector3.one;
    if (_body2 != null) {
      wAnchorNoScale2 = Matrix4x4.TRS(
          _body2.transform.position,
          _body2.transform.rotation,
          Vector3.one) * wAnchorNoScale2;
      l2ScaleW = new Vector3(
          1.0f / _body2.transform.lossyScale.x,
          1.0f / _body2.transform.lossyScale.y,
          1.0f / _body2.transform.lossyScale.z);
    }

    Matrix4x4 savedPosition = Matrix4x4.identity;
    if (_body1 != null) {
      savedPosition = Matrix4x4.TRS(
          _body1.transform.position,
          _body1.transform.rotation,
          Vector3.one);
      Matrix4x4 initialPosition = wAnchorNoScale2 * a2AngularLimitsOffset * _a1Body1;
      SetBodyPositionAndRotation(JointBody.BODY1, initialPosition.MultiplyPoint3x4(Vector3.zero),
          initialPosition.rotation);
    } else if (_body2 != null) {
      savedPosition = Matrix4x4.TRS(
          _body2.transform.position,
          _body2.transform.rotation,
          Vector3.one);
      Matrix4x4 initialPosition = wAnchorNoScale2 * a2AngularLimitsOffset.inverse * _a2Body2;
      SetBodyPositionAndRotation(JointBody.BODY2, initialPosition.MultiplyPoint3x4(Vector3.zero),
          initialPosition.rotation);
    }

    // Reform the underlying configurable joint.
    if (_configurableJoint != null) {
      Destroy(_configurableJoint);
    }
    _configurableJoint = _hxJointParameters.AddConfigurableJointToGameObject(gameObject);
    _configurableJoint.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
    if (Frozen) {
      _configurableJoint.xMotion = ConfigurableJointMotion.Locked;
      _configurableJoint.yMotion = ConfigurableJointMotion.Locked;
      _configurableJoint.zMotion = ConfigurableJointMotion.Locked;
      _configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
      _configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
      _configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;
    }

    // Disable autoConfigureConnectedAnchor so connectedAnchor doesn't also change when change
    // values to apply offsets.
    _configurableJoint.autoConfigureConnectedAnchor = false;

    // Apply linear limits offset.
    Vector3 l2LinearLimitsOffset =
        _l2Anchor2.MultiplyVector(Vector3.Scale(a2LinearLimitsOffset, l2ScaleW));
    _configurableJoint.anchor += l2LinearLimitsOffset;

    // Restore the moved body back to its initial transform.
    SetBodyPositionAndRotation(_body1 != null ? JointBody.BODY1 : JointBody.BODY2,
        savedPosition.MultiplyPoint3x4(Vector3.zero), savedPosition.rotation);
  }

  //! @brief Teleports body1 such that anchor1 now has the given position and rotation in 
  //! anchor2's frame.
  //!
  //! Ignores transform hierarchy.
  //!
  //! @param newLocation Anchor1's new location.
  //! @param newRotation Anchor1's new rotation.
  public void TeleportAnchor1(Vector3 newLocation, Quaternion newRotation) {
    if (_body1 == null || !enabled) {
      return;
    }

    // Construct a transform out of the new position and rotation.
    Matrix4x4 a2AnchorTarget1 = Matrix4x4.TRS(newLocation, newRotation, Vector3.one);

    // Calculate bone2's world location and rotation.
    Matrix4x4 wBody2 = Matrix4x4.identity;
    if (_body2 != null) {
      wBody2 = Matrix4x4.TRS(_body2.transform.position, _body2.transform.rotation, Vector3.one);
    }

    // Calculate body1's new world location and rotation.
    Matrix4x4 wBody1 = wBody2 * _l2Anchor2 * a2AnchorTarget1 * _a1Body1;

    // Do the teleport.
    SetBodyPositionAndRotation(JointBody.BODY1, wBody1.MultiplyPoint3x4(Vector3.zero),
        wBody1.rotation);

    // If we're frozen we have to update our joint or we'll snap back to the previous position.
    if (Frozen) {
      UpdateJoint();
    }

    // Update each HxDof.
    foreach (DegreeOfFreedom degreeOfFreedom in Enum.GetValues(typeof(DegreeOfFreedom))) {
      HxDof dof = GetDof(degreeOfFreedom);
      if (dof != null) {
        if (degreeOfFreedom.Domain() == DofDomain.LINEAR) {
          dof.Update(Transform1D.GetTranslation(a2AnchorTarget1, degreeOfFreedom.Axis()), true);
        } else {
          dof.Update(Transform1D.GetRotation(a2AnchorTarget1, degreeOfFreedom.Axis()), true);
        }
      }
    }
  }

  //! @brief Teleports body1 such that anchor1 now has the given position in anchor2's frame 
  //! along a specified degree of freedom.
  //!
  //! Ignores transform hierarchy.
  //!
  //! @param newPosition Anchor1's new position along anchor2's @p degreeOfFreedom.
  //! @param degreeOfFreedom Anchor2's degree of freedom to teleport along.
  public void TeleportAnchor1AlongDof(float newPosition,
      DegreeOfFreedom degreeOfFreedom) {
    if (_body1 == null || !enabled) {
      return;
    }

    // The current location and rotation about the given direction.
    Matrix4x4 a2Anchor1 = CalculateJointTransform();

    // Perform the teleportation.
    Vector3 direction = degreeOfFreedom.Axis().GetDirection();
    if (degreeOfFreedom.Domain() == DofDomain.ANGULAR) {
      TeleportAnchor1(
          a2Anchor1.MultiplyPoint3x4(Vector3.zero),
          (a2Anchor1.rotation * Quaternion.AngleAxis(
              newPosition - Transform1D.GetRotation(a2Anchor1, degreeOfFreedom.Axis()),
              direction)));
    } else {
      TeleportAnchor1(
          a2Anchor1.MultiplyPoint3x4(Vector3.zero) +
          (newPosition - Transform1D.GetTranslation(a2Anchor1, degreeOfFreedom.Axis())) *
          direction, a2Anchor1.rotation);
    }

    // Update each HxDof.
    HxDof dof = dofs.GetDof(degreeOfFreedom);
    if (dof != null) {
      dof.Update(newPosition, true);
    }
  }

  // The relative thickness to draw torque arrows.
  private static readonly float VisForceTorqueThicknessScale = 0.011f;

  //! Apply force at anchor.
  //!
  //! @param force The force value to apply.
  //! @param anchor Which anchor to apply @p force to.
  //! @param space The space to apply @p force in.
  //! @param forceMode What mode to apply @p force in.
  //! @param visualize Whether to visualize @p force. Lasts for one frame.
  public void AddForceAtAnchor(Vector3 force, Anchor anchor, AnchorForceTorqueSpace space,
      ForceMode forceMode = ForceMode.Force, bool visualize = false) {
    // The relative length to draw force arrows.
    const float ForceScale = 0.1f;
    // The color to draw force arrows.
    Color ForceColor = Color.red;

    if (!enabled) {
      return;
    }

    if (anchor == Anchor.ANCHOR1) {
      if (_body1 != null && !_body1.isKinematic) {
        // Calculate the force in world space.
        Vector3 wForce1 = force;
        if (space == AnchorForceTorqueSpace.ANCHOR2) {
          Matrix4x4 wAnchor2 = GetAnchor2Transform(Anchor2Space.WORLD);
          wForce1 = wAnchor2.rotation * force;
        }

        if (wForce1.magnitude > Mathf.Epsilon) {
          // Calculate the torque generated by the force.
          Matrix4x4 wAnchor1 = GetAnchor1Transform(Anchor1Space.WORLD);
          Vector3 wAnchorPosition1 = wAnchor1.MultiplyPoint3x4(Vector3.zero);
          Vector3 wDistance1 = wAnchorPosition1 - _body1.worldCenterOfMass;
          Vector3 wTorque1 = Vector3.Cross(wDistance1, wForce1);

          // Apply the force and torque.
          _body1.AddForce(wForce1, forceMode);
          _body1.AddTorque(wTorque1, forceMode);

          // Visualize the force/impulse.
          if (visualize) {
            // Visualize the force.
            _stopDrawingActions.Add(HxDebugMesh.DrawLine(
                wAnchorPosition1,
                wAnchorPosition1 + ForceScale * wForce1,
                VisForceTorqueThicknessScale * _visScale,
                ForceColor,
                Application.isPlaying));
          }
        }
      }
    } else {
      if (_body2 != null && !_body2.isKinematic) {
        // Calculate the force in world space.
        Vector3 wForce2 = force;
        Matrix4x4 wAnchor2 = GetAnchor2Transform(Anchor2Space.WORLD);
        if (space == AnchorForceTorqueSpace.ANCHOR2) {
          wForce2 = wAnchor2.rotation * wForce2;
        }

        if (wForce2.magnitude > Mathf.Epsilon) {
          // Calculate the torque generated by the force.
          Vector3 wAnchorPosition2 = wAnchor2.MultiplyPoint3x4(Vector3.zero);
          Vector3 wDistance2 = wAnchorPosition2 - _body2.worldCenterOfMass;
          Vector3 wTorque2 = Vector3.Cross(wDistance2, wForce2);

          // Apply as either a force or an impulse.
          _body2.AddForce(wForce2, forceMode);
          _body2.AddTorque(wTorque2, forceMode);

          // Visualize the force/impulse.
          if (visualize) {
            // Visualize the force.
            _stopDrawingActions.Add(HxDebugMesh.DrawLine(
                wAnchorPosition2,
                wAnchorPosition2 + ForceScale * wForce2,
                VisForceTorqueThicknessScale * _visScale,
                ForceColor,
                Application.isPlaying));
          }
        }
      }
    }
  }

  //! Apply torque at anchor.
  //!
  //! @param torque The torque value to apply.
  //! @param anchor Which anchor to apply @p torque to.
  //! @param space The space to apply @p torque in.
  //! @param forceMode What mode to apply @p torque in.
  //! @param visualize Whether to visualize @p torque. Lasts for one frame.
  public void AddTorqueAtAnchor(Vector3 torque, Anchor anchor, AnchorForceTorqueSpace space,
      ForceMode forceMode = ForceMode.Force, bool visualize = false) {
    // The relative length to draw torque arrows.  
    const float TorqueScale = 0.01f;
    // The color to draw torque arrows.
    Color TorqueColor = Color.blue;

    if (!enabled) {
      return;
    }

    Vector3 wTorque = torque;
    if (space == AnchorForceTorqueSpace.ANCHOR2) {
      Matrix4x4 wAnchor2 = GetAnchor2Transform(Anchor2Space.WORLD);
      wTorque = wAnchor2.rotation * wTorque;
    }

    if (wTorque.magnitude > Mathf.Epsilon) {
      if (anchor == Anchor.ANCHOR1) {
        if (_body1 != null && !_body1.isKinematic) {
          _body1.AddTorque(wTorque, forceMode);

          // Visualize the applied torque/impulse.
          if (visualize) {
            _stopDrawingActions.Add(HxDebugMesh.DrawLine(
                _body1.worldCenterOfMass,
                _body1.worldCenterOfMass + TorqueScale * wTorque,
                VisForceTorqueThicknessScale * _visScale,
                TorqueColor,
                Application.isPlaying));
          }
        }
      } else {
        if (_body2 != null && !_body2.isKinematic) {
          _body2.AddTorque(wTorque, forceMode);

          // Visualize the applied torque/impulse.
          if (visualize) {
            _stopDrawingActions.Add(HxDebugMesh.DrawLine(
                _body2.worldCenterOfMass,
                _body2.worldCenterOfMass + TorqueScale * wTorque,
                VisForceTorqueThicknessScale * _visScale,
                TorqueColor,
                Application.isPlaying));
          }
        }
      }
    }
  }

  //! @brief Get the transform of anchor1 in @p space.
  //!
  //! Scale is always one.
  //!
  //! @param space The space to get anchor1's transform in.
  //! @returns The transform of anchor1 in @p space.
  public Matrix4x4 GetAnchor1Transform(Anchor1Space space) {
    if (!enabled) {
      return Matrix4x4.identity;
    }

    switch (space) {
      case Anchor1Space.WORLD: {
          // Calculate the current position of anchor1 in the world's frame.
          if (_anchor1 == null) {
            HxDebug.LogError("Invalid anchor1 GameObject.", this);
            return Matrix4x4.identity;
          } else {
            if (_anchor1.transform.lossyScale != Vector3.one) {
              SyncAnchor1();
            }
            return _anchor1.transform.localToWorldMatrix;
          }
        }
      case Anchor1Space.BODY1:
        // The current position of anchor1 in body1's frame is known.
        return _l1Anchor1;
      default:
        // This should never happen.
        Debug.LogError("HxJoint.getAnchor1Transform() called with an invalid Anchor1Space.", this);
        return Matrix4x4.identity;
    }
  }

  //! @brief Get the transform of anchor2 in @p space.
  //!
  //! Scale is always one.
  //!
  //! @param space The space to get anchor2's transform in.
  //! @returns The transform of anchor2 in @p space.
  public Matrix4x4 GetAnchor2Transform(Anchor2Space space) {
    if (!enabled) {
      return Matrix4x4.identity;
    }

    switch (space) {
      case Anchor2Space.WORLD: {
          // Calculate the current position of anchor2 in the world's frame.
          if (_anchor2 == null) {
            HxDebug.LogError("Invalid anchor2 GameObject.", this);
            return Matrix4x4.identity;
          } else {
            if (_anchor2.transform.lossyScale != Vector3.one) {
              SyncAnchor2();
            }
            return _anchor2.transform.localToWorldMatrix;
          }
        }
      case Anchor2Space.BODY2:
        // The current position of anchor2 in body2's frame is known.
        return _l2Anchor2;
      default:
        // This should never happen.
        Debug.LogError("HxJoint.getAnchor2Transform() called with an invalid Anchor2Space.", this);
        return Matrix4x4.identity;
    }
  }

  //! @brief Calculates the constraint space transform being used to drive physical behaviors and 
  //! states. 
  //!
  //! This is the local transform of body1's anchor in body2's anchor's frame. Scale will always be
  //! one. See @ref section_unity_hx_joint_constraint_space for more information about constraint 
  //! space.
  //!
  //! @returns The constraint space transform being used to drive physical behaviors and 
  //! states. 
  public Matrix4x4 CalculateJointTransform() {
    if (!enabled) {
      return Matrix4x4.identity;
    }

    // Calculate the inverse of the world position of anchor2.
    Matrix4x4 a2World = GetAnchor2Transform(Anchor2Space.WORLD).inverse;

    // Calculate the world position of anchor1.
    Matrix4x4 wAnchor1 = GetAnchor1Transform(Anchor1Space.WORLD);

    // Calculate the current position of anchor1 in anchor2's frame. This is the "constraint space"
    // transform that drives behaviors and states.
    return a2World * wAnchor1;
  }

  //! Get the HxDof for the given degree of freedom.
  //!
  //! @param degreeOfFreedom The degree of freedom of interest.
  //! @returns The corresponding HxDof.
  public HxDof GetDof(DegreeOfFreedom degreeOfFreedom) {
    return dofs.GetDof(degreeOfFreedom);
  }

  //! Get the HxLinearDof for the given axis.
  //!
  //! @param axis The axis of interest.
  //! @returns The corresponding HxLinearDof.
  public HxLinearDof GetLinearDof(DofAxis axis) {
    return dofs.GetLinearDof(axis);
  }

  //! Get the HxAngularDof for the given axis.
  //!
  //! @param axis The axis of interest.
  //! @returns The corresponding HxAngularDof.
  public HxAngularDof GetAngularDof(DofAxis axis) {
    return dofs.GetAngularDof(axis);
  }

  //! Get the current position of anchor1 in anchor2's frame about a given degree of 
  //! freedom.
  //!
  //! @param degreeOfFreedom The degree of freedom of interest.
  //! @returns The current position of anchor1 in anchor2's frame.
  public float GetPositionAlongDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom) {
    HxDof dof = dofs.GetDof(degreeOfFreedom);
    if (dof != null) {
      return dof.CurrentPosition;
    } else {
      return 0.0f;
    }
  }

  //! Freeze the joint in place, locking all degrees of freedom.
  public void Freeze() {
    if (!_frozen) {
      _frozen = true;
      UpdateJoint();
    }
  }

  //! Unfreeze the joint, allowing motion defined by other settings once more.
  public void Unfreeze() {
    if (_frozen) {
      _frozen = false;
      UpdateJoint();
    }
  }

  //! @brief Put this joint to sleep.
  //!
  //! Puts to sleep any rigidbodies participating in the joint.
  public void Sleep() {
    if (_body1 != null) {
      _body1.Sleep();
    }
    if (_body1SleepMonitor != null) {
      _body1SleepMonitor.Sleep();
    }
    if (_body2 != null) {
      _body2.Sleep();
    }
    if (_body2SleepMonitor != null) {
      _body2SleepMonitor.Sleep();
    }
  }

  //! @brief Wake this joint up.
  //!
  //! Awakens any rigidbodies participating in the joint.
  public void WakeUp() {
    if (_body1 != null) {
      _body1.WakeUp();
    }
    if (_body1SleepMonitor != null) {
      _body1SleepMonitor.WakeUp();
    }
    if (_body2 != null) {
      _body2.WakeUp();
    }
    if (_body2SleepMonitor != null) {
      _body2SleepMonitor.WakeUp();
    }
  }

  //! @brief Whether this joint is sleeping.
  //!
  //! The joint will sleep if all non-null rigidbodies participating in the joint are sleeping.
  //!
  //! @returns True if this joint is sleeping.
  public bool IsSleeping() {
    return (_body1 == null || _body1.IsSleeping()) && (_body2 == null || _body2.IsSleeping());
  }

  //! @brief Gets initial ConfigurableJoint settings. 
  //! 
  //! May be overridden by child classes for different effects.
  protected virtual ConfigurableJointParameters GetInitialJointParameters() {
    return _supportedConfigurableJointParametersHaptxJoint.Unwrap();
  }

  //! @brief Configures lower-level ConfigurableJoint and higher-level HaptX settings on this
  //! component.
  //!
  //! Gets called just-in-time in #UpdateJoint().
  protected virtual void ConfigureJoint() { }

  //! Initializes both HxJoint and ConfigurableJoint functionality.
  protected void InitJoint() {
    // A lower bound on the size of this visualizer.
    const float VisScaleMin = 0.01f;

    // Update member variables.
    _body1 = _hxJointParameters.ConnectedBody;
    _body2 = GetComponent<Rigidbody>();
    if (_hxJointParameters.swapBodies) {
      Rigidbody bodyTemp = _body1;
      _body1 = _body2;
      _body2 = bodyTemp;
    }

    // Update GameObjects.
    CreateHelperGameObjects();

    // Calculate information about the two bodies.
    Matrix4x4 wBodyNoScale1 = Matrix4x4.identity;  // The world transform of body1 with scale one.
    Vector3 wScale1 = Vector3.one;  // The world scale of body1.
    float wBodySize1 = 0.0f;  // The world size of body1.
    if (_body1 != null) {
      wBodyNoScale1 = Matrix4x4.TRS(
          _body1.transform.position,
          _body1.transform.rotation,
          Vector3.one);
      wScale1 = _body1.transform.lossyScale;
      wBodySize1 = HxShared.GetGameObjectBounds(_body1.gameObject).extents.magnitude;
    }
    Matrix4x4 wBodyNoScale2 = Matrix4x4.identity;  // The world transform of body2 with scale one.
    Vector3 wScale2 = Vector3.one;  // The world scale of body2.
    float wBodySize2 = 0.0f;  // The world size of body2.
    if (_body2 != null) {
      wBodyNoScale2 = Matrix4x4.TRS(
          _body2.transform.position,
          _body2.transform.rotation,
          Vector3.one);
      wScale2 = _body2.transform.lossyScale;
      wBodySize2 = HxShared.GetGameObjectBounds(_body2.gameObject).extents.magnitude;
    }

    // The initial rotation of the joint. If configuredInWorldSpace is false, this is in
    // body2's frame. If configuredInWorldSpace is true, this is in the world's frame.
    Quaternion jointAnchorsRotation = GetJointAnchorsRotation();

    // Calculate the position anchor2 in body2's frame.
    if (!_hxJointParameters.configuredInWorldSpace) {
      _l2Anchor2 = Matrix4x4.TRS(
          Vector3.Scale(wScale2, _hxJointParameters.anchor),
          jointAnchorsRotation,
          Vector3.one);
    } else {
      _l2Anchor2 = Matrix4x4.TRS(
          Vector3.Scale(wScale2, _hxJointParameters.anchor),
          (Quaternion.Inverse(wBodyNoScale2.rotation) * jointAnchorsRotation),
          Vector3.one);
    }
    _a2Body2 = _l2Anchor2.inverse;

    // Calculate the position anchor1 in body1's frame.
    Vector3 l1Anchor1Position = _hxJointParameters.autoConfigureConnectedAnchor ?
        (wBodyNoScale1.inverse * wBodyNoScale2).MultiplyPoint3x4(
        _l2Anchor2.MultiplyPoint3x4(Vector3.zero)) : 
        Vector3.Scale(wScale1, _hxJointParameters.connectedAnchor);
    if (!_hxJointParameters.configuredInWorldSpace) {
      _l1Anchor1 = Matrix4x4.TRS(
          l1Anchor1Position,
          (Quaternion.Inverse(wBodyNoScale1.rotation) * wBodyNoScale2.rotation *
          jointAnchorsRotation), Vector3.one);
    } else {
      _l1Anchor1 = Matrix4x4.TRS(
          l1Anchor1Position,
          (Quaternion.Inverse(wBodyNoScale1.rotation) * jointAnchorsRotation),
          Vector3.one);
    }
    _a1Body1 = _l1Anchor1.inverse;

    // Position anchor GameObjects at their respective anchors.
    SyncAnchor2();
    SyncAnchor1();

    // Size the visualizers based on object extents and anchor lengths.
    _visScale = 0.0f;
    float wAnchorLength1 = _l1Anchor1.MultiplyPoint3x4(Vector3.zero).magnitude;
    float wAnchorLength2 = _l2Anchor2.MultiplyPoint3x4(Vector3.zero).magnitude;
    if (Math.Abs(wBodySize1) < float.Epsilon) {
      _visScale = wBodySize2;
    } else if (Math.Abs(wBodySize2) < float.Epsilon) {
      _visScale = wBodySize1;
    } else if (Math.Abs(wAnchorLength1) < float.Epsilon) {
      _visScale = wBodySize1;
    } else if (Math.Abs(wAnchorLength2) < float.Epsilon) {
      _visScale = wBodySize2;
    } else {
      float anchorLengthRatio = wAnchorLength1 / (wAnchorLength1 + wAnchorLength2);
      _visScale = Mathf.Lerp(wBodySize1, wBodySize2, anchorLengthRatio);
    }
    _visScale = Math.Max(_visScale, VisScaleMin);

    // Only create and configure a ConfigurableJoint component if the application is running.
    UpdateJoint();
  }

  //! @brief Calculate the initial rotation of the joint. 
  //! 
  //! If hxJointParameters.configuredInWorldSpace is false, this is in body2's frame. If
  //! hxJointParameters.configuredInWorldSpace is true, this is in the world's frame.
  private Quaternion GetJointAnchorsRotation() {
    Vector3 tertiaryAxis = Vector3.Cross(_hxJointParameters.axis,
    _hxJointParameters.secondaryAxis);
    if (tertiaryAxis.magnitude > 0.0f) {
      tertiaryAxis.Normalize();
      return Quaternion.LookRotation(
          tertiaryAxis,
          Vector3.Cross(tertiaryAxis, _hxJointParameters.axis));
    } else {
      return Quaternion.identity;
    }
  }

  //! Set the world transform of either body. 
  //!
  //! @param jointBody Which body to set the transform of.
  //! @param position The position to set.
  //! @param rotation The rotation to set.
  //! @param ignoreParenting Whether to ignore the transform hierarchy when performing this 
  //! operation.
  private void SetBodyPositionAndRotation(JointBody jointBody, Vector3 position,
      Quaternion rotation, bool ignoreParenting = true) {
    Rigidbody movingBody = jointBody == JointBody.BODY1 ? _body1 : _body2;
    Rigidbody otherBody = jointBody == JointBody.BODY1 ? _body2 : _body1;

    if (movingBody != null) {
      Transform otherParentBackup = null;
      if (otherBody != null && ignoreParenting) {
        otherParentBackup = otherBody.transform.parent;
        otherBody.transform.parent = null;
      }
      movingBody.transform.SetPositionAndRotation(position, rotation);
      if (otherBody != null && ignoreParenting) {
        otherBody.transform.parent = otherParentBackup;
      }
    }
  }

  //! Syncs the position and rotation of #_anchor1 with #_l1Anchor1 and sets its scale to 
  //! Vector3.one.
  private void SyncAnchor1() {
    if (_anchor1 != null) {
      Transform parent = null;
      Matrix4x4 wAnchor1 = _l1Anchor1;
      if (_body1 != null) {
        Matrix4x4 wBody1 = Matrix4x4.TRS(_body1.transform.position, _body1.transform.rotation,
            Vector3.one);
        wAnchor1 = wBody1 * _l1Anchor1;
        parent = _body1.transform;
      }

      _anchor1.transform.parent = null;
      _anchor1.transform.position = wAnchor1.MultiplyPoint3x4(Vector3.zero);
      _anchor1.transform.rotation = wAnchor1.rotation;
      _anchor1.transform.localScale = Vector3.one;
      _anchor1.transform.parent = parent;
    }
  }

  //! Syncs the position and rotation of #_anchor2 with #_l2Anchor2 and sets its scale to 
  //! Vector3.one.
  private void SyncAnchor2() {
    if (_anchor2 != null) {
      Transform parent = null;
      Matrix4x4 wAnchor2 = _l2Anchor2;
      if (_body2 != null) {
        Matrix4x4 wBody2 = Matrix4x4.TRS(_body2.transform.position, _body2.transform.rotation,
            Vector3.one);
        wAnchor2 = wBody2 * _l2Anchor2;
        parent = _body2.transform;
      }

      _anchor2.transform.parent = null;
      _anchor2.transform.position = wAnchor2.MultiplyPoint3x4(Vector3.zero);
      _anchor2.transform.rotation = wAnchor2.rotation;
      _anchor2.transform.localScale = Vector3.one;
      _anchor2.transform.parent = parent;
    }
  }

  //! @brief Creates all GameObjects required by this component.
  //!
  //! Depends on #_body1 and #_body1 being properly set.
  private void CreateHelperGameObjects() {
    DestroyHelperGameObjects();

    _anchor1 = new GameObject();
    _anchor1.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
    _anchor2 = new GameObject();
    _anchor2.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;

    if (Application.isPlaying) {
      if (_body1 != null) {
        _body1SleepMonitor = _body1.GetComponent<HxSleepMonitor>();
        if (_body1SleepMonitor == null) {
          _body1SleepMonitor = _body1.gameObject.AddComponent<HxSleepMonitor>();
          _body1SleepMonitor.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
          _body1SleepMonitor.NotifyMonitoringBegin();
        }
      }
      if (_body2 != null) {
        _body2SleepMonitor = _body2.GetComponent<HxSleepMonitor>();
        if (_body2SleepMonitor == null) {
          _body2SleepMonitor = _body2.gameObject.AddComponent<HxSleepMonitor>();
          _body2SleepMonitor.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
          _body2SleepMonitor.NotifyMonitoringBegin();
        }
      }
    }
  }

  //! Destroys all GameObjects created by this component.
  private void DestroyHelperGameObjects() {
    if (Application.isPlaying) {
      if (_anchor1 != null) {
        Destroy(_anchor1);
        _anchor1 = null;
      }
      if (_anchor2 != null) {
        Destroy(_anchor2);
        _anchor2 = null;
      }
      if (_body1SleepMonitor != null) {
        _body1SleepMonitor.NotifyMonitoringEnd();
        _body1SleepMonitor = null;
      }
      if (_body2SleepMonitor != null) {
        _body2SleepMonitor.NotifyMonitoringEnd();
        _body2SleepMonitor = null;
      }
    }
  }

  // Serialized fields.
  [SerializeField]
  private HxDofSerializedContainer dofsSerialized = new HxDofSerializedContainer();
  [SerializeField]
  private HxStateFunctionSerializedContainer stateFunctionsSerialized =
      new HxStateFunctionSerializedContainer();
  [SerializeField]
  private HxDofBehaviorSerializedContainer dofBehaviorsSerialized =
      new HxDofBehaviorSerializedContainer();
  [SerializeField]
  private HxPhysicalModelSerializedContainer physicalModelsSerialized =
      new HxPhysicalModelSerializedContainer();

  //! Called after Unity serializes this object.
  public void OnAfterDeserialize() {
    dofs = new HxDofs();
    dofsSerialized.Deserialize(dofs);
    stateFunctionsSerialized.Deserialize(dofs);
    dofBehaviorsSerialized.Deserialize(dofs);
    physicalModelsSerialized.Deserialize(dofs);
  }

  //! Called before Unity serializes this object.
  public void OnBeforeSerialize() {
    dofsSerialized = new HxDofSerializedContainer();
    dofsSerialized.Serialize(dofs);

    stateFunctionsSerialized = new HxStateFunctionSerializedContainer();
    stateFunctionsSerialized.Serialize(dofs);

    dofBehaviorsSerialized = new HxDofBehaviorSerializedContainer();
    dofBehaviorsSerialized.Serialize(dofs);

    physicalModelsSerialized = new HxPhysicalModelSerializedContainer();
    physicalModelsSerialized.Serialize(dofs);
  }

  //! @brief Wrapper class for ConfigurableJointParameters that contains only fields supported by 
  //! HxJoint.
  //!
  //! Comments and tool-tips sourced from 
  //! https://docs.unity3d.com/Manual/class-ConfigurableJoint.html;
  [Serializable]
  private class SupportedConfigurableJointParameters {

    //! Wraps ConfigurableJointParameters.connectedBody.
    [Tooltip("The other Rigidbody object to which the joint is connected.")]
    public Rigidbody connectedBody = null;

    //! Wraps ConfigurableJointParameters.anchor.
    [Tooltip("The point where the center of the joint is defined. All physics-based simulation will use this point as the center in calculations")]
    public Vector3 anchor = Vector3.zero;

    //! Wraps ConfigurableJointParameters.axis.
    [Tooltip("The local axis that will define the object’s natural rotation based on physics simulation")]
    public Vector3 axis = new Vector3(1.0f, 0.0f, 0.0f);

    //! Wraps ConfigurableJointParameters.autoConfigureConnectedAnchor.
    [Tooltip("If this is enabled, then the Connected Anchor position will be calculated automatically to match the global position of the anchor property.")]
    public bool autoConfigureConnectedAnchor = true;

    //! Wraps ConfigurableJointParameters.connectedAnchor.
    [Tooltip("Manual configuration of the connected anchor position.")]
    public Vector3 connectedAnchor = Vector3.zero;

    //! Wraps ConfigurableJointParameters.secondaryAxis.
    [Tooltip("Together, Axis and Secondary Axis define the local coordinate system of the joint. The third axis is set to be orthogonal to the other two.")]
    public Vector3 secondaryAxis = new Vector3(0.0f, 1.0f, 0.0f);

    //! Wraps ConfigurableJointParameters.xMotion.
    [Tooltip("Allow movement along the X axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion xMotion = ConfigurableJointMotion.Free;

    //! Wraps ConfigurableJointParameters.yMotion.
    [Tooltip("Allow movement along the Y axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion yMotion = ConfigurableJointMotion.Free;

    //! Wraps ConfigurableJointParameters.zMotion.
    [Tooltip("Allow movement along the Z axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion zMotion = ConfigurableJointMotion.Free;

    //! Wraps ConfigurableJointParameters.angularXMotion.
    [Tooltip("Allow movement along the X axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion angularXMotion = ConfigurableJointMotion.Free;

    //! Wraps ConfigurableJointParameters.angularYMotion.
    [Tooltip("Allow movement along the Y axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion angularYMotion = ConfigurableJointMotion.Free;

    //! Wraps ConfigurableJointParameters.angularZMotion.
    [Tooltip("Allow movement along the Z axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion angularZMotion = ConfigurableJointMotion.Free;

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimitSpring.linearLimitSpring.
    [Tooltip("A spring force applied to pull the object back when it goes past the limit position.")]
    public ConfigurableJointParameters.SerializedSoftJointLimitSpring linearLimitSpring =
        new ConfigurableJointParameters.SerializedSoftJointLimitSpring();

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimit.linearLimit.
    [Tooltip("Limit on the joint’s linear movement (ie, movement over distance rather than rotation), specified as a distance from the joint’s origin.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit linearLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimitSpring.angularXLimitSpring.
    [Tooltip("A spring torque applied to rotate the object back when it goes past the limit angle of the joint.")]
    public ConfigurableJointParameters.SerializedSoftJointLimitSpring angularXLimitSpring =
        new ConfigurableJointParameters.SerializedSoftJointLimitSpring();

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimit.lowAngularXLimit.
    [Tooltip("Lower limit on the joint’s rotation around the X axis, specified as a angle from the joint’s original rotation.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit lowAngularXLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimit.highAngularXLimit.
    [Tooltip("This is similar to the Low Angular X Limit property described above but it determines the upper angular limit of the joint’s rotation rather than the lower limit.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit highAngularXLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimitSpring.angularYZLimitSpring.
    [Tooltip("This is similar to the Angular X Limit Spring described above but applies to rotation around both the Y and Z axes.")]
    public ConfigurableJointParameters.SerializedSoftJointLimitSpring angularYZLimitSpring =
        new ConfigurableJointParameters.SerializedSoftJointLimitSpring();

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimit.angularYLimit.
    [Tooltip("Analogous to the Angular X Limit properties described above but applies to the Y axis and regards both the upper and lower angular limits as being the same.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit angularYLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimit.angularZLimit.
    [Tooltip("Analogous to the Angular X Limit properties described above but applies to the Z axis and regards both the upper and lower angular limits as being the same.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit angularZLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();

    //! Wraps SerializedJointDrive.xDrive.
    [Tooltip("The drive force that moves the joint linearly along its local X axis.")]
    public SerializedJointDrive xDrive = new SerializedJointDrive();

    //! Wraps SerializedJointDrive.yDrive.
    [Tooltip("This is analogous to the X Drive described above but applies to the joint’s Y axis.")]
    public SerializedJointDrive yDrive = new SerializedJointDrive();

    //! Wraps SerializedJointDrive.zDrive.
    [Tooltip("This is analogous to the X Drive described above but applies to the joint’s Z axis.")]
    public SerializedJointDrive zDrive = new SerializedJointDrive();

    //! Wraps SerializedJointDrive.angularXDrive.
    [Tooltip("This specifies how the joint will be rotated by the drive torque around its local X axis. It is used only if the Rotation Drive Mode property described above is set to X & YZ.")]
    public SerializedJointDrive angularXDrive = new SerializedJointDrive();

    //! Wraps SerializedJointDrive.angularYZDrive.
    [Tooltip("This is analogous to the Angular X Drive described above but applies to both the joint’s Y and Z axes.")]
    public SerializedJointDrive angularYZDrive = new SerializedJointDrive();

    //! Wraps ConfigurableJointParameters.projectionMode.
    [Tooltip("This defines how the joint will be snapped back to its constraints when it unexpectedly moves beyond them (due to the physics engine being unable to reconcile the current combination of forces within the simulation).")]
    public JointProjectionMode projectionMode = JointProjectionMode.PositionAndRotation;

    //! Wraps ConfigurableJointParameters.projectionDistance.
    [Tooltip("The distance the joint must move beyond its constraints before the physics engine will attempt to snap it back to an acceptable position.")]
    public float projectionDistance = 0.1f;

    //! Wraps ConfigurableJointParameters.projectionAngle.
    [Tooltip("The angle the joint must rotate beyond its constraints before the physics engine will attempt to snap it back to an acceptable position.")]
    public float projectionAngle = 180.0f;

    //! Wraps ConfigurableJointParameters.configuredInWorldSpace.
    [Tooltip("Should the values set by the various target and drive properties be calculated in world space instead of the object’s local space?")]
    public bool configuredInWorldSpace = false;

    //! Wraps ConfigurableJointParameters.enableCollision.
    [Tooltip("Should the object with the joint be able to collide with the connected object (as opposed to just passing through each other)?")]
    public bool enableCollision = false;

    //! Wraps ConfigurableJointParameters.enablePreprocessing.
    [Tooltip("If preprocessing is disabled then certain “impossible” configurations of the joint will be kept more stable rather than drifting wildly out of control.")]
    public bool enablePreprocessing = true;

    //! Wraps ConfigurableJointParameters.massScale.
    [Tooltip("The scale to apply to the inverse mass and inertia tensor of the body prior to solving the constraints.")]
    public float massScale = 1.0f;

    //! Wraps ConfigurableJointParameters.connectedMassScale.
    [Tooltip("The scale to apply to the inverse mass and inertia tensor of the connected body prior to solving the constraints.")]
    public float connectedMassScale = 1.0f;

    //! Unwraps this instance.
    //!
    //! @returns The unwrapped instance.
    public ConfigurableJointParameters Unwrap() {
      return new ConfigurableJointParameters {
        ConnectedBody = connectedBody,
        anchor = anchor,
        axis = axis,
        autoConfigureConnectedAnchor = autoConfigureConnectedAnchor,
        connectedAnchor = connectedAnchor,
        secondaryAxis = secondaryAxis,
        xMotion = xMotion,
        yMotion = yMotion,
        zMotion = zMotion,
        angularXMotion = angularXMotion,
        angularYMotion = angularYMotion,
        angularZMotion = angularZMotion,
        linearLimitSpring = linearLimitSpring,
        linearLimit = linearLimit,
        angularXLimitSpring = angularXLimitSpring,
        lowAngularXLimit = lowAngularXLimit,
        highAngularXLimit = highAngularXLimit,
        angularYZLimitSpring = angularYZLimitSpring,
        angularYLimit = angularYLimit,
        angularZLimit = angularZLimit,
        targetPosition = Vector3.zero,
        targetVelocity = Vector3.zero,
        xDrive = xDrive.Unwrap(),
        yDrive = yDrive.Unwrap(),
        zDrive = zDrive.Unwrap(),
        targetRotation = new ConfigurableJointParameters.SerializedQuaternion(Quaternion.identity),
        targetAngularVelocity = Vector3.zero,
        rotationDriveMode = RotationDriveMode.XYAndZ,
        angularXDrive = angularXDrive.Unwrap(),
        angularYZDrive = angularYZDrive.Unwrap(),
        slerpDrive = new ConfigurableJointParameters.SerializedJointDrive(0.0f, 0.0f),
        projectionMode = projectionMode,
        projectionDistance = projectionDistance,
        projectionAngle = projectionAngle,
        configuredInWorldSpace = configuredInWorldSpace,
        swapBodies = false,
        breakForce = float.PositiveInfinity,
        breakTorque = float.PositiveInfinity,
        enableCollision = enableCollision,
        enablePreprocessing = enablePreprocessing,
        massScale = massScale,
        connectedMassScale = connectedMassScale
      };
    }
  }

  //! @brief Wrapper class for ConfigurableJointParameters.SerializedJointDrive that contains only
  //! the fields supported by HxJoint.
  //!
  //! SerializedJointDrive.positionSpring is unsupported.
  [Serializable]
  public class SerializedJointDrive {

    //! Wraps ConfigurableJointParameters.SerializedJointDrive.positionDamper.
    [Tooltip("The reduction of the spring force in proportion to the speed of the joint’s movement.")]
    public float positionDamper = 0.0f;

    //! Wraps ConfigurableJointParameters.SerializedJointDrive.maximumForce.
    [Tooltip("The force used to accelerate the joint toward its target velocity.")]
    public float maximumForce = float.MaxValue;

    //! Unwraps this instance.
    //!
    //! @returns The unwrapped instance.
    public ConfigurableJointParameters.SerializedJointDrive Unwrap() {
      return new ConfigurableJointParameters.SerializedJointDrive() {
        positionDamper = positionDamper,
        maximumForce = maximumForce
      };
    }
  }
}
