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
        Tag = 1,

        /// <summary>
        /// Nested template block with opening and closing structural tags.
        /// </summary>
        Block = 2
    }

    /// <summary>
    /// Base type for nodes in a parsed template document.
    /// </summary>
    public abstract class TemplateNode
    {
        /// <summary>
        /// Creates a template node with its kind and exact source span.
        /// </summary>
        internal TemplateNode(
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
        internal TemplateTextNode(
            string text,
            SourceSpan span
        )
            : base(TemplateNodeKind.Text, span)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

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
        internal TemplateTagNode(
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
        internal TemplateTagNode(
            string name,
            SourceSpan span,
            SourceSpan nameSpan,
            TemplateArgument[] arguments
        )
            : base(TemplateNodeKind.Tag, span)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

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

    /// <summary>
    /// Represents a nested block such as "{{#first}}...{{/first}}".
    /// Block names have no host-specific meaning to the parser.
    /// </summary>
    public sealed class TemplateBlockNode : TemplateNode
    {
        private readonly TemplateArgument[] _arguments;
        private readonly TemplateNode[] _children;

        /// <summary>
        /// Creates a parsed template block.
        /// </summary>
        internal TemplateBlockNode(
            string name,
            SourceSpan span,
            SourceSpan openTagSpan,
            SourceSpan openNameSpan,
            SourceSpan closeTagSpan,
            SourceSpan closeNameSpan,
            bool hasClosingTag,
            TemplateArgument[] arguments,
            TemplateNode[] children
        )
            : base(TemplateNodeKind.Block, span)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            if (children == null)
                throw new ArgumentNullException(nameof(children));

            Name = name;
            OpenTagSpan = openTagSpan;
            OpenNameSpan = openNameSpan;
            CloseTagSpan = closeTagSpan;
            CloseNameSpan = closeNameSpan;
            HasClosingTag = hasClosingTag;

            _arguments =
                new TemplateArgument[arguments.Length];

            Array.Copy(
                arguments,
                _arguments,
                arguments.Length
            );

            _children =
                new TemplateNode[children.Length];

            Array.Copy(
                children,
                _children,
                children.Length
            );
        }

        /// <summary>
        /// Gets the block name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the complete opening structural tag span.
        /// </summary>
        public SourceSpan OpenTagSpan { get; private set; }

        /// <summary>
        /// Gets the block-name span in the opening structural tag.
        /// </summary>
        public SourceSpan OpenNameSpan { get; private set; }

        /// <summary>
        /// Gets the complete closing structural tag span, or an empty span
        /// at the recovery point when the closing tag is missing.
        /// </summary>
        public SourceSpan CloseTagSpan { get; private set; }

        /// <summary>
        /// Gets the block-name span in the closing structural tag, or an
        /// empty span when the closing tag is missing.
        /// </summary>
        public SourceSpan CloseNameSpan { get; private set; }

        /// <summary>
        /// Gets whether a matching closing structural tag was parsed.
        /// </summary>
        public bool HasClosingTag { get; private set; }

        /// <summary>
        /// Gets a defensive copy of opening-block arguments.
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

        /// <summary>
        /// Gets a defensive copy of nested child nodes in source order.
        /// </summary>
        public TemplateNode[] Children
        {
            get
            {
                var copy =
                    new TemplateNode[_children.Length];

                Array.Copy(
                    _children,
                    copy,
                    _children.Length
                );

                return copy;
            }
        }
    }
}
