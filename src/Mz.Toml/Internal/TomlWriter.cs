using System;
using System.Globalization;
using System.Text;

namespace Mz.Toml.Internal
{
    internal static class TomlWriter
    {
        public static string Write(TomlDocument document)
        {
            var sb = new StringBuilder();

            foreach (var pair in document.Root)
            {
                if (pair.Value.Kind != TomlNodeKind.Value)
                {
                    throw new InvalidOperationException(
                        "Slice 1 can only write root-level scalar values.");
                }

                AppendKey(sb, pair.Key);
                sb.Append(" = ");
                AppendValue(sb, (TomlValue)pair.Value);
                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static void AppendKey(StringBuilder sb, string key)
        {
            if (IsBareKey(key))
            {
                sb.Append(key);
                return;
            }

            AppendBasicString(sb, key);
        }

        private static void AppendValue(StringBuilder sb, TomlValue value)
        {
            switch (value.ValueKind)
            {
                case TomlValueKind.String:
                    AppendBasicString(sb, value.AsString());
                    return;

                case TomlValueKind.Integer:
                    sb.Append(value.AsInteger().ToString(CultureInfo.InvariantCulture));
                    return;

                case TomlValueKind.Float:
                {
                    var number = value.AsFloat();

                    if (double.IsNaN(number))
                    {
                        sb.Append("nan");
                        return;
                    }

                    if (double.IsPositiveInfinity(number))
                    {
                        sb.Append("inf");
                        return;
                    }

                    if (double.IsNegativeInfinity(number))
                    {
                        sb.Append("-inf");
                        return;
                    }

                    var text = number.ToString("R", CultureInfo.InvariantCulture);
                    if (text.IndexOf('.') < 0 &&
                        text.IndexOf('e') < 0 &&
                        text.IndexOf('E') < 0)
                    {
                        text += ".0";
                    }

                    sb.Append(text);
                    return;
                }

                case TomlValueKind.Boolean:
                    sb.Append(value.AsBoolean() ? "true" : "false");
                    return;

                default:
                    throw new InvalidOperationException(
                        "Unsupported TOML scalar kind: " + value.ValueKind);
            }
        }

        private static void AppendBasicString(StringBuilder sb, string value)
        {
            sb.Append('"');

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    default:
                        if (c < 0x20 || c == 0x7F)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            sb.Append('"');
        }

        private static bool IsBareKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            for (var i = 0; i < key.Length; i++)
            {
                var c = key[i];
                if ((c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_' ||
                    c == '-')
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}