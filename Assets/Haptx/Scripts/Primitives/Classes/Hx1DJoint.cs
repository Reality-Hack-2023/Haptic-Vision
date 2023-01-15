// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using UnityEngine;

//! @brief Uses features that can be added to @link HxJoint HxJoints @endlink that only need to 
//! operate in one degree of freedom.
//!
//! See the @ref section_unity_hx_1d_components "Unity Haptic Primitive Guide" for a high level 
//! overview.
//!
//! @ingroup group_unity_haptic_primitives
public abstract class Hx1DJoint : HxJoint {

  //! @brief The initial position to teleport the constrained object to after the joint has formed.
  //!
  //! This happens only once in Start().
  [Tooltip("The initial position to teleport the constrained object to after the joint has formed.")]
  public float initialPosition = 0.0f;

  //! @brief Whether to put limits on this joint's motion.
  //!
  //! See #LowerLimit and #UpperLimit.
  [Tooltip("Whether to put limits on this joint's motion.")]
  public bool limitMotion = false;

  //! See #LowerLimit.
  [Tooltip("The lower bound on this joint's position limits.")]
  [SerializeField]
  protected float _lowerLimit = 0.0f;

  //! The lower bound on this joint's position limits.
  public float LowerLimit {
    get {
      return _lowerLimit;
    }
  }

  //! See #UpperLimit.
  [Tooltip("The upper bound on this joint's position limits.")]
  [SerializeField]
  protected float _upperLimit = 0.0f;

  //! The upper bound on this joint's position limits.
  public float UpperLimit {
    get {
      return _upperLimit;
    }
  }


  //! Whether to lock the other domain (linear/angular).
  [Tooltip("Whether to lock the other domain (linear/angular).")]
  [SerializeField]
  protected bool _lockOtherDomain = true;

  //! Whether to lock the other domain (linear/angular).
  public bool LockOtherDomain {
    get {
      return _lockOtherDomain;
    }
  }

  //! see #Damping.
  [Tooltip("How much the joint's motion gets damped.")]
  [SerializeField]
  protected float damping = 0.1f;

  //! @brief How much the joint's motion gets damped.
  //!
  //! This gets passed directly to the relevant ConfigurableJoint property.
  public float Damping {
    get {
      return damping;
    }
    set {
      damping = value;
      UpdateJoint();
    }
  }

  //! Which domain this joint operates in.
  public abstract DofDomain OperatingDomain { get; }

  //! See #OperatingAxis.
  [Tooltip("Which axis the joint operates along.")]
  [SerializeField]
  protected DofAxis _operatingAxis = DofAxis.Z;

  //! Which axis the joint operates along.
  public DofAxis OperatingAxis {
    get {
      return _operatingAxis;
    }
  }

  //! The DegreeOfFreedom that this component is operating in.
  public DegreeOfFreedom OperatingDegreeOfFreedom {
    get {
      return DegreeOfFreedomExtensions.FromDomainAndAxis(OperatingDomain, OperatingAxis);
    }
  }

  //! @brief Get the HxDof that this component is operating with.
  //!
  //! @returns The HxDof that this component is operating with.
  public HxDof GetOperatingDof() {
    return dofs.GetDof(OperatingDegreeOfFreedom);
  }

  //! See #Hx1DJoint.SupportedConfigurableJointParameters.
  [SerializeField]
  [Tooltip("Supported ConfigurableJoint parameters.")]
  private SupportedConfigurableJointParameters _supportedConfigurableJointParametersHaptx1DJoint =
    new SupportedConfigurableJointParameters();

  //! Called on the first frame when a script is enabled.
  private void Start() {
    if (Application.isPlaying) {
      HxDof dof = GetOperatingDof();
      if (dof != null) {
        dof.forceUpdate = true;
      }

      TeleportAnchor1AlongDof(initialPosition, OperatingDegreeOfFreedom);
    }
  }

  //! @brief Set the lower and upper limits of this joint.
  //!
  //! If you expect this to take immediate effect, make sure #limitMotion is true.
  //!
  //! @param lowerLimit The new lower limit.
  //! @param upperLimit The new upper limit.
  public void SetLimits(float lowerLimit, float upperLimit) {
    this._lowerLimit = lowerLimit;
    this._upperLimit = upperLimit;

    UpdateJoint();
  }

  //! @brief Teleport anchor1 to the new position along the operating HxDof.
  //!
  //! Ignores transform hierarchy.
  //!
  //! @param newPosition The new position along the operating HxDof.
  public void TeleportAnchor1AlongOperatingDof(float newPosition) {
    TeleportAnchor1AlongDof(newPosition, OperatingDegreeOfFreedom);
  }

  //! Apply force at anchor along operating HxDof.
  //!
  //! @param force The force value to apply.
  //! @param anchor Which anchor to apply @p force to.
  //! @param forceMode What mode to apply the force in.
  //! @param visualize Whether to visualize @p force. Lasts for one frame.
  public void AddForceAtAnchorAlongOperatingDof(float force, Anchor anchor,
      ForceMode forceMode = ForceMode.Force, bool visualize = false) {
    AddForceAtAnchor(force * OperatingAxis.GetDirection(), anchor, AnchorForceTorqueSpace.ANCHOR2,
        forceMode, visualize);
  }

  //! Apply torque at anchor along operating HxDof.
  //!
  //! @param torque The torque value to apply.
  //! @param anchor Which anchor to apply @p torque to.
  //! @param forceMode What mode to apply the torque in.
  //! @param visualize Whether to visualize @p torque. Lasts for one frame.
  public void AddTorqueAtAnchorAlongOperatingDof(float torque, Anchor anchor,
      ForceMode forceMode = ForceMode.Force, bool visualize = false) {
    AddTorqueAtAnchor(torque * OperatingAxis.GetDirection(), anchor,
        AnchorForceTorqueSpace.ANCHOR2, forceMode, visualize);
  }

  protected override ConfigurableJointParameters GetInitialJointParameters() {
    return _supportedConfigurableJointParametersHaptx1DJoint.Unwrap(OperatingDegreeOfFreedom);
  }

  protected override void ConfigureJoint() {
    base.ConfigureJoint();

    // Lock degrees of freedom.
    _hxJointParameters.xMotion =
        ((OperatingDomain == DofDomain.LINEAR && OperatingAxis == DofAxis.X) ||
        (OperatingDomain == DofDomain.ANGULAR && !_lockOtherDomain)) ?
        ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
    _hxJointParameters.yMotion =
        ((OperatingDomain == DofDomain.LINEAR && OperatingAxis == DofAxis.Y) ||
        (OperatingDomain == DofDomain.ANGULAR && !_lockOtherDomain)) ?
        ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
    _hxJointParameters.zMotion =
        ((OperatingDomain == DofDomain.LINEAR && OperatingAxis == DofAxis.Z) ||
        (OperatingDomain == DofDomain.ANGULAR && !_lockOtherDomain)) ?
        ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
    _hxJointParameters.angularXMotion =
        ((OperatingDomain == DofDomain.ANGULAR && OperatingAxis == DofAxis.X) ||
        (OperatingDomain == DofDomain.LINEAR && !_lockOtherDomain)) ?
        ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
    _hxJointParameters.angularYMotion =
        ((OperatingDomain == DofDomain.ANGULAR && OperatingAxis == DofAxis.Y) ||
        (OperatingDomain == DofDomain.LINEAR && !_lockOtherDomain)) ?
        ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
    _hxJointParameters.angularZMotion =
        ((OperatingDomain == DofDomain.ANGULAR && OperatingAxis == DofAxis.Z) ||
        (OperatingDomain == DofDomain.LINEAR && !_lockOtherDomain)) ?
        ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;

    // Apply damping.
    _hxJointParameters.xDrive.positionDamper = OperatingDegreeOfFreedom == DegreeOfFreedom.X_LIN ?
        damping : 0.0f;
    _hxJointParameters.yDrive.positionDamper = OperatingDegreeOfFreedom == DegreeOfFreedom.Y_LIN ?
        damping : 0.0f;
    _hxJointParameters.zDrive.positionDamper = OperatingDegreeOfFreedom == DegreeOfFreedom.Z_LIN ?
        damping : 0.0f;
    _hxJointParameters.angularXDrive.positionDamper =
        OperatingDegreeOfFreedom == DegreeOfFreedom.X_ANG ? damping : 0.0f;
    _hxJointParameters.angularYZDrive.positionDamper =
        (OperatingDegreeOfFreedom == DegreeOfFreedom.Y_ANG || OperatingDegreeOfFreedom == 
        DegreeOfFreedom.Z_ANG) ? damping : 0.0f;

    // Validate limits.
    if (limitMotion) {
      // Guarantee order.
      if (_lowerLimit > _upperLimit) {
        HxDebug.LogWarning(string.Format(
            "Lower limit higher then upper limit: low {1} > high {2}. Swapping them.",
            _lowerLimit, _upperLimit), this);
        float temp = _lowerLimit;
        _lowerLimit = _upperLimit;
        _upperLimit = temp;
      }
    }
  }

  //! @brief Wrapper class for ConfigurableJointParameters that contains only fields supported by 
  //! Hx1DJoint.
  //!
  //! Comments and tool-tips sourced from 
  //! https://docs.unity3d.com/Manual/class-ConfigurableJoint.html;
  [Serializable]
  private class SupportedConfigurableJointParameters {

    //! Wraps ConfigurableJointParameters.connectedBody.
    [Tooltip("The other Rigidbody object to which the joint is connected.")]
    public Rigidbody connectedBody = null;

    //! Wraps ConfigurableJointParameters.anchor.
    [Tooltip("The point where the center of the joint is defined.")]
    public Vector3 anchor = Vector3.zero;

    //! Wraps ConfigurableJointParameters.axis.
    [Tooltip("The local axis that will define the object’s natural rotation based on physics simulation.")]
    public Vector3 axis = new Vector3(1.0f, 0.0f, 0.0f);

    //! Wraps ConfigurableJointParameters.autoConfigureConnectedAnchor.
    [Tooltip("If this is enabled, then the Connected Anchor position will be calculated automatically to match the global position of the anchor property.")]
    public bool autoConfigureConnectedAnchor = true;

    //! Wraps ConfigurableJointParameters.connectedAnchor.
    [Tooltip("Manual configuration of the connected anchor position.")]
    public Vector3 connectedAnchor = Vector3.zero;

    //! Wraps ConfigurableJointParameters.secondaryAxis.
    [Tooltip("Together, Axis and Secondary Axis define the local coordinate system of the joint.")]
    public Vector3 secondaryAxis = new Vector3(0.0f, 1.0f, 0.0f);

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimitSpring.
    [Tooltip("A spring force applied to pull the object back when it goes past the limit position.")]
    public ConfigurableJointParameters.SerializedSoftJointLimitSpring limitSpring =
        new ConfigurableJointParameters.SerializedSoftJointLimitSpring();

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimit.
    [Tooltip("Limit on the joint’s linear movement (ie, movement over distance rather than rotation), specified as a distance from the joint’s origin.")]
    public SerializedSoftJointLimit limit = new SerializedSoftJointLimit();

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

    //! @brief Unwraps this instance.
    //!
    //! @param degreeOfFreedom Used to determine several settings.
    //! @returns The unwrapped instance.
    public ConfigurableJointParameters Unwrap(DegreeOfFreedom degreeOfFreedom) {
      return new ConfigurableJointParameters {
        ConnectedBody = connectedBody,
        anchor = anchor,
        axis = axis,
        autoConfigureConnectedAnchor = autoConfigureConnectedAnchor,
        connectedAnchor = connectedAnchor,
        secondaryAxis = secondaryAxis,
        xMotion = degreeOfFreedom == DegreeOfFreedom.X_LIN ? ConfigurableJointMotion.Free : 
            ConfigurableJointMotion.Locked,
        yMotion = degreeOfFreedom == DegreeOfFreedom.Y_LIN ? ConfigurableJointMotion.Free :
            ConfigurableJointMotion.Locked,
        zMotion = degreeOfFreedom == DegreeOfFreedom.Z_LIN ? ConfigurableJointMotion.Free :
            ConfigurableJointMotion.Locked,
        angularXMotion = degreeOfFreedom == DegreeOfFreedom.X_ANG ? ConfigurableJointMotion.Free :
            ConfigurableJointMotion.Locked,
        angularYMotion = degreeOfFreedom == DegreeOfFreedom.Y_ANG ? ConfigurableJointMotion.Free :
            ConfigurableJointMotion.Locked,
        angularZMotion = degreeOfFreedom == DegreeOfFreedom.Z_ANG ? ConfigurableJointMotion.Free :
            ConfigurableJointMotion.Locked,
        linearLimitSpring = degreeOfFreedom.Domain() == DofDomain.LINEAR ? limitSpring :
            new ConfigurableJointParameters.SerializedSoftJointLimitSpring(0.0f, 0.0f),
        linearLimit = degreeOfFreedom.Domain() == DofDomain.LINEAR ? limit.Unwrap() :
            new ConfigurableJointParameters.SerializedSoftJointLimit(0.0f, 0.0f, 0.0f),
        angularXLimitSpring = degreeOfFreedom == DegreeOfFreedom.X_ANG ? limitSpring :
            new ConfigurableJointParameters.SerializedSoftJointLimitSpring(0.0f, 0.0f),
        lowAngularXLimit = degreeOfFreedom == DegreeOfFreedom.X_ANG ? limit.Unwrap() :
            new ConfigurableJointParameters.SerializedSoftJointLimit(0.0f, 0.0f, 0.0f),
        highAngularXLimit = degreeOfFreedom == DegreeOfFreedom.X_ANG ? limit.Unwrap() :
            new ConfigurableJointParameters.SerializedSoftJointLimit(0.0f, 0.0f, 0.0f),
        angularYZLimitSpring = (degreeOfFreedom == DegreeOfFreedom.Y_ANG || degreeOfFreedom ==
            DegreeOfFreedom.Z_ANG) ? limitSpring : 
            new ConfigurableJointParameters.SerializedSoftJointLimitSpring(0.0f, 0.0f),
        angularYLimit = degreeOfFreedom == DegreeOfFreedom.Y_ANG ? limit.Unwrap() :
            new ConfigurableJointParameters.SerializedSoftJointLimit(0.0f, 0.0f, 0.0f),
        angularZLimit = degreeOfFreedom == DegreeOfFreedom.Z_ANG ? limit.Unwrap() :
            new ConfigurableJointParameters.SerializedSoftJointLimit(0.0f, 0.0f, 0.0f),
        targetPosition = Vector3.zero,
        targetVelocity = Vector3.zero,
        xDrive = new ConfigurableJointParameters.SerializedJointDrive(0.0f, 0.0f),
        yDrive = new ConfigurableJointParameters.SerializedJointDrive(0.0f, 0.0f),
        zDrive = new ConfigurableJointParameters.SerializedJointDrive(0.0f, 0.0f),
        targetRotation = new ConfigurableJointParameters.SerializedQuaternion(Quaternion.identity),
        targetAngularVelocity = Vector3.zero,
        rotationDriveMode = RotationDriveMode.XYAndZ,
        angularXDrive = new ConfigurableJointParameters.SerializedJointDrive(0.0f, 0.0f),
        angularYZDrive = new ConfigurableJointParameters.SerializedJointDrive(0.0f, 0.0f),
        slerpDrive = new ConfigurableJointParameters.SerializedJointDrive(),
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

  //! @brief Wrapper class for ConfigurableJointParameters.SerializedSoftJointLimit that contains
  //! only the fields supported by Hx1DJoint.
  //! 
  //! Removes the limit field since it gets calculated automatically.
  [Serializable]
  public class SerializedSoftJointLimit {

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimit.bounciness.
    [Tooltip("Bounce force applied to the object to push it back when it reaches the limit distance.")]
    public float bounciness = 0.0f;

    //! Wraps ConfigurableJointParameters.SerializedSoftJointLimit.contactDistance.
    [Tooltip("The minimum distance tolerance (between the joint position and the limit) at which the limit will be enforced.")]
    public float contactDistance = 0.0f;

    //! Unwraps this instance.
    //!
    //! @returns The unwrapped instance.
    public ConfigurableJointParameters.SerializedSoftJointLimit Unwrap() {
      return new ConfigurableJointParameters.SerializedSoftJointLimit() {
        bounciness = bounciness,
        contactDistance = contactDistance
      };
    }
  }
}
