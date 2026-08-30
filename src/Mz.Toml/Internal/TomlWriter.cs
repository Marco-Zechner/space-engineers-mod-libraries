using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Mz.Toml.Internal
{
    internal static class TomlWriter
    {
        public static string Write(
            TomlDocument document)
        {
            var sb = new StringBuilder();

            var wroteAnything =
                AppendValueEntries(
                    sb,
                    document.Root);

            var path = new List<string>();

            AppendChildTables(
                sb,
                document.Root,
                path,
                ref wroteAnything);

            return sb.ToString();
        }

        private static bool AppendValueEntries(
            StringBuilder sb,
            TomlTable table)
        {
            var wroteAny = false;

            foreach (var pair in table)
            {
                if (pair.Value.Kind ==
                    TomlNodeKind.Table)
                {
                    var child =
                        (TomlTable)pair.Value;

                    if (child.DefinitionKind !=
                        TomlTableDefinitionKind.Inline)
                    {
                        continue;
                    }
                }

                AppendKey(
                    sb,
                    pair.Key);

                sb.Append(" = ");

                AppendNode(
                    sb,
                    pair.Value);

                sb.Append('\n');
                wroteAny = true;
            }

            return wroteAny;
        }

        private static void AppendChildTables(
            StringBuilder sb,
            TomlTable table,
            List<string> path,
            ref bool wroteAnything)
        {
            foreach (var pair in table)
            {
                if (pair.Value.Kind !=
                    TomlNodeKind.Table)
                {
                    continue;
                }

                var child =
                    (TomlTable)pair.Value;

                if (child.DefinitionKind ==
                    TomlTableDefinitionKind.Inline)
                {
                    continue;
                }

                path.Add(pair.Key);

                if (wroteAnything)
                    sb.Append('\n');

                AppendHeader(
                    sb,
                    path);

                sb.Append('\n');

                AppendValueEntries(
                    sb,
                    child);

                wroteAnything = true;

                AppendChildTables(
                    sb,
                    child,
                    path,
                    ref wroteAnything);

                path.RemoveAt(
                    path.Count - 1);
            }
        }

        private static void AppendHeader(
            StringBuilder sb,
            IList<string> path)
        {
            sb.Append('[');

            for (var i = 0;
                 i < path.Count;
                 i++)
            {
                if (i > 0)
                    sb.Append('.');

                AppendKey(
                    sb,
                    path[i]);
            }

            sb.Append(']');
        }

        private static void AppendKey(
            StringBuilder sb,
            string key)
        {
            if (IsBareKey(key))
            {
                sb.Append(key);
                return;
            }

            AppendBasicString(
                sb,
                key);
        }

        private static void AppendNode(
            StringBuilder sb,
            TomlNode node)
        {
            switch (node.Kind)
            {
                case TomlNodeKind.Value:
                    AppendValue(
                        sb,
                        (TomlValue)node);
                    return;

                case TomlNodeKind.Array:
                    AppendArray(
                        sb,
                        (TomlArray)node);
                    return;

                case TomlNodeKind.Table:
                    AppendInlineTable(
                        sb,
                        (TomlTable)node);
                    return;

                default:
                    throw new InvalidOperationException(
                        "Unsupported TOML node kind: " +
                        node.Kind);
            }
        }

        private static void AppendArray(
            StringBuilder sb,
            TomlArray array)
        {
            sb.Append('[');

            for (var i = 0;
                 i < array.Count;
                 i++)
            {
                if (i > 0)
                    sb.Append(", ");

                AppendNode(
                    sb,
                    array[i]);
            }

            sb.Append(']');
        }

        private static void AppendInlineTable(
            StringBuilder sb,
            TomlTable table)
        {
            sb.Append('{');

            var first = true;

            foreach (var pair in table)
            {
                if (!first)
                    sb.Append(", ");

                AppendKey(
                    sb,
                    pair.Key);

                sb.Append(" = ");

                AppendNode(
                    sb,
                    pair.Value);

                first = false;
            }

            sb.Append('}');
        }

        private static void AppendValue(
            StringBuilder sb,
            TomlValue value)
        {
            switch (value.ValueKind)
            {
                case TomlValueKind.String:
                    AppendBasicString(
                        sb,
                        value.AsString());
                    return;

                case TomlValueKind.Integer:
                    sb.Append(
                        value.AsInteger().ToString(
                            CultureInfo.InvariantCulture));
                    return;

                case TomlValueKind.Float:
                {
                    var number =
                        value.AsFloat();

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

                    if (number == 0.0 &&
                        double.IsNegativeInfinity(
                            1.0 / number))
                    {
                        sb.Append("-0.0");
                        return;
                    }

                    var text =
                        number.ToString(
                            "R",
                            CultureInfo.InvariantCulture);

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
                    sb.Append(
                        value.AsBoolean()
                            ? "true"
                            : "false");
                    return;

                default:
                    throw new InvalidOperationException(
                        "Unsupported TOML scalar kind: " +
                        value.ValueKind);
            }
        }

        private static void AppendBasicString(
            StringBuilder sb,
            string value)
        {
            sb.Append('"');

            for (var i = 0;
                 i < value.Length;
                 i++)
            {
                var c = value[i];

                if (c >= 0xD800 &&
                    c <= 0xDBFF)
                {
                    if (i + 1 >= value.Length ||
                        value[i + 1] < 0xDC00 ||
                        value[i + 1] > 0xDFFF)
                    {
                        throw new InvalidOperationException(
                            "Cannot write a TOML string containing an unpaired UTF-16 surrogate.");
                    }

                    sb.Append(c);
                    sb.Append(value[i + 1]);
                    i++;
                    continue;
                }

                if (c >= 0xDC00 &&
                    c <= 0xDFFF)
                {
                    throw new InvalidOperationException(
                        "Cannot write a TOML string containing an unpaired UTF-16 surrogate.");
                }

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
                        if (c < 0x20 ||
                            c == 0x7F)
                        {
                            sb.Append("\\u");
                            sb.Append(
                                ((int)c).ToString(
                                    "X4",
                                    CultureInfo.InvariantCulture));
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

        private static bool IsBareKey(
            string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            for (var i = 0;
                 i < key.Length;
                 i++)
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