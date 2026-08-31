using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Describes the host-provided semantic role of a normal template tag.
    /// </summary>
    public enum TemplateTagRole
    {
        /// <summary>
        /// Tag that resolves to a host-provided value.
        /// </summary>
        Value = 0,

        /// <summary>
        /// Tag that performs a host-provided formatting or layout command.
        /// </summary>
        Command = 1
    }

    /// <summary>
    /// Defines one normal tag understood by a template host.
    /// </summary>
    public sealed class TemplateTagDefinition
    {
        /// <summary>
        /// Creates a host tag definition.
        /// </summary>
        public TemplateTagDefinition(
            string name,
            TemplateTagRole role
        )
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (name.Length == 0)
                throw new ArgumentException(
                    "Template tag definition name cannot be empty.",
                    "name"
                );

            if (
                role != TemplateTagRole.Value
                && role != TemplateTagRole.Command
            )
            {
                throw new ArgumentException(
                    "Unsupported template tag role.",
                    "role"
                );
            }

            Name = name;
            Role = role;
        }

        /// <summary>
        /// Gets the exact ordinal tag name understood by the host.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets whether the tag represents a value or command.
        /// </summary>
        public TemplateTagRole Role { get; private set; }
    }

    /// <summary>
    /// Defines one block name understood by a template host.
    /// </summary>
    public sealed class TemplateBlockDefinition
    {
        /// <summary>
        /// Creates a host block definition.
        /// </summary>
        public TemplateBlockDefinition(
            string name
        )
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (name.Length == 0)
                throw new ArgumentException(
                    "Template block definition name cannot be empty.",
                    "name"
                );

            Name = name;
        }

        /// <summary>
        /// Gets the exact ordinal block name understood by the host.
        /// </summary>
        public string Name { get; private set; }
    }

    /// <summary>
    /// Describes the normal tags and blocks understood by one template host.
    /// Tag and block names occupy separate namespaces and are matched using
    /// ordinal string comparison.
    /// </summary>
    public sealed class TemplateLanguageDefinition
    {
        private readonly TemplateTagDefinition[] _tags;
        private readonly TemplateBlockDefinition[] _blocks;
        private readonly Dictionary<string, TemplateTagDefinition> _tagLookup;
        private readonly Dictionary<string, TemplateBlockDefinition> _blockLookup;

        /// <summary>
        /// Creates a host language definition from tag and block definitions.
        /// </summary>
        public TemplateLanguageDefinition(
            TemplateTagDefinition[] tags,
            TemplateBlockDefinition[] blocks
        )
        {
            if (tags == null)
                throw new ArgumentNullException("tags");

            if (blocks == null)
                throw new ArgumentNullException("blocks");

            _tags =
                new TemplateTagDefinition[tags.Length];

            _tagLookup =
                new Dictionary<string, TemplateTagDefinition>(
                    StringComparer.Ordinal
                );

            for (
                int index = 0;
                index < tags.Length;
                index++
            )
            {
                TemplateTagDefinition definition =
                    tags[index];

                if (definition == null)
                {
                    throw new ArgumentException(
                        "Template tag definitions cannot contain null entries.",
                        "tags"
                    );
                }

                if (
                    _tagLookup.ContainsKey(
                        definition.Name
                    )
                )
                {
                    throw new ArgumentException(
                        "Duplicate template tag definition '" + definition.Name + "'.",
                        "tags"
                    );
                }

                _tags[index] = definition;

                _tagLookup.Add(
                    definition.Name,
                    definition
                );
            }

            _blocks =
                new TemplateBlockDefinition[blocks.Length];

            _blockLookup =
                new Dictionary<string, TemplateBlockDefinition>(
                    StringComparer.Ordinal
                );

            for (
                int index = 0;
                index < blocks.Length;
                index++
            )
            {
                TemplateBlockDefinition definition =
                    blocks[index];

                if (definition == null)
                {
                    throw new ArgumentException(
                        "Template block definitions cannot contain null entries.",
                        "blocks"
                    );
                }

                if (
                    _blockLookup.ContainsKey(
                        definition.Name
                    )
                )
                {
                    throw new ArgumentException(
                        "Duplicate template block definition '" + definition.Name + "'.",
                        "blocks"
                    );
                }

                _blocks[index] = definition;

                _blockLookup.Add(
                    definition.Name,
                    definition
                );
            }
        }

        /// <summary>
        /// Gets a defensive copy of normal tag definitions.
        /// </summary>
        public TemplateTagDefinition[] Tags
        {
            get
            {
                var copy =
                    new TemplateTagDefinition[_tags.Length];

                Array.Copy(
                    _tags,
                    copy,
                    _tags.Length
                );

                return copy;
            }
        }

        /// <summary>
        /// Gets a defensive copy of block definitions.
        /// </summary>
        public TemplateBlockDefinition[] Blocks
        {
            get
            {
                var copy =
                    new TemplateBlockDefinition[_blocks.Length];

                Array.Copy(
                    _blocks,
                    copy,
                    _blocks.Length
                );

                return copy;
            }
        }

        internal bool TryGetTag(
            string name,
            out TemplateTagDefinition definition
        )
        {
            return
                _tagLookup.TryGetValue(
                    name,
                    out definition
                );
        }

        internal bool ContainsBlock(
            string name
        )
        {
            return
                _blockLookup.ContainsKey(
                    name
                );
        }
    }
}