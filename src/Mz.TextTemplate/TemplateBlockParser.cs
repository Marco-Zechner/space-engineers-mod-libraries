using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    internal sealed class TemplateBlockParser
    {
        private sealed class BlockFrame
        {
            public readonly string Name;
            public readonly SourceSpan OpenTagSpan;
            public readonly SourceSpan OpenNameSpan;
            public readonly TemplateArgument[] Arguments;
            public readonly List<TemplateNode> Children;

            public BlockFrame(
                string name,
                SourceSpan openTagSpan,
                SourceSpan openNameSpan,
                TemplateArgument[] arguments
            )
            {
                Name = name;
                OpenTagSpan = openTagSpan;
                OpenNameSpan = openNameSpan;
                Arguments = arguments;
                Children = new List<TemplateNode>();
            }
        }

        private readonly IList<TemplateNode> _rootNodes;
        private readonly IList<TemplateDiagnostic> _diagnostics;
        private readonly IList<TemplateSyntaxSpan> _syntaxSpans;
        private readonly List<BlockFrame> _stack;

        internal TemplateBlockParser(
            IList<TemplateNode> rootNodes,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            if (rootNodes == null)
                throw new ArgumentNullException(nameof(rootNodes));

            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            if (syntaxSpans == null)
                throw new ArgumentNullException(nameof(syntaxSpans));

            _rootNodes = rootNodes;
            _diagnostics = diagnostics;
            _syntaxSpans = syntaxSpans;
            _stack = new List<BlockFrame>();
        }

        internal IList<TemplateNode> CurrentNodes
        {
            get
            {
                if (_stack.Count == 0)
                    return _rootNodes;

                return _stack[_stack.Count - 1].Children;
            }
        }

        internal void ParseOpen(
            string source,
            int tagStart,
            int close,
            int markerPosition
        )
        {
            var markerSpan = new SourceSpan(markerPosition, 1);

            _syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.BlockMarker,
                    markerSpan
                )
            );

            int position = markerPosition + 1;
            SkipWhitespace(source, ref position, close);

            int nameStart = position;

            while (position < close && !char.IsWhiteSpace(source[position]))
                position++;

            int nameLength = position - nameStart;
            string name = nameLength == 0
                ? string.Empty
                : source.Substring(nameStart, nameLength);

            var openTagSpan = new SourceSpan(
                tagStart,
                close + 2 - tagStart
            );

            var nameSpan = new SourceSpan(nameStart, nameLength);

            ValidateName(
                name,
                nameSpan,
                openTagSpan
            );

            var arguments = new List<TemplateArgument>();

            while (position < close)
            {
                SkipWhitespace(source, ref position, close);

                if (position >= close)
                    break;

                TemplateArgumentParser.Parse(
                    source,
                    ref position,
                    close,
                    arguments,
                    _diagnostics,
                    _syntaxSpans
                );
            }

            _syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.Delimiter,
                    new SourceSpan(close, 2)
                )
            );

            if (nameLength == 0)
                return;

            _stack.Add(
                new BlockFrame(
                    name,
                    openTagSpan,
                    nameSpan,
                    arguments.ToArray()
                )
            );
        }

        internal void ParseClose(
            string source,
            int tagStart,
            int close,
            int markerPosition
        )
        {
            _syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.BlockMarker,
                    new SourceSpan(markerPosition, 1)
                )
            );

            int nameStart = markerPosition + 1;

            while (nameStart < close && char.IsWhiteSpace(source[nameStart]))
                nameStart++;

            int nameEnd = close;

            while (nameEnd > nameStart && char.IsWhiteSpace(source[nameEnd - 1]))
                nameEnd--;

            int nameLength = nameEnd - nameStart;
            string name = nameLength == 0
                ? string.Empty
                : source.Substring(nameStart, nameLength);

            var closeTagSpan = new SourceSpan(
                tagStart,
                close + 2 - tagStart
            );

            var nameSpan = new SourceSpan(nameStart, nameLength);

            ValidateName(
                name,
                nameSpan,
                closeTagSpan
            );

            _syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.Delimiter,
                    new SourceSpan(close, 2)
                )
            );

            if (nameLength == 0)
                return;

            if (_stack.Count == 0)
            {
                _diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateParser.UnexpectedClosingBlockDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Closing block '" + name + "' has no open block.",
                        closeTagSpan
                    )
                );

                return;
            }

            int matchingIndex = FindMatchingFrame(name);

            if (matchingIndex < 0)
            {
                BlockFrame currentFrame = _stack[_stack.Count - 1];

                _diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateParser.MismatchedClosingBlockDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Closing block '" + name
                            + "' does not match open block '"
                            + currentFrame.Name + "'.",
                        nameSpan
                    )
                );

                return;
            }

            RecoverInnerBlocks(tagStart, matchingIndex);

            BlockFrame frame = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);

            CurrentNodes.Add(
                new TemplateBlockNode(
                    frame.Name,
                    new SourceSpan(
                        frame.OpenTagSpan.Start,
                        closeTagSpan.End - frame.OpenTagSpan.Start
                    ),
                    frame.OpenTagSpan,
                    frame.OpenNameSpan,
                    closeTagSpan,
                    nameSpan,
                    true,
                    frame.Arguments,
                    frame.Children.ToArray()
                )
            );
        }

        internal void Finalize(string source)
        {
            for (int index = 0; index < _stack.Count; index++)
            {
                BlockFrame frame = _stack[index];

                _diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateParser.UnclosedBlockDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Block '" + frame.Name + "' is missing its closing tag.",
                        frame.OpenTagSpan
                    )
                );
            }

            while (_stack.Count > 0)
            {
                int frameIndex = _stack.Count - 1;
                BlockFrame frame = _stack[frameIndex];
                _stack.RemoveAt(frameIndex);

                var recoverySpan = new SourceSpan(source.Length, 0);

                CurrentNodes.Add(
                    new TemplateBlockNode(
                        frame.Name,
                        new SourceSpan(
                            frame.OpenTagSpan.Start,
                            source.Length - frame.OpenTagSpan.Start
                        ),
                        frame.OpenTagSpan,
                        frame.OpenNameSpan,
                        recoverySpan,
                        recoverySpan,
                        false,
                        frame.Arguments,
                        frame.Children.ToArray()
                    )
                );
            }
        }

        private int FindMatchingFrame(string name)
        {
            int index = _stack.Count - 1;

            while (index >= 0
                && !string.Equals(
                    _stack[index].Name,
                    name,
                    StringComparison.Ordinal
                ))
            {
                index--;
            }

            return index;
        }

        private void RecoverInnerBlocks(
            int recoveryPosition,
            int matchingIndex
        )
        {
            while (_stack.Count - 1 > matchingIndex)
            {
                int unclosedIndex = _stack.Count - 1;
                BlockFrame frame = _stack[unclosedIndex];
                _stack.RemoveAt(unclosedIndex);

                _diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateParser.UnclosedBlockDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Block '" + frame.Name + "' is missing its closing tag.",
                        frame.OpenTagSpan
                    )
                );

                var recoverySpan = new SourceSpan(recoveryPosition, 0);

                CurrentNodes.Add(
                    new TemplateBlockNode(
                        frame.Name,
                        new SourceSpan(
                            frame.OpenTagSpan.Start,
                            recoveryPosition - frame.OpenTagSpan.Start
                        ),
                        frame.OpenTagSpan,
                        frame.OpenNameSpan,
                        recoverySpan,
                        recoverySpan,
                        false,
                        frame.Arguments,
                        frame.Children.ToArray()
                    )
                );
            }
        }

        private void ValidateName(
            string name,
            SourceSpan nameSpan,
            SourceSpan tagSpan
        )
        {
            if (name.Length == 0)
            {
                _diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateParser.EmptyBlockNameDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Block name cannot be empty.",
                        tagSpan
                    )
                );

                return;
            }

            _syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.BlockName,
                    nameSpan
                )
            );

            int invalidOffset =
                TemplateNameRules.FindInvalidConstructNameOffset(name);

            if (invalidOffset < 0)
                return;

            _diagnostics.Add(
                new TemplateDiagnostic(
                    TemplateParser.InvalidBlockNameDiagnosticCode,
                    TemplateDiagnosticSeverity.Error,
                    "Block names must use dot-separated identifiers containing letters, digits, '_', or '-'.",
                    new SourceSpan(
                        nameSpan.Start + invalidOffset,
                        1
                    )
                )
            );
        }

        private static void SkipWhitespace(
            string source,
            ref int position,
            int end
        )
        {
            while (position < end && char.IsWhiteSpace(source[position]))
                position++;
        }
    }
}
