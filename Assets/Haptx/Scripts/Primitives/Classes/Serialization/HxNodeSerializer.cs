// Copyright (C) 2018-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//! Represents a serializable node in an arbitrary graph.
public interface INode<NodeSerialized> {

  //! Create a serializable instance.
  //!
  //! @returns A serializable instance.
  NodeSerialized Serialize();
}

//! Represents the serializable form of a node from an arbitrary graph.
public interface INodeSerialized<Node> {

  // Deserialize this instance.
  //!
  //! @returns A deserialized instance.
  Node Deserialize();
}

//! @brief Represents a root node in an arbitrary graph that is capable of containing nodes of a 
//! particular type. 
//!
//! Must be implemented N times where N is the number of node types that may be contained.
public interface IRootNode<NodeSerialized> {

  //! Add a node to the graph.
  //!
  //! @param node The node to add.
  void AddNode(NodeSerialized node);

  //! Get all child nodes (already serializable).
  //!
  //! @param[out] nodes Populated with serializable child nodes.
  void GetNodes(out IEnumerable<NodeSerialized> nodes);
}

//! @brief A class that is capable of managing the serialization of a single type of node in
//! an arbitrary graph.
//!
//! These relationships are defined through the interfaces required below. 
//! Implementations of this class should contain serializable lists of all child classes of 
//! NodeSerialized. These lists will be found automatically upon serialization.
public abstract class HxNodeSerializer<Node, NodeSerialized, RootNode>
    where Node : INode<NodeSerialized>
    where NodeSerialized : INodeSerialized<Node>
    where RootNode : IRootNode<NodeSerialized> {

  //! Deserialize all valid NodeSerialized objects found in fields of this class into the 
  //! provided RootNode.
  //!
  //! @param root The root node to deserialize all NodeSerialized objects into.
  public void Deserialize(RootNode root) {
    if (root == null) {
      return;
    }

    // Loops over all fields in this class looking for IEnumerable objects that contain
    // NodeSerialized objects.
    foreach (var field in GetType().GetFields()) {
      IList list = (IList)field.GetValue(this);
      if (list != null) {
        foreach (object item in list) {
          NodeSerialized nodeSerialized =
              (NodeSerialized)item;
          if (nodeSerialized != null) {
            root.AddNode(nodeSerialized);
          }
        }
      }
    }
  }

  //! Serialize all Nodes from the provided RootNode into the corresponding fields found on
  //! this class.
  //!
  //! @param root The root node to serialize all of the NodeSerialized objects from.
  public void Serialize(RootNode root) {
    // Loop over all Nodes from the RootNode and serialize them into corresponding fields.
    IEnumerable<NodeSerialized> serializedNodes = null;
    root.GetNodes(out serializedNodes);
    foreach (NodeSerialized nodeSerialized in serializedNodes) {
      if (nodeSerialized == null) {
        continue;
      }

      bool nodePlaced = false;
      foreach (var field in GetType().GetFields()) {
        IList list = (IList)field.GetValue(this);
        if (list != null &&
            list.GetType().GetGenericArguments().Length > 0 &&
            list.GetType().GetGenericArguments()[0] == nodeSerialized.GetType()) {
          list.Add(nodeSerialized);
          nodePlaced = true;
          break;
        }
      }
      if (!nodePlaced) {
        Debug.LogError(
            string.Format("Failed to place serialized node of type \"{0}\" into matching field",
            nodeSerialized.GetType()));
      }
    }
  }
}
