// Copyright (C) 2017-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//! Utility functions for System.Collections.Generic.List<T>.
public class ListUtilities : MonoBehaviour {

  //! Converts a list into a serializable string.
  //!
  //! @param list The list to convert.
  //! @returns @p list as a string.
  public static string ToString(List<float> list) {
    if (list == null) {
      return string.Empty;
    }

    string[] listStrings = list.Select(x => x.ToString()).ToArray();
    return string.Join(",", listStrings);
  }

  //! Converts a serialized string into a list.
  //!
  //! @param listString The string to convert.
  //! @returns @p listString as a list.
  public static List<float> FromString(string listString) {
    return listString.Split(',').ToList().Select(x => float.Parse(x)).ToList();
  }
}
