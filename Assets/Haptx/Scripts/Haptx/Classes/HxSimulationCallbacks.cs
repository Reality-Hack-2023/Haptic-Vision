// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! Callbacks associated with Rigidbodies being used by the HaptX SDK.
class HxRigidbodyCallbacks : HaptxApi.SimulationCallbacks {

  //! The associated Rigidbody.
  private readonly Rigidbody _rigidbody = null;

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Vector3D _positionM = HxReusableObjectPool<HaptxApi.Vector3D>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Quaternion _rotation = HxReusableObjectPool<HaptxApi.Quaternion>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Vector3D _lossyScale = HxReusableObjectPool<HaptxApi.Vector3D>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Transform _transform = HxReusableObjectPool<HaptxApi.Transform>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Vector3D _linearVelocityM_S = HxReusableObjectPool<HaptxApi.Vector3D>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Vector3D _angularVelocityRad_S = HxReusableObjectPool<HaptxApi.Vector3D>.Get();

  //! Associate a Rigidbody with a set of HaptX SDK callbacks.
  //!
  //! @param rigidbody The rigidbody to associate these callbacks with.
  public HxRigidbodyCallbacks(Rigidbody rigidbody) {
    _rigidbody = rigidbody;
  }

  //! Gets the world position of the Rigidbody's center of mass.
  //!
  //! @returns The world position of the Rigidbody's center of mass.
  public override HaptxApi.Vector3D getPositionM() {
    if (_rigidbody != null) {
      HxShared.HxFromUnity(_rigidbody.worldCenterOfMass, _positionM);
    }
    return _positionM;
  }

  //! Gets the world rotation of the Rigidbody.
  //!
  //! @returns The world rotation of the Rigidbody.
  public override HaptxApi.Quaternion getRotation() {
    if (_rigidbody != null) {
      HxShared.HxFromUnity(_rigidbody.rotation, _rotation);
    }
    return _rotation;
  }

  //! Gets the lossy scale of the Rigidbody.
  //!
  //! @returns The lossy scale of the Rigidbody.
  public override HaptxApi.Vector3D getLossyScale() {
    if (_rigidbody != null) {
      HxShared.HxFromUnityScale(_rigidbody.transform.lossyScale, _lossyScale);
    }
    return _lossyScale;
  }

  //! Gets the local-to-world matrix of the Rigidbody.
  //!
  //! @returns The local-to-world matrix of the Rigidbody.
  public override HaptxApi.Transform getTransform() {
    if (_rigidbody != null) {
      Matrix4x4 wTransform =
          _rigidbody.transform.localToWorldMatrix * Matrix4x4.Translate(_rigidbody.centerOfMass);
      HxShared.HxFromUnity(wTransform, _transform);
    }
    return _transform;
  }

  //! Gets the world linear velocity of the Rigidbody.
  //!
  //! @returns The world linear velocity of the Rigidbody.
  public override HaptxApi.Vector3D getLinearVelocityM_S() {
    if (_rigidbody != null) {
      HxShared.HxFromUnity(_rigidbody.velocity, _linearVelocityM_S);
    }
    return _linearVelocityM_S;
  }

  //! Gets the world angular velocity of the Rigidbody.
  //!
  //! @returns The world angular velocity of the Rigidbody.
  public override HaptxApi.Vector3D getAngularVelocityRad_S() {
    if (_rigidbody != null) {
      HxShared.HxFromUnityAngularVelocity(_rigidbody.angularVelocity, _angularVelocityRad_S);
    }
    return _angularVelocityRad_S;
  }
}

//! Callbacks associated with Transforms being used by the HaptX SDK.
class HxTransformCallbacks : HaptxApi.SimulationCallbacks {

  //! The associated Transform.
  private readonly Transform _transform = null;

  //! The Rigidbody that this transform follows (if any).
  private readonly Rigidbody _rigidbody = null;

  //! The local transform to track in #_transform's frame.
  private readonly Matrix4x4 _lTransform = Matrix4x4.identity;

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Vector3D _positionM = HxReusableObjectPool<HaptxApi.Vector3D>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Quaternion _rotation = HxReusableObjectPool<HaptxApi.Quaternion>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Vector3D _lossyScale = HxReusableObjectPool<HaptxApi.Vector3D>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Transform _hxTransform = HxReusableObjectPool<HaptxApi.Transform>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Vector3D _linearVelocityM_S = HxReusableObjectPool<HaptxApi.Vector3D>.Get();

  //! Re-used to avoid unnecessary GC allocations caused by SWIG.
  private readonly HaptxApi.Vector3D _angularVelocityRad_S = HxReusableObjectPool<HaptxApi.Vector3D>.Get();

  //! Associate a Transform with a set of HaptX SDK callbacks.
  //!
  //! @param transform The transform to associate these callbacks with.
  //! @param lTransform The local transform to track in @p transform's frame.
  public HxTransformCallbacks(Transform transform, Matrix4x4 lTransform) {
    _transform = transform;
    if (_transform != null) {
      _rigidbody = transform.GetComponentInParent<Rigidbody>();
    }
    _lTransform = lTransform;
  }

  //! Gets the world position of the transform.
  //!
  //! @returns The world position of the transform.
  public override HaptxApi.Vector3D getPositionM() {
    if (_transform != null) {
      HxShared.HxFromUnity(GetUnityWorldTransform().MultiplyPoint3x4(Vector3.zero), _positionM);
    }
    return _positionM;
  }

  //! Gets the world rotation of the transform.
  //!
  //! @returns The world rotation of the transform.
  public override HaptxApi.Quaternion getRotation() {
    if (_transform != null) {
      HxShared.HxFromUnity(GetUnityWorldTransform().rotation, _rotation);
    }
    return _rotation;
  }

  //! Gets the lossy scale of the transform.
  //!
  //! @returns The lossy scale of the transform.
  public override HaptxApi.Vector3D getLossyScale() {
    if (_transform != null) {
      HxShared.HxFromUnityScale(GetUnityWorldTransform().lossyScale, _lossyScale);
    }
    return _lossyScale;
  }

  //! Gets the local-to-world matrix of the transform.
  //!
  //! @returns The local-to-world matrix of the transform.
  public override HaptxApi.Transform getTransform() {
    if (_transform != null) {
      HxShared.HxFromUnity(GetUnityWorldTransform(), _hxTransform);
    }
    return _hxTransform;
  }

  //! Gets the world linear velocity of the Rigidbody the transform is attached to.
  //!
  //! @returns The world linear velocity of the Rigidbody the transform is attached to.
  public override HaptxApi.Vector3D getLinearVelocityM_S() {
    if (_rigidbody != null) {
      HxShared.HxFromUnity(_rigidbody.GetPointVelocity(
          GetUnityWorldTransform().MultiplyPoint3x4(Vector3.zero)), _linearVelocityM_S);
    }
    return _linearVelocityM_S;
  }

  //! Gets the world angular velocity of the Rigidbody the transform is attached to.
  //!
  //! @returns The world angular velocity of the Rigidbody the transform is attached to.
  public override HaptxApi.Vector3D getAngularVelocityRad_S() {
    if (_rigidbody != null) {
      HxShared.HxFromUnityAngularVelocity(_rigidbody.angularVelocity, _angularVelocityRad_S);
    }
    return _angularVelocityRad_S;
  }

  //! Get the world transform being used by these callbacks.
  //!
  //! @returns The world transform being used by these callbacks.
  public Matrix4x4 GetUnityWorldTransform() {
    return _transform.localToWorldMatrix* _lTransform;
  }

  //! Get the local transform being used by these callbacks.
  //!
  //! @returns The local transform being used by these callbacks.
  public Matrix4x4 GetUnityLocalTransform() {
    return _lTransform;
  }
}
