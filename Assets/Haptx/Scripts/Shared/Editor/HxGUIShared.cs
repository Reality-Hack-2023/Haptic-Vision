// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

//! Functionality accessible to multiple HaptX GUI classes.
public static class HxGUIShared {

  //! Adds a menu option for creating @link CurveAsset CurveAssets @endlink.
  //!
  //! @returns A new curve asset.
  [MenuItem("Assets/Create/HaptX/Curve Asset")]
  public static CurveAsset CreateCurveAsset() {
    CurveAsset curve = ScriptableObject.CreateInstance<CurveAsset>();
    AssetDatabase.CreateAsset(curve, AssetDatabase.GenerateUniqueAssetPath(
        string.Format("{0}/Curve.asset", HxGUIShared.GetProjectWindowPath())));
    AssetDatabase.SaveAssets();
    return curve;
  }

  //! Get the current path in the editor Project window.
  //!
  //! @returns The current path in the editor Project window.
  public static string GetProjectWindowPath() {
    string path = "Assets";
    foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object),
        SelectionMode.Assets)) {
      path = AssetDatabase.GetAssetPath(obj);
      if (File.Exists(path)) {
        path = Path.GetDirectoryName(path);
      }
      break;
    }

    return path;
  }

  //! Recommends a unique name.
  //!
  //! @param existingNames The set of existing names.
  //! @param prefix A prefix for the new name.
  //! @returns A unique name containing @p prefix.
  public static string RecommendName(ICollection<string> existingNames, string prefix) {
    int suffix = 0;
    while (existingNames.Contains(prefix + suffix.ToString())) {
      suffix++;
    }
    return prefix + suffix.ToString();
  }

  //! Determine whether a given string is a valid name.
  //!
  //! @param name The name to validate.
  //! @returns True if the name is valid.
  public static bool ValidateName(string name) {
    if (name == string.Empty) {
      Debug.LogError("Please input a valid name");
      return false;
    }

    return true;
  }

  //! Get variable label and tooltip based on reflection info.
  //!
  //! @param type The type of object the GUIContent is for.
  //! @param expression An expression that implies which field to display. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //!
  //! @returns GUIContent (a label and a tooltip).
  public static GUIContent GetGUIContent<T>(Type type, Expression<Func<T>> expression) {
    string fieldName = GetFieldName(expression);
    string tooltip = GetTooltip(type.GetField(fieldName));

    return new GUIContent(CamelCaseToTitleCase(fieldName), tooltip);
  }

  //! Get variable label and tooltip based on reflection info using the tooltip of a private
  //! field.
  //!
  //! @param type The type of object the GUIContent is for.
  //! @param expression An expression that implies which field to display. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //! @param privateFieldName The private field to get tooltip info from.
  //! @returns GUIContent (a label and a tooltip).
  public static GUIContent GetGUIContent<T>(Type type, Expression<Func<T>> expression,
      string privateFieldName) {
    string fieldName = GetFieldName(expression);
    string tooltip = GetTooltip(type.GetField(privateFieldName,
        BindingFlags.NonPublic | BindingFlags.Instance));

    return new GUIContent(CamelCaseToTitleCase(fieldName), tooltip);
  }

  //! Get variable label and tooltip based on reflection info using the tooltip of a private
  //! field.
  //!
  //! @param type The type of object the GUIContent is for.
  //! @param text The text to use.
  //! @param privateFieldName The private field to get tooltip info from.
  //! @returns GUIContent (a label and a tooltip).
  public static GUIContent GetGUIContent(Type type, string text, string privateFieldName) {
    string tooltip = GetTooltip(type.GetField(privateFieldName,
        BindingFlags.NonPublic | BindingFlags.Instance));

    return new GUIContent(text, tooltip);
  }

  //! Get the tooltip of a field based on reflection info.
  //!
  //! @param type The type of object that contains the tooltip.
  //! @param expression An expression that implies which field has the tooltip. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //!
  //! @returns The tooltip if it exists, or string.Empty otherwise.
  public static string GetTooltip<T>(Type type, Expression<Func<T>> expression) {
    return GetTooltip(type.GetField(GetFieldName(expression)));
  }

  //! Get the tooltip of a field (if it has one).
  //!
  //! @param field The field to extract the tooltip from.
  //! @param inherit Whether to inherit attributes.
  //! @returns The tooltip if it exists, or string.Empty otherwise.
  public static string GetTooltip(FieldInfo field, bool inherit = true) {
    if (field == null) {
      return string.Empty;
    }

    TooltipAttribute[] attributes =
        (TooltipAttribute[])field.GetCustomAttributes(typeof(TooltipAttribute), inherit);

    return attributes.Length > 0 ? attributes[0].tooltip : string.Empty;
  }

  //! Get text indicating that a field has changed based on reflection info.
  //!
  //! @param expression An expression that implies which field has changed. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //! @param firstLetterCapitalized Whether to capitalize the first letter of the return text.
  //! @returns Text indicating that a field has changed.
  public static string GetChangeText<T>(Expression<Func<T>> expression,
      bool firstLetterCapitalized = false) {
    string text = string.Format("change {0}", GetFieldName(expression));
    return firstLetterCapitalized ? text[0].ToString().ToUpper() + text.Substring(1) : text;
  }

  //! brief Get the name of a field based on reflection info.
  //!
  //! @param expression An expression that implies which field's name to get. E.g. 
  //! @code () => hxJoint.visualizeAnchors @endcode
  //!
  //! @returns The name of a field.
  public static string GetFieldName<T>(Expression<Func<T>> expression) {
    MemberExpression expressionBody = (MemberExpression)expression.Body;
    return expressionBody.Member.Name;
  }

  //! Get text indicating that an instance has been added based on reflection info.
  //!
  //! @param type The type of instance that has been added.
  //! @param firstLetterCapitalized Whether to capitalize the first letter of the return text.
  //! @returns Text indicating that an instance has been added.
  public static string GetAddText(Type type, bool firstLetterCapitalized = false) {
    string text = string.Format("add {0}", type.ToString());
    return firstLetterCapitalized ? text[0].ToString().ToUpper() + text.Substring(1) : text;
  }

  //! Get text indicating that an instance has been removed based on reflection info.
  //!
  //! @param type The type of instance that has been removed.
  //! @param firstLetterCapitalized Whether to capitalize the first letter of the return text.
  //! @returns Text indicating that an instance has been removed.
  public static string GetRemoveText(Type type, bool firstLetterCapitalized = false) {
    string text = string.Format("remove {0}", type.ToString());
    return firstLetterCapitalized ? text[0].ToString().ToUpper() + text.Substring(1) : text;
  }

  //! Get text based on the given type.
  //!
  //! @param type The type of interest.
  //! @returns Text matching the type.
  public static string GetTypeText(Type type) {
    return string.Format("{0} type", type.ToString());
  }

  //! Converts a string given in camel case (or pascal case) to title case. 
  //!
  //! Title case is the same as sentence case with the first letter of each word being capitalized.
  //!
  //! @param camelCase The string to convert.
  //! @returns The converted string.
  public static string CamelCaseToTitleCase(string camelCase) {
    if (camelCase == string.Empty) {
      return string.Empty;
    }
    char[] camelCaseSpaced = Regex.Replace(camelCase.Substring(1), @"[A-Z]", " $0").ToCharArray();
    return camelCase[0].ToString().ToUpper() + new string(camelCaseSpaced);
  }

  //! Returns whether more than one component of a given type is selected.
  //!
  //! @returns True if more than one component of a given type is selected.
  public static bool AreMultipleComponentsSelected<T>() where T : UnityEngine.Object {
    bool firstObjectFound = false;
    foreach (UnityEngine.Object obj in Selection.objects) {
      GameObject gameObj = obj as GameObject;
      if (gameObj && gameObj.GetComponent<T>() != null) {
        if (firstObjectFound) {
          return true;
        } else {
          firstObjectFound = true;
        }
      }
    }

    return false;
  }
}