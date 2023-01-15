// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using Mirror;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

//! Delegate for left hand initialized events.
public delegate void OnLeftHandInitializedEvent(HxHand hand);

//! Delegate for right hand initialized events.
public delegate void OnRightHandInitializedEvent(HxHand hand);

//! Extension methods for the RelDir enum.
public static class RelDirExtensions {

  //! Converts a RelDir value to a HaptxApi::RelativeDirection value.
  //!
  //! @param rel_dir The value to convert.
  //! @returns The converted value.
  public static HaptxApi.RelativeDirection ToHx(this RelDir rel_dir) {
    switch (rel_dir) {
      case RelDir.LEFT:
        return HaptxApi.RelativeDirection.RD_LEFT;
      case RelDir.RIGHT:
        return HaptxApi.RelativeDirection.RD_RIGHT;
      default:
        return HaptxApi.RelativeDirection.RD_LAST;
    }
  }
}

//! Different ways hand animation can be optimized.
public enum HandAnimationOptimizationMode {
  //! Optimizes for fingertip position when a finger is near the thumb, and joint angles otherwise.
  DYNAMIC = 0,
  //! Always optimizes for joint angles. Users may find that the avatar fingers don't touch when
  //! their real fingers do.
  JOINT_ANGLES,
  //! Always optimizes for fingertip positions. Users may find their knuckles scrunched when they
  //! should be straight.
  FINGERTIP_POSITIONS
};

//! @brief Represents one HaptX Glove.
//!
//! See the @ref section_hx_hand_script "Unity Plugin Guide" for a high level overview.
//!
//! @ingroup group_unity_plugin
public class HxHand : NetworkBehaviour, HxCore.IHxUpdate {
  [Header("Configuration")]

  //! Whether this is a left or right hand.
  [Tooltip("Whether this is a left or right hand.")]
  [SerializeField, SyncVar]
  public RelDir hand;

  //! Map hand segments to bone names.
  [Tooltip("Map hand segments to bone names.")]
  [SerializeField]
  HandBoneNames boneNames = null;

  //! See #HandScaleFactor.
  [Tooltip("A scale factor that gets applied to the hand after user hand profile scaling is applied."),
      Range(0.01f, float.MaxValue)]
  [SerializeField, SyncVar(hook = nameof(HandScaleFactorHook))]
  float handScaleFactor = 1.0f;

  //! A scale factor that gets applied to the hand after user hand profile scaling is
  //! applied.
  public float HandScaleFactor {
    get {
      return handScaleFactor;
    }
    set {
      handScaleFactor = Mathf.Max(value, 0.01f);
      SetScale(handScaleFactor * _userProfileScaleFactor);
    }
  }

  [Header("Physics")]

  //! Linear drive parameters used in the joint driving the palm.
  [Tooltip("Linear drive parameters used in the joint driving the palm.")]
  [SerializeField()]
  ConfigurableJointParameters.SerializedJointDrive linearDrive = null;

  //! Angular drive parameters used in the joint driving the palm.
  [Tooltip("Angular drive parameters used in the joint driving the palm.")]
  [SerializeField()]
  ConfigurableJointParameters.SerializedJointDrive angularDrive = null;

  //! Whether damping constraints can form between the palm and objects contacting it.
  [Tooltip("Whether damping constraints can form between the palm and objects contacting it.")]
  public bool enableContactDamping = true;

  //! @brief The maximum separation below which contact damping is triggered.
  //!
  //! Only meaningful if less than Physics.defaultContactOffset.
  [Tooltip("The maximum separation below which contact damping is triggered."),
      Range(0.0f, float.MaxValue)]
  public float maxContactDampingSeparation = 0.001f;

  //! Linear damping used in the joint that forms between the palm and objects contacting
  //! it.
  [Tooltip("Linear damping used in the joint that forms between the palm and objects contacting it.")]
  public float linearContactDamping = 0.0f;

  //! Angular damping used in the joint that forms between the palm and objects contacting
  //! it.
  [Tooltip("Angular damping used in the joint that forms between the palm and objects contacting it.")]
  public float angularContactDamping = 0.033f;

  //! Whether to override the default solver iteration values on Rigidbodies in this hand.
  [SerializeField, Tooltip(
      "Whether to override the default solver iteration values on Rigidbodies in this hand.")]
  bool _overrideSolverIterations = true;

  //! The position solver iteration value to use if overriding the default setting.
  [SerializeField, Tooltip(
      "The position solver iteration value to use if overriding the default setting.")]
  int rigidbodySolverIterations = 32;

  //! The velocity solver iteration value to use if overriding the default setting.
  [SerializeField, Tooltip(
      "The velocity solver iteration value to use if overriding the default setting.")]
  int rigidbodySolverVelocityIterations = 4;

  //! Whether to override the default contact offset setting on colliders in this hand.
  [SerializeField, Tooltip(
      "Whether to override the default contact offset setting on colliders in this hand.")]
  bool _overrideContactOffset = true;

  //! The contact offset to use if overriding the default setting.
  [SerializeField, Range(0.0f, 0.02f), Tooltip(
      "The contact offset to use if overriding the default.")]
  float _contactOffset = 0.005f;

  [Header("Mocap")]

  //! @brief Which hand animation optimization mode to use.
  [Tooltip("Which hand animation optimization mode to use.")]
  public HandAnimationOptimizationMode handAnimOptimizationMode =
      HandAnimationOptimizationMode.DYNAMIC;

  //! @brief The relative distance threshold to use if #handAnimOptimizationMode is set to
  //! HandAnimationOptimizationMode::DYNAMIC.
  //!
  //! A value of 0 effectively disables dynamic hand animation optimization and a value of 1 makes
  //! it engage as early as possible.
  [Range(0.0f, 1.0f), Tooltip("The relative distance threshold to use if handAnimOptimizationMode is set to HandAnimationOptimizationMode::DYNAMIC A value of 0 effectively disables dynamic hand animation optimization and a value of 1 makes it engage as early as possible.")]
  public float dynamicHandAnimRelDistThreshold = 0.5f;

  //! @brief Whether to automatically compensate for Glove slippage.
  //!
  //! This subroutine kicks in when the user's fingers are out flat, and incrementally offsets the
  //! avatar's palm until its fingers are also out flat.
  [Tooltip("Whether to automatically compensate for Glove slippage. This subroutine kicks in when the user's fingers are out flat, and incrementally offsets the avatar's palm until its fingers are also out flat.")]
  public bool enableGloveSlipCompensation = true;

  //! Holds the parameters that characterize Glove slip compensation.
  [Serializable]
  public class GloveSlipCompensationParameters {

    //! Multiplied by delta time [s] to get LERP alpha. Increase to make slip compensation happen
    //! faster.
    [Tooltip("Multiplied by delta time [s] to get LERP alpha. Increase to make slip compensation happen faster."),
        Range(0.0f, 10.0f)]
    public float aggressiveness_1_s = 1.0f;

    //! How straight the user's fingers need to be for Glove slip compensation to
    //! engage. Values should range between [0, 1]. A value of 1 indicates perfectly straight
    //! (such that Glove slip compensation will likely never engage), and a value of 0 indicates that
    //! Glove slip compensation is always engaged.
    [Tooltip("How straight the user's fingers need to be for Glove slip compensation to engage. Values should range between [0, 1]. A value of 1 indicates perfectly straight (such that Glove slip compensation will likely never engage), and a value of 0 indicates that Glove slip compensation is always engaged."),
        Range(0.0f, 1.0f)]
    public float onThreshold = 0.95f;
  };

  //! Parameters that characterize Glove slip compensation.
  [Tooltip("Parameters that characterize Glove slip compensation.")]
  public GloveSlipCompensationParameters gloveSlipCompensationParameters;

  //! @brief Whether to automatically compensate for thimble thickness.
  //!
  //! This subroutine kicks in when the user's fingertips are close to the thumb tip and
  //! linearly reduces the gaps to zero.
  [Tooltip("Whether to automatically compensate for thimble thickness. This subroutine kicks in when the user's fingertips are close to the thumb tip and linearly reduces the gaps to zero.")]
  public bool enableThimbleCompensation = true;

  //! Holds the parameters that characterize thimble compensation.
  [Serializable]
  public class ThimbleCompensationParameters {

    //! The distance to the thumb tip within which we correct the position of other fingertips [m].
    //! Should always be >= maxCorrectionDistM.
    [Tooltip("The distance to the thumb tip within which we correct the position of other fingertips [m]. Should always be >= maxCorrectionDistM."),
        Range(0.0f, 0.1f)]
    public float correctionDistThresholdM = 0.05f;

    //! The distance to the thumb tip at which we apply maxCorrectionAmountM [m]. Should always
    //! be >= maxCorrectionAmountM and <= correctionDistThresholdM.
    [Tooltip("The distance to the thumb tip at which we apply maxCorrectionAmountM [m]. Should always be >= maxCorrectionAmountM and <= correctionDistThresholdM."),
        Range(0.0f, 0.05f)]
    public float maxCorrectionDistM = 0.01f;

    //! The maximum correction we can apply to fingertip positions [m]. Should always be <=
    //! maxCorrectionDistM and >= 0.
    [Tooltip("The maximum correction we can apply to fingertip positions [m]. Should always be <= maxCorrectionDistM and >= 0."),
        Range(0.0f, 0.05f)]
    public float maxCorrectionAmountM = 0.01f;
  };

  //! Parameters that characterize thimble compensation.
  [Tooltip("Parameters that characterize thimble compensation.")]
  public ThimbleCompensationParameters thimbleCompensationParameters;

  //! Whether to teleport the hand to its tracked location and rotation if it deviates by a
  //! specified distance.
  [Tooltip("Whether to teleport the hand to its tracked location and rotation if it deviates by a specified distance.")]
  public bool enableCorrectiveTeleportation = true;

  //! @brief The max distance the hand can deviate from its tracked location before being
  //! teleported to it.
  //!
  //! We automatically scale this when HandScaleFactor is greater than 1. Does not occur if
  //! #enableCorrectiveTeleportation is false.
  [Range(0.0f, float.MaxValue)]
  [Tooltip("The max distance the hand can deviate from its tracked location before being teleported to it.")]
  public float correctiveTeleportationDistance = 0.5f;

  //! The Transform we currently treat as the origin for the motion capture system.
  [Tooltip("The Transform we currently treat as the origin for the motion capture system.")]
  [SyncVar]
  public GameObject mocapOriginObject;

  //! How quickly simulated poses change.
  [Range(0.0f, 100.0f), Tooltip("How quickly simulated poses change.")]
  public float simulatedAnimationAggressiveness_1_s = 10.0f;

  [Header("Contact Interpreter")]

  //! Parameters controlling how fingers are physically modeled in the
  //! HaptxApi::ContactInterpreter.
  [Tooltip("Parameters controlling how fingers are physically modeled in the contact interpreter.")]
  [SerializeField()]
  BodyParameters fingerBodyParameters = null;

  //! Parameters controlling how the palm is physically modeled in the
  //! HaptxApi::ContactInterpreter.
  [Tooltip("Parameters controlling how the palm is physically modeled in the contact interpreter.")]
  [SerializeField()]
  BodyParameters palmBodyParameters = null;

  //! Force feedback parameters for each finger.
  [Tooltip("Force feedback parameters for each finger.")]
  [SerializeField]
  AllRetractuatorParameters retractuatorParameters = null;


  //! Parameters controlling the radius of the sphere traces on each of this hand's patches
  [Range(0.0f, 0.004f), Tooltip("The radius of this hand's SphereTraces [m], 0 converts to line traces")]
  public float sphereTraceRadiusM = 0.00280625f;

  //! Delegate event and related variables to update the sphere traces on the patches when the value changes
  //! This supports runtime changes made in the editor, done for ease of tuning. 
  float _patchTraceRadiusM = 0.00280625f;
  public delegate void OnShpereTraceRadiusDelegate(HxHand hand, float newVal);
  public event OnShpereTraceRadiusDelegate OnShpereTraceRadiusChange;

  [Header("Networking")]

  //! @brief The layer assigned to hands controlled by the local player.
  //!
  //! The expected configuration is that hands with this layer will not be able to collide with
  //! or feel each other, but will be able to collide with and feel hands with the remote layer.
  //! Configure which layers these hands collide with using Unity's native collision matrix, and
  //! configure whether these hands can be felt using HxCore's "Tactile Feedback Layers" and
  //! "Force Feedback Layers".
  [SerializeField, Tooltip(
      "The layer assigned to hands controlled by the local player. The expected configuration is that hands with this layer will not be able to collide with or feel each other, but will be able to collide with and feel hands with the remote layer. Configure which layers these hands collide with using Unity's native collision matrix, and configure whether these hands can be felt using HxCore's \"Tactile Feedback Layers\" and \"Force Feedback Layers\".")]
  HxLayer _localHandsLayer = new HxLayer();

  //! @brief The layer assigned to hands controlled by remote players.
  //!
  //! The expected configuration is that the local player's hands will be able to collide with and
  //! feel hands with this layer, but not their own hands. Configure which layers these hands
  //! collide with using Unity's native collision matrix, and configure whether these hands can be
  //! felt using HxCore's "Tactile Feedback Layers" and "Force Feedback Layers".
  [SerializeField, Tooltip(
      "The layer assigned to hands controlled by remote players. The expected configuration is that the local player's hands will be able to collide with and feel hands with this layer, but not their own hands. Configure which layers these hands collide with using Unity's native collision matrix, and configure whether these hands can be felt using HxCore's \"Tactile Feedback Layers\" and \"Force Feedback Layers\".")]
  HxLayer _remoteHandsLayer = new HxLayer();

  //! @brief The frequency [Hz] at which to transmit physics targets for multiplayer.
  //!
  //! Increasing this value reduces lag, but increases the volume of network traffic.
  [SerializeField, SyncVar, Range(1.0f, 100.0f), Tooltip(
      "The frequency [Hz] at which to transmit physics targets for multiplayer. Increasing this value reduces lag, but increases the volume of network traffic.")]
  float physicsTargetsTransmissionFrequencyHz = 50.0f;

  //! @brief The target amount of time [s] frames are buffered.
  //!
  //! Increasing this value increases lag, but improves the smoothness and stability of physics
  //! networking.
  [SerializeField, Range(0.0f, 1.0f), Tooltip(
      "The target amount of time [s] frames are buffered. Increasing this value increases lag, but improves the smoothness and stability of physics networking.")]
  float physicsTargetsBufferDurationS = 0.05f;

  //! @brief The frequency [Hz] at which to transmit physics state for multiplayer.
  //!
  //! Increasing this value reduces lag, but increases the volume of network traffic.
  [SerializeField, SyncVar, Range(1.0f, 100.0f), Tooltip(
      "The frequency [Hz] at which to transmit physics state for multiplayer. Increasing this value reduces lag, but increases the volume of network traffic.")]
  float physicsStateTransmissionFrequencyHz = 50.0f;

  //! @brief The target amount of time [s] frames are buffered.
  //!
  //! Increasing this value increases lag, but improves the smoothness and stability of physics
  //! networking.
  [SerializeField, Range(0.0f, 1.0f), Tooltip(
      "The target amount of time [s] frames are buffered. Increasing this value increases lag, but improves the smoothness and stability of physics networking.")]
  float physicsStateBufferDurationS = 0.05f;

  //! @brief The radius of the sphere collider that defines this hand's physics authority zone.
  //!
  //! A larger radius means that objects farther from the hand will be synced, but bandwidth
  //! consumption will go up.
  [SerializeField, Range(0.0f, 1.0f), Tooltip(
      "The radius of the sphere collider that defines this hand's physics authority zone. A larger radius means that objects farther from the hand will be synced with high fidelity, but bandwidth consumption will also go up.")]
  float _physicsAuthorityZoneRadiusM = 0.15f;

  //! @brief As soon as the physics authority zone overlaps another physics authority zone the
  //! radius will increase by a multiplier equal to 1 plus this value. As soon as it is no longer
  //! overlapping any physics authority zones it will go back to its original size.
  //!
  //! This prevents fluttering of state if physics authority zones are oscillating in and out of
  //! each other.
  [SerializeField, Range(0.0f, 1.0f), Tooltip(
      "As soon as the physics authority zone overlaps another physics authority zone the radius will increase by a multiplier equal to 1 plus this value. As soon as it is no longer overlapping any physics authority zones it will go back to its original size. This prevents fluttering of state if physics authority zones are oscillating in and out of each other.")]
  float _physicsAuthorityZoneRadiusHysteresis = 0.1f;

  //! The zone that contains objects which are evaluated for physics authority.
  private SphereCollider _physicsAuthorityZone = null;

  //! The layer assigned to the physics authority zone.
  [SerializeField, Tooltip("The layer assigned to the physics authority zone.")]
  HxLayer _physicsAuthorityZoneLayer = new HxLayer();

  [Header("Visualization")]

  [Tooltip(
    "Visualize force feedback. Elements include:\n" +
    "1. Black bars: actuation normals\n" +
    "2. Gray bars: actuation thresholds\n" +
    "3. Blue bars: forces\n" +
    "4. Teal: represents actuation")]
  public ForceFeedbackVisualizer forceFeedbackVisualizer =
      new ForceFeedbackVisualizer(KeyCode.Alpha4, false, true, false);

  //! @brief Represents parameters used in the force feedback visualizer.
  //!
  //! This struct only exists for organizational purposes in the Inspector.
  [Serializable]
  public class ForceFeedbackVisualizer : HxVisualizer {

    //! Default constructor.
    //!
    //! @param key The key that toggles this visualizer.
    //! @param alt Whether alt also needs to be pressed to toggle this visualizer.
    //! @param shift Whether shift also needs to be pressed to toggle this visualizer.
    //! @param control Whether control also needs to be pressed to toggle this visualizer.
    public ForceFeedbackVisualizer(KeyCode key, bool alt, bool shift, bool control) :
        base(key, alt, shift, control) { }

    //! How much forces get scaled in the force feedback visualizer. Increase to make force
    //! feedback visualizations taller.
    [Tooltip("How much to scale forces in the force feedback visualizer. Increase to make force feedback visualizations taller."),
        Range(0.0f, float.MaxValue)]
    public float forceScaleM_N = 1.8e-4f;
  }

  //! Whether to visualize locations and rotations of mocap tracked segments.
  [Tooltip("Whether to visualize locations and rotations of mocap tracked segments.")]
  public HxVisualizer motionCaptureVisualizer =
      new HxVisualizer(KeyCode.Alpha1, false, true, false);

  //! Whether to visualize hand animation intermediate steps and data.
  [Tooltip("Whether to visualize hand animation intermediate steps and data.")]
  public HxVisualizer handAnimationVisualizer =
      new HxVisualizer(KeyCode.Alpha6, false, true, false);

  //! @brief Represents parameters used in the displacement visualizer.
  //!
  //! This struct only exists for organizational purposes in the Inspector.
  [Serializable]
  public class DisplacementVisualizer : HxVisualizer {

    //! Default constructor.
    //!
    //! @param key The key that toggles this visualizer.
    //! @param alt Whether alt also needs to be pressed to toggle this
    //! visualizer.
    //! @param shift Whether shift also needs to be pressed to toggle this
    //! visualizer.
    //! @param control Whether control also needs to be pressed to toggle this
    //! visualizer.
    public DisplacementVisualizer(KeyCode key, bool alt, bool shift, bool control) :
        base(key, alt, shift, control) { }

    //! The color of the visualizer hand.
    [Tooltip("The color of the visualizer hand.")]
    public Color color = Color.white;

    //! The displacements at which parts of the hand start to fade in.
    [Tooltip("The displacements at which parts of the hand start to fade in.")]
    public float minDisplacementM = 0.02f;

    //! The displacements at which parts of the hand are fully faded in.
    [Tooltip("The displacements at which parts of the hand are fully faded in.")]
    public float maxDisplacementM = 0.05f;

    //! The opacity of hand parts at max displacement.
    [Tooltip("The opacity of hand parts at max displacement.")]
    [Range(0.0f, 1.0f)]
    public float maxOpacity = 0.05f;

    //! The filter strength that gets applied to hand opacity.
    [Tooltip("The filter strength that gets applied to hand opacity.")]
    public float opacityFilterStrengthS = 0.1f;
  }

  //! Whether to visualize displacement of the virtual hand from motion capture and hand
  // animation targets.
  [Tooltip("Whether to visualize displacement of the virtual hand from motion capture and hand animation targets.")]
  public DisplacementVisualizer displacementVisualizer =
      new DisplacementVisualizer(KeyCode.Alpha8, false, true, false);

  //! Whether to visualize which objects are being damped to better sit in the palm.
  [Tooltip("Whether to visualize which objects are being damped to better sit in the palm.")]
  public HxVisualizer contactDampingVisualizer =
      new HxVisualizer(KeyCode.Alpha0, false, true, false);

  [Header("Meshes and Materials")]

  //! All the meshes/prefabs that the hand can take shape with.
  [Tooltip("All the meshes/prefabs that the hand can take shape with.")]
  public HandMeshes handMeshes = new HandMeshes();

  //! @brief All the meshes/prefabs that the hand can take shape with.
  //!
  //! This struct only exists for organizational purposes in the Inspector.
  [Serializable]
  public struct HandMeshes {

    //! Which prefab to use for the left female hand.
    [Tooltip("Which prefab to use for the left female hand.")]
    public GameObject leftFemaleHandPrefab;  // This gets a default value in Reset().

    //! Which prefab to use for the right female hand.
    [Tooltip("Which prefab to use for the right female hand.")]
    public GameObject rightFemaleHandPrefab;  // This gets a default value in Reset().

    //! Which prefab to use for the left male hand.
    [Tooltip("Which prefab to use for the left male hand.")]
    public GameObject leftMaleHandPrefab;  // This gets a default value in Reset().

    //! Which prefab to use for the right male hand.
    [Tooltip("Which prefab to use for the right male hand.")]
    public GameObject rightMaleHandPrefab;  // This gets a default value in Reset().
  }

  //! All the meshes/prefabs that the hand can take shape with.
  [Tooltip("All the meshes/prefabs that the hand can take shape with.")]
  public HandMaterials handMaterials = new HandMaterials();

  //! @brief All the materials that the hand can have assigned.
  //!
  //! This struct only exists for organizational purposes in the Inspector.
  [Serializable]
  public struct HandMaterials {

    //! The material to use on the female hand for light skin.
    [Tooltip("The material to use on the female hand for light skin.")]
    public Material lightFemaleHandMaterial;  // This gets a default value in Reset().

    //! The material to use on the female hand for medium skin.
    [Tooltip("The material to use on the female hand for medium skin.")]
    public Material mediumFemaleHandMaterial;  // This gets a default value in Reset().

    //! The material to use on the female hand for dark skin.
    [Tooltip("The material to use on the female hand for dark skin.")]
    public Material darkFemaleHandMaterial;  // This gets a default value in Reset().

    //! The material to use on the female hand for neutral skin.
    [Tooltip("The material to use on the female hand for neutral skin.")]
    public Material neutralFemaleHandMaterial;  // This gets a default value in Reset().

    //! The material to use on the male hand for light skin.
    [Tooltip("The material to use on the male hand for light skin.")]
    public Material lightMaleHandMaterial;  // This gets a default value in Reset().

    //! The material to use on the male hand for medium skin.
    [Tooltip("The material to use on the male hand for medium skin.")]
    public Material mediumMaleHandMaterial;  // This gets a default value in Reset().

    //! The material to use on the male hand for dark skin.
    [Tooltip("The material to use on the male hand for dark skin.")]
    public Material darkMaleHandMaterial;  // This gets a default value in Reset().

    //! The material to use on the male hand for neutral skin.
    [Tooltip("The material to use on the male hand for neutral skin.")]
    public Material neutralMaleHandMaterial;  // This gets a default value in Reset().
  }

  [Header("SteamVR")]
  //! The SteamVR action to handle grasps with simulated hands.
  [Tooltip("The SteamVR action to handle grasps with simulated hands.")]
  public Valve.VR.SteamVR_Action_Single progressSimulatedPoseAction;
  //! The SteamVR action to handle switching to the next pose with simulated hands.
  [Tooltip("The SteamVR action to handle switching to the next pose with simulated hands.")]
  public Valve.VR.SteamVR_Action_Boolean nextSimulatedPoseAction;
  //! The SteamVR action to handle switching to the last pose with simulated hands
  [Tooltip("The SteamVR action to handle switching to the last pose with simulated hands.")]
  public Valve.VR.SteamVR_Action_Boolean previousSimulatedPoseAction;

  //! Get the currently active left hand.
  //!
  //! @returns The currently active left hand. Null if there isn't an active left hand.
  public static HxHand LeftHand {
    get {
      return _LeftHand;
    }
  }

  //! See #LeftHand.
  private static HxHand _LeftHand = null;

  //! Get the currently active right hand.
  //!
  //! @returns The currently active right hand. Null if there isn't an active right hand.
  public static HxHand RightHand {
    get {
      return _RightHand;
    }
  }

  //! See #RightHand.
  private static HxHand _RightHand = null;

  //! The player that spawned this hand (if any).
  [SyncVar, NonSerialized]
  public GameObject player = null;

  //! Event that fires when the left hand is initialized.
  public static OnLeftHandInitializedEvent OnLeftHandInitialized;

  //! Event that fires when the right hand is initialized.
  public static OnRightHandInitializedEvent OnRightHandInitialized;

  //! The return code of the last call to openvr wrapper tracking functions.
  private HaptxApi.OpenvrWrapper.ReturnCode _lastOpenvrWrapperReturnCode = HaptxApi.OpenvrWrapper.ReturnCode.SUCCESS;

  //! Reference to the HxCore pseudo-singleton.
  HxCore core = null;

  //! Which gesture we're using (if simulating hand animation).
  HaptxApi.Gesture _gesture = HaptxApi.Gesture.PRECISION_GRASP;

  //! The last anim frame we used (if simulating hand animation).
  HaptxApi.AnimFrame _lastSimulatedAnimFrame = null;

  //! The mocap system driving the fingers on this hand.
  HaptxApi.HyleasSystem _mocapSystem = null;

  //! The Glove slip compensator working on this hand.
  HaptxApi.GloveSlipCompensator _gloveSlipCompensator = null;

  //! @copydoc #Peripheral
  HaptxApi.Glove _glove = null;

  //! The HaptxApi.Peripheral this hand represents.
  public HaptxApi.Peripheral Peripheral {
    get {
      return _glove;
    }
  }

  //! The user of this hand. Only populated on hands with local authority.
  HaptxApi.UserProfile _userProfile = null;

  //! A profile derived from the dimensions of the avatar hand. The overall size of this hand is
  //! based on the real user profile.
  HaptxApi.UserProfile _avatarProfile = null;

  //! The profile whose dimensions slide between #_userProfile and #_avatarProfile based on
  //! avatar animation optimization.
  HaptxApi.UserProfile _avatarAnimOptimizedProfile = null;

  //! The biological sex of the user.
  [SyncVar(hook = nameof(BiologicalSexHook))]
  HaptxApi.BiologicalSex _biologicalSex = HaptxApi.BiologicalSex.DONT_RECORD;

  //! The skin tone of the user.
  [SyncVar(hook = nameof(SkinToneHook))]
  HaptxApi.SkinTone _skinTone = HaptxApi.SkinTone.NEUTRAL;

  //! The user's middle finger length [m].
  [SyncVar(hook = nameof(UserMiddleFingerLengthMHook))]
  float _userMiddleFingerLengthM = 0.0f;

  //! The hand's default world scale calculated from user hand profile information.
  float _userProfileScaleFactor = 1.0f;

  //! The joint responsible for moving the hand around the scene.
  ConfigurableJoint palmJoint = null;

  //! Information that HxHand stores on a per-bone basis.
  class HxHandBoneData {

    //! The game object representing this bone.
    public GameObject gameObject = null;

    //! Whether #ciBodyId has been defined for this bone.
    public bool hasCiBodyId = false;

    //! The ID of this bone to the contact interpreter interface.
    public long ciBodyId = 0u;

    //! The rigid body part associated with this bone.
    public HaptxApi.RigidBodyPart rigidBodyPart = HaptxApi.RigidBodyPart.LAST;

    //! Whether #gdBodyId has been defined for this bone.
    public bool hasGdBodyId = false;

    //! The ID of this bone to the grasp detector interface.
    public long gdBodyId = 0u;

    //! Which retractuator operates on this bone (if any).
    public HaptxApi.Retractuator retractuator = null;

    //! Whether this bone can engage contact damping.
    public bool canEngageContactDamping = false;
  }

  //! The HaptxApi::GraspDetector body ID referring to the hand as a whole.
  long wholeHandGdBodyId;

  //! Mapping from bone name to extra information associated with said bone.
  Dictionary<HxName, HxHandBoneData> boneDataFromBoneName =
      new Dictionary<HxName, HxHandBoneData>();

  //! Mapping from GameObject instance ID to the same data in #boneDataFromBoneName.
  Dictionary<int, HxHandBoneData> boneDataFromInstanceId =
      new Dictionary<int, HxHandBoneData>();

  //! @brief The ConfigurableJoints driving the hand.
  //!
  //! Indexed by HaptxApi::Finger and HaptxApi::FingerJoint.
  ConfigurableJoint[,] joints;

  //! @brief Initial rotation offsets that end up baked into configurable joints.
  //!
  //! Indexed by HaptxApi::Finger and HaptxApi::FingerJoint.
  Quaternion[,] jointRotationOffsets;

  //! Whether we've warned about the Vive tracking reference being off recently.
  bool recentlyWarnedAboutTrackingRefBeingOff = false;

  // The magnitude of the palm's compound bounding box extents.
  float palmExtent = 0.0f;

  //! The set of all objects that hit the hand this frame.
  HashSet<int> contactingObjectIds = new HashSet<int>();

  //! @brief Wraps all information about an object experiencing contact damping.
  //!
  //! This class exists only for organizational purposes.
  class ContactDampingObjectData {

    //! The joint applying the contact damping.
    public ConfigurableJoint joint = null;

    //! @brief The linear drive settings to use when the Y drive is engaged.
    //!
    //! The Y drive gets disabled when the object is moving toward the palm so as not to inhibit
    //! any forces that may contribute to tactile feedback.
    public JointDrive linearDrive = new JointDrive();
  }

  // A map to existing contact damping joints from their associated object id's.
  Dictionary<int, ContactDampingObjectData> contactDampingDataFromObjectId =
      new Dictionary<int, ContactDampingObjectData>();

  //! Whether we've already configured the controllers for simulated mocap on the first
  //! Update().
  bool firstUpdateHasHappened = false;

  //! True if the palm has never been teleported.
  bool palmNeedsFirstTeleport = true;

  //! The message key used to log the tracking ref warning message for this hand.
  int trackingRefOffDebugMessageKey = -1;

  //! Whether the displacement visualizer has been initialized.
  bool disVisInitialized = false;

  //! Whether the displacement visualizer has failed to initialize.
  bool disVisFailedToInitialize = false;

  //! @brief The skinned mesh rendering the physical hand.
  SkinnedMeshRenderer physSmr = null;

  //! @brief The bones of the physical skinned mesh renderer.
  Transform[] physSmrBones = null;

  //! @brief The skinned mesh rendering the displacement visualizer.
  //!
  //! Only valid if #disVisInitialized is true.
  SkinnedMeshRenderer disVisSmr = null;

  //! @brief The bones of the visualizer skinned mesh renderer.
  //!
  //! Only valid if #disVisInitialized is true.
  Transform[] visSmrBones = null;

  //! @brief Digital filters on bone opacities.
  //!
  //! Only valid if #disVisInitialized is true.
  HxLowPassFilter[] disVisOpacityFilters = null;

  //! @brief The visualizer game objects whose rotations may be updated with joint angles to
  //! animate the hand.
  //!
  //! Indexed by HaptxApi::Finger and HaptxApi::FingerJoint.
  GameObject[,] disVisJoints = null;

  //! @brief The base pose of the hand.
  //!
  //! The transforms of each bone relative to their parents. Null for the root bone.
  Tuple<Vector3, Quaternion>[] basePose = null;

  //! The suffix appended to mesh names for bone weight texture files.
  public static readonly string BoneWeightsFileSuffix = "_bone_weights";

  //! Whether hand physics is authoritative on the client or the server.
  //!
  //! @note This is different than Mirror's notion of authority. It is possible for the client to
  //! have Mirror authority and the server to have physics authority, and visa versa.
  [SyncVar(hook = nameof(IsClientPhysicsAuthorityHook))]
  bool _isClientPhysicsAuthority = true;

  //! Whether this hand currently has physics authority.
  bool IsPhysicsAuthority {
    get {
      return (isServer && !_isClientPhysicsAuthority) || (hasAuthority && _isClientPhysicsAuthority);
    }
  }

  //! @brief Current physics state. Constructed piece-meal to be communicated over the network at
  //! regular intervals.
  //!
  //! Only hands with physics authority use this data member.
  HandPhysicsState _physicsState;

  //! Buffer of physics targets frames.
  CircularBuffer<HandPhysicsTargetsFrame> _physicsTargetsBuffer =
      new CircularBuffer<HandPhysicsTargetsFrame>(128);

  //! Where sequential data in #_physicsTargetsBuffer begins.
  int _physicsTargetsBufferTailIndex = 0;

  //! Where sequential data in #_physicsTargetsBuffer ends.
  int _physicsTargetsBufferHeadIndex = 0;

  //! Whether data insertion into #_physicsTargetsBuffer has started.
  bool _physicsTargetsBufferStarted = false;

  //! Buffer of physics state frames.
  CircularBuffer<HandPhysicsStateFrame> _physicsStateBuffer =
      new CircularBuffer<HandPhysicsStateFrame>(128);

  //! Where sequential data in #_physicsStateBuffer begins.
  int _physicsStateBufferTailIndex = 0;

  //! Where sequential data in #_physicsStateBuffer ends.
  int _physicsStateBufferHeadIndex = 0;

  //! Whether data insertion into #_physicsStateBuffer has started.
  bool _physicsStateBufferStarted = false;

  //! The last time that this hand transmitted a physics update (relative to the beginning of the
  //! game).
  float _timeOfLastPhysicsTransmissionS = 0.0f;

  //! The effective world time from the simulation on the other end of the network that we're using
  //! to interpolate values.
  float _followTimeS = 0.0f;

  //! How many other physics authority zones are currently being overlapped.
  int _numPhysicsAuthorityZoneOverlaps = 0;

  //! All the objects currently overlapping the hand mapped to the number of overlap events from
  //! that object.
  Dictionary<NetworkIdentity, int> _objectsInPhysicsAuthorityZone =
      new Dictionary<NetworkIdentity, int>();

  //! @brief A static map tracking global, inter-hand data about objects that are in at least one
  //! physics authority zone.
  //!
  //! If more than one client would claim authority over an object, the server claims it instead.
  static Dictionary<NetworkIdentity, GlobalPhysicsAuthorityObjectData>
      globalPhysicsAuthorityDataFromNetIdentity =
      new Dictionary<NetworkIdentity, GlobalPhysicsAuthorityObjectData>();

  public override void OnStartClient() {
    if (!ConnectToCore()) {
      HxDebug.LogError("HxHand.OnStartClient(): Failed to connect to core.", this, true);
      HardDisable();
      return;
    }
    if (hasAuthority) {
      if (CheckForDuplicateHands()) {
        HxDebug.LogError(string.Format(
            "Multiple hands claim to be the {0} hand, disabling myself!", HandAsText()), this,
            true);
        HardDisable();
        return;
      }

      LoadUserProfile();
      RegisterRetractuators();
    }
  }

  void FixedUpdate() {
    if (!enabled) {
      return;
    }

    if (isServer && !hasAuthority && IsPhysicsAuthority) {
      // The server being authoritative over a hand being driven by a client.
      InterpolatePhysicsTargets(Time.fixedDeltaTime);
    } else if (!IsPhysicsAuthority) {
      // Any other hand that isn't a physics authority.
      InterpolatePhysicsState(Time.fixedDeltaTime);
    }

    ManageContactDamping();
  }

  //! Called every frame by HxCore if enabled.
  public void HxUpdate() {
    if (!enabled) {
      return;
    }

    if (hasAuthority) {
      // Configuration that happens only once per session and that has to happen after Awake().
      if (!firstUpdateHasHappened) {
        firstUpdateHasHappened = true;
        if (!enabled) {
          return;
        }

        if (hand == RelDir.LEFT) {
          _LeftHand = this;
          if (OnLeftHandInitialized != null) {
            OnLeftHandInitialized(this);
          }
        } else {
          _RightHand = this;
          if (OnRightHandInitialized != null) {
            OnRightHandInitialized(this);
          }
        }
      }

      UpdateHandAnimation();
      UpdatePatchSphereTraceRadius();
    }

    if (core._networkStateVisualizer.visualize) {
      VisualizeNetworkState();
    }
  }

  void LateUpdate() {
    if (!enabled) {
      return;
    }

    if (isServer) {
      UpdatePhysicsAuthority();
    }

    if (IsPhysicsAuthority) {
      float timeS = (float)NetworkTime.time;
      float periodS = 1.0f / physicsStateTransmissionFrequencyHz;
      // Insert a half period of offset for left hands to stagger messages.
      if (timeS - _timeOfLastPhysicsTransmissionS -
          (hand == RelDir.LEFT ? 0.5f * periodS : 0.0f) > periodS) {
        RigidbodyState.GetRigidbodyStates(gameObject, ref _physicsState.wBodyStates);
        GetPhysicsStatesOfObjectsInAuthorityZone(ref _physicsState.wObjectStates);
        if (isServer) {
          RpcClientUpdatePhysicsState(timeS, _physicsState);
        } else {
          CmdUpdatePhysicsState(timeS, _physicsState);
        }
        _timeOfLastPhysicsTransmissionS = timeS;
      }
    }

    if (hasAuthority) {
      motionCaptureVisualizer.Update();
      handAnimationVisualizer.Update();

      forceFeedbackVisualizer.Update();
      if (forceFeedbackVisualizer.visualize) {
        VisualizeForceFeedbackOutput();
      }

      displacementVisualizer.Update();
      if (displacementVisualizer.visualize) {
        if (!disVisInitialized && !disVisFailedToInitialize) {
          if (InitializeDisplacementVisualizer()) {
            disVisInitialized = true;
          } else {
            UninitializeDisplacementVisualizer();
            disVisFailedToInitialize = true;
          }
        }

        if (disVisInitialized) {
          UpdateDisplacementVisualizer();
        }
      } else if (disVisInitialized) {
        UninitializeDisplacementVisualizer();
        disVisInitialized = false;
        disVisFailedToInitialize = false;
      }

      contactDampingVisualizer.Update();
      if (contactDampingVisualizer.visualize) {
        VisualizeContactDamping();
      }
    }
  }

  //! Called when the scene ends or when manually destroyed.
  void OnDestroy() {
    if (core != null) {
      core.UnregisterHxUpdate(this);
    }
  }

  //! Reset to default values.
  void Reset() {
#if UNITY_EDITOR
    // Assign best guess defaults to new instances of the script
    handMeshes.leftFemaleHandPrefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHandLeftFemale.prefab", typeof(GameObject));
    handMeshes.rightFemaleHandPrefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHandRightFemale.prefab", typeof(GameObject));
    handMeshes.leftMaleHandPrefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHandLeftMale.prefab", typeof(GameObject));
    handMeshes.rightMaleHandPrefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHandRightMale.prefab", typeof(GameObject));

    handMaterials.lightFemaleHandMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand/Materials/LightFemaleHand.mat", typeof(Material));
    handMaterials.mediumFemaleHandMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand/Materials/MediumFemaleHand.mat", typeof(Material));
    handMaterials.darkFemaleHandMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand/Materials/DarkFemaleHand.mat", typeof(Material));
    handMaterials.neutralFemaleHandMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand/Materials/NeutralHand.mat", typeof(Material));
    handMaterials.lightMaleHandMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand/Materials/LightMaleHand.mat", typeof(Material));
    handMaterials.mediumMaleHandMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand/Materials/MediumMaleHand.mat", typeof(Material));
    handMaterials.darkMaleHandMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand/Materials/DarkMaleHand.mat", typeof(Material));
    handMaterials.neutralMaleHandMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
        "Assets/Haptx/HaptxHand/Materials/NeutralHand.mat", typeof(Material));
#endif

    if (linearDrive == null) {
      linearDrive = new ConfigurableJointParameters.SerializedJointDrive();
    }
    linearDrive.positionSpring = 1.1e5f;
    linearDrive.positionDamper = 1.1e3f;
    linearDrive.maximumForce = 1.1e2f;

    if (angularDrive == null) {
      angularDrive = new ConfigurableJointParameters.SerializedJointDrive();
    }
    angularDrive.positionSpring = 6.3e7f;
    angularDrive.positionDamper = 6.3e5f;
    angularDrive.maximumForce = 1.1e2f;
  }

  //! Configure the joint driving the hand in the scene.
  void InitPalmJoint() {
    if (!enabled) {
      return;
    }

    // Create and configure the palm joint.
    GameObject palmGameObject = boneDataFromBoneName[boneNames.palm].gameObject;
    Vector3 pos_cache = palmGameObject.transform.position;
    Quaternion rot_cache = palmGameObject.transform.rotation;
    palmGameObject.transform.position = new Vector3(0, 0, 0);
    palmGameObject.transform.rotation = Quaternion.identity;

    palmJoint = palmGameObject.AddComponent<ConfigurableJoint>();
    palmJoint.autoConfigureConnectedAnchor = false;
    palmJoint.xDrive = linearDrive.Unwrap();
    palmJoint.yDrive = linearDrive.Unwrap();
    palmJoint.zDrive = linearDrive.Unwrap();
    palmJoint.rotationDriveMode = RotationDriveMode.Slerp;
    palmJoint.slerpDrive = angularDrive.Unwrap();
    palmJoint.configuredInWorldSpace = true;
    palmJoint.swapBodies = true;

    // We want the origin of the palm to be the location of middle1 instead of the location of the
    // palm.
    GameObject middle1GameObject = boneDataFromBoneName[boneNames.middle1].gameObject;
    palmJoint.connectedAnchor = Quaternion.Inverse(palmGameObject.transform.rotation) *
        (middle1GameObject.transform.position - palmGameObject.transform.position);

    palmGameObject.transform.position = pos_cache;
    palmGameObject.transform.rotation = rot_cache;
  }

  //! Spawn and configure the physics authority zone of this hand.
  void InitPhysicsAuthorityZone() {
    GameObject physAuthZoneObject = new GameObject("PhysicsAuthorityZone");
    physAuthZoneObject.layer = _physicsAuthorityZoneLayer.value;
    physAuthZoneObject.transform.position =
        boneDataFromBoneName[boneNames.middle1].gameObject.transform.position;
    physAuthZoneObject.transform.rotation =
        boneDataFromBoneName[boneNames.middle1].gameObject.transform.rotation;
    physAuthZoneObject.transform.parent =
        boneDataFromBoneName[boneNames.palm].gameObject.transform;
    _physicsAuthorityZone = physAuthZoneObject.AddComponent<SphereCollider>();
    _physicsAuthorityZone.radius = _physicsAuthorityZoneRadiusM;
    _physicsAuthorityZone.isTrigger = true;
  }

  //! Attempt to figure out the hand's location and rotation from SteamVR.
  //!
  //! @param [out] wMcp3 The world transform of MCP3.
  //! @param [out] wVive The world transform of the tracked VIVE object.
  //! @returns Whether the hand's location and rotation were successfully populated.
  bool TryGetHandLocationAndRotation(ref Matrix4x4 wMcp3, ref Matrix4x4 wVive) {
    if (core == null || _glove == null) {
      return false;
    }

    if (!HaptxApi.OpenvrWrapper.isReady()) {
      _lastOpenvrWrapperReturnCode = HaptxApi.OpenvrWrapper.ReturnCode.NOT_INITIALIZED;
      return false;
    }

    bool success = false;
    HaptxApi.Transform ovr = HxReusableObjectPool<HaptxApi.Transform>.Get();
    if (!_glove.is_simulated) {
      if (Peripheral.vive_trackers.Count < 1) {
        return false;
      }
      _lastOpenvrWrapperReturnCode = HaptxApi.OpenvrWrapper.getTrackerTransform(
          Peripheral.vive_trackers[0].serial, ref ovr);
      success = _lastOpenvrWrapperReturnCode == HaptxApi.OpenvrWrapper.ReturnCode.SUCCESS;
      if (success) {
        wMcp3 = HxShared.UnityFromHx(ovr.operator_mult(
            Peripheral.vive_trackers[0].transform.inverse()));
      }
    } else {
      _lastOpenvrWrapperReturnCode = HaptxApi.OpenvrWrapper.getControllerTransform(hand.ToHx(), ref ovr);
      success = _lastOpenvrWrapperReturnCode == HaptxApi.OpenvrWrapper.ReturnCode.SUCCESS;
      if (success) {
        wMcp3 = HxShared.UnityFromHx(ovr);
      }
    }

    if (success) {
      wVive = HxShared.UnityFromHx(ovr);
      if (mocapOriginObject != null) {
        wMcp3 = mocapOriginObject.transform.localToWorldMatrix * wMcp3;
        wVive = mocapOriginObject.transform.localToWorldMatrix * wVive;
      }
    }
    HxReusableObjectPool<HaptxApi.Transform>.Release(ovr);
    return success;
  }

  //! Checks to see if there's another HxHand in the scene right now with the same #hand.
  //!
  //! @returns True if there is at least one duplicate.
  bool CheckForDuplicateHands() {
    UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(HxHand));

    foreach (UnityEngine.Object obj in objects) {
      HxHand hh = obj as HxHand;
      if (hh != null && hh != this && hh.player == player && hh.hand == hand && hh.enabled) {
        return true;
      }
    }

    return false;
  }

  //! Warn the user that the tracking reference is off for this hand as long as we haven't
  //! done it too recently.
  void WarnAboutTrackingRefOff() {
    // If we haven't recently warned about the same thing
    if (!recentlyWarnedAboutTrackingRefBeingOff) {
      string error_code_string = HaptxApi.OpenvrWrapper.toString(_lastOpenvrWrapperReturnCode);
      string device_string = _glove.is_simulated ? "controller" : "Tracker";

      trackingRefOffDebugMessageKey = HxDebug.LogWarning(
          $"VIVE {device_string} is not tracking for the {HandAsText()} hand: " + 
          $"error code {error_code_string}.", context: this, addToScreen: true);

      recentlyWarnedAboutTrackingRefBeingOff = true;
    }
  }

  //! Build the data structures for making quick lookups.
  void BuildDataStructures() {
    // Find all objects matching names in the names struct.
    boneDataFromBoneName.Clear();
    boneDataFromInstanceId.Clear();
    foreach (FieldInfo field in typeof(HandBoneNames).GetFields()) {
      // Extract the game object name from the field.
      HxName boneName = (HxName)field.GetValue(boneNames);
      boneDataFromBoneName.Add(boneName, new HxHandBoneData());

      // Try to find a game object matching the name.
      Transform boneTransform = HxShared.GetChildGameObjectByName(transform, boneName.String);
      if (boneTransform != null) {
        boneDataFromBoneName[boneName].gameObject = boneTransform.gameObject;
        boneDataFromInstanceId[boneTransform.gameObject.GetInstanceID()] =
            boneDataFromBoneName[boneName];
      } else {
        HxDebug.LogError(string.Format(
            "Failed to find a child game object with name {0} on the {1} hand.", boneName,
            HandAsText()), this);
        HardDisable();
        return;
      }
    }

    // Fill joints with the joints on each game object. The indices of the game objects in this
    // array match the corresponding values in the HandJoints enum.
    joints = new ConfigurableJoint[(int)HaptxApi.Finger.F_LAST,
        (int)HaptxApi.FingerJoint.FJ_LAST];
    jointRotationOffsets = new Quaternion[(int)HaptxApi.Finger.F_LAST,
        (int)HaptxApi.FingerJoint.FJ_LAST];
    HxName[,] nameFromHandJoint = GetNameFromHandJoint();
    for (int f_i = 0; f_i < (int)HaptxApi.Finger.F_LAST; f_i++) {
      for (int fj_i = 0; fj_i < (int)HaptxApi.FingerJoint.FJ_LAST; fj_i++) {
        GameObject jointObject = boneDataFromBoneName[nameFromHandJoint[f_i, fj_i]].gameObject;
        jointRotationOffsets[f_i, fj_i] = jointObject.transform.localRotation;
        ConfigurableJoint configurableJoint = jointObject.GetComponent<ConfigurableJoint>();
        if (configurableJoint != null) {
          joints[f_i, fj_i] = configurableJoint;
        } else {
          HxDebug.LogWarning(string.Format(
              "Game object {0} on the {1} hand does not contain a ConfigurableJoint.",
              jointObject.name, HandAsText()), this);
          HardDisable();
          return;
        }
      }
    }
  }

  //! @brief Build the data structures for making quick lookups.
  //!
  //! Depends on ConnectToModules().
  void RegisterBones() {
    if (!ConnectToCore() || core == null) {
      return;
    }

    // Contact interpreter body registration.
    // palm
    boneDataFromBoneName[boneNames.palm].ciBodyId =
        boneDataFromBoneName[boneNames.palm].gameObject.GetInstanceID();
    boneDataFromBoneName[boneNames.palm].rigidBodyPart = hand == RelDir.LEFT ?
        HaptxApi.RigidBodyPart.LEFT_PALM : HaptxApi.RigidBodyPart.RIGHT_PALM;

    // Proximal finger segments are treated as the same body as the palm.
    // thumb 1
    boneDataFromBoneName[boneNames.thumb1].ciBodyId =
        boneDataFromBoneName[boneNames.palm].ciBodyId;
    boneDataFromBoneName[boneNames.thumb1].rigidBodyPart = hand == RelDir.LEFT ?
        HaptxApi.RigidBodyPart.LEFT_PALM : HaptxApi.RigidBodyPart.RIGHT_PALM;

    // index1
    boneDataFromBoneName[boneNames.index1].ciBodyId =
        boneDataFromBoneName[boneNames.palm].ciBodyId;
    boneDataFromBoneName[boneNames.index1].rigidBodyPart = hand == RelDir.LEFT ?
        HaptxApi.RigidBodyPart.LEFT_PALM : HaptxApi.RigidBodyPart.RIGHT_PALM;

    // middle1
    boneDataFromBoneName[boneNames.middle1].ciBodyId =
        boneDataFromBoneName[boneNames.palm].ciBodyId;
    boneDataFromBoneName[boneNames.middle1].rigidBodyPart = hand == RelDir.LEFT ?
        HaptxApi.RigidBodyPart.LEFT_PALM : HaptxApi.RigidBodyPart.RIGHT_PALM;

    // ring1
    boneDataFromBoneName[boneNames.ring1].ciBodyId =
        boneDataFromBoneName[boneNames.palm].ciBodyId;
    boneDataFromBoneName[boneNames.ring1].rigidBodyPart = hand == RelDir.LEFT ?
        HaptxApi.RigidBodyPart.LEFT_PALM : HaptxApi.RigidBodyPart.RIGHT_PALM;

    //pinky1
    boneDataFromBoneName[boneNames.pinky1].ciBodyId =
        boneDataFromBoneName[boneNames.palm].ciBodyId;
    boneDataFromBoneName[boneNames.pinky1].rigidBodyPart = hand == RelDir.LEFT ?
        HaptxApi.RigidBodyPart.LEFT_PALM : HaptxApi.RigidBodyPart.RIGHT_PALM;

    // Medial and distal segments are treated as the same body
    // thumb2 & thumb3
    boneDataFromBoneName[boneNames.thumb2].ciBodyId =
        boneDataFromBoneName[boneNames.thumb2].gameObject.GetInstanceID();
    boneDataFromBoneName[boneNames.thumb2].rigidBodyPart = HaptxApiSwig.getRigidBodyPart(
        hand.ToHx(), HaptxApi.Finger.F_THUMB, HaptxApi.FingerBone.FB_MEDIAL);
    boneDataFromBoneName[boneNames.thumb3].ciBodyId =
        boneDataFromBoneName[boneNames.thumb2].ciBodyId;
    boneDataFromBoneName[boneNames.thumb3].rigidBodyPart =
        boneDataFromBoneName[boneNames.thumb2].rigidBodyPart;

    // index2 & index3
    boneDataFromBoneName[boneNames.index2].ciBodyId =
        boneDataFromBoneName[boneNames.index2].gameObject.GetInstanceID();
    boneDataFromBoneName[boneNames.index2].rigidBodyPart = HaptxApiSwig.getRigidBodyPart(
        hand.ToHx(), HaptxApi.Finger.F_INDEX, HaptxApi.FingerBone.FB_MEDIAL);
    boneDataFromBoneName[boneNames.index3].ciBodyId =
        boneDataFromBoneName[boneNames.index2].ciBodyId;
    boneDataFromBoneName[boneNames.index3].rigidBodyPart =
        boneDataFromBoneName[boneNames.index2].rigidBodyPart;

    // middle2 & middle3
    boneDataFromBoneName[boneNames.middle2].ciBodyId =
        boneDataFromBoneName[boneNames.middle2].gameObject.GetInstanceID();
    boneDataFromBoneName[boneNames.middle2].rigidBodyPart = HaptxApiSwig.getRigidBodyPart(
        hand.ToHx(), HaptxApi.Finger.F_MIDDLE, HaptxApi.FingerBone.FB_MEDIAL);
    boneDataFromBoneName[boneNames.middle3].ciBodyId =
        boneDataFromBoneName[boneNames.middle2].ciBodyId;
    boneDataFromBoneName[boneNames.middle3].rigidBodyPart =
        boneDataFromBoneName[boneNames.middle2].rigidBodyPart;

    // ring2 & ring3
    boneDataFromBoneName[boneNames.ring2].ciBodyId =
        boneDataFromBoneName[boneNames.ring2].gameObject.GetInstanceID();
    boneDataFromBoneName[boneNames.ring2].rigidBodyPart = HaptxApiSwig.getRigidBodyPart(
        hand.ToHx(), HaptxApi.Finger.F_RING, HaptxApi.FingerBone.FB_MEDIAL);
    boneDataFromBoneName[boneNames.ring3].ciBodyId =
        boneDataFromBoneName[boneNames.ring2].ciBodyId;
    boneDataFromBoneName[boneNames.ring3].rigidBodyPart =
        boneDataFromBoneName[boneNames.ring2].rigidBodyPart;

    // pinky2 & pinky3
    boneDataFromBoneName[boneNames.pinky2].ciBodyId =
        boneDataFromBoneName[boneNames.pinky2].gameObject.GetInstanceID();
    boneDataFromBoneName[boneNames.pinky2].rigidBodyPart = HaptxApiSwig.getRigidBodyPart(
        hand.ToHx(), HaptxApi.Finger.F_PINKY, HaptxApi.FingerBone.FB_MEDIAL);
    boneDataFromBoneName[boneNames.pinky3].ciBodyId =
        boneDataFromBoneName[boneNames.pinky2].ciBodyId;
    boneDataFromBoneName[boneNames.pinky3].rigidBodyPart =
        boneDataFromBoneName[boneNames.pinky2].rigidBodyPart;

    foreach (var keyValue in boneDataFromBoneName) {
      Rigidbody rigidbody = keyValue.Value.gameObject.GetComponent<Rigidbody>();
      if (rigidbody == null) {
        continue;
      }
      core.RegisterCiBody(keyValue.Value.ciBodyId, rigidbody, keyValue.Key == boneNames.palm ?
          palmBodyParameters : fingerBodyParameters, keyValue.Value.rigidBodyPart);
      keyValue.Value.hasCiBodyId = true;
    }

    // Define grasp detector interface body IDs.
    HaptxApi.GraspDetector gd = core.GraspDetector;
    wholeHandGdBodyId = gd.registerBody();
    boneDataFromBoneName[boneNames.palm].gdBodyId = gd.registerBody(wholeHandGdBodyId);
    // Thumb1 and the palm are treated as the same body to prevent objects from getting stuck
    // between them.
    boneDataFromBoneName[boneNames.thumb1].gdBodyId =
        boneDataFromBoneName[boneNames.palm].gdBodyId;
    boneDataFromBoneName[boneNames.thumb2].gdBodyId = gd.registerBody(wholeHandGdBodyId);
    boneDataFromBoneName[boneNames.thumb3].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.thumb2].gdBodyId);
    boneDataFromBoneName[boneNames.index1].gdBodyId = gd.registerBody(wholeHandGdBodyId);
    boneDataFromBoneName[boneNames.index2].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.index1].gdBodyId);
    boneDataFromBoneName[boneNames.index3].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.index2].gdBodyId);
    boneDataFromBoneName[boneNames.middle1].gdBodyId = gd.registerBody(wholeHandGdBodyId);
    boneDataFromBoneName[boneNames.middle2].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.middle1].gdBodyId);
    boneDataFromBoneName[boneNames.middle3].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.middle2].gdBodyId);
    boneDataFromBoneName[boneNames.ring1].gdBodyId = gd.registerBody(wholeHandGdBodyId);
    boneDataFromBoneName[boneNames.ring2].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.ring1].gdBodyId);
    boneDataFromBoneName[boneNames.ring3].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.ring2].gdBodyId);
    boneDataFromBoneName[boneNames.pinky1].gdBodyId = gd.registerBody(wholeHandGdBodyId);
    boneDataFromBoneName[boneNames.pinky2].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.pinky1].gdBodyId);
    boneDataFromBoneName[boneNames.pinky3].gdBodyId =
        gd.registerBody(boneDataFromBoneName[boneNames.pinky2].gdBodyId);
    core.RegisterGdBody(wholeHandGdBodyId,
        boneDataFromBoneName[boneNames.palm].gameObject.GetComponent<Rigidbody>(), true);
    foreach (var keyValue in boneDataFromBoneName) {
      Rigidbody rigidbody = keyValue.Value.gameObject.GetComponent<Rigidbody>();
      if (rigidbody == null) {
        continue;
      }
      core.RegisterGdBody(keyValue.Value.gdBodyId, rigidbody);
      keyValue.Value.hasGdBodyId = true;
    }

    boneDataFromBoneName[boneNames.palm].canEngageContactDamping = true;
    boneDataFromBoneName[boneNames.thumb1].canEngageContactDamping = true;
    boneDataFromBoneName[boneNames.index1].canEngageContactDamping = true;
    boneDataFromBoneName[boneNames.middle1].canEngageContactDamping = true;
    boneDataFromBoneName[boneNames.ring1].canEngageContactDamping = true;
    boneDataFromBoneName[boneNames.pinky1].canEngageContactDamping = true;
  }

  //! Register retractuators on this hand's peripheral with the HaptxApi::ContactInterpreter.
  void RegisterRetractuators() {
    foreach (HaptxApi.Retractuator retractuator in Peripheral.retractuators) {
      if (retractuator.restrictions.empty()) {
        continue;
      }

      HaptxApi.Finger finger = HaptxApi.Finger.F_LAST;
      bool multi_finger_retractuator = false;
      foreach (var restriction in retractuator.restrictions) {
        if (finger == HaptxApi.Finger.F_LAST) {
          finger = HaptxApiSwig.getFinger(restriction.Key);
        } else if (finger != HaptxApiSwig.getFinger(restriction.Key)) {
          multi_finger_retractuator = true;
          break;
        }
      }
      if (multi_finger_retractuator) {
        HxDebug.LogWarning(
            "HxHand.RegisterRetractuators(): Multi-finger retractuator not supported.", this);
        continue;
      }

      core.ContactInterpreter.registerRetractuator(Peripheral.id, retractuator,
          retractuatorParameters.getParametersForFinger(finger).Unwrap());
    }
  }

  //! Updates the radius of traces and broadcasts the event to patches.
  void UpdatePatchSphereTraceRadius() {
    if(_patchTraceRadiusM == sphereTraceRadiusM) {
      return;
    }

    _patchTraceRadiusM = sphereTraceRadiusM;
    if (OnShpereTraceRadiusChange != null) {
      OnShpereTraceRadiusChange(this, _patchTraceRadiusM);
    }
  }

  //! Gets the simulated pose progression amount for this hand (likely the controller trigger).
  private float GetSimulatedPoseProgressAmount() {
    Valve.VR.SteamVR_Input_Sources steamVRHand = hand == RelDir.LEFT ?
      Valve.VR.SteamVR_Input_Sources.LeftHand :
      Valve.VR.SteamVR_Input_Sources.RightHand;
    return progressSimulatedPoseAction.GetAxis(steamVRHand);
  }

  //! Updates hand pose with mocap data from the HaptxApi::HandAnimationInterface.
  void UpdateHandAnimation() {
    if (palmJoint == null) {
      return;
    }

    // World space positioning of the hand.
    Matrix4x4 wMcp3Original = Matrix4x4.TRS(palmJoint.targetPosition, palmJoint.targetRotation,
        Vector3.one);
    Matrix4x4 wVive = Matrix4x4.identity;
    if (TryGetHandLocationAndRotation(ref wMcp3Original, ref wVive)) {
      if (recentlyWarnedAboutTrackingRefBeingOff) {
        recentlyWarnedAboutTrackingRefBeingOff = false;
        HxOnScreenLog.ClearFromScreen(trackingRefOffDebugMessageKey);
      }
    } else {
      WarnAboutTrackingRefOff();
    }

    // Select which profile to animate with based on optimization mode.
    HaptxApi.UserProfile animProfile = null;
    switch (handAnimOptimizationMode) {
      case HandAnimationOptimizationMode.DYNAMIC:
        animProfile = _avatarAnimOptimizedProfile;
        break;
      case HandAnimationOptimizationMode.JOINT_ANGLES:
        animProfile = _userProfile;
        break;
      case HandAnimationOptimizationMode.FINGERTIP_POSITIONS:
        animProfile = _avatarProfile;
        break;
      default:
        animProfile = _userProfile;
        break;
    }

    using (var mocapFrameOriginal = HxReusableObjectPool<HaptxApi.MocapFrame>.GetWrapped()) // Directly from hardware
    using (var mocapFrame = HxReusableObjectPool<HaptxApi.MocapFrame>.GetWrapped()) // Mocap adjusted for compensators
    using (var animFrame = HxReusableObjectPool<HaptxApi.AnimFrame>.GetWrapped()) { // Hand animation information
      mocapFrameOriginal.ReusableObject.clear();
      mocapFrame.ReusableObject.clear();
      animFrame.ReusableObject.clear();
      Matrix4x4 wMcp3 = wMcp3Original;  // MCP3 placement adjusted for compensators
      // Simulated animation of the fingertips.
      if (_glove.is_simulated) {
        if (_lastSimulatedAnimFrame == null) {
          _lastSimulatedAnimFrame = HaptxApi.SimulatedGestures.getAnimFrame(_gesture, 0.0f);
        }

        Valve.VR.SteamVR_Input_Sources steamVRHand = hand == RelDir.LEFT ?
          Valve.VR.SteamVR_Input_Sources.LeftHand :
          Valve.VR.SteamVR_Input_Sources.RightHand;
        if (nextSimulatedPoseAction.GetStateDown(steamVRHand)) {
          _gesture = (HaptxApi.Gesture)(((int)_gesture + 1) % (int)HaptxApi.Gesture.LAST);
        }
        if (previousSimulatedPoseAction.GetStateDown(steamVRHand)) {
          _gesture = (HaptxApi.Gesture)(((int)_gesture - 1 + (int)HaptxApi.Gesture.LAST) %
              (int)HaptxApi.Gesture.LAST);
        }

        float triggerValue = GetSimulatedPoseProgressAmount();
        HaptxApi.AnimFrame targetAnimFrame = HaptxApi.SimulatedGestures.getAnimFrame(_gesture, 
          triggerValue);
        _lastSimulatedAnimFrame = HaptxApi.AnimFrame.slerp(_lastSimulatedAnimFrame,
            targetAnimFrame, Time.deltaTime * simulatedAnimationAggressiveness_1_s);
        animFrame.ReusableObject.operator_assignment(_lastSimulatedAnimFrame);

        // Motion capture based animation of the fingertips.
      } else {
        if (_mocapSystem != null && _mocapSystem.isReady()) {
          HaptxApi.HyleasSystem.ReturnCode hsRet = _mocapSystem.update();
          if (hsRet != HaptxApi.HyleasSystem.ReturnCode.SUCCESS) {
            HxDebug.LogError(string.Format(
                "HxHand.UpdateHandAnimation(): Motion capture system failed to update with error code {0}: {1}.",
                (int)hsRet, HaptxApi.HyleasSystem.toString(hsRet)), this);
          }

          hsRet = _mocapSystem.addToMocapFrame(mocapFrameOriginal.ReusableObject);
          if (hsRet != HaptxApi.HyleasSystem.ReturnCode.SUCCESS) {
            HxDebug.LogError(string.Format(
                "HxHand.UpdateHandAnimation(): Motion capture system failed to add to mocap frame with error code {0}: {1}.",
                (int)hsRet, HaptxApi.HyleasSystem.toString(hsRet)), this);
          }

          mocapFrame.ReusableObject.operator_assignment(mocapFrameOriginal.ReusableObject);
        }

        if (enableGloveSlipCompensation && _gloveSlipCompensator != null &&
            _gloveSlipCompensator.applyToMocapFrame(mocapFrame.ReusableObject, Time.deltaTime,
            gloveSlipCompensationParameters.aggressiveness_1_s,
            gloveSlipCompensationParameters.onThreshold)) {
          wMcp3 = Matrix4x4.TRS(wMcp3.MultiplyVector(HxShared.UnityFromHx(
              _gloveSlipCompensator.getMcp3SlipOffsetM())), Quaternion.identity,
              Vector3.one) * wMcp3;
        }

        if (enableThimbleCompensation) {
          HaptxApi.ThimbleCompensator.applyToMocapFrame(mocapFrame.ReusableObject,
              thimbleCompensationParameters.correctionDistThresholdM,
              thimbleCompensationParameters.maxCorrectionDistM,
              thimbleCompensationParameters.maxCorrectionAmountM);
        }

        if (handAnimOptimizationMode == HandAnimationOptimizationMode.DYNAMIC &&
            !HaptxApi.AvatarAnimationOptimizer.optimize(hand.ToHx(), _userProfile, _avatarProfile,
            mocapFrame.ReusableObject, ref _avatarAnimOptimizedProfile)) {
          HxDebug.LogError("HxHand.updateHandAnimation(): Failed to optimize hand animation.", this);
          animProfile = _userProfile;
        }

        HaptxApi.DefaultHandIk.addToAnimFrame(_glove, mocapFrame.ReusableObject, _userProfile, animProfile, animFrame.ReusableObject);
      }

      if (motionCaptureVisualizer.visualize) {
        // Mocap visualizer shows unadjusted values.
        VisualizeMocapData(mocapFrameOriginal.ReusableObject, wMcp3Original, wVive);
      }

      if (handAnimationVisualizer.visualize) {
        VisualizeHandAnimation(animFrame.ReusableObject, animProfile, wMcp3);
      }

      // Generate physics targets
      Quaternion[] lJointOrients =
          new Quaternion[(int)HaptxApi.Finger.F_LAST * (int)HaptxApi.FingerJoint.FJ_LAST];
      for (int f_i = 0; f_i < (int)HaptxApi.Finger.F_LAST; f_i++) {
        for (int fj_i = 0; fj_i < (int)HaptxApi.FingerJoint.FJ_LAST; fj_i++) {
          HaptxApi.BodyPartJoint joint = HaptxApiSwig.getBodyPartJoint(hand.ToHx(),
              (HaptxApi.Finger)f_i, (HaptxApi.FingerJoint)fj_i);

          int flatIndex = (int)HaptxApi.FingerJoint.FJ_LAST * f_i + fj_i;
          if (animFrame.ReusableObject.l_orientations.ContainsKey(joint)) {
            lJointOrients[flatIndex] = HxShared.UnityFromHx(animFrame.ReusableObject.l_orientations[joint]);
          } else {
            lJointOrients[flatIndex] = Quaternion.identity;
          }
        }
      }
      // This is subtle, but we know an error can exist between the hardware idealized location of
      // MCP3 (which almost everything is positioned relative to), and the user's actual MCP3 location
      // (which is where we would ideally like to position the avatar hand). By correcting for that
      // error at the last minute we can position the hand correctly without messing up all our motion
      // tracking offsets.
      HaptxApi.Vector3D lMcp3User = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
      _userProfile.getJoint1PosOffsetsValueM(hand.ToHx(), HaptxApi.Finger.F_MIDDLE, ref lMcp3User);
      Matrix4x4 wMcp3User = wMcp3 * Matrix4x4.TRS(HxShared.UnityFromHx(lMcp3User),
          Quaternion.identity, Vector3.one);
      HandPhysicsTargets targets = new HandPhysicsTargets() {
        wMiddle1PosM = wMcp3User.MultiplyPoint3x4(Vector3.zero),
        wMiddle1Orient = wMcp3User.rotation,
        lJointOrients = lJointOrients};

      if (!IsPhysicsAuthority) {
        float timeS = (float)NetworkTime.time;
        float periodS = 1.0f / physicsTargetsTransmissionFrequencyHz;
        // Insert a half period of offset for left hands to stagger messages.
        if (timeS - _timeOfLastPhysicsTransmissionS -
            (hand == RelDir.LEFT ? 0.5f * periodS : 0.0f) > periodS) {
          CmdUpdatePhysicsTargets(timeS, targets);
          _timeOfLastPhysicsTransmissionS = timeS;
        }
      } else {
        UpdatePhysicsTargets(targets);
      }

      float tpDistThreshold = correctiveTeleportationDistance * Mathf.Max(handScaleFactor, 1.0f);
      float positionError = (boneDataFromBoneName[boneNames.middle1].gameObject.transform.position -
          targets.wMiddle1PosM).magnitude;

      // Teleport the palm (if needed). Always happens in the first frame of good tracking.
      if (palmNeedsFirstTeleport ||
          (enableCorrectiveTeleportation && tpDistThreshold < positionError)) {
        TeleportMiddle1(targets.wMiddle1PosM, targets.wMiddle1Orient);
        ZeroAllVelocities();
        palmNeedsFirstTeleport = false;
      }
    }
  }

  //! @brief Does the per-frame work necessary to manage contact damping joints.
  //!
  //! Execute in FixedUpdate().
  void ManageContactDamping() {
    List<int> objectIdsToRemove = new List<int>();
    foreach (var keyValue in contactDampingDataFromObjectId) {
      // Alias for convenience.
      int objectId = keyValue.Key;

      // Check if this joint is outdated.
      if (!contactingObjectIds.Contains(objectId)) {
        objectIdsToRemove.Add(objectId);
        continue;
      }
    }

    // Remove any outdated joints.
    foreach (int objectIdToRemove in objectIdsToRemove) {
      ContactDampingObjectData data;
      if (contactDampingDataFromObjectId.TryGetValue(objectIdToRemove, out data) && data != null) {
        if (data.joint != null) {
          data.joint.connectedBody = null;
          Destroy(data.joint);
        }
        contactDampingDataFromObjectId.Remove(objectIdToRemove);
      }
    }

    // Clear the list of contacting objects, which will get populated again after the next physics
    // tick.
    contactingObjectIds.Clear();
  }

  //! Teleports the root GameObject of the hand (the palm) to a given world location and rotation.
  //!
  //! @param newPosition The new palm location.
  //! @param newRotation The new palm rotation.
  public void TeleportPalm(Vector3 newPosition, Quaternion newRotation) {
    if (IsPhysicsAuthority) {
      TeleportHand(newPosition, newRotation);
    } else {
      CmdTeleportHand(newPosition, newRotation);
    }
  }

  //! Teleports the tracked GameObject of the hand (middle1) to a given world location and
  //! rotation.
  //!
  //! @param newPosition The new middle1 location.
  //! @param newRotation The new middle1 rotation.
  public void TeleportMiddle1(Vector3 newPosition, Quaternion newRotation) {
    if (!boneDataFromBoneName.ContainsKey(boneNames.palm) ||
        boneDataFromBoneName[boneNames.palm].gameObject == null) {
      HxDebug.LogError("Failed to teleport middle1. Couldn't find palm GameObject.", this);
      return;
    }
    if (!boneDataFromBoneName.ContainsKey(boneNames.middle1) ||
        boneDataFromBoneName[boneNames.middle1].gameObject == null) {
      HxDebug.LogError("Failed to teleport middle1. Couldn't find middle1 GameObject.", this);
      return;
    }

    Matrix4x4 wPalm = Matrix4x4.TRS(newPosition, newRotation, Vector3.one)
        * boneDataFromBoneName[boneNames.middle1].gameObject.transform.worldToLocalMatrix
        * boneDataFromBoneName[boneNames.palm].gameObject.transform.localToWorldMatrix;
    Vector3 wPositionM = wPalm.MultiplyPoint3x4(Vector3.zero);
    Quaternion wOrient = wPalm.rotation;
    if (IsPhysicsAuthority) {
      TeleportHand(wPositionM, wOrient);
    } else {
      CmdTeleportHand(wPositionM, wOrient);
    }
  }

  //! Teleports the authoritative hand to a new world position and orientation.
  //!
  //! @param wPositionM The new world position.
  //! @param wOrient The new world orientation.
  [Command]
  private void CmdTeleportHand(Vector3 wPositionM, Quaternion wOrient) {
    if (IsPhysicsAuthority) {
      TeleportHand(wPositionM, wOrient);
    }
  }

  //! Teleports the hand to a new world position and orientation.
  //!
  //! @param wPositionM The new world position.
  //! @param wOrient The new world orientation.
  private void TeleportHand(Vector3 wPositionM, Quaternion wOrient) {
    if (!boneDataFromBoneName.ContainsKey(boneNames.palm) ||
        boneDataFromBoneName[boneNames.palm].gameObject == null) {
      HxDebug.LogError("Failed to teleport palm. Couldn't find palm GameObject.", this);
      return;
    }

    boneDataFromBoneName[boneNames.palm].gameObject.transform.position = wPositionM;
    boneDataFromBoneName[boneNames.palm].gameObject.transform.rotation = wOrient;
  }

  //! Receive a relayed collision from an HxHandSegment.
  //!
  //! @param collision Information about the collision.
  //! @param handSegment The relaying hand segment.
  public void ReceiveOnCollisionStay(Collision collision, HxHandSegment handSegment) {
    HxHandBoneData boneData;
    if (!enabled || collision == null || handSegment == null || core == null ||
        !boneDataFromInstanceId.TryGetValue(handSegment.gameObject.GetInstanceID(),
        out boneData)) {
      return;
    }

    // Ignore collisions with our own player.
    HxHand otherHand = collision.collider.GetComponentInParent<HxHand>();
    if (otherHand != null && player == otherHand.player) {
      return;
    }

    // Calculate the contact location and normal as the average of contacts in the collision.
    Vector3 contactLocation = new Vector3();
    Vector3 contactNormal = new Vector3();
    float contactSeparation = 0.0f;
    foreach (ContactPoint contactPoint in collision.contacts) {
      contactLocation += contactPoint.point;
      contactNormal += contactPoint.normal;
      contactSeparation += contactPoint.separation;
    }
    contactLocation /= collision.contacts.Length;
    contactNormal.Normalize();
    contactSeparation /= collision.contacts.Length;

    // Use average contact normal to detect when Unity is handing us an impulse that's backwards
    HaptxApi.Vector3D impulse = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(Vector3.Dot(contactNormal, collision.impulse) < 0 ? -collision.impulse :
        collision.impulse, impulse);
    if (hasAuthority && collision.impulse.sqrMagnitude > 0.0f) {
      // Register contact with CI.
      long ciObjectId = 0;
      if (boneData == null || !boneData.hasCiBodyId) {
        HxDebug.LogError(string.Format(
            "HxHand::ReceiveOnCollisionStay(): Bone not registered with CI {0}.",
            handSegment.gameObject.name), this);
      } else if (!core.TryRegisterCollider(collision.collider, false, out ciObjectId)) {
        HxDebug.LogError(string.Format(
            "HxHand::ReceiveOnCollisionStay(): Object not registered with CI {0}.",
            collision.gameObject.name), this);
      } else {
        core.ContactInterpreter.addContact(ciObjectId, boneData.ciBodyId, impulse);
      }
    }

    // Register contact with GD.
    if (collision.rigidbody != null) {
      if (collision.impulse.sqrMagnitude > 0.0f) {
        long gdObjectId;
        if (boneData == null || !boneData.hasGdBodyId) {
          HxDebug.LogError(string.Format(
              "HxHand::ReceiveOnCollisionStay(): Bone not registered with GD {0}.",
              handSegment.gameObject.name), this);
        } else if (!core.TryRegisterRigidbody(collision.rigidbody, false, out gdObjectId)) {
          HxDebug.LogError(string.Format(
              "HxHand::ReceiveOnCollisionStay(): Object not registered with GD {0}.",
              collision.rigidbody.gameObject.name), this);
        } else {
          HaptxApi.Vector3D hxContactLoaction = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
          HxShared.HxFromUnity(contactLocation, hxContactLoaction);
          HaptxApi.GraspDetector.GraspContactInfo gci =
              HxReusableObjectPool<HaptxApi.GraspDetector.GraspContactInfo>.Get();
          gci.object_id = gdObjectId;
          gci.grasp_body_id = boneData.gdBodyId;
          gci.contact_location = hxContactLoaction;
          gci.impulse = impulse;
          core.GraspDetector.addGraspContact(gci);
          HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxContactLoaction);
          HxReusableObjectPool<HaptxApi.GraspDetector.GraspContactInfo>.Release(gci);
        }
      }

      // Damp the motion of objects in the palm to help with holding.
      int otherId = collision.rigidbody.GetInstanceID();
      if (!collision.rigidbody.isKinematic &&
          collision.rigidbody.useGravity &&
          boneData.canEngageContactDamping &&
          !contactingObjectIds.Contains(otherId)) {
        HxRigidbodyProperties hxRigidbodyProperties =
            collision.rigidbody.GetComponent<HxRigidbodyProperties>();
        bool contactDampingEnabled = (hxRigidbodyProperties != null &&
            hxRigidbodyProperties.overrideContactDampingEnabled) ?
            hxRigidbodyProperties.contactDampingEnabled : enableContactDamping;
        float maxSeparation = (hxRigidbodyProperties != null &&
            hxRigidbodyProperties.overrideMaxContactDampingSeparation) ?
            hxRigidbodyProperties.maxContactDampingSeparation : maxContactDampingSeparation;
        if (contactDampingEnabled && contactSeparation < maxSeparation) {
          // The color to draw the contact damping trace if it hits the hand.
          Color HandHitColor = HxShared.DebugPurpleOrTeal;
          // The color to draw the contact damping trace if it misses the hand.
          Color HandMissedColor = HxShared.DebugRedOrGreen;
          // How thick to draw the contact damping trace.
          const float LTraceThicknessM = 0.0007f;
          // The radius of the sphere to draw at the contact damping trace hit location
          // (assuming it hits).
          const float HitRadiusM = 0.002f;

          // Only allow contact damping if the hand is "below" the object in the context of
          // gravity.
          Vector3 wTraceStartM = collision.rigidbody.worldCenterOfMass;
          Vector3 wTraceDir = Physics.gravity.normalized;
          Rigidbody palmRigidBody =
              boneDataFromBoneName[boneNames.palm].gameObject.GetComponent<Rigidbody>();
          float wTraceDistanceM = Mathf.Abs(Vector3.Dot(collision.rigidbody.worldCenterOfMass -
              palmRigidBody.worldCenterOfMass, wTraceDir)) + palmExtent;

          RaycastHit handHitData;
          bool handHit = Physics.Raycast(collision.rigidbody.worldCenterOfMass, wTraceDir,
              out handHitData, wTraceDistanceM, 1 << palmRigidBody.gameObject.layer);
          HxHandBoneData handHitBoneData = null;
          if (handHit && boneDataFromInstanceId.TryGetValue(
              handHitData.rigidbody.gameObject.GetInstanceID(), out handHitBoneData) &&
              handHitBoneData.canEngageContactDamping) {
            if (!contactDampingDataFromObjectId.ContainsKey(otherId)) {
              collision.rigidbody.velocity = Vector3.zero;
              ContactDampingObjectData data = InitiateContactDamping(collision.rigidbody);
              contactDampingDataFromObjectId.Add(otherId, data);
            }
            contactingObjectIds.Add(otherId);

            if (contactDampingVisualizer.visualize) {
              HxDebugMesh.DrawSphere(wTraceStartM + (handHitData.distance * wTraceDir),
                  Quaternion.identity, transform.lossyScale * HitRadiusM * 2, HandHitColor);
            }
          }

          if (contactDampingVisualizer.visualize) {
            HxDebugMesh.DrawLine(
                wTraceStartM,
                wTraceStartM + wTraceDistanceM * wTraceDir,
                transform.lossyScale.magnitude * LTraceThicknessM,
                handHit ? HandHitColor : HandMissedColor);
          }
        }
      }
    }

    HxReusableObjectPool<HaptxApi.Vector3D>.Release(impulse);
  }

  //! Receive a relayed trigger entry from a HxHandSegment.
  //!
  //! @param collider The collider being overlapped with.
  public void ReceiveOnTriggerEnter(Collider collider) {
    if (collider == null) {
      return;
    }

    // If it's another hand, increment our tracker and return early.
    HxHand otherHand = collider.GetComponentInParent<HxHand>();
    if (otherHand != null) {
      if (otherHand.player != player) {
        _numPhysicsAuthorityZoneOverlaps++;
        if (_numPhysicsAuthorityZoneOverlaps == 1) {
          _physicsAuthorityZone.radius =
              (1.0f + _physicsAuthorityZoneRadiusHysteresis) * _physicsAuthorityZoneRadiusM;
        }
      }
      return;
    }

    // If it's a networked object we can sync it.
    NetworkIdentity objectIdentity = collider.GetComponentInParent<NetworkIdentity>();
    if (objectIdentity == null) {
      return;
    }

    // Increment our internal overlap tracking map.
    if (_objectsInPhysicsAuthorityZone.ContainsKey(objectIdentity)) {
      _objectsInPhysicsAuthorityZone[objectIdentity]++;
    } else {
      _objectsInPhysicsAuthorityZone[objectIdentity] = 1;
    }

    // Increment the global overlap tracking map.
    GlobalPhysicsAuthorityObjectData globalData = null;
    if (globalPhysicsAuthorityDataFromNetIdentity.ContainsKey(objectIdentity)) {
      globalData = globalPhysicsAuthorityDataFromNetIdentity[objectIdentity];
    } else {
      globalData = new GlobalPhysicsAuthorityObjectData();
      globalPhysicsAuthorityDataFromNetIdentity[objectIdentity] = globalData;
    }
    if (globalData.physicsAuthorityZoneCountFromPlayer.ContainsKey(player)) {
      globalData.physicsAuthorityZoneCountFromPlayer[player]++;
    } else {
      globalData.physicsAuthorityZoneCountFromPlayer[player] = 1;
    }

    // While an object is inside the hand's physics authority zone we disable its rigidbody sync
    // feature since its state will be synced precisely along with the hand's state.
    if (globalData.physicsAuthorityZoneCountFromPlayer.Count == 1 &&
        globalData.physicsAuthorityZoneCountFromPlayer[player] == 1) {
      HxNetworkRigidbodyBase[] networkRigidbodies =
          objectIdentity.gameObject.GetComponentsInChildren<HxNetworkRigidbodyBase>();
      foreach (HxNetworkRigidbodyBase networkRigidbody in networkRigidbodies) {
        networkRigidbody.PauseSync = true;
      }
    }
  }

  //! Receive a relayed trigger exit from a HxHandSegment.
  //!
  //! @param collider The collider no longer being overlapped with.
  public void ReceiveOnTriggerExit(Collider collider) {
    if (collider == null) {
      return;
    }

    HxHand otherHand = collider.GetComponentInParent<HxHand>();
    if (otherHand != null) {
      if (otherHand.player != player) {
        _numPhysicsAuthorityZoneOverlaps--;
        if (_numPhysicsAuthorityZoneOverlaps == 0) {
          _physicsAuthorityZone.radius = _physicsAuthorityZoneRadiusM;
        }
      }
      return;
    }

    NetworkIdentity objectIdentity = collider.GetComponentInParent<NetworkIdentity>();
    if (objectIdentity == null) {
      return;
    }

    if (_objectsInPhysicsAuthorityZone.ContainsKey(objectIdentity)) {
      _objectsInPhysicsAuthorityZone[objectIdentity]--;

      if (_objectsInPhysicsAuthorityZone[objectIdentity] <= 0) {
        _objectsInPhysicsAuthorityZone.Remove(objectIdentity);
      }
    }

    GlobalPhysicsAuthorityObjectData globalData = null;
    if (globalPhysicsAuthorityDataFromNetIdentity.ContainsKey(objectIdentity)) {
      globalData = globalPhysicsAuthorityDataFromNetIdentity[objectIdentity];

      if (globalData.physicsAuthorityZoneCountFromPlayer.ContainsKey(player)) {
        globalData.physicsAuthorityZoneCountFromPlayer[player]--;

        if (globalData.physicsAuthorityZoneCountFromPlayer[player] <= 0) {
          globalData.physicsAuthorityZoneCountFromPlayer.Remove(player);

          if (globalData.physicsAuthorityZoneCountFromPlayer.Count == 0) {
            HxNetworkRigidbodyBase[] networkRigidbodies =
                objectIdentity.gameObject.GetComponentsInChildren<HxNetworkRigidbodyBase>();
            foreach (HxNetworkRigidbodyBase networkRigidbody in networkRigidbodies) {
              networkRigidbody.PauseSync = false;
            }

            globalPhysicsAuthorityDataFromNetIdentity.Remove(objectIdentity);
          }
        }
      }
    }
  }

  //! @brief Sets the scale of the hand to a given uniform value.
  //!
  //! Only use this method if you want the hand to have a specific scale regardless of the user's
  //! hand size. Otherwise, set #HandScaleFactor.
  //!
  //! @param scale The new hand scale.
  public void SetScale(float scale) {
    if (!enabled) {
      return;
    }

    if (!boneDataFromBoneName.ContainsKey(boneNames.palm) ||
        boneDataFromBoneName[boneNames.palm].gameObject == null) {
      HxDebug.LogError("Failed to set scale. Couldn't find palm GameObject.", this);
      return;
    }

    GameObject palmGameObject = boneDataFromBoneName[boneNames.palm].gameObject;
    float wPalmScaleInitialZ = palmGameObject.transform.lossyScale.z;
    palmGameObject.transform.localScale = new Vector3(scale, scale, scale);

    // Refresh each of the configurable joints.
    // This method seems to trigger needed functionality to account for each joints new world
    // scale.
    foreach (ConfigurableJoint configurableJoint in joints) {
      configurableJoint.anchor = configurableJoint.anchor;
      configurableJoint.connectedAnchor = configurableJoint.connectedAnchor;
    }
    float wPalmScaleRatio = palmGameObject.transform.lossyScale.z / wPalmScaleInitialZ;
    palmJoint.anchor = wPalmScaleRatio * palmJoint.anchor;
    palmJoint.connectedAnchor = wPalmScaleRatio * palmJoint.connectedAnchor;

    // Compute the extent of the palm
    Rigidbody palmRigidbody = palmGameObject.GetComponent<Rigidbody>();
    if (palmRigidbody != null) {
      Collider[] palmColliders = palmRigidbody.GetComponentsInChildren<Collider>();
      Bounds palmBounds = new Bounds(palmRigidbody.worldCenterOfMass, Vector3.zero);
      foreach (Collider palmCollider in palmColliders) {
        palmBounds.Encapsulate(palmCollider.bounds);
      }
      palmExtent = palmBounds.extents.magnitude;
    }

    // ############################################################################################
    // # Update the avatar profile to match the exact dimensions of the avatar hand (which may be
    // # significantly different than the user's hand).
    // ############################################################################################
    if (_userProfile == null) {
      return;
    }
    _avatarProfile = new HaptxApi.UserProfile(_userProfile);
    HaptxApi.RelativeDirection relDir = hand.ToHx();
    HaptxApi.RelativeDirection otherRelDir = relDir == HaptxApi.RelativeDirection.RD_LEFT ?
        HaptxApi.RelativeDirection.RD_RIGHT : HaptxApi.RelativeDirection.RD_LEFT;

    // This is the measured location of the user's MCP3 joint relative to the hardware idealized
    // MCP3 location. We keep using it so the avatar palm stays perfectly aligned with the user's
    // palm.
    HaptxApi.Vector3D mcp3Mcp3 = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    _userProfile.getJoint1PosOffsetsValueM(relDir, HaptxApi.Finger.F_MIDDLE, ref mcp3Mcp3);

    Vector3 wMiddle1 = boneDataFromBoneName[boneNames.middle1].gameObject.transform.position;
    // MCP3's rotation changes with hand animation so we use the palm's rotation instead.
    Quaternion palmWorld =
        Quaternion.Inverse(boneDataFromBoneName[boneNames.palm].gameObject.transform.rotation);
    float wCompScale = boneDataFromBoneName[boneNames.palm].gameObject.transform.lossyScale.x;
    if (wCompScale == 0.0f) {
      HxDebug.LogError("HxHand.SetScale(): Component scale cannot be zero.", this);
    } else if (_userProfileScaleFactor == 0.0f) {
      HxDebug.LogError("HxHand.SetScale(): User profile scale cannot be zero.", this);
    } else {
      // Used to filter out component scale.
      float scaleFactor = _userProfileScaleFactor / wCompScale;
      HxName[,] nameFromHandJoint = GetNameFromHandJoint();
      HxName[] fingertipNameFromFinger = GetFingertipNameFromFinger();
      for (int f_i = 0; f_i < (int)HaptxApi.Finger.F_LAST; f_i++) {
        HaptxApi.Finger f = (HaptxApi.Finger)f_i;

        // Compute the location of joint1 relative to MCP3 in world space.
        Vector3 wJoint1 = boneDataFromBoneName[nameFromHandJoint[
            f_i, (int)HaptxApi.FingerJoint.FJ_JOINT1]].gameObject.transform.position;
        Vector3 mcp3Joint1 = palmWorld * (wJoint1 - wMiddle1) / scaleFactor;

        HaptxApi.Vector3D hxMcp3Joint1 = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
        HxShared.HxFromUnity(mcp3Joint1, hxMcp3Joint1);
        hxMcp3Joint1.operator_plusequals(mcp3Mcp3);
        _avatarProfile.setJoint1PosOffsetsValueM(relDir, f, hxMcp3Joint1);
        hxMcp3Joint1.y_ *= -1.0f;
        _avatarProfile.setJoint1PosOffsetsValueM(otherRelDir, f, hxMcp3Joint1);
        HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxMcp3Joint1);

        _avatarProfile.setFingerBoneLengthsValueM(
            f, HaptxApi.FingerBone.FB_PROXIMAL, (1.0f / scaleFactor) * Vector3.Distance(
            boneDataFromBoneName[nameFromHandJoint[f_i, (int)HaptxApi.FingerJoint.FJ_JOINT2]].gameObject.transform.position,
            boneDataFromBoneName[nameFromHandJoint[f_i, (int)HaptxApi.FingerJoint.FJ_JOINT1]].gameObject.transform.position));
        _avatarProfile.setFingerBoneLengthsValueM(
            f, HaptxApi.FingerBone.FB_MEDIAL, (1.0f / scaleFactor) * Vector3.Distance(
            boneDataFromBoneName[nameFromHandJoint[f_i, (int)HaptxApi.FingerJoint.FJ_JOINT3]].gameObject.transform.position,
            boneDataFromBoneName[nameFromHandJoint[f_i, (int)HaptxApi.FingerJoint.FJ_JOINT2]].gameObject.transform.position));
        _avatarProfile.setFingerBoneLengthsValueM(
            f, HaptxApi.FingerBone.FB_DISTAL, (1.0f / scaleFactor) * Vector3.Distance(
            boneDataFromBoneName[fingertipNameFromFinger[f_i]].gameObject.transform.position,
            boneDataFromBoneName[nameFromHandJoint[f_i, (int)HaptxApi.FingerJoint.FJ_JOINT3]].gameObject.transform.position));
      }
      HxReusableObjectPool<HaptxApi.Vector3D>.Release(mcp3Mcp3);
    }
}

  //! Get the world transform of a locating feature.
  //!
  //! @param locatingFeature Which locating feature.
  //! @param [out] wTransform Populated with the locating feature's world transform.
  //! @returns True if @p wTransform is successfully populated.
  public bool TryGetLocatingFeatureTransform(string locatingFeature, out Matrix4x4 wTransform) {
    HaptxApi.HaptxName locatingFeatureName = new HaptxApi.HaptxName(locatingFeature);
    if (locatingFeatureName.operator_comp_eq(HaptxApiSwig.getName(HaptxApiSwig.getBodyPartJoint(
        hand.ToHx(), HaptxApi.Finger.F_MIDDLE, HaptxApi.FingerJoint.FJ_JOINT1)))) {
      // Middle1's rotation changes with hand animation in the game engine so we use the palm's
      // rotation instead.
      Matrix4x4 wPalm =
          boneDataFromBoneName[boneNames.palm].gameObject.transform.localToWorldMatrix;
      Matrix4x4 wMiddle1 =
          boneDataFromBoneName[boneNames.middle1].gameObject.transform.localToWorldMatrix;
      wTransform = Matrix4x4.TRS(wMiddle1.MultiplyPoint3x4(Vector3.zero), wPalm.rotation,
          wMiddle1.lossyScale);
      return true;
    } else if (locatingFeatureName.operator_comp_eq(HaptxApiSwig.getFingertipName(hand.ToHx(),
        HaptxApi.Finger.F_THUMB))) {
      wTransform = boneDataFromBoneName[boneNames.thumb4].gameObject.transform.localToWorldMatrix;
      return true;
    } else if (locatingFeatureName.operator_comp_eq(HaptxApiSwig.getFingertipName(hand.ToHx(),
         HaptxApi.Finger.F_INDEX))) {
      wTransform = boneDataFromBoneName[boneNames.index4].gameObject.transform.localToWorldMatrix;
      return true;
    } else if (locatingFeatureName.operator_comp_eq(HaptxApiSwig.getFingertipName(hand.ToHx(),
        HaptxApi.Finger.F_MIDDLE))) {
      wTransform = boneDataFromBoneName[boneNames.middle4].gameObject.transform.localToWorldMatrix;
      return true;
    } else if (locatingFeatureName.operator_comp_eq(HaptxApiSwig.getFingertipName(hand.ToHx(),
        HaptxApi.Finger.F_RING))) {
      wTransform = boneDataFromBoneName[boneNames.ring4].gameObject.transform.localToWorldMatrix;
      return true;
    } else if (locatingFeatureName.operator_comp_eq(HaptxApiSwig.getFingertipName(hand.ToHx(),
        HaptxApi.Finger.F_PINKY))) {
      wTransform = boneDataFromBoneName[boneNames.pinky4].gameObject.transform.localToWorldMatrix;
      return true;
    } else {
      wTransform = Matrix4x4.identity;
      return false;
    }
  }

  //! Get the HaptxApi::ContactInterpreter body ID of the HxHand.
  //!
  //! @param patch The patch calling this function.
  //! @param[out] ciBodyId The body ID associated with the HxHand the patch is attached to. Null
  //! if not associated.
  //!
  //! @returns Whether a body ID was found.
  public bool TryGetCiBodyId(HxPatch patch, out long ciBodyId) {
    Rigidbody parentRigidbody = patch.GetComponentInParent<Rigidbody>();
    if (parentRigidbody != null) {

      HxHandBoneData data;
      if (boneDataFromInstanceId.TryGetValue(parentRigidbody.gameObject.GetInstanceID(),
          out data) && data.gameObject.GetComponent<Rigidbody>() != null) {
        ciBodyId = data.ciBodyId;
        return true;
      }
    } else {
      Debug.LogError(string.Format("HxPatch {0} is not a child of a Rigidbody.",
          patch.name), this);
    }

    ciBodyId = 0;
    return false;
  }

  //! The displacement vector between the root bone of the physics hand and the same spot on
  //! the tracked hand (in world space), or the zero vector if the displacement could not be
  //! calculated.
  public Vector3 TrackedDisplacement {
    get {
      GameObject boneObject = boneDataFromBoneName[boneNames.middle1].gameObject;
      if (boneObject == null) {
        return Vector3.zero;
      }
      Vector3 wPosM = boneObject.transform.position;
      Vector3 wTargetPosM = _physicsState.targets.wMiddle1PosM;

      return wTargetPosM - wPosM;
    }
  }

  //! Draw force feedback visualization information.
  void VisualizeForceFeedbackOutput() {
    //! The visual offset [m] of force feedback information from respective hand segments
    //! in the direction of the actuation normal.
    const float LOffsetM = 0.03f;
    //! The color to draw actuation normals when the retractuator is disengaged.
    Color DisengagedColor = HxShared.DebugBlack;
    //! The color to draw actuation normals when the retractuator is engaged.
    Color EngagedColor = HxShared.DebugPurpleOrTeal;
    //! The size to draw actuation normals.
    Vector3 LNormalSize = new Vector3(0.002f, 0.002f, 0.02f);
    //! The color to draw actuation thresholds.
    Color ThresholdColor = HxShared.DebugGray;
    //! The thickness to draw actuation thresholds.
    const float LThresholdThicknessM = 0.0035f;
    //! The color to draw contact forces.
    Color ForceColor = HxShared.DebugBlueOrYellow;
    //! The thickness to draw contact forces.
    const float LForceThickness = 0.0036f;

    foreach (HaptxApi.Retractuator retractuator in Peripheral.retractuators) {
      float forceTargetN = 0.0f;
      core.ContactInterpreter.tryGetRetractuatorForceTargetN(Peripheral.id, retractuator.id,
          ref forceTargetN);

      HaptxApi.PassiveForceActuator.State state = HaptxApi.PassiveForceActuator.State.DISENGAGED;
      core.ContactInterpreter.tryGetRetractuatorStateTarget(Peripheral.id, retractuator.id,
          ref state);

      foreach (var restriction in retractuator.restrictions) {
        HaptxApi.Finger finger = HaptxApiSwig.getFinger(restriction.Key);
        HaptxApi.FingerBone fingerBone = HaptxApiSwig.getFingerBone(restriction.Key);
        if (finger == HaptxApi.Finger.F_LAST || fingerBone == HaptxApi.FingerBone.FB_LAST ||
            fingerBone != HaptxApi.FingerBone.FB_PROXIMAL) {  // Only draw on proximal segments.
          continue;
        }

        HxName boneName = GetNameFromHandBone()[(int)finger, (int)fingerBone];
        Matrix4x4 wBoneTransform =
            boneDataFromBoneName[boneName].gameObject.transform.localToWorldMatrix;
        foreach (HaptxApi.Vector3D actuation_normal in restriction.Value) {
          Vector3 lActuationNormal = HxShared.UnityFromHx(actuation_normal);
          Vector3 wActuationNormal = wBoneTransform.rotation * lActuationNormal;
          Quaternion wActuationNormalOrientation = wBoneTransform.rotation *
              Quaternion.LookRotation(lActuationNormal);
          Vector3 wActuationNormalBase = wBoneTransform.MultiplyPoint3x4(Vector3.zero) +
              HandScaleFactor * LOffsetM * wActuationNormal;

          // Draw the actuation normal.
          HxDebugMesh.DrawCube(wActuationNormalBase, wActuationNormalOrientation,
              HandScaleFactor * LNormalSize, state == HaptxApi.PassiveForceActuator.State.ENGAGED ?
              EngagedColor : DisengagedColor, true);

          // Is there a non-zero actuation threshold?
          RetractuatorParameters rParams = retractuatorParameters.getParametersForFinger(finger);
          if (rParams != null && rParams.actuationThresholdN > 0.0f) {
            Vector3 wThresholdSize =
                HandScaleFactor * new Vector3(LThresholdThicknessM, LThresholdThicknessM,
                forceFeedbackVisualizer.forceScaleM_N * rParams.actuationThresholdN);

            HxDebugMesh.DrawCube(wActuationNormalBase, wActuationNormalOrientation, wThresholdSize,
                state == HaptxApi.PassiveForceActuator.State.ENGAGED ?
                EngagedColor : ThresholdColor, true);
          }

          // Draw the force target.
          if (forceTargetN > 0.0f) {
            Vector3 wForceScale = HandScaleFactor * new Vector3(LForceThickness, LForceThickness,
                forceFeedbackVisualizer.forceScaleM_N * forceTargetN);
            HxDebugMesh.DrawCube(
                wActuationNormalBase, wActuationNormalOrientation, wForceScale,
                state == HaptxApi.PassiveForceActuator.State.ENGAGED ?
                EngagedColor : ForceColor, true);
          }
        }
      }
    }
  }

  //! Draw mocap data in VR.
  //!
  //! @param mocapFrame The mocap data to draw.
  //! @param wMcp3 The world transform of MCP3.
  //! @param wVive The world transform of the tracked VIVE device.
  void VisualizeMocapData(HaptxApi.MocapFrame mocapFrame, Matrix4x4 wMcp3, Matrix4x4 wVive) {
    //! The thickness to draw mocap vis tracked segment basis vectors.
    const float LMocapVisMeshThicknessM = 0.002f;
    //! The length to draw mocap vis tracked segment basis vectors.
    const float LMocapVisMeshLengthM = 0.02f;

    // Draw the world transform of MCP3.
    const float MCP3_REL_SIZE = 1.6f;
    HxDebugMesh.DrawCoordinateFrame(wMcp3.MultiplyPoint3x4(Vector3.zero), wMcp3.rotation,
        MCP3_REL_SIZE * LMocapVisMeshLengthM, MCP3_REL_SIZE * LMocapVisMeshThicknessM);

    // Draw the world transform of the tracked VIVE object.
    const float VIVE_REL_SIZE = 1.4f;
    HxDebugMesh.DrawCoordinateFrame(wVive.MultiplyPoint3x4(Vector3.zero),
        wVive.rotation, VIVE_REL_SIZE * LMocapVisMeshLengthM,
        VIVE_REL_SIZE * LMocapVisMeshThicknessM);

    if (Peripheral != null) {
      const float HYLEAS_SOURCE_REL_SIZE = 1.2f;
      if (Peripheral.is_simulated) {
        // For simulated Gloves we can draw where the VIVE tracker and Hyleas source would be, so
        // why not?
        if (Peripheral.vive_trackers.Count > 0) {
          HaptxApi.ViveTracker viveTracker = Peripheral.vive_trackers[0];
          Matrix4x4 wViveTracker = wVive * HxShared.UnityFromHx(viveTracker.transform);
          HxDebugMesh.DrawCoordinateFrame(wViveTracker.MultiplyPoint3x4(Vector3.zero),
              wViveTracker.rotation,
              VIVE_REL_SIZE * LMocapVisMeshLengthM,
              VIVE_REL_SIZE * LMocapVisMeshThicknessM);
        }

        if (Peripheral.hyleas_sources.Count > 0) {
          HaptxApi.HyleasSource hyleasSource = Peripheral.hyleas_sources[0];
          Matrix4x4 wHyleasSource = wVive * HxShared.UnityFromHx(hyleasSource.transform);
          HxDebugMesh.DrawCoordinateFrame(wHyleasSource.MultiplyPoint3x4(Vector3.zero),
              wHyleasSource.rotation,
              HYLEAS_SOURCE_REL_SIZE * LMocapVisMeshLengthM,
              HYLEAS_SOURCE_REL_SIZE * LMocapVisMeshThicknessM);
        }
      } else if (Peripheral.vive_trackers.Count > 0 && Peripheral.hyleas_sources.Count > 0) {
        // For real Gloves we only need to draw the Hyleas source, and we only know it's rigid
        // connection relative to the VIVE tracker.
        HaptxApi.ViveTracker viveTracker = Peripheral.vive_trackers[0];
        HaptxApi.HyleasSource hyleasSource = Peripheral.hyleas_sources[0];
        Matrix4x4 wHyleasSource = wVive * HxShared.UnityFromHx(
            viveTracker.transform.inverse().operator_mult(hyleasSource.transform));
        HxDebugMesh.DrawCoordinateFrame(wHyleasSource.MultiplyPoint3x4(Vector3.zero),
            wHyleasSource.rotation,
            HYLEAS_SOURCE_REL_SIZE * LMocapVisMeshLengthM,
            HYLEAS_SOURCE_REL_SIZE * LMocapVisMeshThicknessM);
      }
    }

    // Draw all the mocap transforms that are relative to MCP3.
    HaptxApi.HaptxName mcp3Name = HaptxApiSwig.getName(HaptxApiSwig.getBodyPartJoint(
        hand.ToHx(), HaptxApi.Finger.F_MIDDLE, HaptxApi.FingerJoint.FJ_JOINT1));
    foreach (var keyValue in mocapFrame.transforms) {
      if (keyValue.Key.parent.operator_comp_eq(mcp3Name)) {
        Matrix4x4 wDatum = wMcp3 * HxShared.UnityFromHx(keyValue.Value);

        HxDebugMesh.DrawCoordinateFrame(wDatum.MultiplyPoint3x4(Vector3.zero),
            wDatum.rotation, LMocapVisMeshLengthM, LMocapVisMeshThicknessM);
      }
    }
  }

  //! Draw hand animation state.
  //!
  //! @param animFrame The anim frame to draw.
  //! @param profile The user of the anim frame to draw.
  //! @param wMcp3 The world transform of MCP3.
  void VisualizeHandAnimation(HaptxApi.AnimFrame animFrame, HaptxApi.UserProfile profile,
      Matrix4x4 wMcp3) {
    const float COR_RADIUS_M = 0.004f;
    const float BONE_THICKNESS_M = 0.001f;

    // For aesthetic purposes only. Everything is actually located with respect to MCP3.
    Matrix4x4 wPalm = wMcp3 *
        Matrix4x4.TRS(boneDataFromBoneName[boneNames.middle1].gameObject.transform.localPosition,
        Quaternion.identity, Vector3.one).inverse;
    HxDebugMesh.DrawSphere(wPalm.MultiplyPoint3x4(Vector3.zero), wPalm.rotation,
        COR_RADIUS_M * Vector3.one, HxShared.HxOrange);

    for (int f_i = 0; f_i < (int)HaptxApi.Finger.F_LAST; f_i++) {
      HaptxApi.Vector3D l_pos_joint1_m = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
      HaptxApi.Quaternion l_orient_joint1 = HxReusableObjectPool<HaptxApi.Quaternion>.Get();

      profile.getJoint1PosOffsetsValueM(hand.ToHx(), (HaptxApi.Finger)f_i, ref l_pos_joint1_m);
      HaptxApiSwig.getJoint1OrientationConvention(hand.ToHx(), (HaptxApi.Finger)f_i,
          ref l_orient_joint1);
      Matrix4x4 wParent = wMcp3 * Matrix4x4.TRS(HxShared.UnityFromHx(l_pos_joint1_m),
          HxShared.UnityFromHx(l_orient_joint1).normalized, Vector3.one);

      HxReusableObjectPool<HaptxApi.Vector3D>.Release(l_pos_joint1_m);
      HxReusableObjectPool<HaptxApi.Quaternion>.Release(l_orient_joint1);

      // Draw a line to the joint1 (knuckle joint) location.
      HxDebugMesh.DrawLine(wPalm.MultiplyPoint3x4(Vector3.zero),
          wParent.MultiplyPoint3x4(Vector3.zero), BONE_THICKNESS_M, HxShared.HxGreen);

      for (int fj_i = 0; fj_i < (int)HaptxApi.FingerJoint.FJ_LAST; fj_i++) {
        // Draw a sphere at this joint location.
        HxDebugMesh.DrawSphere(wParent.MultiplyPoint3x4(Vector3.zero),
            Quaternion.identity, COR_RADIUS_M * Vector3.one, HxShared.HxOrange);

        HaptxApi.BodyPartJoint bpj = HaptxApiSwig.getBodyPartJoint(hand.ToHx(),
            (HaptxApi.Finger)f_i, (HaptxApi.FingerJoint)fj_i);
        if (!animFrame.l_orientations.ContainsKey(bpj)) {
          break;
        } else {
          Quaternion lJoint = HxShared.UnityFromHx(animFrame.l_orientations[bpj]);
          Vector3 lBoneLengthM = profile.getFingerBoneLengthsValueM((HaptxApi.Finger)f_i,
              (HaptxApi.FingerBone)fj_i) * Vector3.forward;
          Vector3 wPosNextM = wParent.MultiplyPoint3x4(lJoint * lBoneLengthM);
          // Draw a line between this joint location and the next.
          HxDebugMesh.DrawLine(wParent.MultiplyPoint3x4(Vector3.zero), wPosNextM, BONE_THICKNESS_M,
              HxShared.HxGreen);
          wParent = Matrix4x4.TRS(wPosNextM, (wParent.rotation * lJoint).normalized, Vector3.one);
        }
      }
      // Draw a sphere at the fingertip.
      HxDebugMesh.DrawSphere(wParent.MultiplyPoint3x4(Vector3.zero),
          Quaternion.identity, COR_RADIUS_M * Vector3.one, HxShared.HxOrange);
    }
  }

  //! Draw contact damping visualization for one frame.
  void VisualizeContactDamping() {
    foreach (var keyValue in contactDampingDataFromObjectId) {
      // Alias for convenience.
      ContactDampingObjectData data = keyValue.Value;

      if (data.joint == null || data.joint.connectedBody == null) {
        continue;
      }

      Collider[] colliders = data.joint.connectedBody.GetComponentsInChildren<Collider>();
      foreach (Collider collider in colliders) {
        HxDebugMesh.DrawCube(collider.bounds.center, Quaternion.identity, collider.bounds.size,
            HxShared.DebugPurpleOrTeal, false, false, HxDebugMesh.MaterialType.WIREFRAME);
      }
    }
  }

  //! Prints a warning when Glove simulator functionality is misused.
  //!
  //! @param functionName The name of the function being misused.
  void SimulatingMocapFunctionMisuseWarning(string functionName) {
    Debug.LogWarning(string.Format("Function {0} should only be called when simulating mocap.",
        functionName), this);
  }

  //! Simple ToString() with non-all-caps text return.
  //!
  //! @returns #hand string in lower case.
  string HandAsText() {
    return hand == RelDir.LEFT ? "left" : "right";
  }

  //! Sets all linear and angular velocities in the hand to zero.
  void ZeroAllVelocities() {
    foreach (var keyValue in boneDataFromBoneName) {
      if (keyValue.Value.gameObject) {
        Rigidbody rigidbody = keyValue.Value.gameObject.GetComponent<Rigidbody>();
        if (rigidbody) {
          rigidbody.velocity = Vector3.zero;
          rigidbody.angularVelocity = Vector3.zero;
        }
      }
    }
  }

  //! Try to connect to the HxCore and disable ourselves if we fail.
  //!
  //! @returns Whether the HxCore is connected.
  bool ConnectToCore() {
    if (core != null) {
      return true;
    }

    // Make sure there's a Core in the scene, and make sure it has tried to open
    core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HardDisable();
      return false;
    }
    core.RegisterHxUpdate(this);

    if (hasAuthority) {
      // Find a mocap system that we can use, starting with Gloves connected to a Dk2AirController.
      foreach (var dk2AirController in core.HaptxSystem.getDk2AirControllers()) {
        IntToHyleasSystem hyleasSystems = new IntToHyleasSystem();
        if (dk2AirController != null && dk2AirController.getHyleasSystems(hyleasSystems) ==
            HaptxApi.AirController.ReturnCode.SUCCESS) {
          foreach (var keyValue in hyleasSystems) {
            if (keyValue.Value != null &&
                keyValue.Value.getRelativeDirection() == hand.ToHx()) {
              _mocapSystem = keyValue.Value;
              _glove = keyValue.Value.getGlove();
              break;
            }
          }

          if (_mocapSystem != null) {
            break;
          }
        }
      }

      // If we didn't find a Glove connected to an Air Controller, look for a Glove connected
      // elsewhere on the system.
      if (_mocapSystem == null) {
        foreach (HaptxApi.HyleasSystem hyleasSystem in core.HaptxSystem.getHyleasSystems()) {
          if (hyleasSystem != null && hyleasSystem.getRelativeDirection() == hand.ToHx()) {
            _mocapSystem = hyleasSystem;
            _glove = hyleasSystem.getGlove();
            break;
          }
        }
      }

      if (_mocapSystem == null) {
        HxDebug.Log(string.Format(
            "HxHand.ConnectToCore(): No hardware detected for the {0} hand. Using simulated peripheral instead.",
            HandAsText()), this);
        string fileName = hand == RelDir.LEFT ?
            HaptxApi.SimulatedPeripheralDatabase.DK2_GLOVE_LARGE_LEFT_FILE_NAME :
            HaptxApi.SimulatedPeripheralDatabase.DK2_GLOVE_LARGE_RIGHT_FILE_NAME;
        HaptxApi.Peripheral peripheral = new HaptxApi.Peripheral();
        HaptxApi.SimulatedPeripheralDatabase.ReturnCode ret =
            HaptxApi.SimulatedPeripheralDatabase.getSimulatedPeripheral(fileName, peripheral);
        if (ret != HaptxApi.SimulatedPeripheralDatabase.ReturnCode.SUCCESS) {
          HxDebug.LogError(string.Format("Failed to load simulated peripheral for the {0} hand.",
              hand.ToString()), this, true);
          HxDebug.LogError(string.Format(
              "HxHand.ConnectToCore(): HaptxApi::SimulatedPeripheralDatabase::getSimulatedPeripheral() failed with error code {0}: {1}.",
              (int)ret, HaptxApi.SimulatedPeripheralDatabase.toString(ret)));
          HardDisable();
          return false;
        }

        _glove = HaptxApi.Glove.dynamicCast(peripheral);
        if (_glove == null) {
          HxDebug.LogError(string.Format("Loaded a non-Glove peripheral for the {0} hand.",
              hand.ToString()), this, true);
          HardDisable();
          return false;
        }

        core.RegisterSimulatedPeripheral(_glove);
      }
    }

    return true;
  }

  //! @brief Create a joint between the given object and the palm that dampens the object's
  //! motion.
  //!
  //! The intent is to make the object easier to hold.
  //!
  //! @param objectRigidbody The object whose motion to damp.
  //! @returns The new joint.
  ContactDampingObjectData InitiateContactDamping(Rigidbody objectRigidbody) {
    if (boneDataFromBoneName[boneNames.palm] == null ||
        boneDataFromBoneName[boneNames.palm].gameObject == null ||
        objectRigidbody == null) {
      return null;
    }
    GameObject palmGameObject = boneDataFromBoneName[boneNames.palm].gameObject;

    ConfigurableJoint dampingJoint = palmGameObject.AddComponent<ConfigurableJoint>();
    dampingJoint.connectedBody = objectRigidbody;

    // Center the joint on the connected body to decouple the bodies translation from the joints
    // rotation.
    dampingJoint.anchor = (palmGameObject.transform.worldToLocalMatrix *
        objectRigidbody.transform.localToWorldMatrix).MultiplyPoint3x4(Vector3.zero);
    dampingJoint.enableCollision = true;

    // Figure out what linear contact damping value should be used, and use it.
    HxRigidbodyProperties hxRigidbodyProperties =
    objectRigidbody.GetComponent<HxRigidbodyProperties>();
    JointDrive linearDrive = new JointDrive {
      positionSpring = 0.0f,
      positionDamper = (hxRigidbodyProperties != null &&
          hxRigidbodyProperties.overrideLinearContactDamping) ?
          hxRigidbodyProperties.linearContactDamping : linearContactDamping,
      maximumForce = float.MaxValue
    };
    dampingJoint.xDrive = linearDrive;
    dampingJoint.yDrive = linearDrive;
    dampingJoint.zDrive = linearDrive;

    // Figure out what angular contact damping value should be used, and use it.
    dampingJoint.rotationDriveMode = RotationDriveMode.Slerp;
    dampingJoint.slerpDrive = new JointDrive() {
      positionSpring = 0.0f,
      positionDamper = (hxRigidbodyProperties != null &&
          hxRigidbodyProperties.overrideAngularContactDamping) ?
          hxRigidbodyProperties.angularContactDamping : angularContactDamping,
      maximumForce = float.MaxValue
    };
    return new ContactDampingObjectData() {
      joint = dampingJoint,
      linearDrive = linearDrive
    };
  }

  //! Disable the functionality of this class.
  void HardDisable() {
    HxDebug.LogRestartMessage();
    enabled = false;
  }

  //! Load user profile from disk.
  void LoadUserProfile() {
    _userProfile = new HaptxApi.UserProfile();
    string activeUsername = string.Empty;
    if (!HaptxApi.UserProfileDatabase.getActiveUsername(ref activeUsername) ||
        !HaptxApi.UserProfileDatabase.getUserProfile(activeUsername, ref _userProfile)) {
      HxDebug.LogWarning("Failed to load active user profile. Using the default profile instead.",
          this, true);
      _userProfile = new HaptxApi.UserProfile();
    }
    // Start the interpolated profile as a perfect copy of the user profile.
    _avatarAnimOptimizedProfile = new HaptxApi.UserProfile(_userProfile);
    if (_glove != null) {
      _gloveSlipCompensator = new HaptxApi.GloveSlipCompensator(_glove, _userProfile);
    }
    float middleFingerLengthM =
        _userProfile.getFingerBoneLengthsValueM(HaptxApi.Finger.F_MIDDLE, HaptxApi.FingerBone.FB_PROXIMAL) +
        _userProfile.getFingerBoneLengthsValueM(HaptxApi.Finger.F_MIDDLE, HaptxApi.FingerBone.FB_MEDIAL) +
        _userProfile.getFingerBoneLengthsValueM(HaptxApi.Finger.F_MIDDLE, HaptxApi.FingerBone.FB_DISTAL);
    CmdApplyUserProfile(_userProfile.sex, _userProfile.skin_tone, middleFingerLengthM);
  }


  //! Apply state loaded from user profile.
  //!
  //! @param sex The user's sex.
  //! @param tone The user's skin tone.
  //! @param middleFingerLengthM The length of the user's middle finger.
  [Command]
  void CmdApplyUserProfile(HaptxApi.BiologicalSex sex, HaptxApi.SkinTone tone,
      float middleFingerLengthM) {
    _biologicalSex = sex;
    _skinTone = tone;
    _userMiddleFingerLengthM = middleFingerLengthM;
  }

  //! Load the prefab matching the given sex.
  //!
  //! @param sex The user's sex.
  void LoadHandPrefab(HaptxApi.BiologicalSex sex) {
    // Find the right prefab and spawn it.
    GameObject prefab;
    if (hand == RelDir.LEFT && sex == HaptxApi.BiologicalSex.FEMALE) {
      prefab = handMeshes.leftFemaleHandPrefab;
    } else if (hand == RelDir.RIGHT && sex == HaptxApi.BiologicalSex.FEMALE) {
      prefab = handMeshes.rightFemaleHandPrefab;
    } else if (hand == RelDir.LEFT && sex != HaptxApi.BiologicalSex.FEMALE) {
      prefab = handMeshes.leftMaleHandPrefab;
    } else {
      prefab = handMeshes.rightMaleHandPrefab;
    }

    if (prefab == null) {
      HxDebug.LogWarning(string.Format("Could not load prefab for {0} {1} hand.",
          sex == HaptxApi.BiologicalSex.FEMALE ? "female" : "male", HandAsText()),
          null, true);
      HardDisable();
      return;
    }

    GameObject hand_prefab = Instantiate(prefab, gameObject.transform);
    HxShared.SetLayerRecursively(hand_prefab, hasAuthority ? _localHandsLayer.value :
        _remoteHandsLayer.value);

    // Record the base pose of the hand.
    SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
    if (smrs.Length != 0) {
      physSmr = smrs[0];
      physSmrBones = physSmr.bones;
      basePose = new Tuple<Vector3, Quaternion>[physSmrBones.Length];
      for (int i = 0; i < physSmrBones.Length; i++) {
        if (physSmrBones[i] == null || physSmrBones[i] == physSmr.rootBone) {
          continue;
        }

        basePose[i] = Tuple.Create(physSmrBones[i].localPosition, physSmrBones[i].localRotation);
      }
    }

    if (_overrideSolverIterations) {
      Rigidbody[] rigidbodies = hand_prefab.GetComponentsInChildren<Rigidbody>();
      foreach (Rigidbody rigidbody in rigidbodies) {
        rigidbody.solverIterations = rigidbodySolverIterations;
        rigidbody.solverVelocityIterations = rigidbodySolverVelocityIterations;
      }
    }

    if (_overrideContactOffset) {
      Collider[] colliders = hand_prefab.GetComponentsInChildren<Collider>();
      foreach (Collider collider in colliders) {
        collider.contactOffset = _contactOffset;
      }
    }

    // Do all the hand initialization work that depends on the prefab's game object hierarchy.
    BuildDataStructures();
    RegisterBones();
    InitPalmJoint();
    InitPhysicsAuthorityZone();
    if (hasAuthority) {
      HxPatch[] patches = gameObject.GetComponentsInChildren<HxPatch>();
      foreach (HxPatch patch in patches) {
        patch.Initialize();
      }

      UninitializeDisplacementVisualizer();
      if (displacementVisualizer.visualize) {
        disVisInitialized = InitializeDisplacementVisualizer();
      }
    }
  }

  //! Changes the material of the hand to match the given skin tone.
  //!
  //! @param tone The tone to use.
  void SetSkinTone(HaptxApi.SkinTone tone) {
    if (physSmr == null) {
      return;
    }

    switch (tone) {
      case HaptxApi.SkinTone.LIGHT: {
          physSmr.material = _biologicalSex == HaptxApi.BiologicalSex.FEMALE ?
              handMaterials.lightFemaleHandMaterial : handMaterials.lightMaleHandMaterial;
          break;
        }
      case HaptxApi.SkinTone.MEDIUM: {
          physSmr.material = _biologicalSex == HaptxApi.BiologicalSex.FEMALE ?
              handMaterials.mediumFemaleHandMaterial : handMaterials.mediumMaleHandMaterial;
          break;
        }
      case HaptxApi.SkinTone.DARK: {
          physSmr.material = _biologicalSex == HaptxApi.BiologicalSex.FEMALE ?
              handMaterials.darkFemaleHandMaterial : handMaterials.darkMaleHandMaterial;
          break;
        }
      case HaptxApi.SkinTone.NEUTRAL: {
          physSmr.material = _biologicalSex == HaptxApi.BiologicalSex.FEMALE ?
              handMaterials.neutralFemaleHandMaterial : handMaterials.neutralMaleHandMaterial;
          break;
        }
    }
  }

  //! Change the hand scale assuming a given user middle finger length [m].
  //!
  //! @param middleFingerLengthM The user's middle finger length [m].
  void SetUserMiddleFingerLengthM(float middleFingerLengthM) {
    if (middleFingerLengthM <= 0.0f || float.IsNaN(middleFingerLengthM)) {
      return;
    }

    HxHandBoneData middle1;
    HxHandBoneData middle4;
    if (!boneDataFromBoneName.TryGetValue(boneNames.middle1, out middle1) ||
        !boneDataFromBoneName.TryGetValue(boneNames.middle4, out middle4)) {
      return;
    }

    SetScale(1.0f);
    float defaultMiddleFingerLengthM = (middle4.gameObject.transform.position -
        middle1.gameObject.transform.position).magnitude;

    if (defaultMiddleFingerLengthM <= 0.0f || float.IsNaN(defaultMiddleFingerLengthM)) {
      HxDebug.LogError("Couldn't resize hand.", this);
      return;
    }

    _userProfileScaleFactor = middleFingerLengthM / defaultMiddleFingerLengthM;
    HandScaleFactor = HandScaleFactor;  // Prompts the hand to actually resize.

    // Undo scaling effect of user hand length on any HaptX Patches that are attached to us
    HxPatch[] patches = gameObject.GetComponentsInChildren<HxPatch>();
    foreach (HxPatch patch in patches) {
      patch.transform.localScale = Vector3.one / _userProfileScaleFactor;
      if (hasAuthority) {
        patch.UpdateTraceOrigins(physSmr);
      }
    }
  }

  //! Initializes the displacement visualizer.
  //!
  //! @returns True if the displacement visualizer was successfully initialized.
  bool InitializeDisplacementVisualizer() {
    if (physSmr == null || basePose == null || basePose.Length != physSmr.bones.Length) {
      return false;
    }

    // Create a new game object for the visualizer skinned mesh, and child it to the root component
    // of the original skinned mesh. This ensures that any scaling applied to the original mesh
    // also affects the visualizer mesh.
    GameObject disVisGameObject = new GameObject("DisplacementVisualizer");
    disVisGameObject.transform.parent = physSmr.rootBone;
    disVisGameObject.transform.localPosition = Vector3.zero;
    disVisGameObject.transform.localRotation = Quaternion.identity;
    disVisGameObject.transform.localScale = Vector3.one;

    // Create the visualizer skinned mesh.
    disVisSmr = disVisGameObject.AddComponent<SkinnedMeshRenderer>();
    if (disVisSmr == null) {
      HxDebug.LogError(
          "HxHand.InitializeDisplacementVisualizer(): Failed to add visualizer mesh.", this);
      return false;
    }
    disVisSmr.sharedMesh = physSmr.sharedMesh;

    // Copy the bone structure from the physical mesh to the visual mesh. This dictionary maps the
    // unique IDs of physical mesh bones to the game objects matching those bones in the visualizer
    // mesh.
    Dictionary<int, GameObject> visGameObjectFromPhysId = new Dictionary<int, GameObject>();
    visSmrBones = new Transform[physSmrBones.Length];
    for (int i = 0; i < physSmrBones.Length; i++) {
      if (physSmrBones[i] == null) {
        continue;
      }

      // Create the new bone and add it to the visualizer skinned mesh.
      GameObject visBone = new GameObject(physSmrBones[i].name);
      visGameObjectFromPhysId.Add(physSmrBones[i].GetInstanceID(), visBone);
      visSmrBones[i] = visBone.transform;
    }
    disVisSmr.bones = visSmrBones;
    disVisOpacityFilters = new HxLowPassFilter[visSmrBones.Length];
    for (int i = 0; i < disVisOpacityFilters.Length; i++) {
      disVisOpacityFilters[i] = new HxLowPassFilter(displacementVisualizer.opacityFilterStrengthS);
    }

    // Set the visualizer skinned mesh root to the game object created to match the physical
    // skinned mesh root.
    GameObject disVisRoot;
    if (physSmr.rootBone == null || !visGameObjectFromPhysId.TryGetValue(
        physSmr.rootBone.GetInstanceID(), out disVisRoot)) {
      HxDebug.LogError(
          "HxHand.InitializeDisplacementVisualizer(): No root bone.", this);
      return false;
    }
    disVisRoot.transform.parent = disVisGameObject.transform;
    disVisRoot.transform.localPosition = Vector3.zero;
    disVisRoot.transform.localRotation = Quaternion.identity;
    disVisRoot.transform.localScale = Vector3.one;
    disVisSmr.rootBone = disVisRoot.transform;

    // Copy the hierarchical relationships from the physical skinned mesh to the visualizer skinned
    // mesh.
    for (int i = 0; i < physSmrBones.Length; i++) {
      // Only look at bones that have parents.
      Transform physChild = physSmrBones[i];
      if (physChild == null) {
        continue;
      }
      Transform physParent = physSmrBones[i].parent;
      if (physSmrBones[i].parent == null) {
        continue;
      }

      // Find the would-be parent among the visualizer mesh's bones.
      GameObject visParent;
      if (!visGameObjectFromPhysId.TryGetValue(physParent.GetInstanceID(), out visParent)) {
        // The bone isn't parented to another bone. This is not a failure mode.
        continue;
      }

      // Find the would-be child among the visualizer mesh's bones.
      GameObject visChild;
      if (!visGameObjectFromPhysId.TryGetValue(physChild.GetInstanceID(), out visChild)) {
        HxDebug.LogError(
            "HxHand.InitializeDisplacementVisualizer(): No child object.", this);
        return false;
      }

      // Make sure we know the base pose for this bone.
      Tuple<Vector3, Quaternion> boneTransform = basePose[i];
      if (boneTransform == null) {
        HxDebug.LogError(string.Format(
            "HxHand.InitializeDisplacementVisualizer(): Missing base pose for bone {0}.",
            physChild.name), this);
        return false;
      }

      // Copy the relative transform from the physical mesh hierarchy over to the visual mesh
      // hierarchy.
      visChild.transform.parent = visParent.transform;
      visChild.transform.localPosition = boneTransform.Item1;
      visChild.transform.localRotation = boneTransform.Item2;
      visChild.transform.localScale = Vector3.one;
    }

    // Setup object references indexed by HandJoints.
    disVisJoints = new GameObject[(int)HaptxApi.Finger.F_LAST, (int)HaptxApi.FingerJoint.FJ_LAST];
    HxName[,] nameFromHandJoint = GetNameFromHandJoint();
    for (int f_i = 0; f_i < (int)HaptxApi.Finger.F_LAST; f_i++) {
      for (int fj_i = 0; fj_i < (int)HaptxApi.FingerJoint.FJ_LAST; fj_i++) {
        Transform bone = HxShared.GetChildGameObjectByName(
            disVisRoot.transform, nameFromHandJoint[f_i, fj_i].String);
        if (bone == null) {
          HxDebug.LogError(string.Format(
              "HxHand.InitializeDisplacementVisualizer(): No visualizer bone named {0} for HaptxApi.Finger {1} and HaptxApi.FingerJoint {2}.",
              nameFromHandJoint[f_i, fj_i], (HaptxApi.Finger)f_i, (HaptxApi.FingerJoint)fj_i),
              this);
          return false;
        } else {
          disVisJoints[f_i, fj_i] = bone.gameObject;
        }
      }
    }

    // Setup our custom shader and sync relevant variables.
    const string shaderPath = "HaptX/DontModify_HxHand_WeightedOpacity";
    Shader shader = Shader.Find(shaderPath);
    if (shader == null) {
      HxDebug.LogError(string.Format(
          "HxHand.InitializeDisplacementVisualizer(): Failed to find shader: {0}", shaderPath),
          this);
      return false;
    } else {
      disVisSmr.material = new Material(shader);
    }

    disVisSmr.material.SetColor("_Color", displacementVisualizer.color);

    disVisSmr.material.SetFloat("_NumBones", visSmrBones.Length);

    int numVertices = disVisSmr.sharedMesh.boneWeights.Length;
    disVisSmr.material.SetFloat("_NumVertices", numVertices);

    // We store vertex indices in a second UV channel to ensure that they are properly synced in
    // our custom shader.
    Vector2[] uvIndexes = new Vector2[numVertices];
    for (int i = 0; i < numVertices; i++) {
      uvIndexes[i] = new Vector2(i, 0);
    }
    disVisSmr.sharedMesh.uv2 = uvIndexes;

    // We've baked bone weights and indices into a special texture file that should be stored in a
    // Resources directory.
    string texturePath = string.Format("{0}{1}", disVisSmr.sharedMesh.name, BoneWeightsFileSuffix);
    Texture2D texture = Resources.Load<Texture2D>(texturePath);
    if (texture == null) {
      HxDebug.LogError(string.Format(
          "HxHand.InitializeDisplacementVisualizer(): Failed to load {0}. It may be generated in the editor via \"Window->HaptX->Generate Bone Weight Texture\".",
          texturePath), this);
      return false;
    }
    disVisSmr.material.SetTexture("_BoneIndicesAndWeights", texture);

    return true;
  }

  //! Updates the displacement visualizer with the most recent bone displacements.
  void UpdateDisplacementVisualizer() {
    if (disVisSmr == null || physSmr == null) {
      return;
    }

    // Compute bone opacities based on the distance between physical bones and associated
    // visualizer bones.
    float[] boneOpacities = new float[physSmrBones.Length];
    for (int i = 0; i < physSmrBones.Length; i++) {
      if (visSmrBones[i] == null || physSmrBones[i] == null) {
        continue;
      }

      float distance = (visSmrBones[i].position - physSmrBones[i].position).magnitude;
      float a = (distance - displacementVisualizer.minDisplacementM) /
          (displacementVisualizer.maxDisplacementM - displacementVisualizer.minDisplacementM);
      float opacityTarget = Mathf.Lerp(0.0f, displacementVisualizer.maxOpacity, a);
      boneOpacities[i] = disVisOpacityFilters[i].ApplyFilter(opacityTarget);
    }
    disVisSmr.material.SetFloatArray("_BoneOpacities", boneOpacities);
  }

  //! Tears down the displacement visualizer.
  void UninitializeDisplacementVisualizer() {
    if (disVisSmr != null) {
      Destroy(disVisSmr.gameObject);
      disVisSmr = null;
    }
    visSmrBones = null;
    disVisOpacityFilters = null;
    disVisJoints = null;
  }

  //! Evaluates whether the client should have physics authority. Should only be called by the
  //! server.
  void UpdatePhysicsAuthority() {
    if (core == null) {
      return;
    }

    // Cache the current value so we can determine if it has changed at the end of the function.
    bool isClientPhysicsAuthorityCached = _isClientPhysicsAuthority;
    switch (core.PhysicsAuthorityMode) {
      case PhysicsAuthorityMode.DYNAMIC: {
          // Evaluate whether any multi-player interactions are happening.
          if (_numPhysicsAuthorityZoneOverlaps > 0) {
            _isClientPhysicsAuthority = false;
          } else {
            bool sharedObjectFound = false;
            foreach (var keyValue in _objectsInPhysicsAuthorityZone) {
              if (keyValue.Key == null) {
                continue;
              }

              GlobalPhysicsAuthorityObjectData globalData = null;
              if (globalPhysicsAuthorityDataFromNetIdentity.TryGetValue(keyValue.Key,
                  out globalData) && globalData.physicsAuthorityZoneCountFromPlayer.Count > 1) {
                sharedObjectFound = true;
                break;
              }
            }
            _isClientPhysicsAuthority = !sharedObjectFound;
          }
          break;
        }
      case PhysicsAuthorityMode.CLIENT:
        _isClientPhysicsAuthority = true;
        break;
      default:
      case PhysicsAuthorityMode.SERVER:
        _isClientPhysicsAuthority = false;
        break;
    }

    if (_isClientPhysicsAuthority != isClientPhysicsAuthorityCached) {
      IsClientPhysicsAuthorityHook(isClientPhysicsAuthorityCached, _isClientPhysicsAuthority);
    }
  }

  //! Visualize network state for one frame.
  void VisualizeNetworkState() {
    // Added to color for hands that are instantiated on the server.
    Color AuthoritativeColor = Color.blue;
    // Added to color for hands that are controlled by the local player.
    Color LocallyControlledColor = Color.green;
    // Added to color for hands that have physics authority.
    Color PhysicsAuthorityColor = Color.red;
    // For objects that are currently syncing rigidbody state.
    Color RigidbodySyncColor = Color.green;
    // For objects that have had rigidbody sync paused.
    Color RigidbodySyncPausedColor = Color.yellow;

    // The hands get a different color for each unique combination of server authoritative, locally
    // controlled, and physics authoritative.
    Color networkStateColor = Color.black;
    if (isServer) {
      networkStateColor += AuthoritativeColor;
    }
    if (hasAuthority) {
      networkStateColor += LocallyControlledColor;
    }
    if (IsPhysicsAuthority) {
      networkStateColor += PhysicsAuthorityColor;
    }
    if (_physicsAuthorityZone != null) {
      Matrix4x4 wPhysAuthZone =
          _physicsAuthorityZone.gameObject.transform.localToWorldMatrix *
          Matrix4x4.TRS(_physicsAuthorityZone.center, Quaternion.identity, Vector3.one);
      float wMaxScaleComponent = Mathf.Max(
          _physicsAuthorityZone.gameObject.transform.lossyScale.x,
          _physicsAuthorityZone.gameObject.transform.lossyScale.y,
          _physicsAuthorityZone.gameObject.transform.lossyScale.z);

      HxDebugMesh.DrawSphere(wPhysAuthZone.MultiplyPoint3x4(Vector3.zero), wPhysAuthZone.rotation,
          2.0f * _physicsAuthorityZone.radius * wMaxScaleComponent * Vector3.one,
          networkStateColor, false, HxDebugMesh.MaterialType.WIREFRAME);
    }

    // Objects that are currently under the purview of this networking system get highlighted.
    foreach (var keyValue in _objectsInPhysicsAuthorityZone) {
      NetworkIdentity objectIdentity = keyValue.Key;
      if (objectIdentity == null) {
        continue;
      }

      Color objectNetworkStateColor = PhysicsAuthorityColor;
      GlobalPhysicsAuthorityObjectData globalData = null;
      if (globalPhysicsAuthorityDataFromNetIdentity.TryGetValue(objectIdentity, out globalData)) {
        // On the server the highlight color changes when multiple players are interacting with an
        // object, indicating that the object is responsible for those players' hands having server
        // authority.
        if (isServer) {
          if (globalData != null && globalData.physicsAuthorityZoneCountFromPlayer.Count > 1) {
            objectNetworkStateColor = AuthoritativeColor;
          }
        // On clients object color depends on whether any rigidbody sync components are found and
        // whether those components are paused.
        } else {
          HxNetworkRigidbodyBase[] networkRigidbodies =
              objectIdentity.gameObject.GetComponentsInChildren<HxNetworkRigidbodyBase>();
          bool foundSyncingRigidbody = false;
          foreach (HxNetworkRigidbodyBase networkRigidbody in networkRigidbodies) {
            if (!networkRigidbody.PauseSync) {
              foundSyncingRigidbody = true;
              break;
            }
          }
          objectNetworkStateColor = foundSyncingRigidbody ? RigidbodySyncColor :
              RigidbodySyncPausedColor;
        }
      }

      Rigidbody[] rigidbodies = objectIdentity.gameObject.GetComponentsInChildren<Rigidbody>();
      foreach (Rigidbody rigidbody in rigidbodies) {
        Collider[] colliders = rigidbody.gameObject.GetComponentsInChildren<Collider>();
        foreach(Collider collider in colliders) {
          HxDebugMesh.DrawCube(collider.bounds.center, Quaternion.identity, collider.bounds.size,
              objectNetworkStateColor, false, false, HxDebugMesh.MaterialType.WIREFRAME);
        }
      }
    }
  }

  //! Adds a new physics targets frame to the buffer.
  //!
  //! @param timeS The world time that the targets were generated.
  //! @param targets New physics targets.
  void PushPhysicsTargets(float timeS, HandPhysicsTargets targets) {
    if (targets.lJointOrients == null) {
      return;
    }

    int writeIndex = _physicsTargetsBufferHeadIndex;
    if (!_physicsTargetsBufferStarted) {
      writeIndex = _physicsTargetsBufferTailIndex;
      _physicsTargetsBufferStarted = true;
    } else if (timeS < _physicsTargetsBuffer[_physicsTargetsBuffer.GetPreviousIndex(
        _physicsTargetsBufferHeadIndex)].timeS) {
      // Don't insert out-of-order frames. TODO: Refactor to insert in middle instead of drop.
      return;
    } else if (_physicsTargetsBufferHeadIndex == _physicsTargetsBufferTailIndex) {
      // Make sure the buffer doesn't eat itself.
      _physicsTargetsBufferTailIndex =
          _physicsTargetsBuffer.GetNextIndex(_physicsTargetsBufferTailIndex);
    }
    _physicsTargetsBufferHeadIndex =
        _physicsTargetsBuffer.GetNextIndex(_physicsTargetsBufferHeadIndex);

    _physicsTargetsBuffer[writeIndex] = new HandPhysicsTargetsFrame {
      timeS = timeS,
      targets = targets};
  }

  //! Interpolates physics targets received across the network.
  //!
  //! @param deltaTimeS The delta time from our physics simulation.
  void InterpolatePhysicsTargets(float deltaTimeS) {
    if (!_physicsTargetsBufferStarted) {
      return;
    }

    // Grow or shrink delta time depending on how expected lag compares to actual lag.
    int newestFrameIndex =
        _physicsTargetsBuffer.GetPreviousIndex(_physicsTargetsBufferHeadIndex);
    if (physicsTargetsBufferDurationS > 0.0f) {
      float lagS = _physicsTargetsBuffer[newestFrameIndex].timeS - _followTimeS;
      float lagWeightedDeltaTimeS = deltaTimeS * lagS / physicsTargetsBufferDurationS;
      _followTimeS += lagWeightedDeltaTimeS;
    } else {
      _followTimeS += deltaTimeS;
    }

    int a_i = _physicsTargetsBufferTailIndex;
    int b_i = _physicsTargetsBufferTailIndex;
    if (_followTimeS < _physicsTargetsBuffer[_physicsTargetsBufferTailIndex].timeS) {
      _followTimeS = _physicsTargetsBuffer[_physicsTargetsBufferTailIndex].timeS;
    } else if (_followTimeS > _physicsTargetsBuffer[newestFrameIndex].timeS) {
      // This is where we would extrapolate.
      return;
    } else {
      for (int i = _physicsTargetsBufferTailIndex; i != _physicsTargetsBufferHeadIndex;
          i = _physicsTargetsBuffer.GetNextIndex(i)) {
        if (_physicsTargetsBuffer[i].timeS <= _followTimeS) {
          a_i = i;

          int nextIndex = _physicsTargetsBuffer.GetNextIndex(i);
          if (nextIndex == _physicsTargetsBufferHeadIndex) {
            b_i = i;
          } else if (_physicsTargetsBuffer[nextIndex].timeS < _followTimeS) {
            _physicsTargetsBufferTailIndex = nextIndex;
          }
        } else {
          // Stop at the first frame ahead of us in time.
          b_i = i;
          break;
        }
      }
    }

    if (a_i == b_i) {
      UpdatePhysicsTargets(_physicsTargetsBuffer[a_i].targets);
    } else {
      HandPhysicsTargetsFrame a = _physicsTargetsBuffer[a_i];
      HandPhysicsTargetsFrame b = _physicsTargetsBuffer[b_i];

      if (b.timeS - a.timeS > 0.0f) {
        float alpha = (_followTimeS - a.timeS) / (b.timeS - a.timeS);
        HandPhysicsTargets c = HandPhysicsTargets.Interpolate(a.targets, b.targets, alpha);
        UpdatePhysicsTargets(c);
      }
    }
  }

  //! Adds a new physics state frame to the buffer.
  //!
  //! @param timeS The world time that the state was generated.
  //! @param state The hand physics state being sent.
  void PushPhysicsState(float timeS, HandPhysicsState state) {
    if (state.wBodyStates == null || state.targets.lJointOrients == null) {
      return;
    }

    int writeIndex = _physicsStateBufferHeadIndex;
    if (!_physicsStateBufferStarted) {
      writeIndex = _physicsStateBufferTailIndex;
      _physicsStateBufferStarted = true;
    } else if (timeS < _physicsStateBuffer[_physicsStateBuffer.GetPreviousIndex(
        _physicsStateBufferHeadIndex)].timeS) {
      // Don't insert out-of-order frames. TODO: Refactor to insert in middle instead of drop.
      return;
    } else if (_physicsStateBufferHeadIndex == _physicsStateBufferTailIndex) {
      // Make sure the buffer doesn't eat itself.
      _physicsStateBufferTailIndex =
          _physicsStateBuffer.GetNextIndex(_physicsStateBufferTailIndex);
    }
    _physicsStateBufferHeadIndex = _physicsStateBuffer.GetNextIndex(_physicsStateBufferHeadIndex);

    _physicsStateBuffer[writeIndex] = new HandPhysicsStateFrame {
      timeS = timeS,
      state = state};
  }

  //! Interpolates physics state received across the network.
  //!
  //! @param deltaTimeS The delta time from our physics simulation.
  void InterpolatePhysicsState(float deltaTimeS) {
    if (!_physicsStateBufferStarted) {
      return;
    }

    // Grow or shrink delta time depending on how expected lag compares to actual lag.
    int newestFrameIndex = _physicsStateBuffer.GetPreviousIndex(_physicsStateBufferHeadIndex);
    if (physicsStateBufferDurationS > 0.0f) {
      float lagS = _physicsStateBuffer[newestFrameIndex].timeS - _followTimeS;
      float lagWeightedDeltaTimeS = deltaTimeS * lagS / physicsStateBufferDurationS;
      _followTimeS += lagWeightedDeltaTimeS;
    } else {
      _followTimeS += deltaTimeS;
    }

    int a_i = _physicsStateBufferTailIndex;
    int b_i = _physicsStateBufferTailIndex;
    if (_followTimeS < _physicsStateBuffer[_physicsStateBufferTailIndex].timeS) {
      _followTimeS = _physicsStateBuffer[_physicsStateBufferTailIndex].timeS;
    } else if (_followTimeS > _physicsStateBuffer[newestFrameIndex].timeS) {
      // This is where we would extrapolate.
      return;
    } else {
      for (int i = _physicsStateBufferTailIndex; i != _physicsStateBufferHeadIndex;
          i = _physicsStateBuffer.GetNextIndex(i)) {
        if (_physicsStateBuffer[i].timeS <= _followTimeS) {
          a_i = i;

          int nextIndex = _physicsStateBuffer.GetNextIndex(i);
          if (nextIndex == _physicsStateBufferHeadIndex) {
            b_i = i;
          } else if (_physicsStateBuffer[nextIndex].timeS < _followTimeS) {
            _physicsStateBufferTailIndex = nextIndex;
          }
        } else {
          // Stop at the first frame ahead of us in time.
          b_i = i;
          break;
        }
      }
    }

    if (a_i == b_i) {
      UpdatePhysicsState(_physicsStateBuffer[a_i].state);
    } else {
      HandPhysicsStateFrame a = _physicsStateBuffer[a_i];
      HandPhysicsStateFrame b = _physicsStateBuffer[b_i];

      if (b.timeS - a.timeS > 0.0f) {
        float alpha = (_followTimeS - a.timeS) / (b.timeS - a.timeS);
        HandPhysicsState c = HandPhysicsState.Interpolate(a.state, b.state, alpha);
        UpdatePhysicsState(c);
      }
    }
  }

  //! Updates the physics targets of all constraints driving the hand.
  //!
  //! @param targets New physics targets.
  void UpdatePhysicsTargets(HandPhysicsTargets targets) {
    _physicsState.targets = targets;
    palmJoint.targetPosition = targets.wMiddle1PosM;
    palmJoint.targetRotation = targets.wMiddle1Orient.normalized;

    if (disVisInitialized) {
      disVisSmr.rootBone.position = targets.wMiddle1PosM - palmJoint.transform.rotation *
          palmJoint.connectedAnchor;
      disVisSmr.rootBone.rotation = targets.wMiddle1Orient;
    }

    if (targets.lJointOrients != null && targets.lJointOrients.Length ==
        (int)HaptxApi.Finger.F_LAST * (int)HaptxApi.FingerJoint.FJ_LAST) {
      for (int f_i = 0; f_i < (int)HaptxApi.Finger.F_LAST; f_i++) {
        for (int fj_i = 0; fj_i < (int)HaptxApi.FingerJoint.FJ_LAST; fj_i++) {
          if (joints[f_i, fj_i] == null) {
            continue;
          }

          int flatIndex = (int)HaptxApi.FingerJoint.FJ_LAST * f_i + fj_i;
          joints[f_i, fj_i].targetRotation = Quaternion.Inverse(targets.lJointOrients[flatIndex]);
          if (disVisInitialized && disVisJoints[f_i, fj_i] != null) {
            disVisJoints[f_i, fj_i].transform.localRotation =
                jointRotationOffsets[f_i, fj_i] * targets.lJointOrients[flatIndex];
          }
        }
      }
    }
  }

  //! Sends physics targets to the server.
  //!
  //! @param timeS The world time that the targets were generated.
  //! @param targets New physics targets.
  [Command(channel = HxVersionCompatability.MirrorChannels.Unreliable)]
  void CmdUpdatePhysicsTargets(float timeS, HandPhysicsTargets targets) {
    if (!hasAuthority) {
      PushPhysicsTargets(timeS, targets);
    }
  }

  //! Updates the physics state of the hand.
  //!
  //! @param state The new physics state.
  void UpdatePhysicsState(HandPhysicsState state) {
    UpdatePhysicsTargets(state.targets);
    RigidbodyState.SetRigidbodyStates(gameObject, state.wBodyStates);
    if (state.wObjectStates != null) {
      foreach (ObjectPhysicsState objectPhysicsState in state.wObjectStates) {
        if (objectPhysicsState.networkIdentity == null) {
          continue;
        }

        RigidbodyState.SetRigidbodyStates(objectPhysicsState.networkIdentity.gameObject,
            objectPhysicsState.rigidbodyStates);
      }
    }
  }

  //! Sends hand physics state to the server.
  //!
  //! @param timeS The world time that the state was generated.
  //! @param state The hand physics state being sent.
  [Command(channel = HxVersionCompatability.MirrorChannels.Unreliable)]
  void CmdUpdatePhysicsState(float timeS, HandPhysicsState state) {
    if (IsPhysicsAuthority && !hasAuthority) {
      // This is the function the client would have called, had it known it didn't have authority.
      // This is expected to execute while an updated value of "isClientPhysicsAuthority = false"
      // is traveling over the network to the client. During this time the client still thinks it has
      // authority so it will be executing CmdUpdatePhysicsState() when it should be executing
      // CmdUpdatePhysicsTargets().
      PushPhysicsTargets(timeS, state.targets);
    } else {
      RpcClientUpdatePhysicsState(timeS, state);
    }
  }

  //! Sends hand physics state to the server and all connected clients.
  //!
  //! @param timeS The world time that the state was generated.
  //! @param state The hand physics state being sent.
  [ClientRpc(channel = HxVersionCompatability.MirrorChannels.Unreliable)]
  void RpcClientUpdatePhysicsState(float timeS, HandPhysicsState state) {
    if (!IsPhysicsAuthority) {
      PushPhysicsState(timeS, state);
    }
  }

  //! Gets the physics states of all objects in our physics authority zone.
  //!
  //! @param [out] objectStates Populated with object states.
  void GetPhysicsStatesOfObjectsInAuthorityZone(ref ObjectPhysicsState[] objectStates) {
    List<ObjectPhysicsState> objectStatesList = new List<ObjectPhysicsState>();
    foreach (var keyValue in _objectsInPhysicsAuthorityZone) {
      NetworkIdentity objectIdentity = keyValue.Key;
      if (objectIdentity == null) {
        continue;
      }

      ObjectPhysicsState objectPhysicsState = new ObjectPhysicsState {
        networkIdentity = objectIdentity};
      RigidbodyState.GetRigidbodyStates(objectIdentity.gameObject,
          ref objectPhysicsState.rigidbodyStates);
      objectStatesList.Add(objectPhysicsState);
    }

    // Convert to array for network serialization purposes.
    objectStates = objectStatesList.ToArray();
  }

  //! Called when #_biologicalSex is synced.
  void BiologicalSexHook(HaptxApi.BiologicalSex oldValue, HaptxApi.BiologicalSex newValue) {
    LoadHandPrefab(newValue);
    SetSkinTone(_skinTone);
    SetUserMiddleFingerLengthM(_userMiddleFingerLengthM);
  }

  //! Called when #_skinTone is synced.
  void SkinToneHook(HaptxApi.SkinTone oldValue, HaptxApi.SkinTone newValue) {
    SetSkinTone(newValue);
  }

  //! Called when #_userMiddleFingerLengthM is synced.
  void UserMiddleFingerLengthMHook(float oldValue, float newValue) {
    SetUserMiddleFingerLengthM(newValue);
  }

  //! Called when #handScaleFactor is synced.
  void HandScaleFactorHook(float oldValue, float newValue) {
    HandScaleFactor = newValue;
  }

  //! Called when #_isClientPhysicsAuthority is synced.
  void IsClientPhysicsAuthorityHook(bool oldValue, bool newValue) {
    if (IsPhysicsAuthority) {
      if (isServer) {
        _physicsTargetsBufferTailIndex = 0;
        _physicsTargetsBufferHeadIndex = 0;
        _physicsTargetsBufferStarted = false;
      }
    } else if (hasAuthority) {
      _physicsStateBufferTailIndex = 0;
      _physicsStateBufferHeadIndex = 0;
      _physicsStateBufferStarted = false;
    }
  }

  //! Gets a list of GameObject names indexed by the HaptxApi::Finger and
  //! HaptxApi::FingerJoint values that they correspond to.
  //!
  //! @returns A list of GameObject names indexed by the HaptxApi::Finger and HaptxApi::FingerJoint
  //! values that they correspond to.
  HxName[,] GetNameFromHandJoint() {
    return new HxName[,] {
      {boneNames.thumb1, boneNames.thumb2, boneNames.thumb3},
      {boneNames.index1, boneNames.index2, boneNames.index3},
      {boneNames.middle1, boneNames.middle2, boneNames.middle3},
      {boneNames.ring1, boneNames.ring2, boneNames.ring3},
      {boneNames.pinky1, boneNames.pinky2, boneNames.pinky3}
    };
  }

  //! Gets a list of GameObject names indexed by the HaptxApi::Finger and
  //! HaptxApi::FingerBone values that they correspond to.
  //!
  //! @returns A list of GameObject names indexed by the HaptxApi::Finger and HaptxApi::FingerBone
  //! values that they correspond to.
  HxName[,] GetNameFromHandBone() {
    return new HxName[,] {
      {boneNames.thumb1, boneNames.thumb2, boneNames.thumb3},
      {boneNames.index1, boneNames.index2, boneNames.index3},
      {boneNames.middle1, boneNames.middle2, boneNames.middle3},
      {boneNames.ring1, boneNames.ring2, boneNames.ring3},
      {boneNames.pinky1, boneNames.pinky2, boneNames.pinky3}
    };
  }

  //! Get fingertip names indexed by HaptxApi::Finger.
  //!
  //! @returns Fingertip names indexed by HaptxApi::Finger.
  HxName[] GetFingertipNameFromFinger() {
    return new HxName[] {boneNames.thumb4, boneNames.index4, boneNames.middle4, boneNames.ring4,
        boneNames.pinky4};
  }

  //! All of the segments expected in a complete hand. Each value represents the name of the
  //! game object that corresponds to that segment.
  [Serializable]
  class HandBoneNames {

    //! The name of the object representing the proximal segment of the thumb.
    [Tooltip("The name of the object representing the proximal segment of the thumb.")]
    public HxName thumb1 = new HxName("thumb1");
    //! The name of the object representing the medial segment of the thumb.
    [Tooltip("The name of the object representing the medial segment of the thumb.")]
    public HxName thumb2 = new HxName("thumb2");
    //! The name of the object representing the distal segment of the thumb.
    [Tooltip("The name of the object representing the distal segment of the thumb.")]
    public HxName thumb3 = new HxName("thumb3");
    //! The name of the object representing the tip of the thumb.
    [Tooltip("The name of the object representing the tip of the thumb.")]
    public HxName thumb4 = new HxName("thumb4");
    //! The name of the object representing the proximal segment of the index finger.
    [Tooltip("The name of the object representing the proximal segment of the index finger.")]
    public HxName index1 = new HxName("index1");
    //! The name of the object representing the medial segment of the index finger.
    [Tooltip("The name of the object representing the medial segment of the index finger.")]
    public HxName index2 = new HxName("index2");
    //! The name of the object representing the distal segment of the index finger.
    [Tooltip("The name of the object representing the distal segment of the index finger.")]
    public HxName index3 = new HxName("index3");
    //! The name of the object representing the tip of the index finger.
    [Tooltip("The name of the object representing the tip of the index finger.")]
    public HxName index4 = new HxName("index4");
    //! The name of the object representing the proximal segment of the middle finger.
    [Tooltip("The name of the object representing the proximal segment of the middle finger.")]
    public HxName middle1 = new HxName("middle1");
    //! The name of the object representing the medial segment of the middle finger.
    [Tooltip("The name of the object representing the medial segment of the middle finger.")]
    public HxName middle2 = new HxName("middle2");
    //! The name of the object representing the distal segment of the middle finger.
    [Tooltip("The name of the object representing the distal segment of the middle finger.")]
    public HxName middle3 = new HxName("middle3");
    //! The name of the object representing the tip of the middle finger.
    [Tooltip("The name of the object representing the tip of the middle finger.")]
    public HxName middle4 = new HxName("middle4");
    //! The name of the object representing the proximal segment of the ring finger.
    [Tooltip("The name of the object representing the proximal segment of the ring finger.")]
    public HxName ring1 = new HxName("ring1");
    //! The name of the object representing the medial segment of the ring finger.
    [Tooltip("The name of the object representing the medial segment of the ring finger.")]
    public HxName ring2 = new HxName("ring2");
    //! The name of the object representing the distal segment of the ring finger.
    [Tooltip("The name of the object representing the distal segment of the ring finger.")]
    public HxName ring3 = new HxName("ring3");
    //! The name of the object representing the tip of the ring finger.
    [Tooltip("The name of the object representing the tip of the ring finger.")]
    public HxName ring4 = new HxName("ring4");
    //! The name of the object representing the proximal segment of the pinky finger.
    [Tooltip("The name of the object representing the proximal segment of the pinky finger.")]
    public HxName pinky1 = new HxName("pinky1");
    //! The name of the object representing the medial segment of the pinky finger.
    [Tooltip("The name of the object representing the medial segment of the pinky finger.")]
    public HxName pinky2 = new HxName("pinky2");
    //! The name of the object representing the distal segment of the pinky finger.
    [Tooltip("The name of the object representing the distal segment of the pinky finger.")]
    public HxName pinky3 = new HxName("pinky3");
    //! The name of the object representing the tip of the pinky finger.
    [Tooltip("The name of the object representing the tip of the pinky finger.")]
    public HxName pinky4 = new HxName("pinky4");
    //! The name of the object representing the palm.
    [Tooltip("The name of the object representing the palm.")]
    public HxName palm = new HxName("root");
  }

  //! Holds the retractuator parameters that get sent to the
  //! HaptxApi::ContactInterpreter for each finger.
  [Serializable]
  class AllRetractuatorParameters {

    //! Retractuator parameters for the thumb.
    [Tooltip("Retractuator parameters for the thumb.")]
    public RetractuatorParameters thumb = new RetractuatorParameters();
    //! Retractuator parameters for the index.
    [Tooltip("Retractuator parameters for the index.")]
    public RetractuatorParameters index = new RetractuatorParameters();
    //! Retractuator parameters for the middle.
    [Tooltip("Retractuator parameters for the middle.")]
    public RetractuatorParameters middle = new RetractuatorParameters();
    //! Retractuator parameters for the ring.
    [Tooltip("Retractuator parameters for the ring.")]
    public RetractuatorParameters ring = new RetractuatorParameters();
    //! Retractuator parameters for the pinky.
    [Tooltip("Retractuator parameters for the pinky.")]
    public RetractuatorParameters pinky = new RetractuatorParameters();

    //! Get the retractuator parameters corresponding to a given finger.
    //!
    //! @param finger Which finger.
    //! @returns The corresponding parameters.
    public RetractuatorParameters getParametersForFinger(HaptxApi.Finger finger) {
      switch (finger) {
        case HaptxApi.Finger.F_THUMB:
          return thumb;
        case HaptxApi.Finger.F_INDEX:
          return index;
        case HaptxApi.Finger.F_MIDDLE:
          return middle;
        case HaptxApi.Finger.F_RING:
          return ring;
        case HaptxApi.Finger.F_PINKY:
          return pinky;
        default:
          return null;
      }
    }
  }

  //! The targets of all constraints driving the hand.
  [Serializable]
  public struct HandPhysicsTargets {

    //! The position target of the constraint driving the palm.
    public Vector3 wMiddle1PosM;

    //! The orientation target of the constraint driving the palm.
    public Quaternion wMiddle1Orient;

    //! @brief The orientation targets of the constraints driving the finger segments.
    //!
    //! Indexed by HaptxApi.HandJoint.
    public Quaternion[] lJointOrients;

    //! @brief Interpolate between two physics targets.
    //!
    //! Vectors are linearly interpolated and Quaternions are spherically interpolated.
    //!
    //! @param a Physics targets for @p alpha = 0.
    //! @param b Physics targets for @p alpha = 1.
    //! @param alpha Interpolation alpha.
    public static HandPhysicsTargets Interpolate(HandPhysicsTargets a, HandPhysicsTargets b,
        float alpha) {
      int minNum = Math.Min(
          a.lJointOrients != null ? a.lJointOrients.Length : 0,
          b.lJointOrients != null ? b.lJointOrients.Length : 0);
      Quaternion[] lJointOrients = new Quaternion[minNum];
      for (int i = 0; i < minNum; i++) {
        lJointOrients[i] = Quaternion.Slerp(a.lJointOrients[i], b.lJointOrients[i], alpha);
      }
      return new HandPhysicsTargets {
        wMiddle1PosM = Vector3.Lerp(a.wMiddle1PosM, b.wMiddle1PosM, alpha),
        wMiddle1Orient = Quaternion.Slerp(a.wMiddle1Orient, b.wMiddle1Orient, alpha).normalized,
        lJointOrients = lJointOrients
    };
    }
  }

  //! A timestamped HandPhysicsTargets.
  [Serializable]
  struct HandPhysicsTargetsFrame {

    //! The time stamp of the frame.
    public float timeS;

    //! The frame itself.
    public HandPhysicsTargets targets;
  }

  //! The physics targets for each constraint driving the hand, the physics state of each
  //! FBodyInstance in the hand, and the physics state of each object near the hand.
  [Serializable]
  public struct HandPhysicsState {

    //! The physics state of each Rigidbody in the hand.
    public RigidbodyState[] wBodyStates;

    //! The physics targets for each constraint driving the hand.
    public HandPhysicsTargets targets;

    //! The physics state of each object near the hand.
    public ObjectPhysicsState[] wObjectStates;

    //! @brief Interpolate between two hand physics states.
    //!
    //! Vectors are linearly interpolated and Quaternions are spherically interpolated.
    //!
    //! @param a Physics state for @p alpha = 0.
    //! @param b Physics state for @p alpha = 1.
    //! @param alpha Interpolation alpha.
    public static HandPhysicsState Interpolate(HandPhysicsState a, HandPhysicsState b,
        float alpha) {
      int minLength = Math.Min(
          a.wBodyStates != null ? a.wBodyStates.Length : 0,
          b.wBodyStates != null ? b.wBodyStates.Length : 0);
      RigidbodyState[] wBodyStates = new RigidbodyState[minLength];
      for (int i = 0; i < minLength; i++) {
        wBodyStates[i] = RigidbodyState.Interpolate(a.wBodyStates[i], b.wBodyStates[i], alpha);
      }

      List<ObjectPhysicsState> wObjectStatesList = new List<ObjectPhysicsState>();
      if (a.wObjectStates != null && a.wObjectStates.Length > 0 && b.wObjectStates != null &&
          b.wObjectStates.Length > 0) {
        for (int a_i = 0; a_i < a.wObjectStates.Length; a_i++) {
          for (int b_i = 0; b_i < b.wObjectStates.Length; b_i++) {
            if (a.wObjectStates[a_i].networkIdentity == b.wObjectStates[b_i].networkIdentity &&
                a.wObjectStates[a_i].rigidbodyStates.Length ==
                b.wObjectStates[b_i].rigidbodyStates.Length) {
              RigidbodyState[] rigidbodyStates =
                  new RigidbodyState[a.wObjectStates[a_i].rigidbodyStates.Length];
              for (int j = 0; j < a.wObjectStates[a_i].rigidbodyStates.Length; j++) {
                rigidbodyStates[j] = RigidbodyState.Interpolate(
                    a.wObjectStates[a_i].rigidbodyStates[j],
                    b.wObjectStates[b_i].rigidbodyStates[j], alpha);
              }

              wObjectStatesList.Add(new ObjectPhysicsState() {
                networkIdentity = a.wObjectStates[a_i].networkIdentity,
                rigidbodyStates = rigidbodyStates
              });
              break;
            }
          }
        }
      }

      return new HandPhysicsState() {
        wBodyStates = wBodyStates,
        targets = HandPhysicsTargets.Interpolate(a.targets, b.targets, alpha),
        wObjectStates = wObjectStatesList.ToArray()};
    }
  }

  //! A timestamped FHandPhysicsState.
  [Serializable]
  struct HandPhysicsStateFrame {

    //! The time stamp of the frame.
    public float timeS;

    //! The frame itself.
    public HandPhysicsState state;
  }

  //! Information about an object inside at least one hand's physics authority zone.
  class GlobalPhysicsAuthorityObjectData {

    //! A map from each player interacting with the object to how many physics authority zones from
    //! that player the object is currently inside.
    public Dictionary<GameObject, int> physicsAuthorityZoneCountFromPlayer =
        new Dictionary<GameObject, int>();
  };

  //! The physics information about a NetworkIdentity that HxHand needs to synchronize object
  //! interactions over a network.
  [Serializable]
  public struct ObjectPhysicsState {

    //! The owning NetworkIdentity.
    public NetworkIdentity networkIdentity;

    //! The states of all Rigidbodies owned by NetworkIdentity.
    public RigidbodyState[] rigidbodyStates;
  };
}
