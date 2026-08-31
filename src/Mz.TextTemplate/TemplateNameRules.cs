namespace Mz.TextTemplate
{
    internal static class TemplateNameRules
    {
        internal static int FindInvalidConstructNameOffset(string name)
        {
            int index = 0;

            while (index < name.Length)
            {
                if (!IsIdentifierStart(name[index]))
                    return index;

                index++;

                while (index < name.Length && IsIdentifierPart(name[index]))
                    index++;

                if (index == name.Length)
                    return -1;

                if (name[index] != '.')
                    return index;

                index++;

                if (index == name.Length)
                    return index - 1;
            }

            return -1;
        }

        internal static int FindInvalidArgumentNameOffset(string name)
        {
            if (name.Length == 0 || !IsIdentifierStart(name[0]))
                return 0;

            for (int index = 1; index < name.Length; index++)
            {
                if (!IsIdentifierPart(name[index]))
                    return index;
            }

            return -1;
        }

        private static bool IsIdentifierStart(char value) => value == '_' || (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');

        private static bool IsIdentifierPart(char value) => IsIdentifierStart(value) || (value >= '0' && value <= '9') || value == '-';
    }
}
