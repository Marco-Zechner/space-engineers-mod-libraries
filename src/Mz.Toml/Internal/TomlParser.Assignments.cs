using System.Collections.Generic;

namespace Mz.Toml.Internal
{
    internal sealed partial class TomlParser
    {
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

                if (!table.TryGetValue(part.Value, out existing))
                {
                    var created = new TomlTable(
                        part.Line,
                        part.Column,
                        TomlTableDefinitionKind.DottedKey);

                    table.Set(part.Value, created);
                    table = created;
                    continue;
                }

                if (existing.Kind != TomlNodeKind.Table)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        $"Key '{part.Value}' is already defined as a value and cannot be used as a table.",
                        part.Line,
                        part.Column);
                    return false;
                }

                var existingTable = (TomlTable)existing;

                if (existingTable.DefinitionKind == TomlTableDefinitionKind.Inline)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        $"Inline table '{part.Value}' is immutable and cannot be extended through a dotted key.",
                        part.Line,
                        part.Column);
                    return false;
                }

                if (existingTable.DefinitionKind == TomlTableDefinitionKind.Explicit)
                {
                    diagnostic = Error(
                        TomlDiagnosticCode.TableConflict,
                        $"Table '{part.Value}' was already explicitly defined and cannot be extended through a dotted key.",
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
                    $"The key '{finalPart.Value}' is already defined.",
                    finalPart.Line,
                    finalPart.Column);
                return false;
            }

            table.Set(finalPart.Value, value);
            return true;
        }
    }
}
