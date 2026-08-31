namespace Mz.Toml.Internal
{
    internal sealed partial class TomlParser
    {
        private bool SkipComment(out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            while (!IsEnd && !IsNewlineStart(Current))
            {
                var c = Current;

                if ((c < 0x20 && c != '\t') || c == 0x7F)
                {
                    diagnostic = Error("Control characters other than tab are not permitted in TOML comments.",
                        _line, _column, TomlDiagnosticCode.InvalidComment);
                    return false;
                }

                AdvanceCharacter();
            }

            return true;
        }

        private bool ConsumeNewline(out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            if (IsEnd)
                return true;

            if (Current == '\n')
            {
                _index++;
                _line++;
                _column = 1;
                return true;
            }

            if (Current != '\r')
                return false;

            if (_index + 1 >= _text.Length || _text[_index + 1] != '\n')
            {
                diagnostic = Error("TOML newlines must use LF or CRLF; a lone CR is invalid.",
                    _line, _column, TomlDiagnosticCode.InvalidNewline);
                return false;
            }

            _index += 2;
            _line++;
            _column = 1;
            return true;
        }

        private void SkipHorizontalWhitespace()
        {
            while (!IsEnd && IsHorizontalWhitespace(Current))
                AdvanceCharacter();
        }

        private void AdvanceCharacter()
        {
            _index++;
            _column++;
        }

        private bool IsEnd => _index >= _text.Length;

        private char Current => _text[_index];

        private static bool LooksNumeric(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            return IsDigit(token[0]) || token[0] == '+' || token[0] == '-';
        }

        private static bool IsBareKeyCharacter(char c) =>
            (c >= 'A' && c <= 'Z') ||
            (c >= 'a' && c <= 'z') ||
            (c >= '0' && c <= '9') ||
            c == '_' || c == '-';

        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private static bool IsHexDigit(char c) =>
            (c >= '0' && c <= '9') ||
            (c >= 'A' && c <= 'F') ||
            (c >= 'a' && c <= 'f');

        private static bool IsHorizontalWhitespace(char c) => c == ' ' || c == '\t';

        private static bool IsNewlineStart(char c) => c == '\n' || c == '\r';

        private static TomlDiagnostic Error(string message, int line, int column, TomlDiagnosticCode code) =>
            new TomlDiagnostic(message, line, column, code);

        private static TomlParseResult Failure(TomlDiagnostic diagnostic) =>
            new TomlParseResult(null, new[] { diagnostic });
    }
}
