// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.


//! Interface for HaptX classes associated with a HaptxApi::Peripheral.
public interface PeripheralLink {
  //! Retrieve the HaptxApi.Peripheral associated with the inheriting class. 
  HaptxApi.Peripheral Peripheral {
    get;
  }
}
