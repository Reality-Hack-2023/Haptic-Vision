// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using UnityEngine;

//! @brief A simple low pass filter.
//!
//! Instantiate one for every value that you intend to filter. To apply the filter, pass unfiltered
//! values to #ApplyFilter() and use the return value or #FilteredValue.
public class HxLowPassFilter {

  //! @brief How aggressively nominal values will be filtered (similar to time constant) [s].
  //!
  //! Smaller values lead to faster responses but more jitter; larger values lead to slower but
  //! smoother responses.
  [Range(0.0f, float.MaxValue)]
  public float filterStrengthS = 0.1f;

  //! @brief The last filtered value.
  //!
  //! If filtering a new value use #ApplyFilter().
  public float FilteredValue {
    get {
      return _filteredValue;
    }
    set {
      _filteredValue = value;
    }
  }

  //! See #FilteredValue;
  private float _filteredValue = 0.0f;

  //! @brief Default constructor.
  //!
  //! Doesn't do anything.
  public HxLowPassFilter() { }

  //! Construct with a filter strength value.
  //!
  //! @param filterStrengthS The filter strength value to use.
  public HxLowPassFilter(float filterStrengthS) {
    this.filterStrengthS = filterStrengthS;
  }

  //! @brief Filter a value.
  //!
  //! Note: using this function just once won't do much good. The expectation is that it will be
  //! called on every new unfiltered value.
  //!
  //! @param unfilteredValue The raw value of interest.
  //! @returns The filtered value.
  public float ApplyFilter(float unfilteredValue) {
    float approach = filterStrengthS > 0.0f ? Time.deltaTime / filterStrengthS : 1.0f;
    if (approach > 1.0f) {
      approach = 1.0f;
    }
    _filteredValue += approach * (unfilteredValue - _filteredValue);
    return _filteredValue;
  }

}
