// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using Mirror;
using UnityEngine;

//! @brief A serializable wrapper of ConfigurableJoint.
//!
//! Comments and tool-tips sourced from 
//! https://docs.unity3d.com/Manual/class-ConfigurableJoint.html;
[System.Serializable]
public class ConfigurableJointParameters {

  //! @brief The other Rigidbody object to which the joint is connected.
  //!
  //! You can set this to None to indicate that the joint is attached to a fixed position in space
  //! rather than another Rigidbody.
  public Rigidbody ConnectedBody {
    get {
      return connectedBody;
    } 
    set {
      connectedBody = value;
    }
  }

  //! @copydoc #ConnectedBody
  //!
  //! Made private to hide from Mirror's reflection-based serialization system. Unity is able to
  //! serialize this to disk for the purpose of asset configuration; however, Mirror is not able
  //! to communicate Rigidbodies.
  [Tooltip("The other Rigidbody object to which the joint is connected.")]
  [SerializeField]
  private Rigidbody connectedBody = null;

  //! @brief The point where the center of the joint is defined.
  //!
  //! All physics-based simulation will use this point as the center in calculations.
  [Tooltip("The point where the center of the joint is defined.")]
  public Vector3 anchor = Vector3.zero;

  //! The local axis that will define the object’s natural rotation based on physics 
  //! simulation.
  [Tooltip("The local axis that will define the object’s natural rotation based on physics simulation.")]
  public Vector3 axis = new Vector3(1.0f, 0.0f, 0.0f);

  //! @brief If this is enabled, then the Connected Anchor position will be calculated 
  //! automatically to match the global position of the anchor property.
  //!
  //! This is the default behavior. If this is disabled, you can configure the position of the 
  //! connected anchor manually.
  [Tooltip("If this is enabled, then the Connected Anchor position will be calculated automatically to match the global position of the anchor property.")]
  public bool autoConfigureConnectedAnchor = true;

  //! Manual configuration of the connected anchor position.
  [Tooltip("Manual configuration of the connected anchor position.")]
  public Vector3 connectedAnchor = Vector3.zero;

  //! Together, Axis and Secondary Axis define the local coordinate system of the joint. The
  //! third axis is set to be orthogonal to the other two.
  [Tooltip("Together, Axis and Secondary Axis define the local coordinate system of the joint. The third axis is set to be orthogonal to the other two.")]
  public Vector3 secondaryAxis = new Vector3(0.0f, 1.0f, 0.0f);

  //! Allow movement along the X axis to be Free, completely Locked, or Limited according to
  //! the limit properties described below.
  [Tooltip("Allow movement along the X axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
  public ConfigurableJointMotion xMotion = ConfigurableJointMotion.Free;

  //! Allow movement along the Y axis to be Free, completely Locked, or Limited according to
  //! the limit properties described below.
  [Tooltip("Allow movement along the Y axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
  public ConfigurableJointMotion yMotion = ConfigurableJointMotion.Free;

  //! Allow movement along the Z axis to be Free, completely Locked, or Limited according to
  //! the limit properties described below.
  [Tooltip("Allow movement along the Z axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
  public ConfigurableJointMotion zMotion = ConfigurableJointMotion.Free;

  //! Allow movement along the X axis to be Free, completely Locked, or Limited according to
  //! the limit properties described below.
  [Tooltip("Allow movement along the X axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
  public ConfigurableJointMotion angularXMotion = ConfigurableJointMotion.Free;

  //! Allow movement along the Y axis to be Free, completely Locked, or Limited according to
  //! the limit properties described below.
  [Tooltip("Allow movement along the Y axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
  public ConfigurableJointMotion angularYMotion = ConfigurableJointMotion.Free;

  //! Allow movement along the Z axis to be Free, completely Locked, or Limited according to
  //! the limit properties described below.
  [Tooltip("Allow movement along the Z axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
  public ConfigurableJointMotion angularZMotion = ConfigurableJointMotion.Free;

  //! A spring force applied to pull the object back when it goes past the limit position.
  [Tooltip("A spring force applied to pull the object back when it goes past the limit position.")]
  public SerializedSoftJointLimitSpring linearLimitSpring = new SerializedSoftJointLimitSpring();

  //! Limit on the joint’s linear movement 
  //! (ie, movement over distance rather than rotation), specified as a distance from the joint’s 
  //! origin.
  [Tooltip("Limit on the joint’s linear movement (ie, movement over distance rather than rotation), specified as a distance from the joint’s origin.")]
  public SerializedSoftJointLimit linearLimit = new SerializedSoftJointLimit();

  //! A spring torque applied to rotate the object back when it goes past the limit angle of
  //! the joint.
  [Tooltip("A spring torque applied to rotate the object back when it goes past the limit angle of the joint.")]
  public SerializedSoftJointLimitSpring angularXLimitSpring = new SerializedSoftJointLimitSpring();

  //! Lower limit on the joint’s rotation around the X axis, specified as a angle from the
  //! joint’s original rotation.
  [Tooltip("Lower limit on the joint’s rotation around the X axis, specified as a angle from the joint’s original rotation.")]
  public SerializedSoftJointLimit lowAngularXLimit = new SerializedSoftJointLimit();

  //! This is similar to the Low Angular X Limit property described above but it determines
  //! the upper angular limit of the joint’s rotation rather than the lower limit.
  [Tooltip("This is similar to the Low Angular X Limit property described above but it determines the upper angular limit of the joint’s rotation rather than the lower limit.")]
  public SerializedSoftJointLimit highAngularXLimit = new SerializedSoftJointLimit();

  //! This is similar to the Angular X Limit Spring described above but applies to rotation
  //! around both the Y and Z axes.
  [Tooltip("This is similar to the Angular X Limit Spring described above but applies to rotation around both the Y and Z axes.")]
  public SerializedSoftJointLimitSpring angularYZLimitSpring = new SerializedSoftJointLimitSpring();

  //! Analogous to the Angular X Limit properties described above but applies to the Y axis
  //! and regards both the upper and lower angular limits as being the same.
  [Tooltip("Analogous to the Angular X Limit properties described above but applies to the Y axis and regards both the upper and lower angular limits as being the same.")]
  public SerializedSoftJointLimit angularYLimit = new SerializedSoftJointLimit();

  //! Analogous to the Angular X Limit properties described above but applies to the Z axis
  //! and regards both the upper and lower angular limits as being the same.
  [Tooltip("Analogous to the Angular X Limit properties described above but applies to the Z axis and regards both the upper and lower angular limits as being the same.")]
  public SerializedSoftJointLimit angularZLimit = new SerializedSoftJointLimit();

  //! The target position that the joint’s drive force should move it to.
  [Tooltip("The target position that the joint’s drive force should move it to.")]
  public Vector3 targetPosition = Vector3.zero;

  //! The desired velocity with which the joint should move to the Target Position under the
  //! drive force.
  [Tooltip("The desired velocity with which the joint should move to the Target Position under the drive force.")]
  public Vector3 targetVelocity = Vector3.zero;

  //! The drive force that moves the joint linearly along its local X axis.
  [Tooltip("The drive force that moves the joint linearly along its local X axis.")]
  public SerializedJointDrive xDrive = new SerializedJointDrive();

  //! This is analogous to the X Drive described above but applies to the joint’s Y axis.
  [Tooltip("This is analogous to the X Drive described above but applies to the joint’s Y axis.")]
  public SerializedJointDrive yDrive = new SerializedJointDrive();

  //! This is analogous to the X Drive described above but applies to the joint’s Z axis.
  [Tooltip("This is analogous to the X Drive described above but applies to the joint’s Z axis.")]
  public SerializedJointDrive zDrive = new SerializedJointDrive();

  //! The orientation that the joint’s rotational drive should rotate towards, specified as
  //! a quaternion.
  [Tooltip("The orientation that the joint’s rotational drive should rotate towards, specified as a quaternion.")]
  public SerializedQuaternion targetRotation = new SerializedQuaternion();

  //! @brief The angular velocity that the joint’s rotational drive should aim to achieve.
  //!
  //! This is specified as a vector whose length specifies the rotational speed and whose direction
  //! defines the axis of rotation.
  [Tooltip("The angular velocity that the joint’s rotational drive should aim to achieve.")]
  public Vector3 targetAngularVelocity = Vector3.zero;

  //! @brief The way in which the drive force will be applied to the object to rotate it to the 
  //! target orientation. 
  //!
  //! If the mode is set to X and YZ, the torque will be applied around these axes as specified by
  //! the Angular X/YZ Drive properties described below. If Slerp mode is used then the Slerp Drive
  //! properties will determine the drive torque.
  [Tooltip("The way in which the drive force will be applied to the object to rotate it to the target orientation.")]
  public RotationDriveMode rotationDriveMode = RotationDriveMode.Slerp;

  //! This specifies how the joint will be rotated by the drive torque around its local X 
  //! axis. It is used only if the Rotation Drive Mode property described above is set to X & YZ.
  [Tooltip("This specifies how the joint will be rotated by the drive torque around its local X axis. It is used only if the Rotation Drive Mode property described above is set to X & YZ.")]
  public SerializedJointDrive angularXDrive = new SerializedJointDrive();

  //! This is analogous to the Angular X Drive described above but applies to both the 
  //! joint’s Y and Z axes.
  [Tooltip("This is analogous to the Angular X Drive described above but applies to both the joint’s Y and Z axes.")]
  public SerializedJointDrive angularYZDrive = new SerializedJointDrive();

  //! This specifies how the joint will be rotated by the drive torque around all local 
  //! axes. It is used only if the Rotation Drive Mode property described above is set to Slerp.
  [Tooltip("This specifies how the joint will be rotated by the drive torque around all local axes. It is used only if the Rotation Drive Mode property described above is set to Slerp.")]
  public SerializedJointDrive slerpDrive = new SerializedJointDrive();

  //! @brief This defines how the joint will be snapped back to its constraints when it 
  //! unexpectedly moves beyond them (due to the physics engine being unable to reconcile the 
  //! current combination of forces within the simulation).
  //!
  //! The options are None and Position and Rotation.
  [Tooltip("This defines how the joint will be snapped back to its constraints when it unexpectedly moves beyond them (due to the physics engine being unable to reconcile the current combination of forces within the simulation).")]
  public JointProjectionMode projectionMode = JointProjectionMode.PositionAndRotation;

  //! The distance the joint must move beyond its constraints before the physics engine will
  //! attempt to snap it back to an acceptable position.
  [Tooltip("The distance the joint must move beyond its constraints before the physics engine will attempt to snap it back to an acceptable position.")]
  public float projectionDistance = 0.1f;

  //! The angle the joint must rotate beyond its constraints before the physics engine will
  //! attempt to snap it back to an acceptable position.
  [Tooltip("The angle the joint must rotate beyond its constraints before the physics engine will attempt to snap it back to an acceptable position.")]
  public float projectionAngle = 180.0f;

  //! Should the values set by the various target and drive properties be calculated in 
  //! world space instead of the object’s local space?
  [Tooltip("Should the values set by the various target and drive properties be calculated in world space instead of the object’s local space?")]
  public bool configuredInWorldSpace = false;

  //! If enabled, this will make the joint behave as though the component were attached to
  //! the connected Rigidbody (ie, the other end of the joint).
  [Tooltip("If enabled, this will make the joint behave as though the component were attached to the connected Rigidbody (ie, the other end of the joint).")]
  public bool swapBodies = false;

  //! If the joint is pushed beyond its constraints by a force larger than this value then
  //! the joint will be permanently “broken” and deleted.
  [Tooltip("If the joint is pushed beyond its constraints by a force larger than this value then the joint will be permanently “broken” and deleted.")]
  public float breakForce = float.PositiveInfinity;

  //! If the joint is rotated beyond its constraints by a torque larger than this value then
  //! the joint will be permanently “broken” and deleted.
  [Tooltip("If the joint is rotated beyond its constraints by a torque larger than this value then the joint will be permanently “broken” and deleted.")]
  public float breakTorque = float.PositiveInfinity;

  //! Should the object with the joint be able to collide with the connected object 
  //! (as opposed to just passing through each other)?
  [Tooltip("Should the object with the joint be able to collide with the connected object (as opposed to just passing through each other)?")]
  public bool enableCollision = false;

  //! If preprocessing is disabled then certain “impossible” configurations of the joint
  //! will be kept more stable rather than drifting wildly out of control.
  [Tooltip("If preprocessing is disabled then certain “impossible” configurations of the joint will be kept more stable rather than drifting wildly out of control.")]
  public bool enablePreprocessing = true;

  //! The scale to apply to the inverse mass and inertia tensor of the body prior to solving
  //! the constraints.
  [Tooltip("The scale to apply to the inverse mass and inertia tensor of the body prior to solving the constraints.")]
  public float massScale = 1.0f;

  //! The scale to apply to the inverse mass and inertia tensor of the connected body prior
  //! to solving the constraints.
  [Tooltip("The scale to apply to the inverse mass and inertia tensor of the connected body prior to solving the constraints.")]
  public float connectedMassScale = 1.0f;

  //! Default constructor.
  public ConfigurableJointParameters() { }

  //! Copy constructor.
  //!
  //! @param toCopy The instance to copy values from.
  public ConfigurableJointParameters(ConfigurableJointParameters toCopy) {
    if (toCopy == null) {
      return;
    }
    connectedBody = toCopy.connectedBody;
    anchor = toCopy.anchor;
    axis = toCopy.axis;
    autoConfigureConnectedAnchor = toCopy.autoConfigureConnectedAnchor;
    connectedAnchor = toCopy.connectedAnchor;
    secondaryAxis = toCopy.secondaryAxis;
    xMotion = toCopy.xMotion;
    yMotion = toCopy.yMotion;
    zMotion = toCopy.zMotion;
    angularXMotion = toCopy.angularXMotion;
    angularYMotion = toCopy.angularYMotion;
    angularZMotion = toCopy.angularZMotion;
    linearLimitSpring = new SerializedSoftJointLimitSpring(toCopy.linearLimitSpring);
    linearLimit = new SerializedSoftJointLimit(toCopy.linearLimit);
    angularXLimitSpring = new SerializedSoftJointLimitSpring(toCopy.angularXLimitSpring);
    lowAngularXLimit = new SerializedSoftJointLimit(toCopy.lowAngularXLimit);
    highAngularXLimit = new SerializedSoftJointLimit(toCopy.highAngularXLimit);
    angularYZLimitSpring = new SerializedSoftJointLimitSpring(toCopy.angularYZLimitSpring);
    angularYLimit = new SerializedSoftJointLimit(toCopy.angularYLimit);
    angularZLimit = new SerializedSoftJointLimit(toCopy.angularZLimit);
    targetPosition = toCopy.targetPosition;
    targetVelocity = toCopy.targetVelocity;
    xDrive = new SerializedJointDrive(toCopy.xDrive);
    yDrive = new SerializedJointDrive(toCopy.yDrive);
    zDrive = new SerializedJointDrive(toCopy.zDrive);
    targetRotation = new SerializedQuaternion(toCopy.targetRotation);
    targetAngularVelocity = toCopy.targetAngularVelocity;
    rotationDriveMode = toCopy.rotationDriveMode;
    angularXDrive = new SerializedJointDrive(toCopy.angularXDrive);
    angularYZDrive = new SerializedJointDrive(toCopy.angularYZDrive);
    slerpDrive = new SerializedJointDrive(toCopy.slerpDrive);
    projectionMode = toCopy.projectionMode;
    projectionDistance = toCopy.projectionDistance;
    projectionAngle = toCopy.projectionAngle;
    configuredInWorldSpace = toCopy.configuredInWorldSpace;
    swapBodies = toCopy.swapBodies;
    breakForce = toCopy.breakForce;
    breakTorque = toCopy.breakTorque;
    enableCollision = toCopy.enableCollision;
    enablePreprocessing = toCopy.enablePreprocessing;
    massScale = toCopy.massScale;
    connectedMassScale = toCopy.connectedMassScale;
  }

  //! Instantiates a ConfigurableJoint with unwrapped settings and adds it to a given
  //! GameObject.
  //!
  //! @param gameObject The GameObject to add the joint to.
  //! @returns A reference to the new joint.
  public ConfigurableJoint AddConfigurableJointToGameObject(GameObject gameObject) {
    ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
    Unwrap(joint);
    return joint;
  }

  //! Wraps values to this instance.
  //!
  //! @param other The values to wrap.
  public void Wrap(ConfigurableJoint other) {
    if (other == null) {
      return;
    }
    connectedBody = other.connectedBody;
    anchor = other.anchor;
    axis = other.axis;
    autoConfigureConnectedAnchor = other.autoConfigureConnectedAnchor;
    connectedAnchor = other.connectedAnchor;
    secondaryAxis = other.secondaryAxis;
    xMotion = other.xMotion;
    yMotion = other.yMotion;
    zMotion = other.zMotion;
    angularXMotion = other.angularXMotion;
    angularYMotion = other.angularYMotion;
    angularZMotion = other.angularZMotion;
    linearLimitSpring.Wrap(other.linearLimitSpring);
    linearLimit.Wrap(other.linearLimit);
    angularXLimitSpring.Wrap(other.angularXLimitSpring);
    lowAngularXLimit.Wrap(other.lowAngularXLimit);
    highAngularXLimit.Wrap(other.highAngularXLimit);
    angularYZLimitSpring.Wrap(other.angularYZLimitSpring);
    angularYLimit.Wrap(other.angularYLimit);
    angularZLimit.Wrap(other.angularZLimit);
    targetPosition = other.targetPosition;
    targetVelocity = other.targetVelocity;
    xDrive.Wrap(other.xDrive);
    yDrive.Wrap(other.yDrive);
    zDrive.Wrap(other.zDrive);
    targetRotation.Wrap(other.targetRotation);
    targetAngularVelocity = other.targetAngularVelocity;
    rotationDriveMode = other.rotationDriveMode;
    angularXDrive.Wrap(other.angularXDrive);
    angularYZDrive.Wrap(other.angularYZDrive);
    slerpDrive.Wrap(other.slerpDrive);
    projectionMode = other.projectionMode;
    projectionDistance = other.projectionDistance;
    projectionAngle = other.projectionAngle;
    configuredInWorldSpace = other.configuredInWorldSpace;
    swapBodies = other.swapBodies;
    breakForce = other.breakForce;
    breakTorque = other.breakTorque;
    enableCollision = other.enableCollision;
    enablePreprocessing = other.enablePreprocessing;
    massScale = other.massScale;
    connectedMassScale = other.connectedMassScale;
  }

  //! Unwraps this instance.
  //!
  //! @param other The joint to unwrap values into.
  public void Unwrap(ConfigurableJoint other) {
    other.connectedBody = connectedBody;
    other.autoConfigureConnectedAnchor = autoConfigureConnectedAnchor;
    other.axis = axis;
    other.secondaryAxis = secondaryAxis;
    other.xMotion = xMotion;
    other.yMotion = yMotion;
    other.zMotion = zMotion;
    other.angularXMotion = angularXMotion;
    other.angularYMotion = angularYMotion;
    other.angularZMotion = angularZMotion;
    other.linearLimitSpring = linearLimitSpring.Unwrap();
    other.linearLimit = linearLimit.Unwrap();
    other.angularXLimitSpring = angularXLimitSpring.Unwrap();
    other.lowAngularXLimit = lowAngularXLimit.Unwrap();
    other.highAngularXLimit = highAngularXLimit.Unwrap();
    other.angularYZLimitSpring = angularYZLimitSpring.Unwrap();
    other.angularYLimit = angularYLimit.Unwrap();
    other.angularZLimit = angularZLimit.Unwrap();
    other.targetPosition = targetPosition;
    other.targetVelocity = targetVelocity;
    other.xDrive = xDrive.Unwrap();
    other.yDrive = yDrive.Unwrap();
    other.zDrive = zDrive.Unwrap();
    other.targetRotation = targetRotation.Unwrap();
    other.targetAngularVelocity = targetAngularVelocity;
    other.rotationDriveMode = rotationDriveMode;
    other.angularXDrive = angularXDrive.Unwrap();
    other.angularYZDrive = angularYZDrive.Unwrap();
    other.slerpDrive = slerpDrive.Unwrap();
    other.projectionMode = projectionMode;
    other.projectionDistance = projectionDistance;
    other.projectionAngle = projectionAngle;
    other.configuredInWorldSpace = configuredInWorldSpace;
    other.swapBodies = swapBodies;
    other.breakForce = breakForce;
    other.breakTorque = breakTorque;
    other.enableCollision = enableCollision;
    other.enablePreprocessing = enablePreprocessing;
    other.massScale = massScale;
    other.connectedMassScale = connectedMassScale;

    // Do these last because internal calculations happen.
    other.connectedAnchor = connectedAnchor;
    other.anchor = anchor;
  }

  //! A serializable wrapper of 
  //! [SoftJointLimitSpring](https://docs.unity3d.com/ScriptReference/SoftJointLimitSpring.html)
  [System.Serializable]
  public class SerializedSoftJointLimitSpring {

    //! @brief The spring force. 
    //!
    //! If this value is set to zero then the limit will be impassable; a value other than zero
    //! will make the limit elastic.
    [Tooltip("The spring force.")]
    public float spring = 0.0f;

    //! @brief The reduction of the spring force in proportion to the speed of the joint’s 
    //! movement.
    //!
    //! Setting a value above zero allows the joint to “dampen” oscillations which would otherwise
    //! carry on indefinitely.
    [Tooltip("The reduction of the spring force in proportion to the speed of the joint’s movement.")]
    public float damper = 0.0f;

    //! Default constructor.
    public SerializedSoftJointLimitSpring() { }

    //! Construct using given values.
    //!
    //! @param spring See #spring.
    //! @param damper See #damper.
    public SerializedSoftJointLimitSpring(float spring, float damper) {
      this.spring = spring;
      this.damper = damper;
    }

    //! Copy constructor.
    //!
    //! @param toCopy The instance to copy settings from.
    public SerializedSoftJointLimitSpring(SerializedSoftJointLimitSpring toCopy) {
      if (toCopy == null) {
        return;
      }
      spring = toCopy.spring;
      damper = toCopy.damper;
    }

    //! Wraps values to this instance.
    //!
    //! @param other The values to wrap.
    public void Wrap(SoftJointLimitSpring other) {
      spring = other.spring;
      damper = other.damper;
    }

    //! Unwraps this instance.
    //!
    //! @returns The unwrapped instance.
    public SoftJointLimitSpring Unwrap() {
      SoftJointLimitSpring softJointLimitSpring = new SoftJointLimitSpring();
      softJointLimitSpring.spring = spring;
      softJointLimitSpring.damper = damper;

      return softJointLimitSpring;
    }
  }

  //! A serializable wrapper of 
  //! [SoftJointLimit](https://docs.unity3d.com/ScriptReference/SoftJointLimit.html)
  [System.Serializable]
  public class SerializedSoftJointLimit {

    //! The distance in world units from the origin to the limit.
    [Tooltip("The distance in world units from the origin to the limit.")]
    public float limit = 0.0f;

    //! Bounce force applied to the object to push it back when it reaches the limit distance.
    [Tooltip("Bounce force applied to the object to push it back when it reaches the limit distance.")]
    public float bounciness = 0.0f;

    //! @brief The minimum distance tolerance (between the joint position and the limit) at which
    //! the limit will be enforced.
    //!
    //! A high tolerance makes the limit less likely to be violated when the object is moving fast.
    //! However, this will also require the limit to be taken into account by the physics 
    //! simulation more often and this will tend to reduce performance slightly.
    [Tooltip("The minimum distance tolerance (between the joint position and the limit) at which the limit will be enforced.")]
    public float contactDistance = 0.0f;

    //! Default constructor.
    public SerializedSoftJointLimit() { }

    //! Construct using given values.
    //!
    //! @param limit See #limit.
    //! @param bounciness See #bounciness.
    //! @param contactDistance See #contactDistance.
    public SerializedSoftJointLimit(float limit, float bounciness, float contactDistance) {
      this.limit = limit;
      this.bounciness = bounciness;
      this.contactDistance = contactDistance;
    }

    //! Copy constructor.
    //!
    //! @param toCopy The instance to copy settings from.
    public SerializedSoftJointLimit(SerializedSoftJointLimit toCopy) {
      if (toCopy == null) {
        return;
      }

      limit = toCopy.limit;
      bounciness = toCopy.bounciness;
      contactDistance = toCopy.contactDistance;
    }

    //! Wraps values to this instance.
    //!
    //! @param other The values to wrap.
    public void Wrap(SoftJointLimit other) {
      limit = other.limit;
      bounciness = other.bounciness;
      contactDistance = other.contactDistance;
    }

    //! Unwraps this instance.
    //!
    //! @returns The unwrapped instance.
    public SoftJointLimit Unwrap() {
      SoftJointLimit softJointLimit = new SoftJointLimit();
      softJointLimit.limit = limit;
      softJointLimit.bounciness = bounciness;
      softJointLimit.contactDistance = contactDistance;

      return softJointLimit;
    }
  }

  //! A serializable wrapper of 
  //! [JointDrive](https://docs.unity3d.com/ScriptReference/JointDrive.html)
  [System.Serializable]
  public class SerializedJointDrive {

    //! @brief The spring force that moves the joint towards its target position. 
    //!
    //! This is only used when the drive mode is set to Position or Position and Velocity.
    [Tooltip("The spring force that moves the joint towards its target position.")]
    public float positionSpring = 0.0f;

    //! @brief The reduction of the spring force in proportion to the speed of the joint’s 
    //! movement. 
    //!
    //! Setting a value above zero allows the joint to “dampen” oscillations which would otherwise
    //! carry on indefinitely. This is only used when the drive mode is set to Position or Position
    //! and Velocity.
    [Tooltip("The reduction of the spring force in proportion to the speed of the joint’s movement.")]
    public float positionDamper = 0.0f;

    //! @brief The force used to accelerate the joint toward its target velocity. 
    //!
    //! This is only used when the drive mode is set to Velocity or Position and Velocity.
    [Tooltip("The force used to accelerate the joint toward its target velocity.")]
    public float maximumForce = float.MaxValue;

    //! Default constructor.
    public SerializedJointDrive() { }

    //! Construct using given values.
    //!
    //! @param positionSpring See #positionSpring.
    //! @param positionDamper See #positionDamper.
    public SerializedJointDrive(float positionSpring, float positionDamper) {
      this.positionSpring = positionSpring;
      this.positionDamper = positionDamper;
    }

    //! Copy constructor.
    //!
    //! @param toCopy The instance to copy settings from.
    public SerializedJointDrive(SerializedJointDrive toCopy) {
      if (toCopy == null) {
        return;
      }
      positionSpring = toCopy.positionSpring;
      positionDamper = toCopy.positionDamper;
    }

    //! Wrap a given JointDrive.
    //!
    //! @param drive The instance to wrap.
    public SerializedJointDrive(JointDrive drive) {
      positionSpring = drive.positionSpring;
      positionDamper = drive.positionDamper;
      maximumForce = drive.maximumForce;
    }

    //! Wraps values to this instance.
    //!
    //! @param other The values to wrap.
    public void Wrap(JointDrive other) {
      positionSpring = other.positionSpring;
      positionDamper = other.positionDamper;
      maximumForce = other.maximumForce;
    }

    //! Unwraps this instance.
    //!
    //! @returns The unwrapped instance.
    public JointDrive Unwrap() {
      JointDrive jointDrive = new JointDrive();
      jointDrive.positionSpring = positionSpring;
      jointDrive.positionDamper = positionDamper;
      jointDrive.maximumForce = maximumForce;

      return jointDrive;
    }
  }

  //! A serializable wrapper of 
  //! [Quaternion](https://docs.unity3d.com/ScriptReference/Quaternion.html)
  [System.Serializable]
  public class SerializedQuaternion {

    //! @brief X component of the Quaternion. 
    //!
    //! Don't modify this directly unless you know quaternions inside out.
    [Tooltip("X component of the Quaternion.")]
    public float x = 0.0f;

    //! @brief Y component of the Quaternion. 
    //!
    //! Don't modify this directly unless you know quaternions inside out.
    [Tooltip("Y component of the Quaternion.")]
    public float y = 0.0f;

    //! @brief Z component of the Quaternion. 
    //!
    //! Don't modify this directly unless you know quaternions inside out.
    [Tooltip("Z component of the Quaternion.")]
    public float z = 0.0f;

    //! @brief W component of the Quaternion. 
    //!
    //! Don't modify this directly unless you know quaternions inside out.
    [Tooltip("W component of the Quaternion.")]
    public float w = 1.0f;

    //! Default constructor.
    public SerializedQuaternion() { }

    //! Construct using given values.
    //!
    //! @param x See #x.
    //! @param y See #y.
    //! @param z See #z.
    //! @param w see #w.
    public SerializedQuaternion(float x, float y, float z, float w) {
      this.x = x;
      this.y = y;
      this.z = z;
      this.w = w;
    }

    //! Wrap a given Quaternion.
    //!
    //! @param quaternion The instance to wrap.
    public SerializedQuaternion(Quaternion quaternion) {
      x = quaternion.x;
      y = quaternion.y;
      z = quaternion.z;
      w = quaternion.w;
    }

    //! Copy constructor.
    //!
    //! @param toCopy The instance to copy settings from.
    public SerializedQuaternion(SerializedQuaternion toCopy) {
      x = toCopy.x;
      y = toCopy.y;
      z = toCopy.z;
      w = toCopy.w;
    }

    //! Wraps values to this instance.
    //!
    //! @param other The values to wrap.
    public void Wrap(Quaternion other) {
      x = other.x;
      y = other.y;
      z = other.z;
      w = other.w;
    }

    //! Unwraps this instance.
    //!
    //! @returns The unwrapped instance.
    public Quaternion Unwrap() {
      return new Quaternion(x, y, z, w);
    }
  }
}
