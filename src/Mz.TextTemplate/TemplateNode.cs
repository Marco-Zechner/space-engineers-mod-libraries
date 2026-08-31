using System;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Identifies the kind of a parsed template syntax-tree node.
    /// </summary>
    public enum TemplateNodeKind
    {
        /// <summary>
        /// Literal source text.
        /// </summary>
        Text = 0,

        /// <summary>
        /// Template tag delimited by "{{" and "}}".
        /// </summary>
        Tag = 1
    }

    /// <summary>
    /// Base type for nodes in a parsed template document.
    /// </summary>
    public abstract class TemplateNode
    {
        /// <summary>
        /// Creates a template node with its kind and exact source span.
        /// </summary>
        protected TemplateNode(
            TemplateNodeKind kind,
            SourceSpan span
        )
        {
            Kind = kind;
            Span = span;
        }

        /// <summary>
        /// Gets the node kind.
        /// </summary>
        public TemplateNodeKind Kind { get; private set; }

        /// <summary>
        /// Gets the complete source span represented by the node.
        /// </summary>
        public SourceSpan Span { get; private set; }
    }

    /// <summary>
    /// Represents literal text in a parsed template.
    /// </summary>
    public sealed class TemplateTextNode : TemplateNode
    {
        /// <summary>
        /// Creates a literal text node.
        /// </summary>
        public TemplateTextNode(
            string text,
            SourceSpan span
        )
            : base(TemplateNodeKind.Text, span)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            Text = text;
        }

        /// <summary>
        /// Gets the exact literal text represented by the node.
        /// </summary>
        public string Text { get; private set; }
    }

    /// <summary>
    /// Represents a template tag such as "{{name}}" or "{{tab 4}}".
    /// </summary>
    public sealed class TemplateTagNode : TemplateNode
    {
        private readonly TemplateArgument[] _arguments;

        /// <summary>
        /// Creates a template tag node without arguments.
        /// </summary>
        public TemplateTagNode(
            string name,
            SourceSpan span,
            SourceSpan nameSpan
        )
            : this(
                name,
                span,
                nameSpan,
                new TemplateArgument[0]
            )
        {
        }

        /// <summary>
        /// Creates a template tag node with parsed arguments.
        /// </summary>
        public TemplateTagNode(
            string name,
            SourceSpan span,
            SourceSpan nameSpan,
            TemplateArgument[] arguments
        )
            : base(TemplateNodeKind.Tag, span)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (arguments == null)
                throw new ArgumentNullException("arguments");

            Name = name;
            NameSpan = nameSpan;

            _arguments =
                new TemplateArgument[arguments.Length];

            Array.Copy(
                arguments,
                _arguments,
                arguments.Length
            );
        }

        /// <summary>
        /// Gets the normalized tag name without surrounding whitespace.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the exact source span occupied by the tag name.
        /// </summary>
        public SourceSpan NameSpan { get; private set; }

        /// <summary>
        /// Gets a defensive copy of parsed arguments in source order.
        /// </summary>
        public TemplateArgument[] Arguments
        {
            get
            {
                var copy =
                    new TemplateArgument[_arguments.Length];

                Array.Copy(
                    _arguments,
                    copy,
                    _arguments.Length
                );

                return copy;
            }
        }
    }
}