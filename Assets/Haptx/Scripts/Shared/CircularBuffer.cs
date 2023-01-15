// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

//! A minimal circular buffer.
public class CircularBuffer<T> {

  //! The underlying linear buffer.
  private T[] _array = null;

  //! The length of this array.
  public int Length {
    get {
      return _array.Length;
    }
  }

  //! Construct a fixed size circular buffer.
  //!
  //! @param size The size of the buffer.
  public CircularBuffer(int size) {
    _array = new T[size];
  }

  //! Indexer operator.
  public T this[int index] {
    get => _array[index];
    set => _array[index] = value;
  }

  //! Get the array index following a given index. Wraps around at #Length.
  //!
  //! @param index The given index.
  //! @returns The index following @p index.
  public int GetNextIndex(int index) {
    return (index + 1) % _array.Length;
  }

  //! Get the array index preceding a given index. Wraps around at #Length.
  //!
  //! @param index The given index.
  //! @returns The index preceding @p index.
  public int GetPreviousIndex(int index) {
    return (index - 1 + _array.Length) % _array.Length;
  }
}
