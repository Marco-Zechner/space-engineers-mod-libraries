using System.Collections.Generic;

namespace Mz.Toml.Internal
{
    internal sealed partial class TomlParser
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

            _root = new TomlTable(1, 1, TomlTableDefinitionKind.Root);
            _currentTable = _root;
        }

        public static TomlParseResult Parse(string text) => new TomlParser(text).ParseDocument();

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

            return new TomlParseResult(new TomlDocument(_root), new TomlDiagnostic[0]);
        }

        private bool SkipDocumentTrivia(out TomlDiagnostic diagnostic)
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

                if (!IsNewlineStart(Current))
                    return true;

                if (!ConsumeNewline(out diagnostic))
                    return false;
            }

            return true;
        }

        private bool ParseAssignment(out TomlDiagnostic diagnostic)
        {
            diagnostic = null;

            List<TomlKeyPart> parts;

            if (!ParseKeyPath('=', false, out parts, out diagnostic))
                return false;

            AdvanceCharacter();
            SkipHorizontalWhitespace();

            if (IsEnd || Current == '#' || IsNewlineStart(Current))
            {
                diagnostic = Error(TomlDiagnosticCode.MissingValue, "Expected a value after '='.", _line, _column);
                return false;
            }

            TomlNode value;

            if (!ParseValue(out value, out diagnostic))
                return false;

            SkipHorizontalWhitespace();

            if (!IsEnd && Current == '#')
            {
                if (!SkipComment(out diagnostic))
                    return false;
            }

            if (IsEnd)
                return AssignKeyPath(_currentTable, parts, value, out diagnostic);

            if (!IsNewlineStart(Current))
            {
                diagnostic = Error(TomlDiagnosticCode.TrailingCharacters, "Unexpected characters after the TOML value.", _line, _column);
                return false;
            }

            if (!ConsumeNewline(out diagnostic))
                return false;

            return AssignKeyPath(_currentTable, parts, value, out diagnostic);
        }
    }
}
