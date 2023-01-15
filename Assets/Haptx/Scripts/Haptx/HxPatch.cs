// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//! @brief Represents a patch of Tactors. Responsible for tactile feedback and sampling the game
//! world for surface characteristics.
//!
//! See the @ref section_hx_patch_script "Unity Plugin Guide" for a high level overview.
//!
//! @note HxPatch must have a uniform lossyScale. Non-uniform lossyScales will produce undefined
//! behavior.
//!
//! @ingroup group_unity_plugin
public class HxPatch : MonoBehaviour, HxCore.IHxUpdate {
  [Header("Configurations")]

  //! The locating feature this patch gets aligned with.
  [SerializeField, Tooltip("The locating feature this patch gets aligned with.")]
  string _locatingFeature = string.Empty;

  //! A position offset applied to this patch in the locating feature's frame.
  [SerializeField, Tooltip(
      "A position offset applied to this patch in the locating feature's frame.")]
  Vector3 _locatingFeaturePositionOffset = Vector3.zero;

  //! A rotation offset applied to this patch in the locating feature's frame.
  [SerializeField, Tooltip(
      "A rotation offset applied to this patch in the locating feature's frame.")]
  Vector3 _locatingFeatureRotationOffset = Vector3.zero;

  //! A scale offset applied to this patch in the locating feature's frame.
  [SerializeField, Tooltip(
      "A scale offset applied to this patch in the locating feature's frame.")]
  Vector3 _locatingFeatureScaleOffset = Vector3.one;

  //! The set of coverage regions this patch represents in the HaptxApi.
  [SerializeField, Tooltip("The set of coverage regions this patch represents in the HaptxApi.")]
  string[] _coverageRegions = new string[0];

  [Header("Contact Interpreter")]

  //! Parameters to be sent to the contact interpreter to configure individual tactor
  //! behavior.
  [Tooltip("Parameters to be sent to the contact interpreter to configure individual tactor behavior.")]
  [SerializeField()]
  TactorParameters tactorParameters = null;

  [Header("Tracing")]

  //! The distance to trace outward from each sample point [m].
  [Tooltip("The distance to trace outward from each sample point [m]."), Range(0.0f, float.MaxValue)]
  public float objectTraceDistanceM = 0.01f;

  //! Amount by which a raytrace begins "inside" of an object to account for positional 
  //! tolerance stack-up and object interpenetration [m].
  [Tooltip("The amount by which a ray trace begins \"inside\" of an object to account for positional tolerance stack-up and object interpenetration [m].")]
  public float objectTraceOffsetM = 0.005f;

  //! Which directions this patch emits ray traces in tactor space.
  [Tooltip("Which directions this patch emits ray traces in tactor space.")]
  public Vector3[] tObjectTraceDirections = {Vector3.down};

  //! Which directions this patch emits ray traces in component space.
  [Tooltip("Which directions this patch emits ray traces in component space.")]
  public Vector3[] lObjectTraceDirections = {};

  //! The amount by which to increment self traces if they fail [m].
  [Tooltip("The amount by which to increment self traces if they fail [m]."),
      Range(0.0f, 0.001f)]
  public float selfTraceIncrementM = 0.001f;

  //! The maximum amount of self trace offset before giving up [m].
  [Tooltip("The maximum amount of self trace offset before giving up [m]."),
      Range(0.0f, 0.05f)]
  public float maxSelfTraceOffsetM = 0.03f;

  [Header("Visualization")]

  //! Visualize trace geometry.
  [Tooltip("Visualize trace geometry.")]
  public HxVisualizer traceVisualizer = new HxVisualizer(KeyCode.Alpha2, false, true, false);

  //! @brief Visualize tactile feedback state.
  //! 
  //! Visualizer displacements match hardware displacements multiplied by a configurable scale 
  //! factor.
  [Tooltip("Visualize tactile feedback state.")]
  public TactileFeedbackVisualizer tactileFeedbackVisualizer =
      new TactileFeedbackVisualizer(KeyCode.Alpha3, false, true, false);

  //! @brief Represents parameters used in the tactile feedback visualizer.
  //!
  //! This struct only exists for organizational purposes in the Inspector.
  [Serializable]
  public class TactileFeedbackVisualizer : HxVisualizer {
    //! Default constructor.
    //!
    //! @param key The key that toggles this visualizer.
    //! @param alt Whether alt also needs to be pressed to toggle this visualizer.
    //! @param shift Whether shift also needs to be pressed to toggle this visualizer.
    //! @param control Whether control also needs to be pressed to toggle this visualizer.
    public TactileFeedbackVisualizer(KeyCode key, bool alt, bool shift, bool control) :
        base(key, alt, shift, control) { }

    //! @brief The amount by which to scale tactor height targets.
    //!
    //! Increase to make tactile feedback visualizations move more when tactors actuate.
    [Tooltip("The amount by which to scale tactor height targets.")]
    public float heightTargetScale = 5.0f;

    //! The distance [m] which tactor output visualizations are offset from tactor 
    //! positions.
    [Tooltip("The distance [m] which tactor output visualizations are offset from tactor positions.")]
    public float drawOffset = 0.03f;
  }

  //! Size of the sphere traces in meters (can be set by hand this patch is attached to.)
  float _sphereTraceRadiusM = 0.00280625f;

  //! Reference to the HxCore pseudo-singleton.
  HxCore core = null;

  //! The UUID of the peripheral this patch is on.
  HaptxApi.HaptxUuid _peripheralId = null;

  //! Data associated with a specific tactor.
  class TactorData {

    //! The tactor itself.
    public HaptxApi.Tactor tactor;

    //! The tactor's simulation callbacks.
    public HxTransformCallbacks callbacks;
    
    //! Whether the following trace origin is valid.
    public bool traceOriginValid;

    //! The trace origin relative to this patch's game object.
    public Matrix4x4 lTraceOrigin;
  }

  //! The list of tactors this patch represents.
  List<TactorData> _tactorData = new List<TactorData>();

  //! @brief Does all initialization work.
  //!
  //! In Unreal the patches have enough context to call this function on their own; however, due to
  //! constraints imposed by Mirror's design we have to rely on owning objects to call this
  //! function. Namely, we can't call this function until network data is known and only
  //! NetworkBehaviors have the proper callbacks for determining when that is the case.
  public void Initialize() {
    if (!enabled) {
      return;
    }

    // Opens all HaptX interfaces
    core = HxCore.GetAndMaintainPseudoSingleton();
    if (core == null) {
      HardDisable();
      return;
    } else {
      core.RegisterHxUpdate(this);
    }

    HxHand hxHandParent = GetComponentInParent<HxHand>();
    if (hxHandParent == null) {
      HxDebug.LogError("HxPatch.Initialize(): Owner doesn't implement HxPatch.ISocket.",
          this);
      HardDisable();
      return;
    }

    RelDir relDir = hxHandParent.hand;
    string subbedLocatingFeature = HxShared.SubstituteRelDir(_locatingFeature, relDir);

    Matrix4x4 wLocatingFeature;
    if (!hxHandParent.TryGetLocatingFeatureTransform(subbedLocatingFeature,
        out wLocatingFeature)) {
      HxDebug.LogError(string.Format(
          "HxPatch.Initialize(): Failed to get transform for locating feature {0}.",
          subbedLocatingFeature), this);
      HardDisable();
      return;
    }

    // Align with locating game object.
    Matrix4x4 wTargetTransform = wLocatingFeature * Matrix4x4.TRS(_locatingFeaturePositionOffset,
        Quaternion.Euler(_locatingFeatureRotationOffset), _locatingFeatureScaleOffset);
    Matrix4x4 lTargetTransform = transform.parent.worldToLocalMatrix * wTargetTransform;
    transform.localPosition = lTargetTransform.MultiplyPoint3x4(Vector3.zero);
    transform.localRotation = lTargetTransform.rotation;
    transform.localScale = lTargetTransform.lossyScale;

    RegisterTactors();

    // Initialize the event to update the sphere traces
    if (GetComponentInParent<HxHand>() != null) {
      GetComponentInParent<HxHand>().OnShpereTraceRadiusChange += UpdateSphereTraceRadius;
    }
  }

  //! Called every frame by HxCore if enabled.
  public void HxUpdate() {
    // If we shouldn't be updating
    if (!enabled) {
      return;
    }

    UpdateTraces();
  }

  void LateUpdate() {
    // If we shouldn't be updating
    if (!enabled) {
      return;
    }

    tactileFeedbackVisualizer.Update();
    traceVisualizer.Update();

    if (tactileFeedbackVisualizer.visualize) {
      VisualizeTactileFeedbackOutput();
    }
  }

  public void OnDestroy() {
    if (core != null) {
      core.UnregisterHxUpdate(this);
    }
  }

  //! Load the tactor list with information provided by the HaptX API.
  void RegisterTactors() {
    if (core == null) {
      HxDebug.LogError(
          "HxPatch.RegisterTactors(): Null core.", this);
      HardDisable();
      return;
    }

    HxHand hxHandParent = GetComponentInParent<HxHand>();
    if (hxHandParent == null) {
      HxDebug.LogError(
          "HxPatch.RegisterTactors(): Owner doesn't have an HxHand.",
          this);
      HardDisable();
      return;
    }

    if (hxHandParent.Peripheral == null) {
      HxDebug.LogError(
          "HxPatch.RegisterTactors(): Null peripheral from owner.", this);
      HardDisable();
      return;
    }
    _peripheralId = hxHandParent.Peripheral.id;

    RelDir relDir = hxHandParent.hand;
    string subbedLocatingFeature = HxShared.SubstituteRelDir(_locatingFeature, relDir);
    HashSet<string> subbedCoverageRegions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (string coverageRegion in _coverageRegions) {
      string subbedCoverageRegion = HxShared.SubstituteRelDir(coverageRegion, relDir);
      if (!subbedCoverageRegions.Contains(subbedCoverageRegion)) {
        subbedCoverageRegions.Add(subbedCoverageRegion);
      }
    }

    Matrix4x4 wLocatingFeature;
    string locatingFeature = HxShared.SubstituteRelDir(_locatingFeature, relDir);
    if (!hxHandParent.TryGetLocatingFeatureTransform(locatingFeature, out wLocatingFeature)) {
      HxDebug.LogError(string.Format(
          "HxPatch.Initialize(): Failed to get transform for locating feature {0}.",
          locatingFeature), this);
      HardDisable();
      return;
    }

    long ciBodyId;
    if (!hxHandParent.TryGetCiBodyId(this, out ciBodyId)) {
      return;
    }

    foreach (HaptxApi.Tactor tactor in hxHandParent.Peripheral.tactors) {
      if (string.Equals(subbedLocatingFeature, tactor.parent.getText(),
          StringComparison.OrdinalIgnoreCase) &&
          subbedCoverageRegions.Contains(tactor.coverage_region.getText())) {
        HxTransformCallbacks callbacks = new HxTransformCallbacks(transform,
            HxShared.UnityFromHx(tactor.transform));
        _tactorData.Add(new TactorData(){
          tactor = tactor,
          callbacks = callbacks,
          traceOriginValid = false,
          lTraceOrigin = Matrix4x4.identity
        });

       core.ContactInterpreter.registerTactor(hxHandParent.Peripheral.id, tactor,
            tactorParameters.Unwrap(), ciBodyId, callbacks);
      }
    }
  }

  //! Computes and caches trace origins on the surface of a given skinned mesh renderer (the hand).
  //!
  //! @param smr The mesh to trace against.
  public void UpdateTraceOrigins(SkinnedMeshRenderer smr) {
    if (smr == null) {
      return;
    }

    // Reset the pose of the hand.
    for (int i = 0; i < smr.bones.Length; i++) {
      Matrix4x4 wBone = smr.rootBone.localToWorldMatrix * smr.sharedMesh.bindposes[i].inverse;
      smr.bones[i].position = wBone.MultiplyPoint3x4(Vector3.zero);
      smr.bones[i].rotation = wBone.rotation;
    }

    // Bake the base pose of the hand to a static mesh.
    Mesh mesh = new Mesh();
    smr.BakeMesh(mesh);
    Vector3[] wVertices = mesh.vertices.Select(
        x => smr.localToWorldMatrix.MultiplyPoint3x4(x)).ToArray();
    mesh.vertices = wVertices;
    mesh.RecalculateBounds();

    // Create a temporary mesh collider.
    GameObject tempObject = new GameObject();
    MeshCollider collider = tempObject.AddComponent<MeshCollider>();
    collider.sharedMesh = mesh;

    // Trace against the mesh from each tactor and cache the location that was hit. This is the
    // best approximation of where the "nerves" on the avatar hand are located that engage
    // corresponding tactors.
    foreach (var tactorDatum in _tactorData) {
      tactorDatum.traceOriginValid = false;

      if (tactorDatum.callbacks == null) {
        HxDebug.LogWarning(string.Format(
            "HxPatch.UpdateTraceOrigins(): Null callbacks for tactor {0}.",
            tactorDatum.tactor.id), this);
        continue;
      }

      Matrix4x4 wTactor = tactorDatum.callbacks.GetUnityWorldTransform();
      Vector3 wTactorPositionM = wTactor.MultiplyPoint3x4(Vector3.zero);
      Vector3 wTraceDirection = wTactor.MultiplyVector(Vector3.down).normalized;

      // Start with really short traces about the tactor origin and walk outward until we either
      // hit the hand or reach our max trace length.
      float traceOffsetM = selfTraceIncrementM;
      while (traceOffsetM <= maxSelfTraceOffsetM) {
        Vector3 wSelfTraceStart = wTactorPositionM + Vector3.Scale(transform.lossyScale,
            traceOffsetM * wTraceDirection);
        Vector3 wSelfTraceDelta = -Vector3.Scale(transform.lossyScale,  
            (2.0f * traceOffsetM) * wTraceDirection);

        Ray ray = new Ray(wSelfTraceStart, wSelfTraceDelta.normalized);
        RaycastHit hit;
        tactorDatum.traceOriginValid = collider.Raycast(ray, out hit, wSelfTraceDelta.magnitude);
        if (tactorDatum.traceOriginValid) {
          Matrix4x4 lTactor = transform.worldToLocalMatrix * wTactor;
          tactorDatum.lTraceOrigin =
              Matrix4x4.TRS(transform.worldToLocalMatrix.MultiplyPoint3x4(hit.point),
              lTactor.rotation, lTactor.lossyScale);
          break;
        } else {
          traceOffsetM += selfTraceIncrementM;
        }
      }
    }

    // I would like to return the SkinnedMeshRenderer to its original pose here; however, that just
    // seems to utterly wreck the physics engine so I can't do it.

    Destroy(tempObject);
  }

  //! The function called when the shpere traces are updated by the hand
  void UpdateSphereTraceRadius(HxHand callingHand, float newRadius) {
        // Only patches attached to the calling hand are updated
        if (GetComponentInParent<HxHand>() != null && GetComponentInParent<HxHand>() == callingHand) {
            _sphereTraceRadiusM = newRadius;
        }
  }

  // This array will hold the complete set of ray traces in world space.
  private Vector3[] _wTraceDirections = null;

  //! Generates trace results and sends them to the HaptxApi::ContactInterpreter.
  void UpdateTraces() {
    //! The thickness to draw trace meshes.
    const float LTraceThicknessM = 0.0007f;
    //! The color to draw with if we hit an object.
    Color ObjectHitColor = HxShared.DebugPurpleOrTeal;
    //! The color to draw with if we hit nothing.
    Color ObjectMissColor = HxShared.DebugBlack;
    //! The color to draw the true tactor location.
    Color TactorColor = HxShared.DebugWhite;
    //! The radius to draw the sphere at the true tactor location.
    float TactorRadiusM = 0.001f;

    // If we won't be able to sample anything
    if ((transform.lossyScale.x == 0 && transform.lossyScale.y == 0 && transform.lossyScale.z == 0)
        || (objectTraceOffsetM + objectTraceDistanceM) == 0) {
      return;
    }

    // Populate the component trace section of the array.
    if (_wTraceDirections == null || _wTraceDirections.Length !=
        lObjectTraceDirections.Length + tObjectTraceDirections.Length) {
      _wTraceDirections =
          new Vector3[lObjectTraceDirections.Length + tObjectTraceDirections.Length];
    }
    for (int i = 0; i < lObjectTraceDirections.Length; i++) {
      _wTraceDirections[i] = transform.localToWorldMatrix.MultiplyVector(
          lObjectTraceDirections[i]).normalized;
    }

    foreach (TactorData tactorDatum in _tactorData) {
      if (tactorDatum.tactor == null) {
        HxDebug.LogWarning("HxPatch.UpdateTraces(): Null tactor.", this);
        continue;
      }

      if (!tactorDatum.traceOriginValid) {
        continue;
      }
      
      // Cached trace origin information from self trace.
      Matrix4x4 wTraceOrigin = transform.localToWorldMatrix * tactorDatum.lTraceOrigin;
      Vector3 wTraceOriginPosM = wTraceOrigin.MultiplyPoint3x4(Vector3.zero);

      // Populate the tactor trace section of the array.
      for (int i = 0; i < tObjectTraceDirections.Length; i++) {
        _wTraceDirections[lObjectTraceDirections.Length + i] =
            wTraceOrigin.MultiplyVector(tObjectTraceDirections[i]).normalized;
      }

      foreach (Vector3 wTraceDirection in _wTraceDirections) {
        // Object trace characterization.
        Vector3 wObjectTraceStart = wTraceOriginPosM - Vector3.Scale(transform.lossyScale,
            (objectTraceOffsetM + _sphereTraceRadiusM) * wTraceDirection);
        Vector3 wObjectTraceDelta = Vector3.Scale(transform.lossyScale, (objectTraceOffsetM +
            objectTraceDistanceM + _sphereTraceRadiusM) * wTraceDirection);
        float wObjectTraceDeltaMag = wObjectTraceDelta.magnitude;

        // Theoretically we should be taking the component average here; however, non-uniformly
        // scaled patches isn't an expected operating mode so the added complexity isn't worth it in
        // the author's opinion.
        float scaledTraceRadiusM = Math.Abs(transform.lossyScale.x) * _sphereTraceRadiusM;

        bool objectHit = false;
        if (scaledTraceRadiusM > 0.0f) {
          if (Physics.SphereCast(wObjectTraceStart, scaledTraceRadiusM, wObjectTraceDelta /
              wObjectTraceDeltaMag, out RaycastHit objectTraceResult, wObjectTraceDeltaMag,
              core.tactileFeedbackLayers.value) &&
              core.TryRegisterCollider(objectTraceResult.collider, false, out long ciObjectId)) {

            objectHit = true;
            InformContactInterpreterOfResult(wTraceDirection, objectTraceResult, tactorDatum, ciObjectId, wTraceOriginPosM);

            if (traceVisualizer.visualize) {
              // Map the visualizer radius between the minimum visible size and the maximum trace radius
              // The visualizer will be more inaccurate in relation to the size of the actual trace the smaller it gets,
              // to a maximum error of 1.6 millimeter radius when the true radius is 0 millimeters 
              float visualizerRadiusM = Mathf.InverseLerp(0.0016f, 0.004f, scaledTraceRadiusM);
              visualizerRadiusM = Mathf.Clamp01(visualizerRadiusM);
              visualizerRadiusM = Mathf.Lerp(0.0016f, 0.004f, visualizerRadiusM);
              // Draw the point where the object was hit.
              HxDebugMesh.DrawSphere(wObjectTraceStart +
                (objectTraceResult.distance * wTraceDirection), Quaternion.identity,
                2.0f * visualizerRadiusM * Vector3.one, ObjectHitColor);
            }
          }
        }
        else {
          if (Physics.Raycast(wObjectTraceStart, wObjectTraceDelta /
              wObjectTraceDeltaMag, out RaycastHit objectTraceResult, wObjectTraceDeltaMag,
              core.tactileFeedbackLayers.value) &&
              core.TryRegisterCollider(objectTraceResult.collider, false, out long ciObjectId)) {

            objectHit = true;
            InformContactInterpreterOfResult(wTraceDirection, objectTraceResult, tactorDatum, ciObjectId, wTraceOriginPosM);

            if (traceVisualizer.visualize) {
              // Draw the point where the object was hit.
              HxDebugMesh.DrawSphere(wObjectTraceStart +
                (objectTraceResult.distance * wTraceDirection), Quaternion.identity,
                2.0f * 0.0016f * Vector3.one, ObjectHitColor);
            }
          }
        }
        if (traceVisualizer.visualize) {
          Color drawColor = objectHit ? ObjectHitColor : ObjectMissColor;

          // Draw a line representing the trace toward objects.
          HxDebugMesh.DrawLine(wObjectTraceStart, wObjectTraceStart + wObjectTraceDelta,
              transform.lossyScale.magnitude * LTraceThicknessM, drawColor);

          // Draw a sphere representing the trace origin.
          HxDebugMesh.DrawSphere(wTraceOriginPosM, Quaternion.identity,
              2.0f * scaledTraceRadiusM * Vector3.one, drawColor);

          if (tactorDatum.callbacks != null) {
            Matrix4x4 wTactor = tactorDatum.callbacks.GetUnityWorldTransform();

            // Draw the true tactor location for completeness.
            HxDebugMesh.DrawSphere(wTactor.MultiplyPoint3x4(Vector3.zero), wTactor.rotation,
                2.0f * TactorRadiusM * transform.lossyScale, TactorColor);
          }
        }
      }
    }
  }

  //! Send the trace results to the HaptxApi::ContactInterpreter.
  void InformContactInterpreterOfResult(Vector3 wTraceDirection, RaycastHit objectTraceResult, TactorData tactorDatum, long ciObjectId, Vector3 wTraceOriginPosM) {
    
    HaptxApi.Vector3D hxTraceDirection = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(wTraceDirection, hxTraceDirection);
    HaptxApi.Vector3D hxObjectHitPoint = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(objectTraceResult.point, hxObjectHitPoint);
    HaptxApi.Vector3D hxNormal = HxReusableObjectPool<HaptxApi.Vector3D>.Get();
    HxShared.HxFromUnity(objectTraceResult.normal, hxNormal);

    VectorOfVector2D vv2D = HxReusableObjectPool<VectorOfVector2D>.Get();
    vv2D.Clear();

    if (objectTraceResult.collider is MeshCollider meshCollider) {
      using (var wrappedHxUV1 = HxReusableObjectPool<HaptxApi.Vector2D>.GetWrapped()) {
        HaptxApi.Vector2D hxUV1 = wrappedHxUV1.ReusableObject;
        HxShared.HxFromUnity(objectTraceResult.textureCoord, hxUV1);
        vv2D.Add(hxUV1);
      }

      if ((meshCollider.sharedMesh?.uv2?.Length ?? 0) != 0 ) {
        using (var wrappedHxUV2 = HxReusableObjectPool<HaptxApi.Vector2D>.GetWrapped()) {
          HaptxApi.Vector2D hxUV2 = wrappedHxUV2.ReusableObject;
          HxShared.HxFromUnity(objectTraceResult.textureCoord2, hxUV2);
        }
      }
    }

    core.ContactInterpreter.addSampleResult(_peripheralId, tactorDatum.tactor.id,
      hxTraceDirection, ciObjectId,
      Mathf.Max(0.0f, Vector3.Dot(objectTraceResult.point - wTraceOriginPosM,
      wTraceDirection)), hxObjectHitPoint, hxNormal, vv2D);
    HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxTraceDirection);
    HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxObjectHitPoint);
    HxReusableObjectPool<HaptxApi.Vector3D>.Release(hxNormal);
    HxReusableObjectPool<VectorOfVector2D>.Release(vv2D);
  }

  //! Draw tactile feedback visualization information.
  void VisualizeTactileFeedbackOutput() {
    //! The default size to draw tactor visualizations when they are not conveying 
    //! information.
    Vector3 LSizeM = new Vector3(0.003f, 0.0015f, 0.003f);
    //! The color to draw tactor output visualizations when tactors are not engaged.
    Color ClosedColor = HxShared.DebugBlack;
    //! The color to draw tactor output visualizations when tactors are engaged, and at their 
    //! lower height target bound.
    Color OpenColorLow = HxShared.DebugBlueOrYellow;
    //! The color to draw tactor output visualizations when tactors are open, and at their 
    //! upper height target bound. A LERP is applied between this value and TA_COLOR_OPEN_LOW based 
    //! on height target.
    Color OpenColorHigh = HxShared.DebugPurpleOrTeal;

    // Draw tactor output visualizations offset from tactor positions so that they can be seen even
    // during contact.
    Vector3 offsetDirection = transform.up;
    Vector3 lTactorVizOffset = tactileFeedbackVisualizer.drawOffset * offsetDirection;
    foreach (TactorData tactorDatum in _tactorData) {
      HaptxApi.Tactor tactor = tactorDatum.tactor;
      if (tactor == null) {
        HxDebug.LogWarning("HxPatch.VisualizeTactileFeedbackOutput(): Null tactor.", this);
        continue;
      }

      if (tactorDatum.callbacks == null) {
        HxDebug.LogWarning(string.Format(
            "HxPatch.VisualizeTactileFeedbackOutput(): Null callbacks for tactor {0}.",
            tactorDatum.tactor.id), this);
        continue;
      }

      Matrix4x4 wTactor = tactorDatum.callbacks.GetUnityWorldTransform();
      Vector3 wTactorPositionM = wTactor.MultiplyPoint3x4(Vector3.zero);
      Quaternion wTactorRotation = wTactor.rotation;

      // Offset position and color based on height target.
      float lTactorHeightTargetM = 0.0f;
      if (core.ContactInterpreter.tryGetTactorHeightTargetM(_peripheralId, tactor.id,
          ref lTactorHeightTargetM)) {
        lTactorHeightTargetM = Mathf.Clamp(lTactorHeightTargetM, tactor.height_min_m,
            tactor.height_max_m);
      }
      Vector3 lTactorHeightTargetOffsetM = tactileFeedbackVisualizer.heightTargetScale *
          lTactorHeightTargetM * (wTactorRotation * Vector3.up);
      Color tactorVizColor = ClosedColor;
      if (lTactorHeightTargetM > 0.0f) {
        tactorVizColor = Color.Lerp(OpenColorLow, OpenColorHigh, lTactorHeightTargetM /
            tactor.height_max_m);
      }

      HxDebugMesh.DrawCube(
          wTactorPositionM +
              Vector3.Scale(transform.lossyScale, lTactorVizOffset + lTactorHeightTargetOffsetM),
          wTactorRotation,
          Vector3.Scale(transform.lossyScale, LSizeM),
          tactorVizColor);
    }
  }

  //! Disable the functionality of this class.
  void HardDisable() {
    HxDebug.LogRestartMessage();
    enabled = false;
  }
}
