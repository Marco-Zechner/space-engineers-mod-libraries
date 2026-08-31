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
        /// Parses the supplied source while preserving exact source positions.
        /// Malformed input produces diagnostics instead of aborting parsing.
        /// </summary>
        public static TemplateParseResult Parse(string source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var nodes = new List<TemplateNode>();
            var diagnostics = new List<TemplateDiagnostic>();
            var syntaxSpans = new List<TemplateSyntaxSpan>();

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
                        nodes,
                        syntaxSpans
                    );

                    int tagStart = index;

                    syntaxSpans.Add(
                        new TemplateSyntaxSpan(
                            TemplateSyntaxKind.Delimiter,
                            new SourceSpan(index, 2)
                        )
                    );

                    int close = source.IndexOf(
                        "}}",
                        index + 2,
                        StringComparison.Ordinal
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

                        nodes.Add(
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

                    int contentStart = tagStart + 2;
                    int contentEnd = close;

                    while (
                        contentStart < contentEnd
                        && char.IsWhiteSpace(source[contentStart])
                    )
                    {
                        contentStart++;
                    }

                    while (
                        contentEnd > contentStart
                        && char.IsWhiteSpace(source[contentEnd - 1])
                    )
                    {
                        contentEnd--;
                    }

                    int nameLength = contentEnd - contentStart;

                    string name =
                        nameLength == 0
                            ? string.Empty
                            : source.Substring(
                                contentStart,
                                nameLength
                            );

                    var tagSpan =
                        new SourceSpan(
                            tagStart,
                            close + 2 - tagStart
                        );

                    var nameSpan =
                        new SourceSpan(
                            contentStart,
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
                            FindInvalidTagNameOffset(name);

                        if (invalidOffset >= 0)
                        {
                            diagnostics.Add(
                                new TemplateDiagnostic(
                                    InvalidTagNameDiagnosticCode,
                                    TemplateDiagnosticSeverity.Error,
                                    "Tag names must use dot-separated identifiers containing letters, digits, '_', or '-'.",
                                    new SourceSpan(
                                        contentStart + invalidOffset,
                                        1
                                    )
                                )
                            );
                        }
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
                            nameSpan
                        )
                    );

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
                nodes,
                syntaxSpans
            );

            return
                new TemplateParseResult(
                    source,
                    new TemplateDocument(nodes),
                    diagnostics,
                    syntaxSpans
                );
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

            var span = new SourceSpan(start, length);

            nodes.Add(
                new TemplateTextNode(
                    source.Substring(start, length),
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

        private static int FindInvalidTagNameOffset(string name)
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
                || (value >= 'A' && value <= 'Z')
                || (value >= 'a' && value <= 'z');
        }

        private static bool IsIdentifierPart(char value)
        {
            return
                IsIdentifierStart(value)
                || (value >= '0' && value <= '9')
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