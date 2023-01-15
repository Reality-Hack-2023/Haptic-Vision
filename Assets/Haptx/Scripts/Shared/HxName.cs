// Copyright (C) 2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;
using System;

//! Just like a string, except only hashed once.
[Serializable]
public class HxName {
  
  //! The string representation of this name.
  public string String {
    get {
      return _string;
    }
  }

  //! The hash representation of this name.
  public int HashCode {
    get {
      if (!_isHashValid) {
        _hashCode = _string.GetHashCode();
        _isHashValid = true;
      }
      return _hashCode;
    }
  }

  //! @copydoc #String
  [SerializeField, Tooltip("The string representation of this name.")]
  private string _string = string.Empty;

  //! @copydoc #HashCode
  private int _hashCode = 0;

  //! Whether #_hashCode has been computed.
  private bool _isHashValid = false;

  //! Construct a default HxName.
  public HxName() {}

  //! Construct an HxName from a string.
  //!
  //! @param inString The string to use.
  public HxName(string inString) {
    _string = inString;
  }

  //! Construct an HxName from a HxName.
  //!
  //! @param inName The name to use.
  public HxName(HxName inName) {
    _string = inName.String;
  }

  public override bool Equals(object obj) {
    HxName otherName = obj as HxName;
    if (otherName != null) {
      return HashCode == otherName.HashCode && String == otherName.String;
    }
    
    string otherString = obj as string;
    if (otherString != null) {
      return String == otherString;
    }

    return false;
  }

  public override int GetHashCode() {
    return HashCode;
  }
}
