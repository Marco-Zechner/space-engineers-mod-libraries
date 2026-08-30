using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Mz.Toml.Internal
{
    internal sealed class TomlParser
    {
        private readonly string _text;
        private readonly TomlTable _root;
        private TomlTable _currentTable;
        private int _index;
        private int _line;
        private int _column;

        private TomlParser(string text)
        {
            _text = text;
            _index = 0;
            _line = 1;
            _column = 1;

            _root = new TomlTable(
                1,
                1,
                TomlTableDefinitionKind.Root);

            _currentTable = _root;
        }

        public static TomlParseResult Parse(string text)
        {
            return new TomlParser(text).ParseDocument();
        }

        private TomlParseResult ParseDocument()
        {
            while (!IsEnd)
            {
                TomlDiagnostic diagnostic;

                if (!SkipDocumentTrivia(out diagnostic))
                    return Failure(diagnostic);

                if (IsEnd)
                    break;

                if (Current == '[')
                {
                    if (!ParseTableHeader(out diagnostic))
                        return Failure(diagnostic);
                }
                else
                {
                    if (!ParseAssignment(out diagnostic))
                        return Failure(diagnostic);
                }
            }

            return new TomlParseResult(
                new TomlDocument(_root),
                new TomlDiagnostic[0]);
        }

        private bool SkipDocumentTrivia(
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            while (!IsEnd)
            {
                SkipHorizontalWhitespace();

                if (IsEnd)
                    return true;

                if (Current == '#')
                {
                    if (!SkipComment(out diagnostic))
                        return false;

                    if (IsEnd)
                        return true;

                    if (!ConsumeNewline(out diagnostic))
                        return false;

                    continue;
                }

                if (IsNewlineStart(Current))
                {
                    if (!ConsumeNewline(out diagnostic))
                        return false;

                    continue;
                }

                return true;
            }

            return true;
        }

        private bool ParseAssignment(
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            List<TomlKeyPart> parts;

            if (!ParseKeyPath(
                '=',
                false,
                out parts,
                out diagnostic))
            {
                return false;
            }

            AdvanceCharacter();
            SkipHorizontalWhitespace();

            if (IsEnd ||
                Current == '#' ||
                IsNewlineStart(Current))
            {
                diagnostic = Error(
                    TomlDiagnosticCode.MissingValue,
                    "Expected a value after '='.",
                    _line,
                    _column);
                return false;
            }

            TomlNode value;

            if (!ParseValue(
                out value,
                out diagnostic))
            {
                return false;
            }

            SkipHorizontalWhitespace();

            if (!IsEnd && Current == '#')
            {
                if (!SkipComment(out diagnostic))
                    return false;
            }

            if (!IsEnd)
            {
                if (!IsNewlineStart(Current))
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TrailingCharacters,
                        "Unexpected characters after the TOML value.",
                        _line,
                        _column);
                    return false;
                }

                if (!ConsumeNewline(out diagnostic))
                    return false;
            }

            return AssignKeyPath(
                _currentTable,
                parts,
                value,
                out diagnostic);
        }

        private bool ParseTableHeader(
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            var headerLine = _line;
            var headerColumn = _column;

            AdvanceCharacter();

            if (!IsEnd && Current == '[')
            {
                diagnostic = Error(
                    TomlDiagnosticCode.UnsupportedSyntax,
                    "Arrays of tables are not implemented yet.",
                    headerLine,
                    headerColumn);
                return false;
            }

            List<TomlKeyPart> parts;

            if (!ParseKeyPath(
                ']',
                true,
                out parts,
                out diagnostic))
            {
                return false;
            }

            AdvanceCharacter();
            SkipHorizontalWhitespace();

            if (!IsEnd && Current == '#')
            {
                if (!SkipComment(out diagnostic))
                    return false;
            }

            if (!IsEnd)
            {
                if (!IsNewlineStart(Current))
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TrailingCharacters,
                        "Unexpected characters after the table header.",
                        _line,
                        _column);
                    return false;
                }

                if (!ConsumeNewline(out diagnostic))
                    return false;
            }

            TomlTable table;

            if (!ResolveTableHeader(
                parts,
                out table,
                out diagnostic))
            {
                return false;
            }

            _currentTable = table;
            return true;
        }

        private bool ParseKeyPath(
            char terminator,
            bool tableHeader,
            out List<TomlKeyPart> parts,
            out TomlDiagnostic diagnostic)
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
                        tableHeader
                            ? TomlDiagnosticCode.InvalidTable
                            : TomlDiagnosticCode.InvalidKey,
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
                    if (!ParseBasicStringText(
                        out key,
                        out diagnostic))
                    {
                        return false;
                    }
                }
                else if (Current == '\'')
                {
                    if (!ParseLiteralKey(
                        out key,
                        out diagnostic))
                    {
                        return false;
                    }
                }
                else
                {
                    var start = _index;

                    while (!IsEnd &&
                           IsBareKeyCharacter(Current))
                    {
                        AdvanceCharacter();
                    }

                    if (_index == start)
                    {
                        diagnostic = Error(
                            TomlDiagnosticCode.InvalidKey,
                            "Expected a bare or quoted TOML key.",
                            _line,
                            _column);
                        return false;
                    }

                    key = _text.Substring(
                        start,
                        _index - start);
                }

                parts.Add(
                    new TomlKeyPart(
                        key,
                        line,
                        column));

                SkipHorizontalWhitespace();

                if (IsEnd ||
                    IsNewlineStart(Current) ||
                    Current == '#')
                {
                    diagnostic = Error(
                        tableHeader
                            ? TomlDiagnosticCode.InvalidTable
                            : TomlDiagnosticCode.MissingEquals,
                        tableHeader
                            ? "Expected ']' after the table name."
                            : "Expected '=' after the key.",
                        _line,
                        _column);
                    return false;
                }

                if (Current == terminator)
                    return true;

                if (Current != '.')
                {
                    diagnostic = Error(
                        tableHeader
                            ? TomlDiagnosticCode.InvalidTable
                            : TomlDiagnosticCode.InvalidKey,
                        "Expected '.' or '" +
                        terminator +
                        "' after the key segment.",
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
                        tableHeader
                            ? TomlDiagnosticCode.InvalidTable
                            : TomlDiagnosticCode.InvalidKey,
                        "Expected a key segment after '.'.",
                        _line,
                        _column);
                    return false;
                }
            }
        }

        private bool ParseLiteralKey(
            out string value,
            out TomlDiagnostic diagnostic)
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

                if ((c < 0x20 && c != '\t') ||
                    c == 0x7F)
                {
                    value = null;
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidKey,
                        "Control character in literal quoted key.",
                        _line,
                        _column);
                    return false;
                }

                sb.Append(c);
                AdvanceCharacter();
            }

            value = null;
            diagnostic = Error(
                TomlDiagnosticCode.InvalidKey,
                "Unterminated literal quoted key.",
                startLine,
                startColumn);
            return false;
        }

        private bool AssignKeyPath(
            TomlTable startTable,
            IList<TomlKeyPart> parts,
            TomlNode value,
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            var table = startTable;

            for (var i = 0; i < parts.Count - 1; i++)
            {
                var part = parts[i];
                TomlNode existing;

                if (!table.TryGetValue(
                    part.Value,
                    out existing))
                {
                    var created = new TomlTable(
                        part.Line,
                        part.Column,
                        TomlTableDefinitionKind.DottedKey);

                    table.Set(
                        part.Value,
                        created);

                    table = created;
                    continue;
                }

                if (existing.Kind != TomlNodeKind.Table)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        "Key '" +
                        part.Value +
                        "' is already defined as a value and cannot be used as a table.",
                        part.Line,
                        part.Column);
                    return false;
                }

                var existingTable =
                    (TomlTable)existing;

                if (existingTable.DefinitionKind ==
                    TomlTableDefinitionKind.Explicit)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        "Table '" +
                        part.Value +
                        "' was already explicitly defined and cannot be extended through a dotted key.",
                        part.Line,
                        part.Column);
                    return false;
                }

                table = existingTable;
            }

            var finalPart = parts[parts.Count - 1];

            if (table.ContainsKey(finalPart.Value))
            {
                diagnostic = Error(
                    TomlDiagnosticCode.DuplicateKey,
                    "The key '" +
                    finalPart.Value +
                    "' is already defined.",
                    finalPart.Line,
                    finalPart.Column);
                return false;
            }

            table.Set(
                finalPart.Value,
                value);

            return true;
        }

        private bool ResolveTableHeader(
            IList<TomlKeyPart> parts,
            out TomlTable result,
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;
            result = null;

            var table = _root;

            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                var isLeaf = i == parts.Count - 1;

                TomlNode existing;

                if (!table.TryGetValue(
                    part.Value,
                    out existing))
                {
                    var created = new TomlTable(
                        part.Line,
                        part.Column,
                        isLeaf
                            ? TomlTableDefinitionKind.Explicit
                            : TomlTableDefinitionKind.Implicit);

                    table.Set(
                        part.Value,
                        created);

                    table = created;
                    continue;
                }

                if (existing.Kind != TomlNodeKind.Table)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        "Key '" +
                        part.Value +
                        "' is already defined as a value and cannot be used as a table.",
                        part.Line,
                        part.Column);
                    return false;
                }

                var existingTable =
                    (TomlTable)existing;

                if (isLeaf)
                {
                    if (existingTable.DefinitionKind ==
                        TomlTableDefinitionKind.Implicit)
                    {
                        existingTable.DefinitionKind =
                            TomlTableDefinitionKind.Explicit;

                        table = existingTable;
                        continue;
                    }

                    if (existingTable.DefinitionKind ==
                        TomlTableDefinitionKind.DottedKey)
                    {
                        diagnostic = Error(
                            TomlDiagnosticCode.TableConflict,
                            "Table '" +
                            part.Value +
                            "' was already defined by a dotted key and cannot be redefined by a table header.",
                            part.Line,
                            part.Column);
                        return false;
                    }

                    diagnostic = Error(
                        TomlDiagnosticCode.DuplicateTable,
                        "Table '" +
                        part.Value +
                        "' is already explicitly defined.",
                        part.Line,
                        part.Column);
                    return false;
                }

                table = existingTable;
            }

            result = table;
            return true;
        }

        private bool ParseValue(
            out TomlNode node,
            out TomlDiagnostic diagnostic)
        {
            node = null;
            diagnostic = null;

            if (Current == '"')
            {
                var line = _line;
                var column = _column;
                string text;

                if (!ParseBasicStringText(
                    out text,
                    out diagnostic))
                {
                    return false;
                }

                node = new TomlValue(
                    TomlValueKind.String,
                    text,
                    line,
                    column);

                return true;
            }

            if (Current == '\'' ||
                Current == '[' ||
                Current == '{')
            {
                diagnostic = Error(
                    TomlDiagnosticCode.UnsupportedSyntax,
                    "This TOML value form is not implemented yet.",
                    _line,
                    _column);
                return false;
            }

            return ParseBareValue(
                out node,
                out diagnostic);
        }

        private bool ParseBasicStringText(
            out string value,
            out TomlDiagnostic diagnostic)
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
                    if (!ParseEscape(
                        sb,
                        out diagnostic))
                    {
                        return false;
                    }

                    continue;
                }

                if ((c < 0x20 && c != '\t') ||
                    c == 0x7F)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidString,
                        "Unescaped control character in TOML basic string.",
                        _line,
                        _column);
                    return false;
                }

                sb.Append(c);
                AdvanceCharacter();
            }

            diagnostic = Error(
                TomlDiagnosticCode.InvalidString,
                "Unterminated TOML basic string.",
                sourceLine,
                sourceColumn);
            return false;
        }

        private bool ParseBareValue(
            out TomlNode node,
            out TomlDiagnostic diagnostic)
        {
            node = null;
            diagnostic = null;

            var start = _index;
            var line = _line;
            var column = _column;

            while (!IsEnd &&
                   !IsHorizontalWhitespace(Current) &&
                   !IsNewlineStart(Current) &&
                   Current != '#')
            {
                AdvanceCharacter();
            }

            var token = _text.Substring(
                start,
                _index - start);

            if (token == "true")
            {
                node = new TomlValue(
                    TomlValueKind.Boolean,
                    true,
                    line,
                    column);
                return true;
            }

            if (token == "false")
            {
                node = new TomlValue(
                    TomlValueKind.Boolean,
                    false,
                    line,
                    column);
                return true;
            }

            if (token == "inf" ||
                token == "+inf")
            {
                node = new TomlValue(
                    TomlValueKind.Float,
                    double.PositiveInfinity,
                    line,
                    column);
                return true;
            }

            if (token == "-inf")
            {
                node = new TomlValue(
                    TomlValueKind.Float,
                    double.NegativeInfinity,
                    line,
                    column);
                return true;
            }

            if (token == "nan" ||
                token == "+nan" ||
                token == "-nan")
            {
                node = new TomlValue(
                    TomlValueKind.Float,
                    double.NaN,
                    line,
                    column);
                return true;
            }

            bool isFloat;

            if (IsDecimalNumber(
                token,
                out isFloat))
            {
                if (isFloat)
                {
                    double value;

                    if (!double.TryParse(
                            token,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out value) ||
                        double.IsInfinity(value) ||
                        double.IsNaN(value))
                    {
                        diagnostic = Error(
                            TomlDiagnosticCode.InvalidNumber,
                            "Floating-point value is outside the supported TOML range.",
                            line,
                            column);
                        return false;
                    }

                    node = new TomlValue(
                        TomlValueKind.Float,
                        value,
                        line,
                        column);
                    return true;
                }

                long integerValue;

                if (!long.TryParse(
                    token,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out integerValue))
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidNumber,
                        "Integer value is outside the signed 64-bit TOML range.",
                        line,
                        column);
                    return false;
                }

                node = new TomlValue(
                    TomlValueKind.Integer,
                    integerValue,
                    line,
                    column);
                return true;
            }

            diagnostic = Error(
                LooksNumeric(token)
                    ? TomlDiagnosticCode.InvalidNumber
                    : TomlDiagnosticCode.InvalidValue,
                "Unrecognized or unsupported TOML value '" +
                token +
                "'.",
                line,
                column);
            return false;
        }

        private bool ParseEscape(
            StringBuilder sb,
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            var escapeLine = _line;
            var escapeColumn = _column;

            AdvanceCharacter();

            if (IsEnd ||
                IsNewlineStart(Current))
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidEscape,
                    "String ends immediately after an escape character.",
                    escapeLine,
                    escapeColumn);
                return false;
            }

            var escaped = Current;
            AdvanceCharacter();

            switch (escaped)
            {
                case 'b':
                    sb.Append('\b');
                    return true;

                case 't':
                    sb.Append('\t');
                    return true;

                case 'n':
                    sb.Append('\n');
                    return true;

                case 'f':
                    sb.Append('\f');
                    return true;

                case 'r':
                    sb.Append('\r');
                    return true;

                case '"':
                    sb.Append('"');
                    return true;

                case '\\':
                    sb.Append('\\');
                    return true;

                case 'u':
                    return ParseUnicodeEscape(
                        sb,
                        4,
                        escapeLine,
                        escapeColumn,
                        out diagnostic);

                case 'U':
                    return ParseUnicodeEscape(
                        sb,
                        8,
                        escapeLine,
                        escapeColumn,
                        out diagnostic);

                default:
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidEscape,
                        "Unknown TOML escape sequence '\\" +
                        escaped +
                        "'.",
                        escapeLine,
                        escapeColumn);
                    return false;
            }
        }

        private bool ParseUnicodeEscape(
            StringBuilder sb,
            int digitCount,
            int escapeLine,
            int escapeColumn,
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            if (_index + digitCount > _text.Length)
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidEscape,
                    "Unicode escape does not contain enough hexadecimal digits.",
                    escapeLine,
                    escapeColumn);
                return false;
            }

            var start = _index;

            for (var i = 0; i < digitCount; i++)
            {
                if (IsEnd ||
                    IsNewlineStart(Current) ||
                    !IsHexDigit(Current))
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidEscape,
                        "Unicode escape contains a non-hexadecimal character.",
                        escapeLine,
                        escapeColumn);
                    return false;
                }

                AdvanceCharacter();
            }

            var hex = _text.Substring(
                start,
                digitCount);

            int codePoint;

            if (!int.TryParse(
                    hex,
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out codePoint) ||
                codePoint > 0x10FFFF ||
                (codePoint >= 0xD800 &&
                 codePoint <= 0xDFFF))
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidEscape,
                    "Unicode escape contains an invalid Unicode scalar value.",
                    escapeLine,
                    escapeColumn);
                return false;
            }

            sb.Append(
                char.ConvertFromUtf32(codePoint));

            return true;
        }

        private bool SkipComment(
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            while (!IsEnd &&
                   !IsNewlineStart(Current))
            {
                var c = Current;

                if ((c < 0x20 && c != '\t') ||
                    c == 0x7F)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidComment,
                        "Control characters other than tab are not permitted in TOML comments.",
                        _line,
                        _column);
                    return false;
                }

                AdvanceCharacter();
            }

            return true;
        }

        private bool ConsumeNewline(
            out TomlDiagnostic diagnostic)
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

            if (_index + 1 >= _text.Length ||
                _text[_index + 1] != '\n')
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidNewline,
                    "TOML newlines must use LF or CRLF; a lone CR is invalid.",
                    _line,
                    _column);
                return false;
            }

            _index += 2;
            _line++;
            _column = 1;
            return true;
        }

        private void SkipHorizontalWhitespace()
        {
            while (!IsEnd &&
                   IsHorizontalWhitespace(Current))
            {
                AdvanceCharacter();
            }
        }

        private void AdvanceCharacter()
        {
            _index++;
            _column++;
        }

        private bool IsEnd
        {
            get { return _index >= _text.Length; }
        }

        private char Current
        {
            get { return _text[_index]; }
        }

        private static bool IsDecimalNumber(
            string token,
            out bool isFloat)
        {
            isFloat = false;

            if (string.IsNullOrEmpty(token))
                return false;

            var i = 0;

            if (token[i] == '+' ||
                token[i] == '-')
            {
                i++;

                if (i >= token.Length)
                    return false;
            }

            var integerStart = i;

            while (i < token.Length &&
                   IsDigit(token[i]))
            {
                i++;
            }

            if (i == integerStart)
                return false;

            var integerLength =
                i - integerStart;

            if (integerLength > 1 &&
                token[integerStart] == '0')
            {
                return false;
            }

            if (i < token.Length &&
                token[i] == '.')
            {
                isFloat = true;
                i++;

                var fractionalStart = i;

                while (i < token.Length &&
                       IsDigit(token[i]))
                {
                    i++;
                }

                if (i == fractionalStart)
                    return false;
            }

            if (i < token.Length &&
                (token[i] == 'e' ||
                 token[i] == 'E'))
            {
                isFloat = true;
                i++;

                if (i < token.Length &&
                    (token[i] == '+' ||
                     token[i] == '-'))
                {
                    i++;
                }

                var exponentStart = i;

                while (i < token.Length &&
                       IsDigit(token[i]))
                {
                    i++;
                }

                if (i == exponentStart)
                    return false;
            }

            return i == token.Length;
        }

        private static bool LooksNumeric(
            string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            return IsDigit(token[0]) ||
                   token[0] == '+' ||
                   token[0] == '-';
        }

        private static bool IsBareKeyCharacter(
            char c)
        {
            return (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') ||
                   c == '_' ||
                   c == '-';
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' &&
                   c <= '9';
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }

        private static bool IsHorizontalWhitespace(
            char c)
        {
            return c == ' ' ||
                   c == '\t';
        }

        private static bool IsNewlineStart(
            char c)
        {
            return c == '\n' ||
                   c == '\r';
        }

        private static TomlDiagnostic Error(
            TomlDiagnosticCode code,
            string message,
            int line,
            int column)
        {
            return new TomlDiagnostic(
                code,
                message,
                line,
                column);
        }

        private static TomlParseResult Failure(
            TomlDiagnostic diagnostic)
        {
            return new TomlParseResult(
                null,
                new[] { diagnostic });
        }
    }
}