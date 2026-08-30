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

            var isArrayOfTables =
                !IsEnd &&
                Current == '[';

            if (isArrayOfTables)
                AdvanceCharacter();

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

            if (isArrayOfTables)
            {
                if (IsEnd ||
                    Current != ']')
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidTable,
                        "Array-of-tables headers must end with two closing brackets.",
                        headerLine,
                        headerColumn);
                    return false;
                }

                AdvanceCharacter();
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
                        "Unexpected characters after the table header.",
                        _line,
                        _column);
                    return false;
                }

                if (!ConsumeNewline(out diagnostic))
                    return false;
            }

            TomlTable table;

            if (isArrayOfTables)
            {
                if (!ResolveArrayTableHeader(
                    parts,
                    out table,
                    out diagnostic))
                {
                    return false;
                }
            }
            else
            {
                if (!ResolveTableHeader(
                    parts,
                    out table,
                    out diagnostic))
                {
                    return false;
                }
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

                if (!AppendRawStringCharacter(
                    sb,
                    TomlDiagnosticCode.InvalidKey,
                    out diagnostic))
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
                    TomlTableDefinitionKind.Inline)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        "Inline table '" +
                        part.Value +
                        "' is immutable and cannot be extended through a dotted key.",
                        part.Line,
                        part.Column);
                    return false;
                }

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
                var isLeaf =
                    i == parts.Count - 1;

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

                if (existing.Kind ==
                    TomlNodeKind.Array)
                {
                    var array =
                        (TomlArray)existing;

                    if (array.DefinitionKind !=
                        TomlArrayDefinitionKind.ArrayOfTables)
                    {
                        diagnostic = Error(
                            TomlDiagnosticCode.TableConflict,
                            "Key '" +
                            part.Value +
                            "' is already defined as a static array and cannot be used as a table.",
                            part.Line,
                            part.Column);
                        return false;
                    }

                    if (isLeaf)
                    {
                        diagnostic = Error(
                            TomlDiagnosticCode.TableConflict,
                            "Array of tables '" +
                            part.Value +
                            "' cannot be redefined as a standard table.",
                            part.Line,
                            part.Column);
                        return false;
                    }

                    if (!TryGetLatestArrayTable(
                        array,
                        part,
                        out table,
                        out diagnostic))
                    {
                        return false;
                    }

                    continue;
                }

                if (existing.Kind !=
                    TomlNodeKind.Table)
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
                    TomlTableDefinitionKind.Inline)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        "Inline table '" +
                        part.Value +
                        "' is immutable and cannot be extended or redefined by a table header.",
                        part.Line,
                        part.Column);
                    return false;
                }

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

        private bool ResolveArrayTableHeader(
            IList<TomlKeyPart> parts,
            out TomlTable result,
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;
            result = null;

            var table = _root;

            for (var i = 0;
                 i < parts.Count;
                 i++)
            {
                var part =
                    parts[i];

                var isLeaf =
                    i == parts.Count - 1;

                TomlNode existing;

                if (!table.TryGetValue(
                    part.Value,
                    out existing))
                {
                    if (isLeaf)
                    {
                        var array =
                            new TomlArray(
                                part.Line,
                                part.Column,
                                TomlArrayDefinitionKind.ArrayOfTables);

                        var element =
                            new TomlTable(
                                part.Line,
                                part.Column,
                                TomlTableDefinitionKind.Explicit);

                        array.Add(element);

                        table.Set(
                            part.Value,
                            array);

                        result = element;
                        return true;
                    }

                    var created =
                        new TomlTable(
                            part.Line,
                            part.Column,
                            TomlTableDefinitionKind.Implicit);

                    table.Set(
                        part.Value,
                        created);

                    table = created;
                    continue;
                }

                if (existing.Kind ==
                    TomlNodeKind.Array)
                {
                    var array =
                        (TomlArray)existing;

                    if (array.DefinitionKind !=
                        TomlArrayDefinitionKind.ArrayOfTables)
                    {
                        diagnostic = Error(
                            TomlDiagnosticCode.TableConflict,
                            "Key '" +
                            part.Value +
                            "' is already defined as a static array and cannot become an array of tables.",
                            part.Line,
                            part.Column);
                        return false;
                    }

                    if (isLeaf)
                    {
                        var element =
                            new TomlTable(
                                part.Line,
                                part.Column,
                                TomlTableDefinitionKind.Explicit);

                        array.Add(element);

                        result = element;
                        return true;
                    }

                    if (!TryGetLatestArrayTable(
                        array,
                        part,
                        out table,
                        out diagnostic))
                    {
                        return false;
                    }

                    continue;
                }

                if (existing.Kind !=
                    TomlNodeKind.Table)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        "Key '" +
                        part.Value +
                        "' is already defined as a value and cannot become an array of tables.",
                        part.Line,
                        part.Column);
                    return false;
                }

                var existingTable =
                    (TomlTable)existing;

                if (existingTable.DefinitionKind ==
                    TomlTableDefinitionKind.Inline)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        "Inline table '" +
                        part.Value +
                        "' is immutable and cannot be extended by an array-of-tables header.",
                        part.Line,
                        part.Column);
                    return false;
                }

                if (isLeaf)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        "Table '" +
                        part.Value +
                        "' is already defined and cannot become an array of tables.",
                        part.Line,
                        part.Column);
                    return false;
                }

                table =
                    existingTable;
            }

            diagnostic = Error(
                TomlDiagnosticCode.InvalidTable,
                "Array-of-tables header did not resolve to a table.",
                _line,
                _column);

            return false;
        }

        private bool TryGetLatestArrayTable(
            TomlArray array,
            TomlKeyPart part,
            out TomlTable table,
            out TomlDiagnostic diagnostic)
        {
            table = null;
            diagnostic = null;

            if (array.Count == 0)
            {
                diagnostic = Error(
                    TomlDiagnosticCode.TableConflict,
                    "Array of tables '" +
                    part.Value +
                    "' has no table element to extend.",
                    part.Line,
                    part.Column);
                return false;
            }

            var latest =
                array[array.Count - 1];

            if (latest.Kind !=
                TomlNodeKind.Table)
            {
                diagnostic = Error(
                    TomlDiagnosticCode.TableConflict,
                    "Array of tables '" +
                    part.Value +
                    "' contains a non-table element.",
                    part.Line,
                    part.Column);
                return false;
            }

            table =
                (TomlTable)latest;

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

                if (IsTripleDelimiter('"'))
                {
                    if (!ParseMultilineBasicStringText(
                        out text,
                        out diagnostic))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!ParseBasicStringText(
                        out text,
                        out diagnostic))
                    {
                        return false;
                    }
                }

                node = new TomlValue(
                    TomlValueKind.String,
                    text,
                    line,
                    column);

                return true;
            }

            if (Current == '\'')
            {
                var line = _line;
                var column = _column;
                string text;

                if (IsTripleDelimiter('\''))
                {
                    if (!ParseMultilineLiteralStringText(
                        out text,
                        out diagnostic))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!ParseLiteralStringText(
                        out text,
                        out diagnostic))
                    {
                        return false;
                    }
                }

                node = new TomlValue(
                    TomlValueKind.String,
                    text,
                    line,
                    column);

                return true;
            }

            if (Current == '[')
            {
                return ParseArray(
                    out node,
                    out diagnostic);
            }

            if (Current == '{')
            {
                return ParseInlineTable(
                    out node,
                    out diagnostic);
            }

            return ParseBareValue(
                out node,
                out diagnostic);
        }

        private bool ParseArray(
            out TomlNode node,
            out TomlDiagnostic diagnostic)
        {
            node = null;
            diagnostic = null;

            var line = _line;
            var column = _column;

            AdvanceCharacter();

            var array =
                new TomlArray(
                    line,
                    column);

            if (!SkipArrayTrivia(
                out diagnostic))
            {
                return false;
            }

            if (IsEnd)
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidValue,
                    "Unterminated TOML array.",
                    line,
                    column);
                return false;
            }

            if (Current == ']')
            {
                AdvanceCharacter();
                node = array;
                return true;
            }

            while (true)
            {
                TomlNode value;

                if (!ParseValue(
                    out value,
                    out diagnostic))
                {
                    return false;
                }

                array.Add(value);

                if (!SkipArrayTrivia(
                    out diagnostic))
                {
                    return false;
                }

                if (IsEnd)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidValue,
                        "Unterminated TOML array.",
                        line,
                        column);
                    return false;
                }

                if (Current == ']')
                {
                    AdvanceCharacter();
                    node = array;
                    return true;
                }

                if (Current != ',')
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidValue,
                        "Expected ',' or ']' after a TOML array element.",
                        _line,
                        _column);
                    return false;
                }

                AdvanceCharacter();

                if (!SkipArrayTrivia(
                    out diagnostic))
                {
                    return false;
                }

                if (IsEnd)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidValue,
                        "Unterminated TOML array.",
                        line,
                        column);
                    return false;
                }

                if (Current == ']')
                {
                    AdvanceCharacter();
                    node = array;
                    return true;
                }
            }
        }

        private bool ParseInlineTable(
            out TomlNode node,
            out TomlDiagnostic diagnostic)
        {
            node = null;
            diagnostic = null;

            var line = _line;
            var column = _column;

            AdvanceCharacter();
            SkipHorizontalWhitespace();

            var table =
                new TomlTable(
                    line,
                    column,
                    TomlTableDefinitionKind.Inline);

            if (IsEnd)
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidValue,
                    "Unterminated TOML inline table.",
                    line,
                    column);
                return false;
            }

            if (Current == '}')
            {
                AdvanceCharacter();
                node = table;
                return true;
            }

            if (IsNewlineStart(Current) ||
                Current == '#')
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidValue,
                    "TOML 1.0 inline tables cannot contain line breaks or comments between entries.",
                    _line,
                    _column);
                return false;
            }

            while (true)
            {
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
                    Current == '}' ||
                    Current == ',' ||
                    Current == '#' ||
                    IsNewlineStart(Current))
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.MissingValue,
                        "Expected a value after '=' in the TOML inline table.",
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

                if (!AssignKeyPath(
                    table,
                    parts,
                    value,
                    out diagnostic))
                {
                    return false;
                }

                SkipHorizontalWhitespace();

                if (IsEnd)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidValue,
                        "Unterminated TOML inline table.",
                        line,
                        column);
                    return false;
                }

                if (Current == '}')
                {
                    AdvanceCharacter();
                    node = table;
                    return true;
                }

                if (Current != ',')
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidValue,
                        "Expected ',' or '}' after a TOML inline-table entry.",
                        _line,
                        _column);
                    return false;
                }

                AdvanceCharacter();
                SkipHorizontalWhitespace();

                if (IsEnd)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidValue,
                        "Unterminated TOML inline table.",
                        line,
                        column);
                    return false;
                }

                if (Current == '}')
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidValue,
                        "TOML 1.0 inline tables do not permit a trailing comma.",
                        _line,
                        _column);
                    return false;
                }

                if (IsNewlineStart(Current) ||
                    Current == '#')
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidValue,
                        "TOML 1.0 inline tables cannot contain line breaks or comments between entries.",
                        _line,
                        _column);
                    return false;
                }
            }
        }

        private bool SkipArrayTrivia(
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
                    if (!SkipComment(
                        out diagnostic))
                    {
                        return false;
                    }

                    if (IsEnd)
                        return true;

                    if (!ConsumeNewline(
                        out diagnostic))
                    {
                        return false;
                    }

                    continue;
                }

                if (IsNewlineStart(Current))
                {
                    if (!ConsumeNewline(
                        out diagnostic))
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }

            return true;
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

                if (!AppendRawStringCharacter(
                    sb,
                    TomlDiagnosticCode.InvalidString,
                    out diagnostic))
                {
                    return false;
                }
            }

            diagnostic = Error(
                TomlDiagnosticCode.InvalidString,
                "Unterminated TOML basic string.",
                sourceLine,
                sourceColumn);
            return false;
        }

        private bool ParseLiteralStringText(
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

                if ((c < 0x20 && c != '\t') ||
                    c == 0x7F)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidString,
                        "Control character in TOML literal string.",
                        _line,
                        _column);
                    return false;
                }

                if (!AppendRawStringCharacter(
                    sb,
                    TomlDiagnosticCode.InvalidString,
                    out diagnostic))
                {
                    return false;
                }
            }

            diagnostic = Error(
                TomlDiagnosticCode.InvalidString,
                "Unterminated TOML literal string.",
                sourceLine,
                sourceColumn);
            return false;
        }

        private bool ParseMultilineBasicStringText(
            out string value,
            out TomlDiagnostic diagnostic)
        {
            value = null;
            diagnostic = null;

            var sourceLine = _line;
            var sourceColumn = _column;

            AdvanceCharacter();
            AdvanceCharacter();
            AdvanceCharacter();

            var sb = new StringBuilder();

            if (!IsEnd &&
                IsNewlineStart(Current))
            {
                if (!ConsumeNewline(
                    out diagnostic))
                {
                    return false;
                }
            }

            while (!IsEnd)
            {
                var c = Current;

                if (c == '"')
                {
                    var quoteCount =
                        CountConsecutive('"');

                    if (quoteCount >= 3)
                    {
                        if (quoteCount <= 5)
                        {
                            for (var i = 0;
                                 i < quoteCount - 3;
                                 i++)
                            {
                                sb.Append('"');
                            }

                            for (var i = 0;
                                 i < quoteCount;
                                 i++)
                            {
                                AdvanceCharacter();
                            }
                        }
                        else
                        {
                            for (var i = 0;
                                 i < 3;
                                 i++)
                            {
                                AdvanceCharacter();
                            }
                        }

                        value = sb.ToString();
                        return true;
                    }

                    for (var i = 0;
                         i < quoteCount;
                         i++)
                    {
                        sb.Append('"');
                        AdvanceCharacter();
                    }

                    continue;
                }

                if (c == '\\')
                {
                    bool consumedContinuation;

                    if (!TryConsumeMultilineContinuation(
                        out consumedContinuation,
                        out diagnostic))
                    {
                        return false;
                    }

                    if (consumedContinuation)
                        continue;

                    if (!ParseEscape(
                        sb,
                        out diagnostic))
                    {
                        return false;
                    }

                    continue;
                }

                if (IsNewlineStart(c))
                {
                    if (!ConsumeNewline(
                        out diagnostic))
                    {
                        return false;
                    }

                    sb.Append('\n');
                    continue;
                }

                if ((c < 0x20 && c != '\t') ||
                    c == 0x7F)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidString,
                        "Unescaped control character in TOML multiline basic string.",
                        _line,
                        _column);
                    return false;
                }

                if (!AppendRawStringCharacter(
                    sb,
                    TomlDiagnosticCode.InvalidString,
                    out diagnostic))
                {
                    return false;
                }
            }

            diagnostic = Error(
                TomlDiagnosticCode.InvalidString,
                "Unterminated TOML multiline basic string.",
                sourceLine,
                sourceColumn);
            return false;
        }

        private bool ParseMultilineLiteralStringText(
            out string value,
            out TomlDiagnostic diagnostic)
        {
            value = null;
            diagnostic = null;

            var sourceLine = _line;
            var sourceColumn = _column;

            AdvanceCharacter();
            AdvanceCharacter();
            AdvanceCharacter();

            var sb = new StringBuilder();

            if (!IsEnd &&
                IsNewlineStart(Current))
            {
                if (!ConsumeNewline(
                    out diagnostic))
                {
                    return false;
                }
            }

            while (!IsEnd)
            {
                var c = Current;

                if (c == '\'')
                {
                    var quoteCount =
                        CountConsecutive('\'');

                    if (quoteCount >= 3)
                    {
                        if (quoteCount <= 5)
                        {
                            for (var i = 0;
                                 i < quoteCount - 3;
                                 i++)
                            {
                                sb.Append('\'');
                            }

                            for (var i = 0;
                                 i < quoteCount;
                                 i++)
                            {
                                AdvanceCharacter();
                            }
                        }
                        else
                        {
                            for (var i = 0;
                                 i < 3;
                                 i++)
                            {
                                AdvanceCharacter();
                            }
                        }

                        value = sb.ToString();
                        return true;
                    }

                    for (var i = 0;
                         i < quoteCount;
                         i++)
                    {
                        sb.Append('\'');
                        AdvanceCharacter();
                    }

                    continue;
                }

                if (IsNewlineStart(c))
                {
                    if (!ConsumeNewline(
                        out diagnostic))
                    {
                        return false;
                    }

                    sb.Append('\n');
                    continue;
                }

                if ((c < 0x20 && c != '\t') ||
                    c == 0x7F)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.InvalidString,
                        "Control character in TOML multiline literal string.",
                        _line,
                        _column);
                    return false;
                }

                if (!AppendRawStringCharacter(
                    sb,
                    TomlDiagnosticCode.InvalidString,
                    out diagnostic))
                {
                    return false;
                }
            }

            diagnostic = Error(
                TomlDiagnosticCode.InvalidString,
                "Unterminated TOML multiline literal string.",
                sourceLine,
                sourceColumn);
            return false;
        }

        private bool TryConsumeMultilineContinuation(
            out bool consumed,
            out TomlDiagnostic diagnostic)
        {
            consumed = false;
            diagnostic = null;

            var scan = _index + 1;

            while (scan < _text.Length &&
                   IsHorizontalWhitespace(
                       _text[scan]))
            {
                scan++;
            }

            if (scan >= _text.Length ||
                !IsNewlineStart(_text[scan]))
            {
                return true;
            }

            AdvanceCharacter();

            while (!IsEnd &&
                   IsHorizontalWhitespace(Current))
            {
                AdvanceCharacter();
            }

            if (!ConsumeNewline(
                out diagnostic))
            {
                return false;
            }

            while (!IsEnd)
            {
                if (IsHorizontalWhitespace(Current))
                {
                    AdvanceCharacter();
                    continue;
                }

                if (IsNewlineStart(Current))
                {
                    if (!ConsumeNewline(
                        out diagnostic))
                    {
                        return false;
                    }

                    continue;
                }

                break;
            }

            consumed = true;
            return true;
        }

        private bool AppendRawStringCharacter(
            StringBuilder sb,
            TomlDiagnosticCode diagnosticCode,
            out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            var c = Current;

            if (IsHighSurrogate(c))
            {
                if (_index + 1 >= _text.Length ||
                    !IsLowSurrogate(
                        _text[_index + 1]))
                {
                    diagnostic = Error(
                        diagnosticCode,
                        "String contains an unpaired UTF-16 surrogate.",
                        _line,
                        _column);
                    return false;
                }

                sb.Append(c);
                AdvanceCharacter();

                sb.Append(Current);
                AdvanceCharacter();

                return true;
            }

            if (IsLowSurrogate(c))
            {
                diagnostic = Error(
                    diagnosticCode,
                    "String contains an unpaired UTF-16 surrogate.",
                    _line,
                    _column);
                return false;
            }

            sb.Append(c);
            AdvanceCharacter();

            return true;
        }

        private bool IsTripleDelimiter(
            char delimiter)
        {
            return _index + 2 < _text.Length &&
                   _text[_index] == delimiter &&
                   _text[_index + 1] == delimiter &&
                   _text[_index + 2] == delimiter;
        }

        private int CountConsecutive(
            char value)
        {
            var count = 0;

            while (_index + count < _text.Length &&
                   _text[_index + count] == value)
            {
                count++;
            }

            return count;
        }

        private static bool IsHighSurrogate(
            char c)
        {
            return c >= 0xD800 &&
                   c <= 0xDBFF;
        }

        private static bool IsLowSurrogate(
            char c)
        {
            return c >= 0xDC00 &&
                   c <= 0xDFFF;
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

            while (!IsEnd)
            {
                if (IsNewlineStart(Current) ||
                    Current == '#' ||
                    Current == ',' ||
                    Current == ']' ||
                    Current == '}')
                {
                    break;
                }

                if (IsHorizontalWhitespace(Current))
                {
                    if (ShouldConsumeDateTimeSpace(
                            start))
                    {
                        AdvanceCharacter();
                        continue;
                    }

                    break;
                }

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

            TomlValue temporalValue;

            if (TomlTemporalParser.TryParse(
                    token,
                    line,
                    column,
                    out temporalValue))
            {
                node = temporalValue;
                return true;
            }

            if (TomlTemporalParser.LooksTemporal(
                    token))
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidDateTime,
                    "Malformed TOML date or time value '" +
                    token +
                    "'.",
                    line,
                    column);
                return false;
            }

            bool isFloat;
            long integerValue;
            double floatValue;
            bool rangeError;

            if (TryParseTomlNumber(
                token,
                out isFloat,
                out integerValue,
                out floatValue,
                out rangeError))
            {
                if (isFloat)
                {
                    node = new TomlValue(
                        TomlValueKind.Float,
                        floatValue,
                        line,
                        column);
                }
                else
                {
                    node = new TomlValue(
                        TomlValueKind.Integer,
                        integerValue,
                        line,
                        column);
                }

                return true;
            }

            if (rangeError)
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidNumber,
                    "Numeric value is outside the supported TOML range.",
                    line,
                    column);
                return false;
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

        private bool ShouldConsumeDateTimeSpace(
            int start)
        {
            if (Current != ' ' ||
                _index != start + 10 ||
                start < 0 ||
                start + 11 >= _text.Length)
            {
                return false;
            }

            if (!IsAsciiDigit(_text[start]) ||
                !IsAsciiDigit(_text[start + 1]) ||
                !IsAsciiDigit(_text[start + 2]) ||
                !IsAsciiDigit(_text[start + 3]) ||
                _text[start + 4] != '-' ||
                !IsAsciiDigit(_text[start + 5]) ||
                !IsAsciiDigit(_text[start + 6]) ||
                _text[start + 7] != '-' ||
                !IsAsciiDigit(_text[start + 8]) ||
                !IsAsciiDigit(_text[start + 9]))
            {
                return false;
            }

            return
                IsAsciiDigit(
                    _text[start + 11]);
        }

        private static bool TryParseTomlNumber(
            string token,
            out bool isFloat,
            out long integerValue,
            out double floatValue,
            out bool rangeError)
        {
            isFloat = false;
            integerValue = 0;
            floatValue = 0.0;
            rangeError = false;

            if (string.IsNullOrEmpty(token))
                return false;

            if (token.Length >= 2 &&
                token[0] == '0')
            {
                int numberBase;

                switch (token[1])
                {
                    case 'x':
                        numberBase = 16;
                        break;

                    case 'o':
                        numberBase = 8;
                        break;

                    case 'b':
                        numberBase = 2;
                        break;

                    default:
                        numberBase = 0;
                        break;
                }

                if (numberBase != 0)
                {
                    return TryParseBaseInteger(
                        token,
                        numberBase,
                        out integerValue,
                        out rangeError);
                }
            }

            var index = 0;

            if (token[index] == '+' ||
                token[index] == '-')
            {
                index++;

                if (index >= token.Length)
                    return false;
            }

            var integerStart = index;
            int integerDigits;

            if (!ConsumeDecimalDigits(
                token,
                ref index,
                out integerDigits))
            {
                return false;
            }

            if (token[integerStart] == '0' &&
                integerDigits > 1)
            {
                return false;
            }

            var hasFraction = false;
            var hasExponent = false;

            if (index < token.Length &&
                token[index] == '.')
            {
                hasFraction = true;
                index++;

                int fractionDigits;

                if (!ConsumeDecimalDigits(
                    token,
                    ref index,
                    out fractionDigits))
                {
                    return false;
                }
            }

            if (index < token.Length &&
                (token[index] == 'e' ||
                 token[index] == 'E'))
            {
                hasExponent = true;
                index++;

                if (index < token.Length &&
                    (token[index] == '+' ||
                     token[index] == '-'))
                {
                    index++;
                }

                int exponentDigits;

                if (!ConsumeDecimalDigits(
                    token,
                    ref index,
                    out exponentDigits))
                {
                    return false;
                }
            }

            if (index != token.Length)
                return false;

            var normalized =
                RemoveNumericUnderscores(token);

            if (hasFraction ||
                hasExponent)
            {
                isFloat = true;

                if (!double.TryParse(
                        normalized,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out floatValue) ||
                    double.IsInfinity(floatValue) ||
                    double.IsNaN(floatValue))
                {
                    rangeError = true;
                    return false;
                }

                return true;
            }

            if (!long.TryParse(
                    normalized,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out integerValue))
            {
                rangeError = true;
                return false;
            }

            return true;
        }

        private static bool TryParseBaseInteger(
            string token,
            int numberBase,
            out long value,
            out bool rangeError)
        {
            value = 0;
            rangeError = false;

            var index = 2;

            if (index >= token.Length)
                return false;

            var firstDigit =
                DigitValue(
                    token[index],
                    numberBase);

            if (firstDigit < 0)
                return false;

            ulong accumulated = 0;

            while (index < token.Length)
            {
                var digit =
                    DigitValue(
                        token[index],
                        numberBase);

                if (digit >= 0)
                {
                    var unsignedDigit =
                        (ulong)digit;

                    if (accumulated >
                        ((ulong)long.MaxValue -
                         unsignedDigit) /
                        (ulong)numberBase)
                    {
                        rangeError = true;
                        return false;
                    }

                    accumulated =
                        accumulated *
                        (ulong)numberBase +
                        unsignedDigit;

                    index++;
                    continue;
                }

                if (token[index] == '_')
                {
                    if (index == 2 ||
                        index + 1 >= token.Length ||
                        DigitValue(
                            token[index - 1],
                            numberBase) < 0 ||
                        DigitValue(
                            token[index + 1],
                            numberBase) < 0)
                    {
                        return false;
                    }

                    index++;
                    continue;
                }

                return false;
            }

            value = (long)accumulated;
            return true;
        }

        private static int DigitValue(
            char c,
            int numberBase)
        {
            int value;

            if (c >= '0' &&
                c <= '9')
            {
                value = c - '0';
            }
            else if (c >= 'a' &&
                     c <= 'f')
            {
                value =
                    10 +
                    c -
                    'a';
            }
            else if (c >= 'A' &&
                     c <= 'F')
            {
                value =
                    10 +
                    c -
                    'A';
            }
            else
            {
                return -1;
            }

            return value < numberBase
                ? value
                : -1;
        }

        private static bool ConsumeDecimalDigits(
            string token,
            ref int index,
            out int digitCount)
        {
            digitCount = 0;

            if (index >= token.Length ||
                !IsAsciiDigit(token[index]))
            {
                return false;
            }

            index++;
            digitCount++;

            while (index < token.Length)
            {
                if (IsAsciiDigit(token[index]))
                {
                    index++;
                    digitCount++;
                    continue;
                }

                if (token[index] == '_')
                {
                    if (index + 1 >= token.Length ||
                        !IsAsciiDigit(
                            token[index + 1]))
                    {
                        return false;
                    }

                    index += 2;
                    digitCount++;
                    continue;
                }

                break;
            }

            return true;
        }

        private static string RemoveNumericUnderscores(
            string token)
        {
            if (token.IndexOf('_') < 0)
                return token;

            var sb =
                new StringBuilder(
                    token.Length);

            for (var i = 0;
                 i < token.Length;
                 i++)
            {
                if (token[i] != '_')
                    sb.Append(token[i]);
            }

            return sb.ToString();
        }

        private static bool IsAsciiDigit(
            char c)
        {
            return c >= '0' &&
                   c <= '9';
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

            uint codePoint;

            if (!uint.TryParse(
                    hex,
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out codePoint) ||
                codePoint > 0x10FFFFu ||
                (codePoint >= 0xD800u &&
                 codePoint <= 0xDFFFu))
            {
                diagnostic = Error(
                    TomlDiagnosticCode.InvalidEscape,
                    "Unicode escape contains an invalid Unicode scalar value.",
                    escapeLine,
                    escapeColumn);
                return false;
            }

            sb.Append(
                char.ConvertFromUtf32(
                    (int)codePoint));

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