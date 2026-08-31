using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    internal static class TemplateArgumentAnalyzer
    {
        public static void Analyze(
            TemplateArgument[] arguments,
            SourceSpan constructNameSpan,
            TemplateArgumentContract contract,
            TemplateDiagnostic[] parserDiagnostics,
            IList<TemplateDiagnostic> diagnostics
        )
        {
            int positionalIndex = 0;
            var seenNamedArguments = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < arguments.Length; index++)
            {
                var positional = arguments[index] as TemplatePositionalArgument;

                if (positional != null)
                {
                    AnalyzePositional(
                        positional,
                        positionalIndex,
                        contract,
                        diagnostics
                    );

                    positionalIndex++;
                    continue;
                }

                var named = arguments[index] as TemplateNamedArgument;
                if (named == null)
                    continue;

                AnalyzeNamed(
                    named,
                    contract,
                    parserDiagnostics,
                    diagnostics,
                    seenNamedArguments
                );
            }

            AddMissingRequiredPositionals(
                positionalIndex,
                constructNameSpan,
                contract,
                diagnostics
            );

            AddMissingRequiredNamed(
                constructNameSpan,
                contract,
                diagnostics,
                seenNamedArguments
            );
        }

        private static void AnalyzePositional(
            TemplatePositionalArgument argument,
            int positionalIndex,
            TemplateArgumentContract contract,
            IList<TemplateDiagnostic> diagnostics
        )
        {
            if (positionalIndex >= contract.PositionalArgumentCount)
            {
                diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateLanguageAnalyzer.UnexpectedPositionalArgumentDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Unexpected positional argument.",
                        argument.Span
                    )
                );

                return;
            }

            TemplatePositionalArgumentDefinition definition =
                contract.GetPositionalArgument(positionalIndex);

            if (argument.Value.Kind == TemplateArgumentValueKind.Missing
                || definition.Allows(argument.Value.Kind))
            {
                return;
            }

            diagnostics.Add(
                new TemplateDiagnostic(
                    TemplateLanguageAnalyzer.InvalidArgumentValueKindDiagnosticCode,
                    TemplateDiagnosticSeverity.Error,
                    "Positional argument uses an unsupported value kind.",
                    argument.Value.Span
                )
            );
        }

        private static void AnalyzeNamed(
            TemplateNamedArgument argument,
            TemplateArgumentContract contract,
            TemplateDiagnostic[] parserDiagnostics,
            IList<TemplateDiagnostic> diagnostics,
            ISet<string> seenNamedArguments
        )
        {
            if (argument.Name.Length == 0
                || TemplateDiagnosticUtilities.HasCodeWithinSpan(
                    parserDiagnostics,
                    TemplateParser.InvalidArgumentNameDiagnosticCode,
                    argument.NameSpan
                ))
            {
                return;
            }

            TemplateNamedArgumentDefinition definition;

            if (!contract.TryGetNamedArgument(argument.Name, out definition))
            {
                diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateLanguageAnalyzer.UnknownNamedArgumentDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Unknown named argument '" + argument.Name + "'.",
                        argument.NameSpan
                    )
                );

                return;
            }

            if (!seenNamedArguments.Add(argument.Name))
            {
                diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateLanguageAnalyzer.DuplicateNamedArgumentDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Named argument '" + argument.Name + "' is supplied more than once.",
                        argument.NameSpan
                    )
                );

                return;
            }

            if (argument.Value.Kind == TemplateArgumentValueKind.Missing
                || definition.Allows(argument.Value.Kind))
            {
                return;
            }

            diagnostics.Add(
                new TemplateDiagnostic(
                    TemplateLanguageAnalyzer.InvalidArgumentValueKindDiagnosticCode,
                    TemplateDiagnosticSeverity.Error,
                    "Named argument '" + argument.Name + "' uses an unsupported value kind.",
                    argument.Value.Span
                )
            );
        }

        private static void AddMissingRequiredPositionals(
            int positionalCount,
            SourceSpan constructNameSpan,
            TemplateArgumentContract contract,
            IList<TemplateDiagnostic> diagnostics
        )
        {
            for (int index = positionalCount;
                index < contract.PositionalArgumentCount;
                index++)
            {
                TemplatePositionalArgumentDefinition definition =
                    contract.GetPositionalArgument(index);

                if (!definition.Required)
                    continue;

                diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateLanguageAnalyzer.MissingRequiredPositionalArgumentDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Required positional argument "
                            + (index + 1).ToString()
                            + " is missing.",
                        constructNameSpan
                    )
                );
            }
        }

        private static void AddMissingRequiredNamed(
            SourceSpan constructNameSpan,
            TemplateArgumentContract contract,
            IList<TemplateDiagnostic> diagnostics,
            ISet<string> seenNamedArguments
        )
        {
            for (int index = 0; index < contract.NamedArgumentCount; index++)
            {
                TemplateNamedArgumentDefinition definition =
                    contract.GetNamedArgument(index);

                if (!definition.Required || seenNamedArguments.Contains(definition.Name))
                    continue;

                diagnostics.Add(
                    new TemplateDiagnostic(
                        TemplateLanguageAnalyzer.MissingRequiredNamedArgumentDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Required named argument '" + definition.Name + "' is missing.",
                        constructNameSpan
                    )
                );
            }
        }
    }
}