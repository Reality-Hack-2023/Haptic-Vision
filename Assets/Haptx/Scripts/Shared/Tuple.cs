// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

//! A generic, 2-item tuple class.
public class Tuple<T1, T2> {
  //! Item 1.
  public T1 Item1 { get; set; }

  //! Item 2.
  public T2 Item2 { get; set; }

  //! Construct using given values.
  //!
  //! @param item1 The first item.
  //! @param item2 The second item.
  public Tuple(T1 item1, T2 item2) {
    Item1 = item1;
    Item2 = item2;
  }
}

//! A generic, 3-item tuple class.
public class Tuple<T1, T2, T3> {
  //! Item 1.
  public T1 Item1 { get; set; }

  //! Item 2.
  public T2 Item2 { get; set; }

  //! Item 3.
  public T3 Item3 { get; set; }

  //! Construct using given values.
  //!
  //! @param item1 The first item.
  //! @param item2 The second item.
  //! @param item3 See thid.
  public Tuple(T1 item1, T2 item2, T3 item3) {
    Item1 = item1;
    Item2 = item2;
    Item3 = item3;
  }
}

//! A static class for creating generic tuples.
public static class Tuple {

  //! Create a 2-tuple.
  //!
  //! @param item1 The first item.
  //! @param item2 The second item.
  //! @returns The 2-tuple.
  public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) {
    return new Tuple<T1, T2>(item1, item2);
  }

  //! Create a 3-tuple.
  //!
  //! @param item1 The first item.
  //! @param item2 The second item.
  //! @param item3 The third item.
  //! @returns The 3-tuple.
  public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) {
    return new Tuple<T1, T2, T3>(item1, item2, item3);
  }
}
