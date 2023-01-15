using UnityEngine;

//! @copydoc HxNetworkRigidbodyBase
public class HxNetworkRigidbodyChild : HxNetworkRigidbodyBase {
  
  //! Which rigidbody to sync.
  [SerializeField]
  [Tooltip("Which rigidbody to sync.")]
  Rigidbody _childRigidbody = null;

  void Start() {
    _rigidbody = _childRigidbody;
  }
}
