// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;

//! A bare-bones reusable object pool. Get new objects with the Get() function, and call
//! Release() when done with them.
public static class HxReusableObjectPool<T> where T : class, new() {
  //! The collection of reusable objects that are not currently being used.
  private static Queue<T> _Queue = new Queue<T>();

  //! Get a reusable object.
  //!
  //! @returns A reusable object from the queue if available, otherwise a new object.
  public static T Get() {
    if (_Queue.Count > 0) {
      T reusableObject = _Queue.Dequeue();
      if (reusableObject != null) {
        return reusableObject;
      }
    }
    return new T();
  }

  //! Release control over a reusable object, adding it to the queue to be redistributed.
  //!
  //! @param reusableObject The object to relinquish control of.
  public static void Release(T reusableObject) {
    if (reusableObject != null) {
      _Queue.Enqueue(reusableObject);
    }
  }

  //! Gets an object from the pool wrapped in a DisposablePoolWrapper.
  //!
  //! @returns An object from the pool wrapped in a DisposablePoolWrapper
  public static DisposablePoolWrapper GetWrapped() {
    return new DisposablePoolWrapper(Get());
  }

  //! An IDisposable object that returns its contents to an HxReusableObjectPool when disposed.
  public class DisposablePoolWrapper : IDisposable {
    //! The underlying object.
    public T ReusableObject { get; private set; }

    //! Constructor that takes an object to wrap.
    //!
    //! @param reusableObject The object to wrap.
    public DisposablePoolWrapper(T reusableObject) {
      ReusableObject = reusableObject;
    }

    //! Returns the underlying object to the object pool if it has not yet.
    public void Dispose() {
      if (ReusableObject != null) {
        HxReusableObjectPool<T>.Release(ReusableObject);
        ReusableObject = null;
      }
    }

    //! Finalizer. Returns the underlying object to the object pool if it has not yet.
    ~DisposablePoolWrapper() {
      Dispose();
    }
  }
}
