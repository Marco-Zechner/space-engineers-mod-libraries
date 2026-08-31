using System;
using System.Collections.Generic;

namespace Mz.Toml.Internal
{
    internal static partial class TomlWriter
    {
        private static void ValidateAcyclicGraph(TomlNode root)
        {
            var activePath = new List<TomlNode>();
            ValidateAcyclicNode(root, activePath);
        }

        private static void ValidateAcyclicNode(TomlNode node, List<TomlNode> activePath)
        {
            for (var i = 0; i < activePath.Count; i++)
            {
                if (object.ReferenceEquals(activePath[i], node))
                {
                    throw new InvalidOperationException(
                        "Cannot write a TOML document containing a cyclic node graph.");
                }
            }

            activePath.Add(node);

            switch (node.Kind)
            {
                case TomlNodeKind.Value:
                    break;

                case TomlNodeKind.Array:
                {
                    var array = (TomlArray)node;

                    for (var i = 0; i < array.Count; i++)
                    {
                        ValidateAcyclicNode(array[i], activePath);
                    }

                    break;
                }

                case TomlNodeKind.Table:
                {
                    var table = (TomlTable)node;

                    foreach (var pair in table)
                    {
                        ValidateAcyclicNode(pair.Value, activePath);
                    }

                    break;
                }

                default:
                    throw new InvalidOperationException(
                        "Unsupported TOML node kind: " +
                        node.Kind);
            }

            activePath.RemoveAt(activePath.Count - 1);
        }
    }
}
