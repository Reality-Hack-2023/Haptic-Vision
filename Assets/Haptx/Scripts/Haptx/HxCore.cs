// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using UnityEngine;

//! @defgroup group_unity_plugin Unity Plugin
//!
//! @brief Integration with the Unity development environment.
//!
//! See @ref page_unity_integration_guide to install and use these classes.
//!
//! See @ref page_unity_plugin_summary for a high level overview.

//! @defgroup group_unity_haptic_primitives Unity Haptic Primitives
//!
//! @brief Design advanced haptic interactions.
//!
//! See the @ref page_unity_integration_guide to install and use these classes.
//!
//! See the @ref page_unity_haptic_primitive_summary for a high level overview.

//! @brief Different techniques for managing physics network authority. Each technique has
//! strengths and weaknesses suited to different applications.
//!
//! Physics authority determines which physics simulation broadcasts its results to be assumed by
//! all other nodes (all other clients and the server) in a Unity/Mirror networking environment.
//! When a player has physics authority, their experiences are indistinguishable from a
//! non-networked environment.
public enum PhysicsAuthorityMode {
  //! "Dynamic" authority mode attempts to dynamically manage whether clients or the server have
  //! physics authority based on interaction context. Use "Dynamic" authority in projects that have
  //! joint interactions and reasonable latencies.
  //!
  //! @note This mode is experimental. There are some unresolved artifacts that can occur during
  //! physics authority transitions. Proceed with caution.
  DYNAMIC,
  //! "Client" authority always assigns clients authority over their interactions. Use this mode in
  //! projects that only have solo interactions as it will provide each player the best experience
  //! possible in all latency environments. Using this mode with joint interactions will result in
  //! undefined behavior.
  CLIENT,
  //! "Server" authority always gives the server authority over interactions. Use this mode in high
  //! latency environments.
  SERVER
};

//! @brief Responsible for global parameters and configuration of the HaptX system.
//!
//! See @ref section_haptx_core_prefab "Unity Plugin Guide" for a high level overview.
//!
//! @ingroup group_unity_plugin
public class HxCore : MonoBehaviour {
  [Header("Contact Interpreter")]

  //! See #EnableTactileFeedback.
  [Tooltip("True to enable tactile feedback. If disabled, per-object properties set to enable tactile feedback will have no effect.")]
  [SerializeField()]
  private bool _enableTactileFeedback = true;

  //! See #EnableForceFeedback.
  [Tooltip("True to enable force feedback. If disabled, per-object properties set to enable force feedback will have no effect.")]
  [SerializeField()]
  private bool _enableForceFeedback = true;

  //! The layers that can generate tactile feedback.
  [Tooltip("The layers that can generate tactile feedback.")]
  public LayerMask tactileFeedbackLayers = 1;

  //! The layers that can generate force feedback.
  [Tooltip("The layers that can generate force feedback.")]
  public LayerMask forceFeedbackLayers = 1;

  //! See #TactorCompressionFilterAttackRatio.
  [SerializeField(), Range(0.0f, 1.0f),
      Tooltip("The ratio that we decrease the current compression filter scale by when a haptic signal containing effect output doesn't fit entirely within the max inflation for a tactor.")]
  private float _tactorCompressionFilterAttackRatio = 0.0f;

  //! See #TactorCompressionFilterReleaseRatio.
  [SerializeField(), Range(1.0f, float.MaxValue),
      Tooltip("The ratio that we increase the current compression filter scale by when a haptic signal comfortably fits entirely within the max inflation for a tactor.")]
  private float _tactorCompressionFilterReleaseRatio = 1.005f;

  [Header("Grasping")]

  //! See #EnableGrasping.
  [Tooltip("True to enable grasping. If disabled, per-object properties set to enable grasping will have no effect.")]
  [SerializeField]
  private bool _enableGrasping = true;

  //! The layers that can be grasped.
  [Tooltip("The layers that can be grasped.")]
  public LayerMask graspLayers = 1;

  //! @brief A threshold that determines how difficult objects are to grasp by default.
  //!
  //! Increase it to make grasping happen less often. We don't recommend values above 200. Add a
  //! HxRigidbodyProperties to your GameObject to configure this value on a per-rigidbody basis.
  [Tooltip("A threshold that determines how difficult objects are to grasp by default.")]
  [SerializeField, Range(0.0f, float.MaxValue)]
  private float _graspThreshold = 18.0f;

  //! @brief A multiplier applied to #_graspThreshold that determines how easily objects are
  //! released by default.
  //!
  //! Increase it to make releasing easier. Add a HxRigidbodyProperties to your GameObject to
  //! configure this value on a per-rigidbody basis.
  [Tooltip("A multiplier applied to grasp threshold that determines how easily objects are released by default.")]
  [SerializeField, Range(0.0f, 1.0f)]
  private float _releaseHysteresis = 0.75f;

  //! The linear drive parameters that assist when grasping an object.
  [Tooltip("The linear drive parameters that assist when grasping an object.")]
  [SerializeField]
  ConfigurableJointParameters.SerializedJointDrive graspLinearDrive =
      new ConfigurableJointParameters.SerializedJointDrive(HxCore.GetDefaultGraspLinearDrive());

  //! @brief The angular drive parameters that assist when pinching an object.
  //!
  //! These parameters will only get used if a "pinching" grasp is detected.
  [Tooltip("The angular drive parameters that assist when pinching an object.")]
  [SerializeField]
  ConfigurableJointParameters.SerializedJointDrive pinchAngularDrive =
      new ConfigurableJointParameters.SerializedJointDrive(HxCore.GetDefaultAngularPinchDrive());

  //! @brief Anchor constraint linear limit settings.
  //!
  //! This class exists solely for organizational purposes in the editor.
  [Serializable]
  public class LinearAnchorConstraintSettings {

    //! Allow movement along the palm's X axis to be Free, completely Locked, or Limited
    //! according to the limit properties described below.
    [Tooltip("Allow movement along the palm's X axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion xMotion;

    //! Allow movement along the palm's Y axis to be Free, completely Locked, or Limited
    //! according to the limit properties described below.
    [Tooltip("Allow movement along the palm's Y axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion yMotion;

    //! Allow movement along the palm's Z axis to be Free, completely Locked, or Limited
    //! according to the limit properties described below.
    [Tooltip("Allow movement along the palm's Z axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion zMotion;

    //! A spring force applied to pull the object back when it goes past the limit position.
    [Tooltip("A spring force applied to pull the object back when it goes past the limit position.")]
    public ConfigurableJointParameters.SerializedSoftJointLimitSpring linearLimitSpring =
        new ConfigurableJointParameters.SerializedSoftJointLimitSpring();

    //! Limit on the joint’s linear movement
    //! (ie, movement over distance rather than rotation), specified as a distance from the joint’s
    //! origin.
    [Tooltip("Limit on the joint’s linear movement (ie, movement over distance rather than rotation), specified as a distance from the joint’s origin.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit linearLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();
  }

  //! The linear limit settings that get applied to grasps. This happens in the anchor's
  //! reference frame, which, for now means the palms' frames.
  [Tooltip("The linear limit settings that get applied to grasps. This happens in the anchor's reference frame, which, for now means the palms' frames.")]
  public LinearAnchorConstraintSettings graspLinearLimits = new LinearAnchorConstraintSettings();

  //! @brief Anchor constraint angular limit settings.
  //!
  //! This class exists solely for organizational purposes in the editor.
  [Serializable]
  public class AngularAnchorConstraintSettings {

    //! Allow movement along the palm's X axis to be Free, completely Locked, or Limited
    //! according to the limit properties described below.
    [Tooltip("Allow movement along the X axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion angularXMotion;

    //! Allow movement along the palm's Y axis to be Free, completely Locked, or Limited
    //! according to the limit properties described below.
    [Tooltip("Allow movement along the Y axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion angularYMotion;

    //! Allow movement along the palm's Z axis to be Free, completely Locked, or Limited
    //! according to the limit properties described below.
    [Tooltip("Allow movement along the Z axis to be Free, completely Locked, or Limited according to the limit properties described below.")]
    public ConfigurableJointMotion angularZMotion;

    //! A spring torque applied to rotate the object back when it goes past the limit angle
    //! of the joint.
    [Tooltip("A spring torque applied to rotate the object back when it goes past the limit angle of the joint.")]
    public ConfigurableJointParameters.SerializedSoftJointLimitSpring angularXLimitSpring =
        new ConfigurableJointParameters.SerializedSoftJointLimitSpring();

    //! Lower limit on the joint’s rotation around the X axis, specified as a angle from the
    //! joint’s original rotation.
    [Tooltip("Lower limit on the joint’s rotation around the X axis, specified as a angle from the joint’s original rotation.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit lowAngularXLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();

    //! This is similar to the Low Angular X Limit property described above but it
    //! determines the upper angular limit of the joint’s rotation rather than the lower limit.
    [Tooltip("This is similar to the Low Angular X Limit property described above but it determines the upper angular limit of the joint’s rotation rather than the lower limit.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit highAngularXLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();

    //! This is similar to the Angular X Limit Spring described above but applies to
    //! rotation around both the Y and Z axes.
    [Tooltip("This is similar to the Angular X Limit Spring described above but applies to rotation around both the Y and Z axes.")]
    public ConfigurableJointParameters.SerializedSoftJointLimitSpring angularYZLimitSpring =
        new ConfigurableJointParameters.SerializedSoftJointLimitSpring();

    //! Analogous to the Angular X Limit properties described above but applies to the Y
    //! axis and regards both the upper and lower angular limits as being the same.
    [Tooltip("Analogous to the Angular X Limit properties described above but applies to the Y axis and regards both the upper and lower angular limits as being the same.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit angularYLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();

    //! Analogous to the Angular X Limit properties described above but applies to the Z
    //! axis and regards both the upper and lower angular limits as being the same.
    [Tooltip("Analogous to the Angular X Limit properties described above but applies to the Z axis and regards both the upper and lower angular limits as being the same.")]
    public ConfigurableJointParameters.SerializedSoftJointLimit angularZLimit =
        new ConfigurableJointParameters.SerializedSoftJointLimit();
  }

  //! The angular limit settings that get applied to grasps. This happens in the anchor's
  //! reference frame, which, for now means the palms' frames.
  [Tooltip("The angular limit settings that get applied to grasps. This happens in the anchor's reference frame, which, for now means the palms' frames.")]
  public AngularAnchorConstraintSettings graspAngularLimits =
      new AngularAnchorConstraintSettings();

  //! @brief A visualizer that displays detected and assisted grasps.
  //!
  //! Elements include:
  //! - Black squares: The base of each grasp visualizer
  //! - Gray bars: Thresholds under which grasps are not being assisted
  //! - Blue bars: Scores for each grasp
  //! - Teal: Indicates grasp assistance
  [Tooltip("A visualizer that displays detected and assisted grasps.")]
  public GraspVisualizer graspVisualizer = new GraspVisualizer(KeyCode.Alpha5, false, true, false);

  //! @brief Represents parameters used in the grasp visualizer.
  //!
  //! This struct only exists for organizational purposes in the Inspector.
  [Serializable]
  public class GraspVisualizer : HxVisualizer {

    //! Default constructor.
    //!
    //! @param key The key that toggles this visualizer.
    //! @param alt Whether alt also needs to be pressed to toggle this visualizer.
    //! @param shift Whether shift also needs to be pressed to toggle this visualizer.
    //! @param control Whether control also needs to be pressed to toggle this visualizer.
    public GraspVisualizer(KeyCode key, bool alt, bool shift, bool control) :
        base(key, alt, shift, control) { }

    //! @brief The ratio of bar height to grasp score [m].
    //!
    //! Increase to make the grasp visualizer taller.
    [Tooltip("The ratio of bar height to grasp score [m]."), Range(0.0f, float.MaxValue)]
    public float scoreToM = 0.0001f;
  }

  //! @brief Which method gets used to evaluate physics authority in a networked environment.
  //!
  //! See PhysicsAuthorityMode for details.
  //!
  //! @note Dynamic authority is experimental. There are some unresolved artifacts that can occur
  //! during physics authority transitions. Proceed with caution.
  public PhysicsAuthorityMode PhysicsAuthorityMode {
    get {
      return _physicsAuthorityMode;
    }
    set {
      _physicsAuthorityMode = value;
    }
  }

  [Header("Networking")]

  //! @copydoc #PhysicsAuthorityMode
  [SerializeField, Tooltip(
      "Which method gets used to evaluate physics authority in a networked environment. Dynamic authority is experimental. There are some unresolved artifacts that can occur during physics authority transitions. Proceed with caution.")]
  PhysicsAuthorityMode _physicsAuthorityMode = PhysicsAuthorityMode.SERVER;

  //! A visualizer that displays network state.
  [Tooltip("A visualizer that displays network state.")]
  public HxVisualizer _networkStateVisualizer =
      new HxVisualizer(KeyCode.Alpha9, false, true, false);

  //! @brief True to enable tactile feedback. If disabled, per-object properties set to enable
  //! tactile feedback will have no effect.
  //!
  //! Permits the ContactInterpreter to request actions of tactors and their
  //! pressure regulators. Add an HxPhysicalMaterial to configure this value on a per-object basis.
  public bool EnableTactileFeedback {
    get {
      return _contactInterpreter.getEnableTactileFeedbackState();
    }

    set {
      _enableTactileFeedback = value;
      _contactInterpreter.setEnableTactileFeedbackState(_enableTactileFeedback);
    }
  }

  //! @brief True to enable force feedback. If disabled, per-object properties set to enable force
  //! feedback will have no effect.
  //!
  //! Permits the ContactInterpreter to request actions of retractuators and their pressure
  //! regulators. Add an HxPhysicalMaterial to configure this value on a per-object basis.
  public bool EnableForceFeedback {
    get {
      return _contactInterpreter.getEnableForceFeedbackState();
    }

    set {
      _enableForceFeedback = value;
      _contactInterpreter.setEnableForceFeedbackState(_enableForceFeedback);
    }
  }

  //! @brief The ratio that we decrease the current compression filter scale by when a haptic
  //! signal containing effect output doesn't fit entirely within the max inflation for a tactor.
  //!
  //! Should always be between 0 and 1 (inclusive). A value of 1 disables attacking; small values
  //! mean an aggressive attack; 0 means a perfect/infinite attack rate, and you'll never see
  //! signal output above max inflation.
  public float TactorCompressionFilterAttackRatio {
    get {
      if (_contactInterpreter != null) {
        return _contactInterpreter.getCompressionFilterAttackRatio();
      }
      return _tactorCompressionFilterAttackRatio;
    }

    set {
      _tactorCompressionFilterAttackRatio = value;
      if (_contactInterpreter != null) {
        _contactInterpreter.setCompressionFilterAttackRatio(value);
      }
    }
  }

  //! @brief The ratio that we increase the current compression filter scale by when a haptic
  //! signal comfortably fits entirely within the max inflation for a tactor.
  //!
  //! Should always be at least 1 (inclusive). A value of 1 disables releasing; large values mean
  //! an aggressive release.
  public float TactorCompressionFilterReleaseRatio {
    get {
      if (_contactInterpreter != null) {
        return _contactInterpreter.getCompressionFilterReleaseRatio();
      }
      return _tactorCompressionFilterReleaseRatio;
    }

    set {
      _tactorCompressionFilterReleaseRatio = value;
      if (_contactInterpreter != null) {
        _contactInterpreter.setCompressionFilterReleaseRatio(value);
      }
    }
  }

  //! @brief A threshold that determines how difficult objects are to grasp by default.
  //!
  //! Increase it to make grasping happen less often. We don't recommend values above 200. Add an
  //! HxRigidbodyProperties to configure this value on a per-object basis.
  public float GraspThreshold {
    get {
      return _graspThreshold;
    }

    set {
      _graspThreshold = value;
      if (_graspDetector != null) {
        _graspDetector.setDefaultGraspThreshold(value);
      }
    }
  }

  //! @brief A multiplier applied to #GraspThreshold that determines how easily objects are
  //! released by default.
  //!
  //! Increase it to make releasing easier. Add a HxRigidbodyProperties to your GameObject to
  //! configure this value on a per-rigidbody basis.
  public float ReleaseHysteresis {
    get {
      return _releaseHysteresis;
    }
    set {
      _releaseHysteresis = value;
      if (_graspDetector != null) {
        _graspDetector.setDefaultReleaseHysteresis(value);
      }
    }
  }

  //! @brief True to enable grasping. If disabled, per-object properties set to enable grasping
  //! will have no effect.
  //!
  //! Determines whether the GraspDetector assists the user with grasping physically simulating
  //! objects. Add an HxRigidbodyProperties to configure this value on a per-object basis.
  public bool EnableGrasping {
    get {
      return _enableGrasping;
    }

    set {
      _enableGrasping = value;
      if (_graspDetector != null) {
        _graspDetector.setEnabled(value);
      }
    }
  }

  //! @copydoc #HaptxSystem
  private HaptxApi.HaptxSystem _haptxSystem = null;

  //! A centralized interface with HaptX systems.
  public HaptxApi.HaptxSystem HaptxSystem {
    get {
      return _haptxSystem;
    }
  }

  //! @copydoc #ContactInterpreter
  private HaptxApi.ContactInterpreter _contactInterpreter = null;

  //! Used for converting physics data from the game engine to haptic feedback set points.
  public HaptxApi.ContactInterpreter ContactInterpreter {
    get {
      return _contactInterpreter;
    }
  }

  //! @copydoc #GraspDetector
  private HaptxApi.GraspDetector _graspDetector = null;

  //! Used for converting physics data from the game engine to haptic feedback set points.
  public HaptxApi.GraspDetector GraspDetector {
    get {
      return _graspDetector;
    }
  }

  //! The return value from the first meaningful call to InitializeHaptxSystem().
  private bool _initializeHaptxSystemResult = false;

  //! Maps from Dk2AirController ID to the HsvController that was configured to mirror it.
  private Dictionary<string, HaptxApi.HsvController> _hsvControllerFromAirControllerId =
      new Dictionary<string, HaptxApi.HsvController>();

  //! The HsvController being used to render simulated peripherals.
  private HaptxApi.HsvController _simulatedHardwareHsv = null;

  //! Populated with Log messages from HaptxApi.
  DequeOfLogMessage _haptxLogMessages = null;

  //! All other objects that send and/or receive information through the HaptX API in
  //! Update().
  HashSet<IHxUpdate> HxUpdates = new HashSet<IHxUpdate>();

  //! A map of grasp-capable body IDs to the game objects that they correspond to.
  private Dictionary<long, GameObject> _gdObjectIdToGameObject =
      new Dictionary<long, GameObject>();

  //! A map of ContactInterpreter object IDs to the callbacks being used to get
  //! simulation information about said objects.
  private Dictionary<long, HaptxApi.SimulationCallbacks> _ciObjectIdToCallbacks =
      new Dictionary<long, HaptxApi.SimulationCallbacks>();

  //! A map of ContactInterpreter body IDs to the callbacks being used to get
  //! simulation information about said bodies.
  private Dictionary<long, HaptxApi.SimulationCallbacks> _ciBodyIdToCallbacks =
      new Dictionary<long, HaptxApi.SimulationCallbacks>();

  //! Delegate for the #OnGrasp event.
  public delegate void GraspAction(GameObject graspedObject);

  //! @brief Gets fired when a grasp is created.
  //!
  //! Bind this event to perform actions when a grasp is created.
  //! See @ref section_haptx_core_prefab_grasping for an example of binding.
  public event GraspAction OnGrasp;

  //! Delegate for the #OnRelease event.
  public delegate void ReleaseAction(GameObject releasedObject);

  //! @brief Gets fired when a grasp is destroyed.
  //!
  //! Bind this event to perform actions when a grasp is destroyed.
  //! See @ref section_haptx_core_prefab_grasping for an example of binding.
  public event ReleaseAction OnRelease;

  //! Delegate for the #OnUpdate event.
  public delegate void UpdateAction(GameObject updatedObject);

  //! @brief Gets fired when a grasp is updated.
  //!
  //! Bind this event to perform actions when a grasp is updated. Grasps will be updated when the
  //! set of participating bodies changes without the grasp score dropping below the
  //! grasp threshold.
  //! See @ref section_haptx_core_prefab_grasping for an example of binding.
  public event UpdateAction OnUpdate;

  //! A map of grasp-capable body Ids to information associated with them for grasping.
  private Dictionary<long, GraspBodyInfo> _bodyIdToGraspBodyInfo =
      new Dictionary<long, GraspBodyInfo>();

  //! A map of grasp Ids to the corresponding grasp objects.
  private Dictionary<long, Grasp> _graspIdToGrasp = new Dictionary<long, Grasp>();

  //! This is the amount of time that has passed in the physics simulation since the
  //! last Update().
  public float PhysicsDeltaTimeS {
    get {
      return _physicsDeltaTimeS;
    }
  }

  //! See #PhysicsDeltaTimeS.
  float _physicsDeltaTimeS = 0.0f;

  //! Reset to default values.
  void Reset() {
    if (graspLinearDrive == null) {
      graspLinearDrive = new ConfigurableJointParameters.SerializedJointDrive();
    }
    graspLinearDrive.positionSpring = 100000.0f;
    graspLinearDrive.positionDamper = 1000.0f;
    graspLinearDrive.maximumForce = 30.0f;

    if (pinchAngularDrive == null) {
      pinchAngularDrive = new ConfigurableJointParameters.SerializedJointDrive();
    }
    pinchAngularDrive.positionSpring = 1000000.0f;
    pinchAngularDrive.positionDamper = 1000.0f;
    pinchAngularDrive.maximumForce = 3.0f;

    graspLinearLimits.xMotion = ConfigurableJointMotion.Free;
    graspLinearLimits.yMotion = ConfigurableJointMotion.Free;
    graspLinearLimits.zMotion = ConfigurableJointMotion.Free;
    graspLinearLimits.linearLimit.limit = 0.01f;

    graspAngularLimits.angularXMotion = ConfigurableJointMotion.Free;
    graspAngularLimits.angularYMotion = ConfigurableJointMotion.Free;
    graspAngularLimits.angularZMotion = ConfigurableJointMotion.Free;
  }

  //! Called when the script is being loaded.
  void Awake() {
    // If a designated core already exists, and it's not us, destroy ourselves.
    if (_DesignatedCore != null && _DesignatedCore != this) {
      // Yield to the chosen one, destroy myself.
      Debug.LogWarning(string.Format(
          "More than one HxCore detected in the scene at Awake(). Deleting from {0}!\nMake sure to only have one if you want to configure the HaptX system, otherwise the one you configure might be deleted due to the arbitrary order of Awake() calls.",
          gameObject.name));
      DestroyImmediate(this);
      return;
    }
  }

  //! Called every physics frame if enabled.
  void FixedUpdate() {
    if (_DesignatedCore != this) {
      return;
    }

    _physicsDeltaTimeS += Time.fixedDeltaTime;

    UpdateGrasps();
  }

  //! Called every frame if enabled.
  void Update() {
    // If I'm not the one that should be handling this logic, or if interfaces failed to open.
    if (_DesignatedCore != this || !IsHaptxSystemInitialized) {
      return;
    }

    foreach (IHxUpdate HxUpdate in HxUpdates) {
      if (HxUpdate != null) {
        HxUpdate.HxUpdate();
      } else {
        Debug.LogError("An IHxUpdate became invalidated without unregistering itself.");
      }
    }

    HaptxApi.AirController.maintainComms();
    UnorderedHaptxUuidToHapticFrame hapticFrames =
        HxReusableObjectPool<UnorderedHaptxUuidToHapticFrame>.Get();
    hapticFrames.Clear();
    _contactInterpreter.commit(PhysicsDeltaTimeS, hapticFrames);

    foreach (HaptxApi.Dk2AirController airController in _haptxSystem.getDk2AirControllers()) {
      if (airController == null) {
        continue;
      }
      HaptxApi.PneumaticFrame pneumaticFrame =
          HxReusableObjectPool<HaptxApi.PneumaticFrame>.Get();
      pneumaticFrame.pressures_pa.Clear();
      IntToPeripheral peripheralFromSlot = HxReusableObjectPool<IntToPeripheral>.Get();
      peripheralFromSlot.Clear();

      HaptxApi.AirController.ReturnCode ret =
          airController.getAttachedPeripherals(peripheralFromSlot);
      if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
        HxDebug.LogError(string.Format(
            "Dk2AirController.getAttachedPeripherals() failed with return code {0}: {1}.",
            (int)ret, HaptxApi.AirController.toString(ret)), this);
      } else {
        foreach(var keyValue in peripheralFromSlot) {
          HaptxApi.Peripheral peripheral = keyValue.Value;
          if (peripheral == null) {
            continue;
          }

          HaptxApi.HapticFrame hapticFrame = null;
          if (!hapticFrames.TryGetValue(peripheral.id, out hapticFrame)) {
            continue;
          }

          if (!HaptxApi.DirectPneumaticCalculator.addToPneumaticFrame(peripheral, hapticFrame,
              airController, pneumaticFrame)) {
            HxDebug.LogError(string.Format(
                "HaptxApi.DirectPneumaticCalculator.addToPneumaticFrame() failed for Peripheral {0}.",
                peripheral.casual_name), this);
          }
        }

        ret = airController.render(pneumaticFrame);
        if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
          HxDebug.LogError(string.Format(
              "HaptxApi.Dk2AirController.render() failed with error code {0}: {1}.", (int)ret,
              HaptxApi.AirController.toString(ret)), this);
        }

        string airControllerId = string.Empty;
        if (airController.getId(ref airControllerId) ==
            HaptxApi.AirController.ReturnCode.SUCCESS) {
          HaptxApi.HsvController hsvController;
          if (_hsvControllerFromAirControllerId.TryGetValue(airControllerId,
              out hsvController) && hsvController != null) {
            ret = hsvController.render(pneumaticFrame);
            if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
              HxDebug.LogError(string.Format(
                  "HaptxApi.HsvController.render() failed with error code {0}: {1}.", (int)ret,
                  HaptxApi.AirController.toString(ret)), this);
            }
          }
        }
      }
      HxReusableObjectPool<HaptxApi.PneumaticFrame>.Release(pneumaticFrame);
      HxReusableObjectPool<IntToPeripheral>.Release(peripheralFromSlot);
    }
    if (_simulatedHardwareHsv != null) {
      HaptxApi.PneumaticFrame pneumaticFrame =
          HxReusableObjectPool<HaptxApi.PneumaticFrame>.Get();
      pneumaticFrame.pressures_pa.Clear();
      IntToPeripheral peripheralFromSlot = HxReusableObjectPool<IntToPeripheral>.Get();
      peripheralFromSlot.Clear();

      HaptxApi.AirController.ReturnCode ret =
          _simulatedHardwareHsv.getAttachedPeripherals(peripheralFromSlot);
      if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
        HxDebug.LogError(string.Format(
            "HsvController.getAttachedPeripherals() failed with return code {0}: {1}.",
            (int)ret, HaptxApi.AirController.toString(ret)), this);
      } else {
        foreach (var keyValue in peripheralFromSlot) {
          HaptxApi.Peripheral peripheral = keyValue.Value;
          if (peripheral == null) {
            continue;
          }

          HaptxApi.HapticFrame hapticFrame = null;
          if (!hapticFrames.TryGetValue(peripheral.id, out hapticFrame)) {
            continue;
          }

          if (!HaptxApi.DirectPneumaticCalculator.addToPneumaticFrame(peripheral, hapticFrame,
              _simulatedHardwareHsv, pneumaticFrame)) {
            HxDebug.LogError(string.Format(
                "HaptxApi.DirectPneumaticCalculator.addToPneumaticFrame() failed for Peripheral {0}.",
                peripheral.casual_name), this);
          }
        }

        ret = _simulatedHardwareHsv.render(pneumaticFrame);
        if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
          HxDebug.LogError(string.Format(
              "HaptxApi.HsvController.render() failed with error code {0}: {1}.", (int)ret,
              HaptxApi.AirController.toString(ret)), this);
        }
      }
      HxReusableObjectPool<HaptxApi.PneumaticFrame>.Release(pneumaticFrame);
      HxReusableObjectPool<IntToPeripheral>.Release(peripheralFromSlot);
    }
    HxReusableObjectPool<UnorderedHaptxUuidToHapticFrame>.Release(hapticFrames);
    _physicsDeltaTimeS = 0.0f;
  }

  //! Called every frame if enabled, but after all Update() calls.
  void LateUpdate() {
    // If I'm not the one that should be handling this logic
    if (_DesignatedCore != this) {
      return;
    }

    // Print out any System Log messages we know about
    PrintLogMessages();

    // Update visualizer state
    graspVisualizer.Update();
    if (graspVisualizer.visualize && IsHaptxSystemInitialized) {
      VisualizeGrasping();
    }

    _networkStateVisualizer.Update();
  }

  //! Called when the scene ends or when manually destroyed.
  private void OnDestroy() {
    if (_DesignatedCore == this) {
      _DesignatedCore = null;

      // Print any final Log messages.
      PrintLogMessages();
      HaptxApi.SystemLogger.unregisterOutput(gameObject.name);
    }
  }

  //! Initializes all interfaces with HaptX systems.
  //!
  //! @returns Whether this object is successfully interfaced with HaptX systems.
  bool InitializeHaptxSystem() {
    // If we've already tried once, quit early.
    if (_haptxSystem != null) {
      return IsHaptxSystemInitialized;
    }

    // Another core has claimed this role.
    if (_DesignatedCore != null) {
      return false;
    }
    _DesignatedCore = this;
    _graspDetector = new HaptxApi.GraspDetector();
    _contactInterpreter = new HaptxApi.ContactInterpreter();

    // Initialize static interfaces.
    bool somethingWentWrong = false;
    HaptxApi.SystemLogger.unregisterOutput(gameObject.name);
    _haptxLogMessages = new DequeOfLogMessage();
    if (!HaptxApi.SystemLogger.registerOutput(gameObject.name, _haptxLogMessages)) {
      HxDebug.LogError("HaptxApi.SystemLogger.registerOutput() failed.", this);
      somethingWentWrong = true;
    }

    // Inflates HaptxSystem with the full picture of connected hardware.
    _haptxSystem = new HaptxApi.HaptxSystem();
    _haptxSystem.discoverDevices();

    // Setup HsvControllers to mirror Dk2AirControllers.
    if (_haptxSystem.getDk2AirControllers() != null && _haptxSystem.getHsvControllers() != null) {
      ListOfDk2AirController.ListOfDk2AirControllerNode airControllerNode =
          _haptxSystem.getDk2AirControllers().First;
      foreach (HaptxApi.HsvController hsvController in _haptxSystem.getHsvControllers()) {
        if (hsvController == null) {
          continue;
        }
        while (airControllerNode != null && airControllerNode.Value == null) {
          airControllerNode = airControllerNode.Next;
        }
        if (airControllerNode == null) {
          // We've run out of Dk2AirControllers to mirror. We can use this last hsv_controller to
          // render simulated hardware.
          _simulatedHardwareHsv = hsvController;
          break;
        }

        IntToPeripheral peripheralFromSlot = HxReusableObjectPool<IntToPeripheral>.Get();
        peripheralFromSlot.Clear();
        HaptxApi.AirController.ReturnCode ret =
            airControllerNode.Value.getAttachedPeripherals(peripheralFromSlot);
        if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
          HxDebug.LogError(string.Format(
              "HaptxApi.Dk2AirController.getAttachedPeripherals() failed with error code {0}: {1}.",
              (int)ret, HaptxApi.AirController.toString(ret)), this);
          somethingWentWrong = true;
        } else {
          foreach (var keyValue in peripheralFromSlot) {
            ret = hsvController.attachPeripheral(keyValue.Key, keyValue.Value);
            if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
              HxDebug.LogError(string.Format(
                  "HaptxApi.HsvController.attachPeripheral() failed with error code {0}: {1}.",
                  (int)ret, HaptxApi.AirController.toString(ret)), this);
              somethingWentWrong = true;
            }
          }
          string airControllerId = string.Empty;
          ret = airControllerNode.Value.getId(ref airControllerId);
          if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
            HxDebug.LogError(string.Format(
                "HaptxApi.Dk2AirController.getId() failed with error code {0}: {1}.",
                (int)ret, HaptxApi.AirController.toString(ret)), this);
            somethingWentWrong = true;
          } else {
            _hsvControllerFromAirControllerId.Add(airControllerId, hsvController);
          }
          airControllerNode = airControllerNode.Next;
        }
        HxReusableObjectPool<IntToPeripheral>.Release(peripheralFromSlot);
      }
    }

    if (!somethingWentWrong) {
      // Sync configurable modules.
      EnableTactileFeedback = _enableTactileFeedback;
      EnableForceFeedback = _enableForceFeedback;
      GraspThreshold = _graspThreshold;
      ReleaseHysteresis = _releaseHysteresis;
      EnableGrasping = _enableGrasping;
      TactorCompressionFilterAttackRatio = _tactorCompressionFilterAttackRatio;
      TactorCompressionFilterReleaseRatio = _tactorCompressionFilterReleaseRatio;
    }

    // Print out Log messages generated above.
    PrintLogMessages();

    _initializeHaptxSystemResult = !somethingWentWrong;
    return _initializeHaptxSystemResult;
  }

  //! Register the existence of a simulated peripheral.
  //!
  //! @param peripheral The simulated peripheral.
  public void RegisterSimulatedPeripheral(HaptxApi.Peripheral peripheral) {
    if (_simulatedHardwareHsv == null) {
      return;
    }

    HaptxApi.AirController.ReturnCode ret = HaptxApi.AirController.ReturnCode.UNKNOWN_ERROR;
    int slot = 0;
    HaptxApi.Glove glove = (HaptxApi.Glove)peripheral;
    if (glove != null) {
      slot = glove.handedness == HaptxApi.RelativeDirection.RD_LEFT ? 1 : 2;
    } else {
      IntToPeripheral peripheralFromSlot = new IntToPeripheral();
      ret = _simulatedHardwareHsv.getAttachedPeripherals(peripheralFromSlot);
      if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
        HxDebug.LogError(string.Format(
            "HsvAirController.getAttachedPeripherals() failed with return code {0}: {1}", (int)ret,
            HaptxApi.AirController.toString(ret)), this);
        return;
      }

      foreach (var keyValue in peripheralFromSlot) {
        if (keyValue.Value.id == peripheral.id) {
          return;
        }
        slot = Math.Max(keyValue.Key, slot);
      }
      slot += 1;
    }

    ret = _simulatedHardwareHsv.attachPeripheral(slot, peripheral);
    if (ret != HaptxApi.AirController.ReturnCode.SUCCESS) {
      HxDebug.LogError(string.Format(
          "HsvAirController.attachPeripheral() failed with return code {0}: {1}", (int)ret,
          HaptxApi.AirController.toString(ret)), this);
    }
  }

  //! True if this object is successfully interfaced with HaptX systems.
  bool IsHaptxSystemInitialized {
    get {
      return _initializeHaptxSystemResult;
    }
  }

  //! @brief Registers a HaptX class that does work in Update() that the core depends on.
  //!
  //! Only HaptX classes should call this function.
  //!
  //! @param HxUpdate The IHxUpdate instance to register.
  public void RegisterHxUpdate(IHxUpdate HxUpdate) {
    if (HxUpdate != null) {
      HxUpdates.Add(HxUpdate);
    } else {
      Debug.LogWarning("Tried to register a null IHxUpdate.");
    }
  }

  //! @brief Unregisters a HaptX class that does work in Update() that the core depends on.
  //!
  //! Only HaptX classes should call this function.
  //!
  //! @param HxUpdate The IHxUpdate instance to unregister.
  public void UnregisterHxUpdate(IHxUpdate HxUpdate) {
    if (HxUpdates.Contains(HxUpdate)) {
      HxUpdates.Remove(HxUpdate);
    } else {
      Debug.LogWarning("The given IHxUpdate is already unregistered.");
    }
  }

  //! Execute any actions recommended by the GraspDetector.
  void UpdateGrasps() {
    _graspDetector.detectGrasps(Time.fixedDeltaTime);
    for (int event_index = 0; event_index < _graspDetector.getGraspHistory().size(); event_index++) {
      HaptxApi.GraspDetector.GraspEvent graspEvent =
          _graspDetector.getGraspHistory().getitem(event_index);
      switch (graspEvent.action) {
        case HaptxApi.GraspDetector.GraspAction.DESTROY:
          Grasp graspToDestroy;
          if (_graspIdToGrasp.TryGetValue(graspEvent.grasp.id, out graspToDestroy) &&
              graspToDestroy != null) {
            DestroyGrasp(graspToDestroy);

            GameObject releaseObject;
            if (OnRelease != null &&
                _gdObjectIdToGameObject.TryGetValue(graspEvent.grasp.result.object_id,
                out releaseObject)) {
              OnRelease(releaseObject);
            }

            _graspIdToGrasp.Remove(graspEvent.grasp.id);
          } else {
            Debug.LogWarning(string.Format(
                "Attempted to destroy grasp {0}, but it didn't exist.",
                graspEvent.grasp.id));
          }

          break;
        case HaptxApi.GraspDetector.GraspAction.CREATE:
          if (!_graspIdToGrasp.ContainsKey(graspEvent.grasp.id)) {
            Grasp createdGrasp = CreateGrasp(graspEvent.grasp.result);

            _graspIdToGrasp.Add(graspEvent.grasp.id, createdGrasp);

            GameObject graspedObject;
            if (OnGrasp != null &&
                _gdObjectIdToGameObject.TryGetValue(graspEvent.grasp.result.object_id,
                out graspedObject)) {
              OnGrasp(graspedObject);
            }
          } else {
            Debug.LogError(string.Format(
              "Attempted to create grasp {0}, but it already existed.",
              graspEvent.grasp.id));
          }

          break;
        case HaptxApi.GraspDetector.GraspAction.UPDATE:
          Grasp grasp;
          if (_graspIdToGrasp.TryGetValue(graspEvent.grasp.id, out grasp) && grasp != null) {
            UpdateGrasp(grasp, graspEvent.grasp.result);

            // Broadcast the on update event.
            GameObject updatedObject;
            if (OnUpdate != null &&
                _gdObjectIdToGameObject.TryGetValue(graspEvent.grasp.result.object_id,
                out updatedObject)) {
              OnUpdate(updatedObject);
            }
          } else {
            Debug.LogWarning(string.Format(
                "Attempted to update grasp {0}, but it didn't exist.",
                graspEvent.grasp.id));
          }

          break;
        default:
          Debug.LogWarning("HaptxApi.GraspDetector recommended an invalid action");
          break;
      }
    }
    _graspDetector.clearGraspHistory();
  }

  //! @brief Performs any steps necessary to cease a given grasp.
  //!
  //! Restores the physical material on any body no longer participating in a grasp, and
  //! destroys any joints that were created in the formation of the given grasp.
  //!
  //! @param grasp The grasp to destroy.
  void DestroyGrasp(Grasp grasp) {
    foreach (long bodyId in grasp.bodyIds) {
      // Look for the body.
      GraspBodyInfo body;
      if (_bodyIdToGraspBodyInfo.TryGetValue(bodyId, out body) && body != null) {
        // Destroy any constraints.
        DestroyGraspJoint(bodyId, grasp.stickJoints);
        DestroyGraspJoint(bodyId, grasp.pinchJoints);
      } else {
        Debug.LogError(string.Format(
            "Attempted to destroy a grasp on body {0}, but couldn't find it.", bodyId));
      }
    }
    grasp.bodyIds.Clear();

    if (grasp.anchorJoint != null) {
      grasp.anchorJoint.connectedBody = null;
      Destroy(grasp.anchorJoint);
    }
  }

  //! @brief Performs any steps necessary to form a grasp.
  //!
  //! Updates physical materials on new bodies participating in a grasp. Creates grasp constraints.
  //!
  //! @param result The grasp to create.
  //! @returns Information about this grasp.
  Grasp CreateGrasp(HaptxApi.GraspDetector.GraspResult result) {
    Grasp grasp = new Grasp();

    // Look for the object.
    GameObject graspedObject = null;
    if (_gdObjectIdToGameObject.TryGetValue(result.object_id, out graspedObject)
        && graspedObject != null) {
      Rigidbody graspedRigidbody = graspedObject.GetComponent<Rigidbody>();
      if (graspedRigidbody != null) {
        grasp.bodyIds.Capacity = result.body_ids.Count;
        for (int bodyIndex = 0; bodyIndex < result.body_ids.Count; bodyIndex++) {
          long bodyId = result.body_ids[bodyIndex];
          grasp.bodyIds.Add(bodyId);

          // Look for the body.
          GraspBodyInfo body;
          if (_bodyIdToGraspBodyInfo.TryGetValue(bodyId, out body) && body != null) {
            // Create a joint to help objects stick to the hand.
            CreateStickJoint(grasp, body, graspedRigidbody);

            // If this grasp only consists of two bodies, create rotational joints.
            if (result.body_ids.Count == 2) {
              CreatePinchJoint(grasp, body, graspedRigidbody,
                  HxShared.UnityFromHx(result.avg_contact_location));
            }
          } else {
            Debug.LogError(string.Format(
                "Attempted to create a grasp on body {0}, but couldn't find it.", bodyId));
          }
        }

        // Look for the parent body if it's not the global root.
        if (result.parent_body_id > 0) {
          GraspBodyInfo parentBody;
          if (_bodyIdToGraspBodyInfo.TryGetValue(result.parent_body_id, out parentBody) &&
              parentBody != null) {
            if (parentBody.isAnchor) {
              CreateAnchorJoint(grasp, parentBody, graspedRigidbody,
                  HxShared.UnityFromHx(result.avg_contact_location));
            }
          } else {
            Debug.LogError(string.Format(
                "Attempted to create a grasp with parent body {0}, but couldn't find it.",
                result.parent_body_id));
          }
        }
      } else {
        Debug.LogWarning(string.Format(
          "Attempted to create a grasp on object {0}, but it doesn't contain a RigidBody component.",
          graspedObject.name));
      }
    } else {
      Debug.LogError(string.Format(
          "Attempted to create a grasp on object {0}, but couldn't find it.", result.object_id));
    }

    return grasp;
  }

  //! Performs any steps necessary to update an existing grasp.
  //!
  //! @param grasp Where to update grasp information.
  //! @param result Information about the new grasp.
  void UpdateGrasp(Grasp grasp, HaptxApi.GraspDetector.GraspResult result) {
    // Destroy old joints.
    foreach (long bodyId in grasp.bodyIds) {
      DestroyGraspJoint(bodyId, grasp.stickJoints);
      DestroyGraspJoint(bodyId, grasp.pinchJoints);
    }
    grasp.bodyIds.Clear();

    // Look for the object.
    GameObject graspedObject = null;
    if (_gdObjectIdToGameObject.TryGetValue(result.object_id, out graspedObject)
        && graspedObject != null) {
      Rigidbody graspedRigidbody = graspedObject.GetComponent<Rigidbody>();
      if (graspedRigidbody != null) {
        grasp.bodyIds.Capacity = result.body_ids.Count;
        for (int bodyIndex = 0; bodyIndex < result.body_ids.Count; bodyIndex++) {
          long bodyId = result.body_ids[bodyIndex];
          grasp.bodyIds.Add(bodyId);

          // Look for the body.
          GraspBodyInfo body;
          if (_bodyIdToGraspBodyInfo.TryGetValue(bodyId, out body) && body != null) {
            // Create new stick constraints.
            CreateStickJoint(grasp, body, graspedRigidbody);

            // Create new pinch constraints.
            if (result.body_ids.Count == 2) {
              CreatePinchJoint(grasp, body, graspedRigidbody,
                  HxShared.UnityFromHx(result.avg_contact_location));
            }
          } else {
            Debug.LogError(string.Format(
                "Attempted to update a grasp on body {0}, but couldn't find it.", bodyId));
          }
        }
      } else {
        Debug.LogWarning(string.Format(
          "Attempted to update a grasp on object {0}, but it doesn't contain a RigidBody component.",
          graspedObject.name));
      }
    } else {
      Debug.LogError(string.Format(
          "Attempted to update a grasp on object {0}, but couldn't find it.", result.object_id));
    }
  }

  //! Creates a stick joint between an object and a body.
  //!
  //! @param grasp The grasp instance to store the joint in.
  //! @param body The body being constrained.
  //! @param objectRigidbody The object being constrained.
  void CreateStickJoint(Grasp grasp, GraspBodyInfo body, Rigidbody objectRigidbody) {
    ConfigurableJoint linearJoint =
        body.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
    linearJoint.connectedBody = objectRigidbody;
    linearJoint.autoConfigureConnectedAnchor = true;
    linearJoint.anchor = body.rigidbody.transform.worldToLocalMatrix.MultiplyPoint3x4(
        body.rigidbody.worldCenterOfMass);
    linearJoint.xDrive = graspLinearDrive.Unwrap();
    linearJoint.yDrive = graspLinearDrive.Unwrap();
    linearJoint.zDrive = graspLinearDrive.Unwrap();
    grasp.stickJoints.Add(body.id, linearJoint);
  }

  //! Creates a pinch joint between an object and a body.
  //!
  //! @param grasp The grasp instance to store the joint in.
  //! @param body The body being constrained.
  //! @param objectRigidbody The object being constrained.
  //! @param jointLocation The location to position the joint before forming.
  void CreatePinchJoint(Grasp grasp, GraspBodyInfo body, Rigidbody objectRigidbody,
      Vector3 jointLocation) {
    ConfigurableJoint pinchJoint =
        body.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
    pinchJoint.connectedBody = objectRigidbody;
    pinchJoint.anchor = Quaternion.Inverse(pinchJoint.transform.rotation)
        * (jointLocation - pinchJoint.transform.position);
    pinchJoint.rotationDriveMode = RotationDriveMode.Slerp;
    pinchJoint.slerpDrive = pinchAngularDrive.Unwrap();
    pinchJoint.targetRotation = Quaternion.identity;
    pinchJoint.targetAngularVelocity = Vector3.zero;
    grasp.pinchJoints.Add(body.id, pinchJoint);
  }

  //! Creates an anchor joint between an object and an anchor body.
  //!
  //! @param grasp The grasp instance to store the joint in.
  //! @param anchor The anchor body being constrained.
  //! @param objectRigidbody The object being constrained.
  //! @param jointLocation The location to position the joint before forming.
  void CreateAnchorJoint(Grasp grasp, GraspBodyInfo anchor, Rigidbody objectRigidbody,
      Vector3 jointLocation) {
    ConfigurableJoint anchorJoint =
        anchor.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
    anchorJoint.connectedBody = objectRigidbody;
    anchorJoint.anchor = Quaternion.Inverse(anchorJoint.transform.rotation)
        * (jointLocation - anchorJoint.transform.position);
    anchorJoint.projectionMode = JointProjectionMode.PositionAndRotation;
    anchorJoint.projectionDistance = 0.001f;
    anchorJoint.projectionAngle = 1.0f;

    HxRigidbodyProperties hxRigidbodyProperties =
        objectRigidbody.GetComponent<HxRigidbodyProperties>();
    LinearAnchorConstraintSettings linearSettings =
        (hxRigidbodyProperties != null && hxRigidbodyProperties.overrideGraspLinearLimits) ?
        hxRigidbodyProperties.graspLinearLimits : graspLinearLimits;
    anchorJoint.xMotion = linearSettings.xMotion;
    anchorJoint.yMotion = linearSettings.yMotion;
    anchorJoint.zMotion = linearSettings.zMotion;
    anchorJoint.linearLimit = linearSettings.linearLimit.Unwrap();
    anchorJoint.linearLimitSpring = linearSettings.linearLimitSpring.Unwrap();

    AngularAnchorConstraintSettings angularSettings =
        (hxRigidbodyProperties != null && hxRigidbodyProperties.overrideGraspAngularLimits) ?
        hxRigidbodyProperties.graspAngularLimits : graspAngularLimits;
    anchorJoint.angularXMotion = angularSettings.angularXMotion;
    anchorJoint.angularYMotion = angularSettings.angularYMotion;
    anchorJoint.angularZMotion = angularSettings.angularZMotion;
    anchorJoint.lowAngularXLimit = angularSettings.lowAngularXLimit.Unwrap();
    anchorJoint.highAngularXLimit = angularSettings.highAngularXLimit.Unwrap();
    anchorJoint.angularYLimit = angularSettings.angularYLimit.Unwrap();
    anchorJoint.angularZLimit = angularSettings.angularZLimit.Unwrap();
    anchorJoint.angularXLimitSpring = angularSettings.angularXLimitSpring.Unwrap();
    anchorJoint.angularYZLimitSpring = angularSettings.angularYZLimitSpring.Unwrap();

    grasp.anchorJoint = anchorJoint;
  }

  //! Destroys a grasp joint.
  //!
  //! @param bodyId The body associated with the joint being destroyed.
  //! @param map The map containing the constraint being destroyed.
  void DestroyGraspJoint(long bodyId, Dictionary<long, ConfigurableJoint> map) {
    ConfigurableJoint joint;
    if (map.TryGetValue(bodyId, out joint)) {
      if (joint != null) {
        joint.connectedBody = null;
        Destroy(joint);
      }
      map.Remove(bodyId);
    }
  }

  //! @brief Visualize calculations happening within the GraspDetector.
  //!
  //! Lasts for one frame.
  void VisualizeGrasping() {
    //! The color of the score bar base.
    Color BaseColor = HxShared.DebugBlack;
    //! The color of the score bar when an object is grasped.
    Color GraspedColor = HxShared.DebugPurpleOrTeal;
    //! The color of the score bar when an object is not grasped.
    Color NotGraspedColor = HxShared.DebugBlueOrYellow;
    //! The color of the grasp threshold bar.
    Color ThresholdColor = HxShared.DebugGray;
    //! The scale of the grasp score bar base.
    Vector3 WBaseSizeM = new Vector3(0.02f, 0.02f, 0.002f);
    //! The vertical offset of the grasp score bars from objects being grasped.
    const float WVerticalOffsetM = 0.03f;
    //! The amount by which to space grasp score bars when there are multiple grasps present.
    const float WHorizontalOffsetM = 0.03f;
    //! The thickness to draw contribution cuboids.
    const float WContributionThicknessM = 0.003f;
    //! The rotation to use on all grasp bars.
    Quaternion WRotation =
        Quaternion.FromToRotation(Vector3.forward, Vector3.up);
    //! The width of the grasp score bar
    float WWidthM = 0.7f * WBaseSizeM.x;
    //! The width and length of the grasp threshold bar.
    float WThresholdWidthM = 0.5f * WBaseSizeM.x;

    // Keep track of how many grasps have formed per game object.
    Dictionary<long, int> objectIdToGraspCount = new Dictionary<long, int>();
    // Loop over all grasp results (even those that do not constitute a grasp).
    DequeOfGraspResult results = _graspDetector.getAllGraspResults();
    for (int resultIndex = 0; resultIndex < results.size(); resultIndex++) {
      HaptxApi.GraspDetector.GraspResult result = results.getitem(resultIndex);

      // Verify that the object exists.
      GameObject graspedObject = null;
      if (_gdObjectIdToGameObject.TryGetValue(result.object_id, out graspedObject) &&
          graspedObject != null) {
        // Increment the grasp count for this game object.
        int graspIndex;
        if (objectIdToGraspCount.TryGetValue(result.object_id, out graspIndex)) {
          objectIdToGraspCount[result.object_id] = graspIndex + 1;
        } else {
          graspIndex = 0;
          objectIdToGraspCount.Add(result.object_id, 1);
        }

        Vector3 wObjectCenterOfMass = graspedObject.transform.position;
        Rigidbody graspedRigidbody = graspedObject.GetComponent<Rigidbody>();
        if (graspedRigidbody != null) {
          wObjectCenterOfMass += graspedObject.transform.rotation *
              graspedObject.GetComponent<Rigidbody>().centerOfMass;
        }

        // Draw the bar at about the object's mesh (if it has one), otherwise draw about
        // its center of mass.
        Vector3 wBarPosition = wObjectCenterOfMass
            + WHorizontalOffsetM * graspIndex * Vector3.right
            + WVerticalOffsetM * Vector3.up;
        MeshFilter meshFilter = graspedObject.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null) {
          wBarPosition +=
              Vector3.Scale(
                  meshFilter.mesh.bounds.extents,
                  graspedObject.transform.lossyScale).magnitude
              * Vector3.up;
        }

        // Draw the base of the bar upside down.
        HxDebugMesh.DrawCube(wBarPosition,
            Quaternion.AngleAxis(180.0f, Vector3.forward) * WRotation, WBaseSizeM,
            BaseColor, true);

        // Draw the threshold.
        float wGrThresholdHeightM = graspVisualizer.scoreToM *
            (result.parameters.override_default_grasp_threshold ?
            result.parameters.grasp_threshold : _graspDetector.getDefaultGraspThreshold());
        Color totalScoreColor = NotGraspedColor;
        HaptxApi.GraspDetector.Grasp grasp =
            _graspDetector.getGrasp(result.object_id, result.parent_body_id);
        if (grasp != null) {
          wGrThresholdHeightM *= (result.parameters.override_default_release_hysteresis ?
            result.parameters.release_hysteresis : _graspDetector.getDefaultReleaseHysteresis());
          totalScoreColor = GraspedColor;
        }
        Vector3 wThresholdScale = new Vector3(
            WThresholdWidthM, WThresholdWidthM, wGrThresholdHeightM);
        HxDebugMesh.DrawCube(
            wBarPosition, WRotation, wThresholdScale, ThresholdColor, true);

        // Draw the total score bar.
        Vector3 wTotalScoreScale =
            new Vector3(WWidthM, WWidthM, result.score * graspVisualizer.scoreToM);
        HxDebugMesh.DrawCube(
            wBarPosition, WRotation, wTotalScoreScale, totalScoreColor, true);

        // Draw lines extending from each body contributing to the grasp, and the object
        // being grasped.
        for (int bodyIndex = 0; bodyIndex < result.body_ids.Count; bodyIndex++) {
          // Verify that the body exists.
          GraspBodyInfo body;
          if (_bodyIdToGraspBodyInfo.TryGetValue(result.body_ids[bodyIndex], out body)
              && body != null && body.rigidbody != null) {
            Vector3 wBodyCenterOfMass = body.rigidbody.transform.position
                + body.rigidbody.transform.rotation *
                  body.rigidbody.centerOfMass;

            // Draw a cuboid from the body to the object.
            HxDebugMesh.DrawLine(wBodyCenterOfMass, wObjectCenterOfMass,
                WContributionThicknessM, totalScoreColor);
          }
        }
      }
    }
  }

  //! Gets the default linear drive parameters used in grasp joints.
  //!
  //! @returns The default linear drive parameters used in grasp joints.
  public static JointDrive GetDefaultGraspLinearDrive() {
    JointDrive linearDrive = new JointDrive();
    linearDrive.positionSpring = 1.0e4f;
    linearDrive.positionDamper = 1.0e2f;
    linearDrive.maximumForce = 3.0f;
    return linearDrive;
  }

  //! Gets the default angular drive parameters used in grasp joints.
  //!
  //! @returns The default angular drive parameters used in grasp joints.
  private static JointDrive GetDefaultAngularPinchDrive() {
    JointDrive angularDrive = new JointDrive();
    angularDrive.positionSpring = 1.0e6f;
    angularDrive.positionDamper = 1.0e3f;
    angularDrive.maximumForce = 3.0f;
    return angularDrive;
  }

  //! @brief Returns the current HxCore in the scene.
  //!
  //! If none can be found, one will spawn and attempt to open all HaptX interfaces. There should
  //! always be an HxCore in the scene when using the HaptX API, but one does not have to be
  //! added to a scene manually. After a call to this function it is guaranteed that there exists
  //! at most one HxCore in the scene. This function can return null, and does so if the call to
  //! the underlying Core singleton fails to open all HaptX interfaces.
  //!
  //! @returns The current HxCore in the scene. Returns null if any HaptX interfaces fail to
  //! open.
  public static HxCore GetAndMaintainPseudoSingleton() {
    // Return early if there is already a designated core.
    if (_DesignatedCore != null) {
      return _DesignatedCore.IsHaptxSystemInitialized ? _DesignatedCore : null;
    }

    // Look for an existing core in the scene.
    HxCore resultCore = null;
    HxCore[] hxCores = FindObjectsOfType<HxCore>();
    if (hxCores.Length > 1) {
      foreach (HxCore hxCore in hxCores) {
        if (resultCore == null) {
          resultCore = hxCore;
        } else {
          Debug.LogWarning(string.Format(
              "More than one HxCore detected in the scene. Deleting from {0}!\nMake sure to only have one if you want to configure the HaptX system, otherwise the one you configure might be deleted due to the arbitrary order of event function calls.",
              hxCore.name));
          DestroyImmediate(hxCore);
        }
      }
    } else if (hxCores.Length == 1) {
      resultCore = hxCores[0];
    }

    // If we did not find an HxCore, spawn one.
    if (resultCore == null) {
      // Spawn one
      GameObject newGameObject = new GameObject("HaptX Core");
      resultCore = newGameObject.AddComponent<HxCore>();
      Debug.Log("A HaptX Core has been added to the scene.");
    }

    // Open the resultHxCore.
    resultCore.InitializeHaptxSystem();

    // If OpenAllInterfaces() succeeded, designatedCore will have been assigned the value of
    // resultHxCore, otherwise designatedCore will be null.
    return _DesignatedCore;
  }

  //! @brief Registers a Rigidbody with the GraspDetector.
  //!
  //! Rigidbodies will be registered automatically the first time they are contacted.
  //!
  //! @param rigidbody The Rigidbody being registered as a HaptxApi::GraspDetector object.
  //! @param registerAgain Registers the object even if it's already registered.
  //! @param [out] objectId The registered ID of the object.
  //! @returns Whether the object is registered at the end of this call.
  public bool TryRegisterRigidbody(Rigidbody rigidbody, bool registerAgain, out long objectId) {
    objectId = 0u;

    if (rigidbody == null) {
      Debug.LogError("Attempted to register a null Rigidbody.");
      return false;
    }

    objectId = rigidbody.GetInstanceID();
    if (!GraspDetector.isObjectRegistered(objectId) || registerAgain) {
      HaptxApi.GraspDetector.ObjectParameters gd_properties =
          HaptxApi.GraspDetector.getDefaultObjectParameters();
      bool causes_grasps = HxShared.IsLayerInMask(rigidbody.gameObject.layer, graspLayers);
      gd_properties.can_be_grasped = causes_grasps;

      // Check to see if this rigidbody has custom properties.
      HxRigidbodyProperties rigidbodyProperties =
          rigidbody.GetComponent<HxRigidbodyProperties>();
      if (rigidbodyProperties != null) {
        if (rigidbodyProperties.overrideGraspingEnabled) {
          gd_properties.can_be_grasped = rigidbodyProperties.graspingEnabled;
        }
        gd_properties.override_default_grasp_threshold =
            rigidbodyProperties.overrideGraspThreshold;
        gd_properties.grasp_threshold = rigidbodyProperties.graspThreshold;
        gd_properties.override_default_release_hysteresis =
            rigidbodyProperties.overrideReleaseHysteresis;
        gd_properties.release_hysteresis = rigidbodyProperties.releaseHysteresis;
      }

      GraspDetector.registerObject(objectId, gd_properties);
      if (!_gdObjectIdToGameObject.ContainsKey(objectId)) {
        _gdObjectIdToGameObject.Add(objectId, rigidbody.gameObject);
      }
    }
    return true;
  }

  //! @brief Registers a Collider with the HaptxApi::ContactInterpreter.
  //!
  //! Colliders will be registered automatically the first time they are contacted.
  //!
  //! @param collider The Collider being registered as a HaptxApi::ContactInterpreter
  //! object.
  //! @param registerAgain Registers the object even if it's already registered.
  //! @param [out] objectId The registered ID of the object.
  //! @returns Whether the object is registered at the end of this call.
  public bool TryRegisterCollider(Collider collider, bool registerAgain, out long objectId) {
    objectId = 0u;

    if (collider == null) {
      Debug.LogError("HxCore.RegisterCollider(): Null collider provided.");
      return false;
    }

    objectId = collider.GetInstanceID();
    if (!ContactInterpreter.isObjectRegistered(objectId) || registerAgain) {
      bool causes_tf = HxShared.IsLayerInMask(collider.gameObject.layer, tactileFeedbackLayers);
      bool causes_ff = HxShared.IsLayerInMask(collider.gameObject.layer, forceFeedbackLayers);

      // The default contact tolerance distance to use on objects registered with the CI.
      const float DEFAULT_CONTACT_TOLERANCE_M = 0;
      // The default object compliance to use on objects registered with the CI.
      const float DEFAULT_OBJECT_COMPLIANCE_M_N = 0;

      HaptxApi.ContactInterpreter.ObjectParameters ci_properties =
          new HaptxApi.ContactInterpreter.ObjectParameters(causes_tf, causes_ff,
          DEFAULT_CONTACT_TOLERANCE_M, DEFAULT_OBJECT_COMPLIANCE_M_N);

      // Search for HxPhysicalMaterial's that may be applying to this GameObject. If Collider
      // shares its GameObject with a Rigidbody, then there's no reason to search. It is already
      // at a physical "root".
      HxPhysicalMaterial physicalMaterial = collider.GetComponent<HxPhysicalMaterial>();
      if (physicalMaterial == null && collider.GetComponent<Rigidbody>() == null) {
        // Search upward until we encounter either a Rigidbody or an HxPhysicalMaterial.
        Transform parent = collider.transform.parent;
        while (parent != null) {
          HxPhysicalMaterial parentMaterial = parent.GetComponent<HxPhysicalMaterial>();
          if (parentMaterial != null) {
            if (parentMaterial.propagateToChildren) {
              physicalMaterial = parentMaterial;
            }
            break;
          }
          Rigidbody rigidbody = parent.GetComponent<Rigidbody>();
          if (rigidbody != null) {
            break;
          }
          parent = parent.transform.parent;
        }
      }

      if (physicalMaterial != null) {
        ci_properties.triggers_tactile_feedback =
            causes_tf && !physicalMaterial.disableTactileFeedback;
        if (physicalMaterial.overrideForceFeedbackEnabled) {
          ci_properties.triggers_force_feedback = physicalMaterial.forceFeedbackEnabled;
        }
        if (physicalMaterial.overrideBaseContactTolerance) {
          ci_properties.base_contact_tolerance_m = physicalMaterial.baseContactToleranceM;
        }
        if (physicalMaterial.overrideCompliance) {
          ci_properties.compliance_m_n = physicalMaterial.complianceM_N;
        }
      }

      HaptxApi.SimulationCallbacks callbacks;
      if (_ciObjectIdToCallbacks.TryGetValue(objectId, out callbacks)) {
        if (callbacks != null) {
          callbacks.Dispose();
        }

        callbacks = new HxTransformCallbacks(collider.transform, Matrix4x4.identity);
        _ciObjectIdToCallbacks[objectId] = callbacks;
      } else {
        callbacks = new HxTransformCallbacks(collider.transform, Matrix4x4.identity);
        _ciObjectIdToCallbacks.Add(objectId, callbacks);
      }
      ContactInterpreter.registerObject(objectId, ci_properties, callbacks);
    }
    return true;
  }

  //! @brief Associates a Rigidbody with a HaptxApi::ContactInterpreter body ID.
  //!
  //! Any independently moving part of an HxHand may be associated with its own
  //! HaptxApi::ContactInterpreter body ID, or it can share one with other parts of the hand.
  //!
  //! @param ciBodyId The HaptxApi::ContactInterpreter body ID being associated with the
  //! following Rigidbody.
  //! @param rigidbody The Rigidbody being associated with the preceding
  //! HaptxAPi::ContactInterpreter body Id.
  //! @param parameters Additional information about this body.
  //! @param rigidBodyPart The HaptxApi::RigidBodyPart associated with the Rigidbody.
  public void RegisterCiBody(long ciBodyId, Rigidbody rigidbody,
      BodyParameters parameters, HaptxApi.RigidBodyPart rigidBodyPart) {
    HaptxApi.SimulationCallbacks callbacks;
    if (_ciBodyIdToCallbacks.TryGetValue(ciBodyId, out callbacks)) {
      if (callbacks != null) {
        callbacks.Dispose();
      }

      callbacks = new HxRigidbodyCallbacks(rigidbody);
      _ciBodyIdToCallbacks[ciBodyId] = callbacks;
    } else if (rigidbody != null) {
      callbacks = new HxRigidbodyCallbacks(rigidbody);
      _ciBodyIdToCallbacks.Add(ciBodyId, callbacks);
    }
    ContactInterpreter.registerBody(ciBodyId, parameters.Unwrap(), rigidBodyPart, callbacks);
  }

  //! @brief Associates a Rigidbody with a HaptxApi::GraspDetector body ID.
  //!
  //! Any independently moving part of an HxHand may be associated with its own
  //! HaptxApi::GraspDetector body ID, or it can share one with other parts of the hand.
  //!
  //! @param gdBodyId The HaptxApi::GraspDetector body ID being associated with the
  //! following Rigidbody.
  //! @param rigidbody The Rigidbody being associated with the preceding
  //! HaptxApi::GraspDetector body ID.
  //! @param isAnchor Whether this body is a valid anchor.
  public void RegisterGdBody(long gdBodyId, Rigidbody rigidbody, bool isAnchor = false) {
    if (!rigidbody) {
      HxDebug.LogError(string.Format("Attempted to register null Rigidbody with GD ID {0}.",
          gdBodyId));
    }

    GraspBodyInfo graspBodyInfo = new GraspBodyInfo(gdBodyId, rigidbody, isAnchor);
    if (!_bodyIdToGraspBodyInfo.ContainsKey(gdBodyId)) {
      _bodyIdToGraspBodyInfo.Add(gdBodyId, graspBodyInfo);
    } else {
      _bodyIdToGraspBodyInfo[gdBodyId] = graspBodyInfo;
    }
  }

  //! Prints all HaptX Log messages currently stored in the Core to the Unity log.
  private void PrintLogMessages() {
    // Log all API messages to the Unity Editor log.
    while (!_haptxLogMessages.empty()) {
      HaptxApi.LogMessage logMessage = _haptxLogMessages.getitem(0);
      string messageText = string.Format("{0}: {1}", logMessage.caller, logMessage.message_data);

      switch (logMessage.severity) {
        case (HaptxApi.ELogSeverity.ELS_WARNING):
          HxDebug.LogWarning(messageText, null, logMessage.access_level ==
              HaptxApi.ELogAccess.ELA_USER);
          break;
        case (HaptxApi.ELogSeverity.ELS_ERROR):
          HxDebug.LogError(messageText, null, logMessage.access_level ==
              HaptxApi.ELogAccess.ELA_USER);
          break;
        default:
          HxDebug.Log(messageText);
          break;
      }
      _haptxLogMessages.pop_front();
    }
  }

  //! The information associated with a body for grasping purposes.
  private class GraspBodyInfo {

    //! Constructor to populate all fields.
    //!
    //! @param id This body's id.
    //! @param rigidbody The Rigidbody associated with this body.
    //! @param isAnchor Whether this body is a valid anchor.
    public GraspBodyInfo(long id, Rigidbody rigidbody, bool isAnchor) {
      this.id = id;
      this.rigidbody = rigidbody;
      this.isAnchor = isAnchor;
    }

    //! This body's ID.
    public long id;

    //! The Rigidbody that represents this body.
    public Rigidbody rigidbody;

    //! Whether this body is a valid anchor.
    public bool isAnchor;
  }

  //! The information associated with a single grasp.
  private class Grasp {
    // All bodies participating in the grasp.
    public List<long> bodyIds = new List<long>();

    //! @brief A map of the bodies participating in a grasp to their associated stick joints.
    //!
    //! Stick joints consist of linear springs in each axis. They help objects stick to bodies
    //! in a way that matches our physical intuition. Every body participating in a grasp is
    //! guaranteed to be represented in this map.
    public Dictionary<long, ConfigurableJoint> stickJoints =
        new Dictionary<long, ConfigurableJoint>();

    //! @brief A map of the bodies participating in a pinch to their associated pinch joints.
    //!
    //! Pinch joints consist of angular springs in each axis. They only form when exactly two
    //! bodies are participating in a grasp, and they help provide a sense of rotational friction
    //! in this case which is otherwise under-constrained.
    public Dictionary<long, ConfigurableJoint> pinchJoints =
        new Dictionary<long, ConfigurableJoint>();

    //! @brief The anchor constraint for this grasp. Can be null if the grasp parent isn't an
    //! anchor.
    //!
    //! Anchor joints consist of linear and angular limits. They allow constrained objects to
    //! move relative to bodies dubbed "anchors" within acceptable limits, then provide a hard
    //! stop. This protects against really large forces that may occur when bodies are accelerating
    //! rapidly or encountering static objects.
    public ConfigurableJoint anchorJoint = null;
  }

  //! The singleton instance that was responsible for opening all the interfaces, and thus
  //! should be responsible for updating and closing all of them.
  static HxCore _DesignatedCore = null;

  //! Allows the HxCore to control the execution order of Update() functions on HaptX
  //! classes that it depends on.
  public interface IHxUpdate {
    //! Called by the HxCore in place of Update().
    void HxUpdate();
  }
}
