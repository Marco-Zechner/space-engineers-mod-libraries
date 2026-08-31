using System;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Identifies whether a template argument is positional or named.
    /// </summary>
    public enum TemplateArgumentKind
    {
        /// <summary>
        /// Argument represented only by a value.
        /// </summary>
        Positional = 0,

        /// <summary>
        /// Argument represented by a name, equals sign, and value.
        /// </summary>
        Named = 1
    }

    /// <summary>
    /// Describes the lexical form of a template argument value.
    /// </summary>
    public enum TemplateArgumentValueKind
    {
        /// <summary>
        /// Value omitted from an otherwise recognizable argument.
        /// </summary>
        Missing = 0,

        /// <summary>
        /// Unquoted value with no more specific lexical classification.
        /// </summary>
        Bare = 1,

        /// <summary>
        /// Double-quoted string value.
        /// </summary>
        String = 2,

        /// <summary>
        /// Numeric-looking unquoted value.
        /// </summary>
        Number = 3,

        /// <summary>
        /// The unquoted literal "true" or "false".
        /// </summary>
        Boolean = 4
    }

    /// <summary>
    /// Represents one lexical value attached to a template argument.
    /// </summary>
    public sealed class TemplateArgumentValue
    {
        /// <summary>
        /// Creates a template argument value.
        /// </summary>
        internal TemplateArgumentValue(
            TemplateArgumentValueKind kind,
            string rawText,
            string text,
            SourceSpan span,
            SourceSpan contentSpan
        )
        {
            if (rawText == null)
                throw new ArgumentNullException(nameof(rawText));

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            Kind = kind;
            RawText = rawText;
            Text = text;
            Span = span;
            ContentSpan = contentSpan;
        }

        /// <summary>
        /// Gets the lexical value kind.
        /// </summary>
        public TemplateArgumentValueKind Kind { get; private set; }

        /// <summary>
        /// Gets the exact source text occupied by the value.
        /// </summary>
        public string RawText { get; private set; }

        /// <summary>
        /// Gets the value text without surrounding string quotes.
        /// Escape sequences remain source-preserved.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Gets the complete source span of the value.
        /// </summary>
        public SourceSpan Span { get; private set; }

        /// <summary>
        /// Gets the source span containing the value content. For quoted
        /// strings this excludes the quotes.
        /// </summary>
        public SourceSpan ContentSpan { get; private set; }
    }

    /// <summary>
    /// Base type for parsed template arguments.
    /// </summary>
    public abstract class TemplateArgument
    {
        /// <summary>
        /// Creates a parsed template argument.
        /// </summary>
        internal TemplateArgument(
            TemplateArgumentKind kind,
            SourceSpan span,
            TemplateArgumentValue value
        )
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Kind = kind;
            Span = span;
            Value = value;
        }

        /// <summary>
        /// Gets whether the argument is positional or named.
        /// </summary>
        public TemplateArgumentKind Kind { get; private set; }

        /// <summary>
        /// Gets the complete source span occupied by the argument.
        /// </summary>
        public SourceSpan Span { get; private set; }

        /// <summary>
        /// Gets the argument value.
        /// </summary>
        public TemplateArgumentValue Value { get; private set; }
    }

    /// <summary>
    /// Represents a positional template argument.
    /// </summary>
    public sealed class TemplatePositionalArgument : TemplateArgument
    {
        /// <summary>
        /// Creates a positional template argument.
        /// </summary>
        internal TemplatePositionalArgument(TemplateArgumentValue value)
            : base(
                TemplateArgumentKind.Positional,
                value == null ? new SourceSpan(0, 0) : value.Span,
                value
            )
        {
        }
    }

    /// <summary>
    /// Represents a named template argument such as "wrap=1".
    /// </summary>
    public sealed class TemplateNamedArgument : TemplateArgument
    {
        /// <summary>
        /// Creates a named template argument.
        /// </summary>
        internal TemplateNamedArgument(
            string name,
            SourceSpan nameSpan,
            SourceSpan equalsSpan,
            TemplateArgumentValue value,
            SourceSpan span
        )
            : base(
                TemplateArgumentKind.Named,
                span,
                value
            )
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Name = name;
            NameSpan = nameSpan;
            EqualsSpan = equalsSpan;
        }

        /// <summary>
        /// Gets the normalized argument name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the exact source span occupied by the argument name.
        /// </summary>
        public SourceSpan NameSpan { get; private set; }

        /// <summary>
        /// Gets the exact source span occupied by the equals sign.
        /// </summary>
        public SourceSpan EqualsSpan { get; private set; }
    }
}
