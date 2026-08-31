namespace Mz.Toml.Internal
{
    internal sealed partial class TomlParser
    {
        private bool ParseValue(out TomlNode node, out TomlDiagnostic diagnostic)
        {
            node = null;
            diagnostic = null;

            if (Current == '"')
            {
                var line = _line;
                var column = _column;
                string text;

                if (IsTripleDelimiter('"'))
                {
                    if (!ParseMultilineBasicStringText(out text, out diagnostic))
                        return false;
                }
                else
                {
                    if (!ParseBasicStringText(out text, out diagnostic))
                        return false;
                }

                node = new TomlValue(TomlValueKind.String, text, line, column);
                return true;
            }

            if (Current == '\'')
            {
                var line = _line;
                var column = _column;
                string text;

                if (IsTripleDelimiter('\''))
                {
                    if (!ParseMultilineLiteralStringText(out text, out diagnostic))
                        return false;
                }
                else
                {
                    if (!ParseLiteralStringText(out text, out diagnostic))
                        return false;
                }

                node = new TomlValue(TomlValueKind.String, text, line, column);
                return true;
            }

            if (Current == '[')
                return ParseArray(out node, out diagnostic);

            if (Current == '{')
                return ParseInlineTable(out node, out diagnostic);

            return ParseBareValue(out node, out diagnostic);
        }
    }
}
