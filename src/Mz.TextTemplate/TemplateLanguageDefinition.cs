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
            : this(
                name,
                role,
                TemplateArgumentContract.NoArguments
            )
        {
        }

        /// <summary>
        /// Creates a host tag definition with its argument contract.
        /// </summary>
        public TemplateTagDefinition(
            string name,
            TemplateTagRole role,
            TemplateArgumentContract argumentContract
        )
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0)
                throw new ArgumentException("Template tag definition name cannot be empty.", nameof(name));

            if (TemplateNameRules.FindInvalidConstructNameOffset(name) >= 0)
                throw new ArgumentException("Template tag definition name is not syntactically valid.", nameof(name));

            if (
                role != TemplateTagRole.Value
                && role != TemplateTagRole.Command
            )
            {
                throw new ArgumentException("Unsupported template tag role.", nameof(role));
            }

            if (argumentContract == null)
                throw new ArgumentNullException(nameof(argumentContract));

            Name = name;
            Role = role;
            ArgumentContract = argumentContract;
        }

        /// <summary>
        /// Gets the exact ordinal tag name understood by the host.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets whether the tag represents a value or command.
        /// </summary>
        public TemplateTagRole Role { get; private set; }

        /// <summary>
        /// Gets the argument contract.
        /// </summary>
        public TemplateArgumentContract ArgumentContract { get; private set; }
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
            : this(
                name,
                TemplateArgumentContract.NoArguments
            )
        {
        }

        /// <summary>
        /// Creates a host block definition with its argument contract.
        /// </summary>
        public TemplateBlockDefinition(
            string name,
            TemplateArgumentContract argumentContract
        )
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0)
                throw new ArgumentException("Template block definition name cannot be empty.", nameof(name));

            if (TemplateNameRules.FindInvalidConstructNameOffset(name) >= 0)
                throw new ArgumentException("Template block definition name is not syntactically valid.", nameof(name));

            if (argumentContract == null)
                throw new ArgumentNullException(nameof(argumentContract));

            Name = name;
            ArgumentContract = argumentContract;
        }

        /// <summary>
        /// Gets the exact ordinal block name understood by the host.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the argument contract.
        /// </summary>
        public TemplateArgumentContract ArgumentContract { get; private set; }
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
                throw new ArgumentNullException(nameof(tags));

            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));

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
                    throw new ArgumentException("Template tag definitions cannot contain null entries.", nameof(tags));
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
                    throw new ArgumentException("Template block definitions cannot contain null entries.", nameof(blocks));
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

        internal bool TryGetBlock(
            string name,
            out TemplateBlockDefinition definition
        )
        {
            return
                _blockLookup.TryGetValue(
                    name,
                    out definition
                );
        }
    }
}
