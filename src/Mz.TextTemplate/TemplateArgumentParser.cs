using System.Collections.Generic;

namespace Mz.TextTemplate
{
    internal static class TemplateArgumentParser
    {
        internal static void Parse(string source, ref int position, int close, IList<TemplateArgument> arguments, IList<TemplateDiagnostic> diagnostics, IList<TemplateSyntaxSpan> syntaxSpans)
        {
            int argumentStart = position;

            if (source[position] == '"')
            {
                var quotedValue = ParseValue(source, ref position, close, diagnostics, syntaxSpans);

                arguments.Add(new TemplatePositionalArgument(quotedValue));
                return;
            }

            int candidateStart = position;

            while (position < close && !char.IsWhiteSpace(source[position]) && source[position] != '=')
            {
                position++;
            }

            int candidateEnd = position;
            int afterCandidate = position;
            SkipWhitespace(source, ref afterCandidate, close);

            bool named = afterCandidate < close && source[afterCandidate] == '=';

            if (!named)
            {
                position = candidateStart;

                var positionalValue = ParseValue(source, ref position, close, diagnostics, syntaxSpans);

                arguments.Add(new TemplatePositionalArgument(positionalValue));
                return;
            }

            string argumentName = source.Substring(candidateStart, candidateEnd - candidateStart);
            var argumentNameSpan = new SourceSpan(candidateStart, candidateEnd - candidateStart);

            if (argumentName.Length == 0)
            {
                diagnostics.Add(new TemplateDiagnostic(TemplateParser.MissingArgumentNameDiagnosticCode, TemplateDiagnosticSeverity.Error, "Named argument is missing its name.", new SourceSpan(afterCandidate, 1)));
            }
            else
            {
                syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.ArgumentName, argumentNameSpan));

                int invalidOffset = TemplateNameRules.FindInvalidArgumentNameOffset(argumentName);

                if (invalidOffset >= 0)
                {
                    diagnostics.Add(new TemplateDiagnostic(TemplateParser.InvalidArgumentNameDiagnosticCode, TemplateDiagnosticSeverity.Error, "Argument names must begin with a letter or '_' and contain only letters, digits, '_', or '-'.", new SourceSpan(candidateStart + invalidOffset, 1)));
                }
            }

            position = afterCandidate;

            var equalsSpan = new SourceSpan(position, 1);

            syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.AssignmentOperator, equalsSpan));

            position++;
            SkipWhitespace(source, ref position, close);

            TemplateArgumentValue value;

            if (position >= close)
            {
                diagnostics.Add(new TemplateDiagnostic(TemplateParser.MissingArgumentValueDiagnosticCode, TemplateDiagnosticSeverity.Error, "Named argument is missing its value.", equalsSpan));

                var missingSpan = new SourceSpan(close, 0);

                value = new TemplateArgumentValue(TemplateArgumentValueKind.Missing, string.Empty, string.Empty, missingSpan, missingSpan);
            }
            else
            {
                value = ParseValue(source, ref position, close, diagnostics, syntaxSpans);
            }

            int argumentEnd = value.Kind == TemplateArgumentValueKind.Missing ? position : value.Span.End;

            arguments.Add(new TemplateNamedArgument(argumentName, argumentNameSpan, equalsSpan, value, new SourceSpan(argumentStart, argumentEnd - argumentStart)));
        }

        private static TemplateArgumentValue ParseValue(string source, ref int position, int close, IList<TemplateDiagnostic> diagnostics, IList<TemplateSyntaxSpan> syntaxSpans)
        {
            if (source[position] == '"')
                return ParseQuotedValue(source, ref position, close, diagnostics, syntaxSpans);

            int valueStart = position;

            while (position < close && !char.IsWhiteSpace(source[position]))
                position++;

            int valueLength = position - valueStart;
            string raw = source.Substring(valueStart, valueLength);
            var valueSpan = new SourceSpan(valueStart, valueLength);

            TemplateArgumentValueKind kind = ClassifyValue(raw);

            syntaxSpans.Add(new TemplateSyntaxSpan(SyntaxKindForValue(kind), valueSpan));

            return new TemplateArgumentValue(kind, raw, raw, valueSpan, valueSpan);
        }

        private static TemplateArgumentValue ParseQuotedValue(string source, ref int position, int close, IList<TemplateDiagnostic> diagnostics, IList<TemplateSyntaxSpan> syntaxSpans)
        {
            int quoteStart = position;
            position++;

            int contentStart = position;
            bool escaped = false;

            while (position < close)
            {
                char current = source[position];

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

                    var span = new SourceSpan(quoteStart, position - quoteStart);
                    var contentSpan = new SourceSpan(contentStart, quoteEnd - contentStart);

                    syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.StringValue, span));

                    return new TemplateArgumentValue(TemplateArgumentValueKind.String, source.Substring(quoteStart, span.Length), source.Substring(contentStart, contentSpan.Length), span, contentSpan);
                }

                position++;
            }

            var unterminatedSpan = new SourceSpan(quoteStart, close - quoteStart);

            diagnostics.Add(new TemplateDiagnostic(TemplateParser.UnterminatedStringDiagnosticCode, TemplateDiagnosticSeverity.Error, "Quoted argument value is missing its closing quote.", unterminatedSpan));
            syntaxSpans.Add(new TemplateSyntaxSpan(TemplateSyntaxKind.StringValue, unterminatedSpan));

            return new TemplateArgumentValue(TemplateArgumentValueKind.String, source.Substring(quoteStart, unterminatedSpan.Length), source.Substring(contentStart, close - contentStart), unterminatedSpan, new SourceSpan(contentStart, close - contentStart));
        }

        private static TemplateArgumentValueKind ClassifyValue(string value)
        {
            if (value == "true" || value == "false")
                return TemplateArgumentValueKind.Boolean;

            if (IsNumber(value))
                return TemplateArgumentValueKind.Number;

            return TemplateArgumentValueKind.Bare;
        }

        private static TemplateSyntaxKind SyntaxKindForValue(TemplateArgumentValueKind kind)
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

            if (value[index] == '+' || value[index] == '-')
            {
                index++;

                if (index == value.Length)
                    return false;
            }

            bool digitSeen = false;
            bool decimalSeen = false;

            while (index < value.Length)
            {
                char current = value[index];

                if (current >= '0' && current <= '9')
                {
                    digitSeen = true;
                    index++;
                    continue;
                }

                if (current == '.' && !decimalSeen)
                {
                    decimalSeen = true;
                    index++;
                    continue;
                }

                return false;
            }

            return digitSeen;
        }

        private static void SkipWhitespace(string source, ref int position, int end)
        {
            while (position < end && char.IsWhiteSpace(source[position]))
                position++;
        }
    }
}
