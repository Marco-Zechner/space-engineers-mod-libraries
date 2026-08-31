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
        public const string UnexpectedClosingDelimiterDiagnosticCode = "MZTT1004";

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
                throw new ArgumentNullException(nameof(source));

            var rootNodes = new List<TemplateNode>();
            var diagnostics = new List<TemplateDiagnostic>();
            var syntaxSpans = new List<TemplateSyntaxSpan>();
            var blockParser = new TemplateBlockParser(rootNodes, diagnostics, syntaxSpans);

            int index = 0;
            int literalStart = 0;

            while (index < source.Length)
            {
                if (Matches(source, index, "{{"))
                {
                    AddLiteral(source, literalStart, index - literalStart, blockParser.CurrentNodes, syntaxSpans);

                    int tagStart = index;

                    syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.Delimiter, new SourceSpan(index, 2)));

                    int close = FindTagClose(source, index + 2);

                    if (close < 0)
                    {
                        diagnostics.Add(new TemplateDiagnostic(UnterminatedTagDiagnosticCode, TemplateDiagnosticSeverity.Error, "Tag is missing its closing '}}' delimiter.", new SourceSpan(tagStart, source.Length - tagStart)));

                        blockParser.CurrentNodes.Add(new TemplateTextNode(source.Substring(tagStart), new SourceSpan(tagStart, source.Length - tagStart)));

                        if (source.Length > tagStart + 2)
                        {
                            syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.LiteralText, new SourceSpan(tagStart + 2, source.Length - tagStart - 2)));
                        }

                        literalStart = source.Length;
                        index = source.Length;
                        break;
                    }

                    int markerPosition = tagStart + 2;

                    SkipWhitespace(source, ref markerPosition, close);

                    if (markerPosition < close && source[markerPosition] == '#')
                    {
                        blockParser.ParseOpen(source, tagStart, close, markerPosition);
                    }
                    else if (markerPosition < close && source[markerPosition] == '/')
                    {
                        blockParser.ParseClose(source, tagStart, close, markerPosition);
                    }
                    else
                    {
                        ParseTag(source, tagStart, close, blockParser.CurrentNodes, diagnostics, syntaxSpans);
                    }

                    index = close + 2;
                    literalStart = index;
                    continue;
                }

                if (Matches(source, index, "}}"))
                {
                    diagnostics.Add(new TemplateDiagnostic(UnexpectedClosingDelimiterDiagnosticCode, TemplateDiagnosticSeverity.Error, "Closing '}}' delimiter has no matching opening '{{' delimiter.", new SourceSpan(index, 2)));

                    index += 2;
                    continue;
                }

                index++;
            }

            AddLiteral(source, literalStart, source.Length - literalStart, blockParser.CurrentNodes, syntaxSpans);

            blockParser.Finalize(source);

            TemplateDiagnosticUtilities.SortBySource(diagnostics);

            return new TemplateParseResult(source, new TemplateDocument(rootNodes), diagnostics, syntaxSpans);
        }

        private static void ParseTag(string source, int tagStart, int close, IList<TemplateNode> nodes, IList<TemplateDiagnostic> diagnostics, IList<TemplateSyntaxSpan> syntaxSpans)
        {
            int position = tagStart + 2;

            SkipWhitespace(source, ref position, close);

            int nameStart = position;

            while (position < close && !char.IsWhiteSpace(source[position]))
            {
                position++;
            }

            int nameLength = position - nameStart;
            string name = nameLength == 0 ? string.Empty : source.Substring(nameStart, nameLength);

            var tagSpan = new SourceSpan(tagStart, close + 2 - tagStart);
            var nameSpan = new SourceSpan(nameStart, nameLength);

            if (nameLength == 0)
            {
                diagnostics.Add(new TemplateDiagnostic(EmptyTagDiagnosticCode, TemplateDiagnosticSeverity.Error, "Tag name cannot be empty.", tagSpan));
            }
            else
            {
                syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.TagName, nameSpan));

                int invalidOffset = TemplateNameRules.FindInvalidConstructNameOffset(name);

                if (invalidOffset >= 0)
                {
                    diagnostics.Add(new TemplateDiagnostic(InvalidTagNameDiagnosticCode, TemplateDiagnosticSeverity.Error, "Tag names must use dot-separated identifiers containing letters, digits, '_', or '-'.", new SourceSpan(nameStart + invalidOffset, 1)));
                }
            }

            var arguments = new List<TemplateArgument>();

            while (position < close)
            {
                SkipWhitespace(source, ref position, close);

                if (position >= close)
                    break;

                TemplateArgumentParser.Parse(source, ref position, close, arguments, diagnostics, syntaxSpans);
            }

            syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.Delimiter, new SourceSpan(close, 2)));

            nodes.Add(new TemplateTagNode(name, tagSpan, nameSpan, arguments.ToArray()));
        }

        private static int FindTagClose(string source, int start)
        {
            int index = start;

            while (index < source.Length)
            {
                if (source[index] == '"')
                {
                    int quoteEnd = FindQuoteEnd(source, index + 1);

                    if (quoteEnd >= 0)
                    {
                        index = quoteEnd + 1;
                        continue;
                    }

                    return source.IndexOf("}}", index + 1, StringComparison.Ordinal);
                }

                if (Matches(source, index, "}}"))
                    return index;

                index++;
            }

            return -1;
        }

        private static int FindQuoteEnd(string source, int start)
        {
            bool escaped = false;

            for (int index = start; index < source.Length; index++)
            {
                char current = source[index];

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

        private static void SkipWhitespace(string source, ref int position, int end)
        {
            while (position < end && char.IsWhiteSpace(source[position]))
            {
                position++;
            }
        }

        private static void AddLiteral(string source, int start, int length, IList<TemplateNode> nodes, IList<TemplateSyntaxSpan> syntaxSpans)
        {
            if (length <= 0)
                return;

            var span = new SourceSpan(start, length);

            nodes.Add(new TemplateTextNode(source.Substring(start, length), span));
            syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.LiteralText, span));
        }

        private static bool Matches(string source, int index, string value)
        {
            if (index < 0 || index + value.Length > source.Length)
                return false;

            for (int valueIndex = 0; valueIndex < value.Length; valueIndex++)
            {
                if (source[index + valueIndex] != value[valueIndex])
                    return false;
            }

            return true;
        }
    }
}
