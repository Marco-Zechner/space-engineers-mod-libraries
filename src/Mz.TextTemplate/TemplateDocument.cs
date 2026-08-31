using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Represents the parsed syntax tree of a complete template.
    /// </summary>
    public sealed class TemplateDocument
    {
        private readonly TemplateNode[] _nodes;

        internal TemplateDocument(IList<TemplateNode> nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            _nodes = new TemplateNode[nodes.Count];

            for (int index = 0; index < nodes.Count; index++)
                _nodes[index] = nodes[index];
        }

        /// <summary>
        /// Gets a defensive copy of the top-level nodes in source order.
        /// </summary>
        public TemplateNode[] Nodes
        {
            get
            {
                var copy = new TemplateNode[_nodes.Length];
                Array.Copy(_nodes, copy, _nodes.Length);
                return copy;
            }
        }
    }
}
