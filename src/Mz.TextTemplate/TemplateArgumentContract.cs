using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Defines one positional argument accepted by a template construct.
    /// </summary>
    public sealed class TemplatePositionalArgumentDefinition
    {
        private readonly TemplateArgumentValueKind[] _allowedValueKinds;

        /// <summary>
        /// Creates a positional argument definition.
        /// </summary>
        public TemplatePositionalArgumentDefinition(
            bool required,
            TemplateArgumentValueKind[] allowedValueKinds
        )
        {
            _allowedValueKinds =
                TemplateArgumentValueKindRules.CopyAndValidate(
                    allowedValueKinds,
                    "allowedValueKinds"
                );

            Required = required;
        }

        /// <summary>
        /// Gets whether the positional argument must be supplied.
        /// </summary>
        public bool Required { get; private set; }

        /// <summary>
        /// Gets the lexical value kinds accepted by this argument.
        /// </summary>
        public TemplateArgumentValueKind[] AllowedValueKinds
        {
            get
            {
                var copy =
                    new TemplateArgumentValueKind[
                        _allowedValueKinds.Length
                    ];

                Array.Copy(
                    _allowedValueKinds,
                    copy,
                    _allowedValueKinds.Length
                );

                return copy;
            }
        }

        internal bool Allows(
            TemplateArgumentValueKind kind
        )
        {
            return
                TemplateArgumentValueKindRules.Contains(
                    _allowedValueKinds,
                    kind
                );
        }
    }

    /// <summary>
    /// Defines one named argument accepted by a template construct.
    /// </summary>
    public sealed class TemplateNamedArgumentDefinition
    {
        private readonly TemplateArgumentValueKind[] _allowedValueKinds;

        /// <summary>
        /// Creates a named argument definition.
        /// </summary>
        public TemplateNamedArgumentDefinition(
            string name,
            bool required,
            TemplateArgumentValueKind[] allowedValueKinds
        )
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0)
            {
                throw new ArgumentException("Template named argument definition name cannot be empty.", nameof(name));
            }

            if (TemplateNameRules.FindInvalidArgumentNameOffset(name) >= 0)
            {
                throw new ArgumentException("Template named argument definition name is not syntactically valid.", nameof(name));
            }

            _allowedValueKinds =
                TemplateArgumentValueKindRules.CopyAndValidate(
                    allowedValueKinds,
                    "allowedValueKinds"
                );

            Name = name;
            Required = required;
        }

        /// <summary>
        /// Gets the exact ordinal argument name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets whether the named argument must be supplied.
        /// </summary>
        public bool Required { get; private set; }

        /// <summary>
        /// Gets the lexical value kinds accepted by this argument.
        /// </summary>
        public TemplateArgumentValueKind[] AllowedValueKinds
        {
            get
            {
                var copy =
                    new TemplateArgumentValueKind[
                        _allowedValueKinds.Length
                    ];

                Array.Copy(
                    _allowedValueKinds,
                    copy,
                    _allowedValueKinds.Length
                );

                return copy;
            }
        }

        internal bool Allows(
            TemplateArgumentValueKind kind
        )
        {
            return
                TemplateArgumentValueKindRules.Contains(
                    _allowedValueKinds,
                    kind
                );
        }
    }

    /// <summary>
    /// Defines the positional and named argument grammar accepted by one
    /// template tag or block.
    /// </summary>
    public sealed class TemplateArgumentContract
    {
        private static readonly TemplateArgumentContract _noArguments =
            new TemplateArgumentContract(
                new TemplatePositionalArgumentDefinition[0],
                new TemplateNamedArgumentDefinition[0]
            );

        private readonly TemplatePositionalArgumentDefinition[] _positionalArguments;
        private readonly TemplateNamedArgumentDefinition[] _namedArguments;
        private readonly Dictionary<string, TemplateNamedArgumentDefinition> _namedLookup;

        /// <summary>
        /// Gets the shared contract for constructs that accept no arguments.
        /// </summary>
        public static TemplateArgumentContract NoArguments
        {
            get { return _noArguments; }
        }

        /// <summary>
        /// Creates an argument contract. A contract containing no definitions
        /// accepts no arguments.
        /// </summary>
        public TemplateArgumentContract(
            TemplatePositionalArgumentDefinition[] positionalArguments,
            TemplateNamedArgumentDefinition[] namedArguments
        )
        {
            if (positionalArguments == null)
                throw new ArgumentNullException(nameof(positionalArguments));

            if (namedArguments == null)
                throw new ArgumentNullException(nameof(namedArguments));

            _positionalArguments =
                new TemplatePositionalArgumentDefinition[
                    positionalArguments.Length
                ];

            bool optionalPositionalSeen = false;

            for (
                int index = 0;
                index < positionalArguments.Length;
                index++
            )
            {
                TemplatePositionalArgumentDefinition definition =
                    positionalArguments[index];

                if (definition == null)
                {
                    throw new ArgumentException("Positional argument definitions cannot contain null entries.", nameof(positionalArguments));
                }

                if (!definition.Required)
                {
                    optionalPositionalSeen = true;
                }
                else if (optionalPositionalSeen)
                {
                    throw new ArgumentException("Required positional arguments cannot follow optional positional arguments.", nameof(positionalArguments));
                }

                _positionalArguments[index] =
                    definition;
            }

            _namedArguments =
                new TemplateNamedArgumentDefinition[
                    namedArguments.Length
                ];

            _namedLookup =
                new Dictionary<string, TemplateNamedArgumentDefinition>(
                    StringComparer.Ordinal
                );

            for (
                int index = 0;
                index < namedArguments.Length;
                index++
            )
            {
                TemplateNamedArgumentDefinition definition =
                    namedArguments[index];

                if (definition == null)
                {
                    throw new ArgumentException("Named argument definitions cannot contain null entries.", nameof(namedArguments));
                }

                if (
                    _namedLookup.ContainsKey(
                        definition.Name
                    )
                )
                {
                    throw new ArgumentException(
                        "Duplicate named argument definition '" + definition.Name + "'.",
                        "namedArguments"
                    );
                }

                _namedArguments[index] =
                    definition;

                _namedLookup.Add(
                    definition.Name,
                    definition
                );
            }
        }

        /// <summary>
        /// Gets positional argument definitions in ordinal position order.
        /// </summary>
        public TemplatePositionalArgumentDefinition[] PositionalArguments
        {
            get
            {
                var copy =
                    new TemplatePositionalArgumentDefinition[
                        _positionalArguments.Length
                    ];

                Array.Copy(
                    _positionalArguments,
                    copy,
                    _positionalArguments.Length
                );

                return copy;
            }
        }

        /// <summary>
        /// Gets named argument definitions.
        /// </summary>
        public TemplateNamedArgumentDefinition[] NamedArguments
        {
            get
            {
                var copy =
                    new TemplateNamedArgumentDefinition[
                        _namedArguments.Length
                    ];

                Array.Copy(
                    _namedArguments,
                    copy,
                    _namedArguments.Length
                );

                return copy;
            }
        }

        internal int PositionalArgumentCount
        {
            get { return _positionalArguments.Length; }
        }

        internal int NamedArgumentCount
        {
            get { return _namedArguments.Length; }
        }

        internal TemplatePositionalArgumentDefinition GetPositionalArgument(
            int index
        )
        {
            return _positionalArguments[index];
        }

        internal TemplateNamedArgumentDefinition GetNamedArgument(
            int index
        )
        {
            return _namedArguments[index];
        }

        internal bool TryGetNamedArgument(
            string name,
            out TemplateNamedArgumentDefinition definition
        )
        {
            return
                _namedLookup.TryGetValue(
                    name,
                    out definition
                );
        }
    }

    internal static class TemplateArgumentValueKindRules
    {
        internal static TemplateArgumentValueKind[] CopyAndValidate(
            TemplateArgumentValueKind[] kinds,
            string parameterName
        )
        {
            if (kinds == null)
                throw new ArgumentNullException(parameterName);

            if (kinds.Length == 0)
            {
                throw new ArgumentException(
                    "At least one allowed argument value kind is required.",
                    parameterName
                );
            }

            var copy =
                new TemplateArgumentValueKind[
                    kinds.Length
                ];

            for (
                int index = 0;
                index < kinds.Length;
                index++
            )
            {
                TemplateArgumentValueKind kind =
                    kinds[index];

                if (
                    kind != TemplateArgumentValueKind.Bare
                    && kind != TemplateArgumentValueKind.String
                    && kind != TemplateArgumentValueKind.Number
                    && kind != TemplateArgumentValueKind.Boolean
                )
                {
                    throw new ArgumentException(
                        "Unsupported argument value kind.",
                        parameterName
                    );
                }

                for (
                    int previous = 0;
                    previous < index;
                    previous++
                )
                {
                    if (copy[previous] == kind)
                    {
                        throw new ArgumentException(
                            "Allowed argument value kinds cannot contain duplicates.",
                            parameterName
                        );
                    }
                }

                copy[index] = kind;
            }

            return copy;
        }

        internal static bool Contains(
            TemplateArgumentValueKind[] kinds,
            TemplateArgumentValueKind kind
        )
        {
            for (
                int index = 0;
                index < kinds.Length;
                index++
            )
            {
                if (kinds[index] == kind)
                    return true;
            }

            return false;
        }
    }
}
