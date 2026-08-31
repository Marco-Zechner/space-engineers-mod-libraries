using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Parses template source into an immutable syntax tree, semantic source
    /// spans, and recoverable diagnostics.
    /// </summary>
    public static class TemplateParser
    {
        /// <summary>
        /// Diagnostic code reported for a tag with no name.
        /// </summary>
        public const string EmptyTagDiagnosticCode = "MZTT1001";

        /// <summary>
        /// Diagnostic code reported for an invalid tag name.
        /// </summary>
        public const string InvalidTagNameDiagnosticCode = "MZTT1002";

        /// <summary>
        /// Diagnostic code reported when an opening tag delimiter has no
        /// matching closing delimiter.
        /// </summary>
        public const string UnterminatedTagDiagnosticCode = "MZTT1003";

        /// <summary>
        /// Diagnostic code reported for a closing delimiter without a
        /// matching opening delimiter.
        /// </summary>
        public const string UnexpectedClosingDelimiterDiagnosticCode =
            "MZTT1004";

        /// <summary>
        /// Diagnostic code reported when a named argument has no name.
        /// </summary>
        public const string MissingArgumentNameDiagnosticCode = "MZTT1005";

        /// <summary>
        /// Diagnostic code reported when a named argument has no value.
        /// </summary>
        public const string MissingArgumentValueDiagnosticCode = "MZTT1006";

        /// <summary>
        /// Diagnostic code reported for a quoted value without a closing
        /// quote.
        /// </summary>
        public const string UnterminatedStringDiagnosticCode = "MZTT1007";

        /// <summary>
        /// Diagnostic code reported for an invalid named-argument name.
        /// </summary>
        public const string InvalidArgumentNameDiagnosticCode = "MZTT1008";

        /// <summary>
        /// Diagnostic code reported for a structural block tag with no name.
        /// </summary>
        public const string EmptyBlockNameDiagnosticCode = "MZTT1009";

        /// <summary>
        /// Diagnostic code reported for an invalid block name.
        /// </summary>
        public const string InvalidBlockNameDiagnosticCode = "MZTT1010";

        /// <summary>
        /// Diagnostic code reported for a closing block without an open block.
        /// </summary>
        public const string UnexpectedClosingBlockDiagnosticCode = "MZTT1011";

        /// <summary>
        /// Diagnostic code reported when a closing block does not match the
        /// currently open block.
        /// </summary>
        public const string MismatchedClosingBlockDiagnosticCode = "MZTT1012";

        /// <summary>
        /// Diagnostic code reported when an opening block reaches end of
        /// source without a matching closing block.
        /// </summary>
        public const string UnclosedBlockDiagnosticCode = "MZTT1013";

        /// <summary>
        /// Parses the supplied source while preserving exact source positions.
        /// Malformed input produces diagnostics instead of aborting parsing.
        /// </summary>
        public static TemplateParseResult Parse(string source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var rootNodes =
                new List<TemplateNode>();

            var blockStack =
                new List<BlockFrame>();

            var diagnostics =
                new List<TemplateDiagnostic>();

            var syntaxSpans =
                new List<TemplateSyntaxSpan>();

            int index = 0;
            int literalStart = 0;

            while (index < source.Length)
            {
                if (Matches(source, index, "{{"))
                {
                    AddLiteral(
                        source,
                        literalStart,
                        index - literalStart,
                        GetCurrentNodes(
                            rootNodes,
                            blockStack
                        ),
                        syntaxSpans
                    );

                    int tagStart = index;

                    syntaxSpans.Add(
                        new TemplateSyntaxSpan(
                            TemplateSyntaxKind.Delimiter,
                            new SourceSpan(index, 2)
                        )
                    );

                    int close =
                        FindTagClose(
                            source,
                            index + 2
                        );

                    if (close < 0)
                    {
                        diagnostics.Add(
                            new TemplateDiagnostic(
                                UnterminatedTagDiagnosticCode,
                                TemplateDiagnosticSeverity.Error,
                                "Tag is missing its closing '}}' delimiter.",
                                new SourceSpan(
                                    tagStart,
                                    source.Length - tagStart
                                )
                            )
                        );

                        GetCurrentNodes(
                            rootNodes,
                            blockStack
                        ).Add(
                            new TemplateTextNode(
                                source.Substring(tagStart),
                                new SourceSpan(
                                    tagStart,
                                    source.Length - tagStart
                                )
                            )
                        );

                        if (source.Length > tagStart + 2)
                        {
                            syntaxSpans.Add(
                                new TemplateSyntaxSpan(
                                    TemplateSyntaxKind.LiteralText,
                                    new SourceSpan(
                                        tagStart + 2,
                                        source.Length - tagStart - 2
                                    )
                                )
                            );
                        }

                        literalStart = source.Length;
                        index = source.Length;
                        break;
                    }

                    int markerPosition =
                        tagStart + 2;

                    SkipWhitespace(
                        source,
                        ref markerPosition,
                        close
                    );

                    if (
                        markerPosition < close
                        && source[markerPosition] == '#'
                    )
                    {
                        ParseBlockOpen(
                            source,
                            tagStart,
                            close,
                            markerPosition,
                            blockStack,
                            diagnostics,
                            syntaxSpans
                        );
                    }
                    else if (
                        markerPosition < close
                        && source[markerPosition] == '/'
                    )
                    {
                        ParseBlockClose(
                            source,
                            tagStart,
                            close,
                            markerPosition,
                            rootNodes,
                            blockStack,
                            diagnostics,
                            syntaxSpans
                        );
                    }
                    else
                    {
                        ParseTag(
                            source,
                            tagStart,
                            close,
                            GetCurrentNodes(
                                rootNodes,
                                blockStack
                            ),
                            diagnostics,
                            syntaxSpans
                        );
                    }

                    index = close + 2;
                    literalStart = index;
                    continue;
                }

                if (Matches(source, index, "}}"))
                {
                    diagnostics.Add(
                        new TemplateDiagnostic(
                            UnexpectedClosingDelimiterDiagnosticCode,
                            TemplateDiagnosticSeverity.Error,
                            "Closing '}}' delimiter has no matching opening '{{' delimiter.",
                            new SourceSpan(index, 2)
                        )
                    );

                    index += 2;
                    continue;
                }

                index++;
            }

            AddLiteral(
                source,
                literalStart,
                source.Length - literalStart,
                GetCurrentNodes(
                    rootNodes,
                    blockStack
                ),
                syntaxSpans
            );

            FinalizeUnclosedBlocks(
                source,
                rootNodes,
                blockStack,
                diagnostics
            );

            SortDiagnosticsBySource(
                diagnostics
            );

            return
                new TemplateParseResult(
                    source,
                    new TemplateDocument(rootNodes),
                    diagnostics,
                    syntaxSpans
                );
        }

        private sealed class BlockFrame
        {
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

            public string Name;
            public SourceSpan OpenTagSpan;
            public SourceSpan OpenNameSpan;
            public TemplateArgument[] Arguments;
            public List<TemplateNode> Children;
        }

        private static IList<TemplateNode> GetCurrentNodes(
            IList<TemplateNode> rootNodes,
            IList<BlockFrame> blockStack
        )
        {
            if (blockStack.Count == 0)
                return rootNodes;

            return
                blockStack[
                    blockStack.Count - 1
                ].Children;
        }

        private static void ParseBlockOpen(
            string source,
            int tagStart,
            int close,
            int markerPosition,
            IList<BlockFrame> blockStack,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            var markerSpan =
                new SourceSpan(
                    markerPosition,
                    1
                );

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.BlockMarker,
                    markerSpan
                )
            );

            int position =
                markerPosition + 1;

            SkipWhitespace(
                source,
                ref position,
                close
            );

            int nameStart = position;

            while (
                position < close
                && !char.IsWhiteSpace(source[position])
            )
            {
                position++;
            }

            int nameLength =
                position - nameStart;

            string name =
                nameLength == 0
                    ? string.Empty
                    : source.Substring(
                        nameStart,
                        nameLength
                    );

            var openTagSpan =
                new SourceSpan(
                    tagStart,
                    close + 2 - tagStart
                );

            var nameSpan =
                new SourceSpan(
                    nameStart,
                    nameLength
                );

            ValidateBlockName(
                name,
                nameSpan,
                openTagSpan,
                diagnostics,
                syntaxSpans
            );

            var arguments =
                new List<TemplateArgument>();

            while (position < close)
            {
                SkipWhitespace(
                    source,
                    ref position,
                    close
                );

                if (position >= close)
                    break;

                ParseArgument(
                    source,
                    ref position,
                    close,
                    arguments,
                    diagnostics,
                    syntaxSpans
                );
            }

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.Delimiter,
                    new SourceSpan(close, 2)
                )
            );

            if (nameLength == 0)
                return;

            blockStack.Add(
                new BlockFrame(
                    name,
                    openTagSpan,
                    nameSpan,
                    arguments.ToArray()
                )
            );
        }

        private static void ParseBlockClose(
            string source,
            int tagStart,
            int close,
            int markerPosition,
            IList<TemplateNode> rootNodes,
            IList<BlockFrame> blockStack,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            var markerSpan =
                new SourceSpan(
                    markerPosition,
                    1
                );

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.BlockMarker,
                    markerSpan
                )
            );

            int nameStart =
                markerPosition + 1;

            while (
                nameStart < close
                && char.IsWhiteSpace(source[nameStart])
            )
            {
                nameStart++;
            }

            int nameEnd = close;

            while (
                nameEnd > nameStart
                && char.IsWhiteSpace(source[nameEnd - 1])
            )
            {
                nameEnd--;
            }

            int nameLength =
                nameEnd - nameStart;

            string name =
                nameLength == 0
                    ? string.Empty
                    : source.Substring(
                        nameStart,
                        nameLength
                    );

            var closeTagSpan =
                new SourceSpan(
                    tagStart,
                    close + 2 - tagStart
                );

            var nameSpan =
                new SourceSpan(
                    nameStart,
                    nameLength
                );

            ValidateBlockName(
                name,
                nameSpan,
                closeTagSpan,
                diagnostics,
                syntaxSpans
            );

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.Delimiter,
                    new SourceSpan(close, 2)
                )
            );

            if (nameLength == 0)
                return;

            if (blockStack.Count == 0)
            {
                diagnostics.Add(
                    new TemplateDiagnostic(
                        UnexpectedClosingBlockDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Closing block '" + name + "' has no open block.",
                        closeTagSpan
                    )
                );

                return;
            }

            int matchingIndex =
                blockStack.Count - 1;

            while (
                matchingIndex >= 0
                && !string.Equals(
                    blockStack[matchingIndex].Name,
                    name,
                    StringComparison.Ordinal
                )
            )
            {
                matchingIndex--;
            }

            if (matchingIndex < 0)
            {
                BlockFrame currentFrame =
                    blockStack[
                        blockStack.Count - 1
                    ];

                diagnostics.Add(
                    new TemplateDiagnostic(
                        MismatchedClosingBlockDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Closing block '" + name + "' does not match open block '" + currentFrame.Name + "'.",
                        nameSpan
                    )
                );

                return;
            }

            while (
                blockStack.Count - 1
                > matchingIndex
            )
            {
                int unclosedIndex =
                    blockStack.Count - 1;

                BlockFrame unclosedFrame =
                    blockStack[
                        unclosedIndex
                    ];

                blockStack.RemoveAt(
                    unclosedIndex
                );

                diagnostics.Add(
                    new TemplateDiagnostic(
                        UnclosedBlockDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Block '" + unclosedFrame.Name + "' is missing its closing tag.",
                        unclosedFrame.OpenTagSpan
                    )
                );

                var recoverySpan =
                    new SourceSpan(
                        tagStart,
                        0
                    );

                var recoveredBlock =
                    new TemplateBlockNode(
                        unclosedFrame.Name,
                        new SourceSpan(
                            unclosedFrame.OpenTagSpan.Start,
                            tagStart
                            - unclosedFrame.OpenTagSpan.Start
                        ),
                        unclosedFrame.OpenTagSpan,
                        unclosedFrame.OpenNameSpan,
                        recoverySpan,
                        recoverySpan,
                        false,
                        unclosedFrame.Arguments,
                        unclosedFrame.Children.ToArray()
                    );

                GetCurrentNodes(
                    rootNodes,
                    blockStack
                ).Add(
                    recoveredBlock
                );
            }

            BlockFrame frame =
                blockStack[
                    blockStack.Count - 1
                ];

            blockStack.RemoveAt(
                blockStack.Count - 1
            );

            var block =
                new TemplateBlockNode(
                    frame.Name,
                    new SourceSpan(
                        frame.OpenTagSpan.Start,
                        closeTagSpan.End
                        - frame.OpenTagSpan.Start
                    ),
                    frame.OpenTagSpan,
                    frame.OpenNameSpan,
                    closeTagSpan,
                    nameSpan,
                    true,
                    frame.Arguments,
                    frame.Children.ToArray()
                );

            GetCurrentNodes(
                rootNodes,
                blockStack
            ).Add(block);
        }

        private static bool ValidateBlockName(
            string name,
            SourceSpan nameSpan,
            SourceSpan tagSpan,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            if (name.Length == 0)
            {
                diagnostics.Add(
                    new TemplateDiagnostic(
                        EmptyBlockNameDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Block name cannot be empty.",
                        tagSpan
                    )
                );

                return false;
            }

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.BlockName,
                    nameSpan
                )
            );

            int invalidOffset =
                FindInvalidNameOffset(name);

            if (invalidOffset < 0)
                return true;

            diagnostics.Add(
                new TemplateDiagnostic(
                    InvalidBlockNameDiagnosticCode,
                    TemplateDiagnosticSeverity.Error,
                    "Block names must use dot-separated identifiers containing letters, digits, '_', or '-'.",
                    new SourceSpan(
                        nameSpan.Start + invalidOffset,
                        1
                    )
                )
            );

            return false;
        }

        private static void FinalizeUnclosedBlocks(
            string source,
            IList<TemplateNode> rootNodes,
            IList<BlockFrame> blockStack,
            IList<TemplateDiagnostic> diagnostics
        )
        {
            for (
                int index = 0;
                index < blockStack.Count;
                index++
            )
            {
                BlockFrame frame =
                    blockStack[index];

                diagnostics.Add(
                    new TemplateDiagnostic(
                        UnclosedBlockDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Block '" + frame.Name + "' is missing its closing tag.",
                        frame.OpenTagSpan
                    )
                );
            }

            while (blockStack.Count > 0)
            {
                int frameIndex =
                    blockStack.Count - 1;

                BlockFrame frame =
                    blockStack[frameIndex];

                blockStack.RemoveAt(
                    frameIndex
                );

                var recoverySpan =
                    new SourceSpan(
                        source.Length,
                        0
                    );

                var block =
                    new TemplateBlockNode(
                        frame.Name,
                        new SourceSpan(
                            frame.OpenTagSpan.Start,
                            source.Length
                            - frame.OpenTagSpan.Start
                        ),
                        frame.OpenTagSpan,
                        frame.OpenNameSpan,
                        recoverySpan,
                        recoverySpan,
                        false,
                        frame.Arguments,
                        frame.Children.ToArray()
                    );

                GetCurrentNodes(
                    rootNodes,
                    blockStack
                ).Add(block);
            }
        }

        private static void SortDiagnosticsBySource(
            IList<TemplateDiagnostic> diagnostics
        )
        {
            for (
                int index = 1;
                index < diagnostics.Count;
                index++
            )
            {
                TemplateDiagnostic current =
                    diagnostics[index];

                int destination =
                    index - 1;

                while (
                    destination >= 0
                    && diagnostics[destination].Span.Start
                    > current.Span.Start
                )
                {
                    diagnostics[destination + 1] =
                        diagnostics[destination];

                    destination--;
                }

                diagnostics[destination + 1] =
                    current;
            }
        }
        private static void ParseTag(
            string source,
            int tagStart,
            int close,
            IList<TemplateNode> nodes,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            int position = tagStart + 2;

            SkipWhitespace(
                source,
                ref position,
                close
            );

            int nameStart = position;

            while (
                position < close
                && !char.IsWhiteSpace(source[position])
            )
            {
                position++;
            }

            int nameLength =
                position - nameStart;

            string name =
                nameLength == 0
                    ? string.Empty
                    : source.Substring(
                        nameStart,
                        nameLength
                    );

            var tagSpan =
                new SourceSpan(
                    tagStart,
                    close + 2 - tagStart
                );

            var nameSpan =
                new SourceSpan(
                    nameStart,
                    nameLength
                );

            if (nameLength == 0)
            {
                diagnostics.Add(
                    new TemplateDiagnostic(
                        EmptyTagDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Tag name cannot be empty.",
                        tagSpan
                    )
                );
            }
            else
            {
                syntaxSpans.Add(
                    new TemplateSyntaxSpan(
                        TemplateSyntaxKind.TagName,
                        nameSpan
                    )
                );

                int invalidOffset =
                    FindInvalidNameOffset(name);

                if (invalidOffset >= 0)
                {
                    diagnostics.Add(
                        new TemplateDiagnostic(
                            InvalidTagNameDiagnosticCode,
                            TemplateDiagnosticSeverity.Error,
                            "Tag names must use dot-separated identifiers containing letters, digits, '_', or '-'.",
                            new SourceSpan(
                                nameStart + invalidOffset,
                                1
                            )
                        )
                    );
                }
            }

            var arguments =
                new List<TemplateArgument>();

            while (position < close)
            {
                SkipWhitespace(
                    source,
                    ref position,
                    close
                );

                if (position >= close)
                    break;

                ParseArgument(
                    source,
                    ref position,
                    close,
                    arguments,
                    diagnostics,
                    syntaxSpans
                );
            }

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.Delimiter,
                    new SourceSpan(close, 2)
                )
            );

            nodes.Add(
                new TemplateTagNode(
                    name,
                    tagSpan,
                    nameSpan,
                    arguments.ToArray()
                )
            );
        }

        private static void ParseArgument(
            string source,
            ref int position,
            int close,
            IList<TemplateArgument> arguments,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            int argumentStart = position;

            if (source[position] == '"')
            {
                var quotedValue =
                    ParseValue(
                        source,
                        ref position,
                        close,
                        diagnostics,
                        syntaxSpans
                    );

                arguments.Add(
                    new TemplatePositionalArgument(
                        quotedValue
                    )
                );

                return;
            }

            int candidateStart = position;

            while (
                position < close
                && !char.IsWhiteSpace(source[position])
                && source[position] != '='
            )
            {
                position++;
            }

            int candidateEnd = position;
            int afterCandidate = position;

            SkipWhitespace(
                source,
                ref afterCandidate,
                close
            );

            bool named =
                afterCandidate < close
                && source[afterCandidate] == '=';

            if (!named)
            {
                position = candidateStart;

                var positionalValue =
                    ParseValue(
                        source,
                        ref position,
                        close,
                        diagnostics,
                        syntaxSpans
                    );

                arguments.Add(
                    new TemplatePositionalArgument(
                        positionalValue
                    )
                );

                return;
            }

            string argumentName =
                source.Substring(
                    candidateStart,
                    candidateEnd - candidateStart
                );

            var argumentNameSpan =
                new SourceSpan(
                    candidateStart,
                    candidateEnd - candidateStart
                );

            if (argumentName.Length == 0)
            {
                diagnostics.Add(
                    new TemplateDiagnostic(
                        MissingArgumentNameDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Named argument is missing its name.",
                        new SourceSpan(
                            afterCandidate,
                            1
                        )
                    )
                );
            }
            else
            {
                syntaxSpans.Add(
                    new TemplateSyntaxSpan(
                        TemplateSyntaxKind.ArgumentName,
                        argumentNameSpan
                    )
                );

                int invalidArgumentNameOffset =
                    FindInvalidArgumentNameOffset(
                        argumentName
                    );

                if (invalidArgumentNameOffset >= 0)
                {
                    diagnostics.Add(
                        new TemplateDiagnostic(
                            InvalidArgumentNameDiagnosticCode,
                            TemplateDiagnosticSeverity.Error,
                            "Argument names must begin with a letter or '_' and contain only letters, digits, '_', or '-'.",
                            new SourceSpan(
                                candidateStart
                                + invalidArgumentNameOffset,
                                1
                            )
                        )
                    );
                }
            }

            position = afterCandidate;

            var equalsSpan =
                new SourceSpan(
                    position,
                    1
                );

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.AssignmentOperator,
                    equalsSpan
                )
            );

            position++;

            SkipWhitespace(
                source,
                ref position,
                close
            );

            TemplateArgumentValue value;

            if (position >= close)
            {
                diagnostics.Add(
                    new TemplateDiagnostic(
                        MissingArgumentValueDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Named argument is missing its value.",
                        equalsSpan
                    )
                );

                value =
                    new TemplateArgumentValue(
                        TemplateArgumentValueKind.Missing,
                        string.Empty,
                        string.Empty,
                        new SourceSpan(
                            close,
                            0
                        ),
                        new SourceSpan(
                            close,
                            0
                        )
                    );
            }
            else
            {
                value =
                    ParseValue(
                        source,
                        ref position,
                        close,
                        diagnostics,
                        syntaxSpans
                    );
            }

            int argumentEnd =
                value.Kind
                == TemplateArgumentValueKind.Missing
                    ? position
                    : value.Span.End;

            arguments.Add(
                new TemplateNamedArgument(
                    argumentName,
                    argumentNameSpan,
                    equalsSpan,
                    value,
                    new SourceSpan(
                        argumentStart,
                        argumentEnd - argumentStart
                    )
                )
            );
        }

        private static TemplateArgumentValue ParseValue(
            string source,
            ref int position,
            int close,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            if (source[position] == '"')
            {
                int quoteStart = position;
                position++;

                int contentStart = position;
                bool escaped = false;

                while (position < close)
                {
                    char current =
                        source[position];

                    if (escaped)
                    {
                        escaped = false;
                        position++;
                        continue;
                    }

                    if (current == '\\')
                    {
                        escaped = true;
                        position++;
                        continue;
                    }

                    if (current == '"')
                    {
                        int quoteEnd = position;
                        position++;

                        var span =
                            new SourceSpan(
                                quoteStart,
                                position - quoteStart
                            );

                        var contentSpan =
                            new SourceSpan(
                                contentStart,
                                quoteEnd - contentStart
                            );

                        syntaxSpans.Add(
                            new TemplateSyntaxSpan(
                                TemplateSyntaxKind.StringValue,
                                span
                            )
                        );

                        return
                            new TemplateArgumentValue(
                                TemplateArgumentValueKind.String,
                                source.Substring(
                                    quoteStart,
                                    span.Length
                                ),
                                source.Substring(
                                    contentStart,
                                    contentSpan.Length
                                ),
                                span,
                                contentSpan
                            );
                    }

                    position++;
                }

                var unterminatedSpan =
                    new SourceSpan(
                        quoteStart,
                        close - quoteStart
                    );

                diagnostics.Add(
                    new TemplateDiagnostic(
                        UnterminatedStringDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Quoted argument value is missing its closing quote.",
                        unterminatedSpan
                    )
                );

                syntaxSpans.Add(
                    new TemplateSyntaxSpan(
                        TemplateSyntaxKind.StringValue,
                        unterminatedSpan
                    )
                );

                return
                    new TemplateArgumentValue(
                        TemplateArgumentValueKind.String,
                        source.Substring(
                            quoteStart,
                            unterminatedSpan.Length
                        ),
                        source.Substring(
                            contentStart,
                            close - contentStart
                        ),
                        unterminatedSpan,
                        new SourceSpan(
                            contentStart,
                            close - contentStart
                        )
                    );
            }

            int valueStart = position;

            while (
                position < close
                && !char.IsWhiteSpace(source[position])
            )
            {
                position++;
            }

            int valueLength =
                position - valueStart;

            string raw =
                source.Substring(
                    valueStart,
                    valueLength
                );

            var valueSpan =
                new SourceSpan(
                    valueStart,
                    valueLength
                );

            TemplateArgumentValueKind kind =
                ClassifyValue(raw);

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    SyntaxKindForValue(kind),
                    valueSpan
                )
            );

            return
                new TemplateArgumentValue(
                    kind,
                    raw,
                    raw,
                    valueSpan,
                    valueSpan
                );
        }

        private static TemplateArgumentValueKind ClassifyValue(
            string value
        )
        {
            if (
                value == "true"
                || value == "false"
            )
            {
                return
                    TemplateArgumentValueKind.Boolean;
            }

            if (IsNumber(value))
            {
                return
                    TemplateArgumentValueKind.Number;
            }

            return
                TemplateArgumentValueKind.Bare;
        }

        private static TemplateSyntaxKind SyntaxKindForValue(
            TemplateArgumentValueKind kind
        )
        {
            switch (kind)
            {
                case TemplateArgumentValueKind.String:
                    return TemplateSyntaxKind.StringValue;

                case TemplateArgumentValueKind.Number:
                    return TemplateSyntaxKind.NumberValue;

                case TemplateArgumentValueKind.Boolean:
                    return TemplateSyntaxKind.BooleanValue;

                default:
                    return TemplateSyntaxKind.BareValue;
            }
        }

        private static bool IsNumber(string value)
        {
            if (value.Length == 0)
                return false;

            int index = 0;

            if (
                value[index] == '+'
                || value[index] == '-'
            )
            {
                index++;

                if (index == value.Length)
                    return false;
            }

            bool digitSeen = false;
            bool decimalSeen = false;

            while (index < value.Length)
            {
                char current =
                    value[index];

                if (
                    current >= '0'
                    && current <= '9'
                )
                {
                    digitSeen = true;
                    index++;
                    continue;
                }

                if (
                    current == '.'
                    && !decimalSeen
                )
                {
                    decimalSeen = true;
                    index++;
                    continue;
                }

                return false;
            }

            return digitSeen;
        }

        private static int FindTagClose(
            string source,
            int start
        )
        {
            int index = start;

            while (index < source.Length)
            {
                if (source[index] == '"')
                {
                    int quoteEnd =
                        FindQuoteEnd(
                            source,
                            index + 1
                        );

                    if (quoteEnd >= 0)
                    {
                        index = quoteEnd + 1;
                        continue;
                    }

                    return source.IndexOf(
                        "}}",
                        index + 1,
                        StringComparison.Ordinal
                    );
                }

                if (Matches(source, index, "}}"))
                    return index;

                index++;
            }

            return -1;
        }

        private static int FindQuoteEnd(
            string source,
            int start
        )
        {
            bool escaped = false;

            for (
                int index = start;
                index < source.Length;
                index++
            )
            {
                char current =
                    source[index];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == '"')
                    return index;
            }

            return -1;
        }

        private static void SkipWhitespace(
            string source,
            ref int position,
            int end
        )
        {
            while (
                position < end
                && char.IsWhiteSpace(source[position])
            )
            {
                position++;
            }
        }

        private static void AddLiteral(
            string source,
            int start,
            int length,
            IList<TemplateNode> nodes,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            if (length <= 0)
                return;

            var span =
                new SourceSpan(
                    start,
                    length
                );

            nodes.Add(
                new TemplateTextNode(
                    source.Substring(
                        start,
                        length
                    ),
                    span
                )
            );

            syntaxSpans.Add(
                new TemplateSyntaxSpan(
                    TemplateSyntaxKind.LiteralText,
                    span
                )
            );
        }

        private static int FindInvalidArgumentNameOffset(
            string name
        )
        {
            if (name.Length == 0)
                return 0;

            if (!IsIdentifierStart(name[0]))
                return 0;

            for (
                int index = 1;
                index < name.Length;
                index++
            )
            {
                if (!IsIdentifierPart(name[index]))
                    return index;
            }

            return -1;
        }

        private static int FindInvalidNameOffset(
            string name
        )
        {
            int index = 0;

            while (index < name.Length)
            {
                if (!IsIdentifierStart(name[index]))
                    return index;

                index++;

                while (
                    index < name.Length
                    && IsIdentifierPart(name[index])
                )
                {
                    index++;
                }

                if (index == name.Length)
                    return -1;

                if (name[index] != '.')
                    return index;

                index++;

                if (index == name.Length)
                    return index - 1;
            }

            return -1;
        }

        private static bool IsIdentifierStart(char value)
        {
            return
                value == '_'
                || (
                    value >= 'A'
                    && value <= 'Z'
                )
                || (
                    value >= 'a'
                    && value <= 'z'
                );
        }

        private static bool IsIdentifierPart(char value)
        {
            return
                IsIdentifierStart(value)
                || (
                    value >= '0'
                    && value <= '9'
                )
                || value == '-';
        }

        private static bool Matches(
            string source,
            int index,
            string value
        )
        {
            if (
                index < 0
                || index + value.Length > source.Length
            )
            {
                return false;
            }

            for (
                int valueIndex = 0;
                valueIndex < value.Length;
                valueIndex++
            )
            {
                if (
                    source[index + valueIndex]
                    != value[valueIndex]
                )
                {
                    return false;
                }
            }

            return true;
        }
    }
}