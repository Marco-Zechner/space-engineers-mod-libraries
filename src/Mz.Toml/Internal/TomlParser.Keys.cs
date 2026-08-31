using System.Collections.Generic;
using System.Text;

namespace Mz.Toml.Internal
{
    internal sealed partial class TomlParser
    {
        private bool ParseKeyPath(char terminator, bool tableHeader, out List<TomlKeyPart> parts, out TomlDiagnostic diagnostic)
        {
            parts = new List<TomlKeyPart>();
            diagnostic = null;

            SkipHorizontalWhitespace();

            while (true)
            {
                if (IsEnd ||
                    IsNewlineStart(Current) ||
                    Current == '#' ||
                    Current == '.' ||
                    Current == terminator)
                {
                    diagnostic = Error(
                        tableHeader ? TomlDiagnosticCode.InvalidTable : TomlDiagnosticCode.InvalidKey,
                        "Expected a TOML key segment.",
                        _line,
                        _column);
                    return false;
                }

                var line = _line;
                var column = _column;
                string key;

                if (Current == '"')
                {
                    if (!ParseBasicStringText(out key, out diagnostic))
                        return false;
                }
                else if (Current == '\'')
                {
                    if (!ParseLiteralKey(out key, out diagnostic))
                        return false;
                }
                else
                {
                    var start = _index;

                    while (!IsEnd && IsBareKeyCharacter(Current))
                        AdvanceCharacter();

                    if (_index == start)
                    {
                        diagnostic = Error(
                            TomlDiagnosticCode.InvalidKey,
                            "Expected a bare or quoted TOML key.",
                            _line,
                            _column);
                        return false;
                    }

                    key = _text.Substring(start, _index - start);
                }

                parts.Add(new TomlKeyPart(key, line, column));

                SkipHorizontalWhitespace();

                if (IsEnd || IsNewlineStart(Current) || Current == '#')
                {
                    diagnostic = Error(
                        tableHeader ? TomlDiagnosticCode.InvalidTable : TomlDiagnosticCode.MissingEquals,
                        tableHeader ? "Expected ']' after the table name." : "Expected '=' after the key.",
                        _line,
                        _column);
                    return false;
                }

                if (Current == terminator)
                    return true;

                if (Current != '.')
                {
                    diagnostic = Error(
                        tableHeader ? TomlDiagnosticCode.InvalidTable : TomlDiagnosticCode.InvalidKey,
                        $"Expected '.' or '{terminator}' after the key segment.",
                        _line,
                        _column);
                    return false;
                }

                AdvanceCharacter();
                SkipHorizontalWhitespace();

                if (IsEnd ||
                    IsNewlineStart(Current) ||
                    Current == '#' ||
                    Current == '.' ||
                    Current == terminator)
                {
                    diagnostic = Error(
                        tableHeader ? TomlDiagnosticCode.InvalidTable : TomlDiagnosticCode.InvalidKey,
                        "Expected a key segment after '.'.",
                        _line,
                        _column);
                    return false;
                }
            }
        }

        private bool ParseLiteralKey(out string value, out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            var startLine = _line;
            var startColumn = _column;

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
                    value = null;
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidKey,
                        "Unterminated literal quoted key.",
                        startLine,
                        startColumn);
                    return false;
                }

                if ((c < 0x20 && c != '\t') || c == 0x7F)
                {
                    value = null;
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidKey,
                        "Control character in literal quoted key.",
                        _line,
                        _column);
                    return false;
                }

                if (!AppendRawStringCharacter(sb, TomlDiagnosticCode.InvalidKey, out diagnostic))
                {
                    value = null;
                    return false;
                }
            }

            value = null;
            diagnostic = Error(
                TomlDiagnosticCode.InvalidKey,
                "Unterminated literal quoted key.",
                startLine,
                startColumn);
            return false;
        }
    }
}
