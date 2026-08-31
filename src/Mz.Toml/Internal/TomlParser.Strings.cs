using System.Text;

namespace Mz.Toml.Internal
{
    internal sealed partial class TomlParser
    {
        private bool ParseBasicStringText(out string value, out TomlDiagnostic diagnostic)
        {
            value = null;
            diagnostic = null;

            var sourceLine = _line;
            var sourceColumn = _column;

            AdvanceCharacter();

            var sb = new StringBuilder();

            while (!IsEnd)
            {
                var c = Current;

                if (c == '"')
                {
                    AdvanceCharacter();
                    value = sb.ToString();
                    return true;
                }

                if (IsNewlineStart(c))
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidString,
                        "Unterminated TOML basic string.",
                        sourceLine,
                        sourceColumn);
                    return false;
                }

                if (c == '\\')
                {
                    if (!ParseEscape(sb, out diagnostic))
                        return false;

                    continue;
                }

                if ((c < 0x20 && c != '\t') || c == 0x7F)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidString,
                        "Unescaped control character in TOML basic string.",
                        _line,
                        _column);
                    return false;
                }

                if (!AppendRawStringCharacter(sb, TomlDiagnosticCode.InvalidString, out diagnostic))
                    return false;
            }

            diagnostic = Error(
                TomlDiagnosticCode.InvalidString,
                "Unterminated TOML basic string.",
                sourceLine,
                sourceColumn);
            return false;
        }

        private bool ParseLiteralStringText(out string value, out TomlDiagnostic diagnostic)
        {
            value = null;
            diagnostic = null;

            var sourceLine = _line;
            var sourceColumn = _column;

            AdvanceCharacter();

            var sb = new StringBuilder();

            while (!IsEnd)
            {
                var c = Current;

                if (c == '\'')
                {
                    AdvanceCharacter();
                    value = sb.ToString();
                    return true;
                }

                if (IsNewlineStart(c))
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidString,
                        "Unterminated TOML literal string.",
                        sourceLine,
                        sourceColumn);
                    return false;
                }

                if ((c < 0x20 && c != '\t') || c == 0x7F)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidString,
                        "Control character in TOML literal string.",
                        _line,
                        _column);
                    return false;
                }

                if (!AppendRawStringCharacter(sb, TomlDiagnosticCode.InvalidString, out diagnostic))
                    return false;
            }

            diagnostic = Error(
                TomlDiagnosticCode.InvalidString,
                "Unterminated TOML literal string.",
                sourceLine,
                sourceColumn);
            return false;
        }
    }
}
