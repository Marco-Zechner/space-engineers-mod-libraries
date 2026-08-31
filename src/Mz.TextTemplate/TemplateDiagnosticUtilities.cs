using System.Collections.Generic;

namespace Mz.TextTemplate
{
    internal static class TemplateDiagnosticUtilities
    {
        internal static bool HasCodeWithinSpan(
            TemplateDiagnostic[] diagnostics,
            string code,
            SourceSpan span
        )
        {
            for (int index = 0; index < diagnostics.Length; index++)
            {
                TemplateDiagnostic diagnostic = diagnostics[index];

                if (diagnostic.Code == code
                    && diagnostic.Span.Start >= span.Start
                    && diagnostic.Span.Start < span.End)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void SortBySource(IList<TemplateDiagnostic> diagnostics)
        {
            for (int index = 1; index < diagnostics.Count; index++)
            {
                TemplateDiagnostic current = diagnostics[index];
                int destination = index - 1;

                while (destination >= 0
                    && diagnostics[destination].Span.Start > current.Span.Start)
                {
                    diagnostics[destination + 1] = diagnostics[destination];
                    destination--;
                }

                diagnostics[destination + 1] = current;
            }
        }
    }
}
