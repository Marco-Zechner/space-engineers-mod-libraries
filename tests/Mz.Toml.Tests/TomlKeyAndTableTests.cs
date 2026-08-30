using Mz.Toml;
using Xunit;

namespace Mz.Toml.Tests
{
    public sealed class TomlKeyAndTableTests
    {
        [Fact]
        public void Parses_Dotted_And_Mixed_Quoted_Keys()
        {
            var document = Toml.Parse(
                "name.first = \"Arthur\"\n" +
                "\"name\".'last' = \"Dent\"\n" +
                "count.a = 1\n" +
                "count . b = 2\n" +
                "\"count\" . \"c\" = 3\n" +
                "'count'.'d' = 4\n" +
                "many.dots.dot.dot.dot = 42\n");

            var name = Table(document.Root, "name");

            Assert.Equal("Arthur", Value(name, "first").AsString());
            Assert.Equal("Dent", Value(name, "last").AsString());

            var count = Table(document.Root, "count");

            Assert.Equal(1L, Value(count, "a").AsInteger());
            Assert.Equal(2L, Value(count, "b").AsInteger());
            Assert.Equal(3L, Value(count, "c").AsInteger());
            Assert.Equal(4L, Value(count, "d").AsInteger());

            var many = Table(document.Root, "many");
            var dots = Table(many, "dots");
            var dot1 = Table(dots, "dot");
            var dot2 = Table(dot1, "dot");

            Assert.Equal(
                42L,
                Value(dot2, "dot").AsInteger());
        }

        [Fact]
        public void Quoted_Dots_Stay_Inside_One_Key_Segment()
        {
            var document = Toml.Parse(
                "\"with.dot\" = 2\n" +
                "[table.withdot]\n" +
                "\"key.with.dots\" = 6\n" +
                "\"escaped\\u002edot\" = 7\n");

            Assert.Equal(
                2L,
                Value(document.Root, "with.dot").AsInteger());

            var table = Table(
                Table(document.Root, "table"),
                "withdot");

            Assert.Equal(
                6L,
                Value(table, "key.with.dots").AsInteger());

            Assert.Equal(
                7L,
                Value(table, "escaped.dot").AsInteger());
        }

        [Theory]
        [InlineData("\"\" = \"blank\"\n")]
        [InlineData("'' = \"blank\"\n")]
        public void Empty_Quoted_Root_Key_Can_Be_A_Value(
            string text)
        {
            var document = Toml.Parse(text);

            Assert.Equal(
                "blank",
                Value(document.Root, "").AsString());
        }

        [Fact]
        public void Empty_Quoted_Key_Segments_Are_Valid()
        {
            var document = Toml.Parse(
                "''.x = \"empty.x\"\n" +
                "x.\"\" = \"x.empty\"\n" +
                "[a]\n" +
                "\"\".'' = \"empty.empty\"\n");

            Assert.Equal(
                "empty.x",
                Value(Table(document.Root, ""), "x").AsString());

            Assert.Equal(
                "x.empty",
                Value(Table(document.Root, "x"), "").AsString());

            Assert.Equal(
                "empty.empty",
                Value(
                    Table(
                        Table(document.Root, "a"),
                        ""),
                    "").AsString());
        }

        [Fact]
        public void Basic_And_Literal_Quoted_Keys_Have_Different_Escape_Rules()
        {
            var document = Toml.Parse(
                "\"\\u0061\" = 1\n" +
                "'\\u0061' = 2\n");

            Assert.Equal(
                1L,
                Value(document.Root, "a").AsInteger());

            Assert.Equal(
                2L,
                Value(document.Root, "\\u0061").AsInteger());
        }

        [Fact]
        public void Parses_Nested_Explicit_Tables()
        {
            var document = Toml.Parse(
                "[a]\n" +
                "key = 1\n" +
                "[a.extend]\n" +
                "key = 2\n" +
                "[a.extend.more]\n" +
                "key = 3\n");

            var a = Table(document.Root, "a");

            Assert.Equal(
                1L,
                Value(a, "key").AsInteger());

            var extend = Table(a, "extend");

            Assert.Equal(
                2L,
                Value(extend, "key").AsInteger());

            Assert.Equal(
                3L,
                Value(
                    Table(extend, "more"),
                    "key").AsInteger());
        }

        [Fact]
        public void Omitted_Super_Tables_Are_Created_Implicitly()
        {
            var document = Toml.Parse(
                "[x.y.z.w]\n" +
                "a = 1\n" +
                "[x]\n" +
                "b = 2\n");

            var x = Table(document.Root, "x");

            Assert.Equal(
                2L,
                Value(x, "b").AsInteger());

            var w = Table(
                Table(
                    Table(x, "y"),
                    "z"),
                "w");

            Assert.Equal(
                1L,
                Value(w, "a").AsInteger());
        }

        [Fact]
        public void Explicit_Parent_Can_Appear_Before_Child()
        {
            var document = Toml.Parse(
                "[a]\n" +
                "better = 43\n" +
                "[a.b.c]\n" +
                "answer = 42\n");

            Assert.Equal(
                43L,
                Value(
                    Table(document.Root, "a"),
                    "better").AsInteger());

            Assert.Equal(
                42L,
                Value(
                    Table(
                        Table(
                            Table(document.Root, "a"),
                            "b"),
                        "c"),
                    "answer").AsInteger());
        }

        [Fact]
        public void New_Subtable_Can_Be_Declared_Under_Dotted_Key_Table()
        {
            var document = Toml.Parse(
                "a.b.value = 1\n" +
                "[a.b.extra]\n" +
                "value = 2\n");

            var b = Table(
                Table(document.Root, "a"),
                "b");

            Assert.Equal(
                1L,
                Value(b, "value").AsInteger());

            Assert.Equal(
                2L,
                Value(
                    Table(b, "extra"),
                    "value").AsInteger());
        }

        [Fact]
        public void Writer_Emits_Canonical_Nested_Tables()
        {
            var document = new TomlDocument();

            document.Root.Set(
                "root",
                TomlValue.FromInteger(1));

            var a = new TomlTable();

            a.Set(
                "with.dot",
                TomlValue.FromInteger(2));

            var b = new TomlTable();

            b.Set(
                "answer",
                TomlValue.FromInteger(42));

            a.Set("b", b);
            document.Root.Set("a", a);

            var empty = new TomlTable();

            document.Root.Set("", empty);

            Assert.Equal(
                "root = 1\n" +
                "\n" +
                "[a]\n" +
                "\"with.dot\" = 2\n" +
                "\n" +
                "[a.b]\n" +
                "answer = 42\n" +
                "\n" +
                "[\"\"]\n",
                Toml.Write(document));
        }

        [Fact]
        public void Parsed_Dotted_Keys_Can_Roundtrip_As_Canonical_Tables()
        {
            var first = Toml.Parse(
                "name.first = \"Arthur\"\n" +
                "name.last = \"Dent\"\n");

            var text = Toml.Write(first);

            Assert.Equal(
                "[name]\n" +
                "first = \"Arthur\"\n" +
                "last = \"Dent\"\n",
                text);

            var second = Toml.Parse(text);
            var name = Table(second.Root, "name");

            Assert.Equal(
                "Arthur",
                Value(name, "first").AsString());

            Assert.Equal(
                "Dent",
                Value(name, "last").AsString());
        }

        private static TomlTable Table(
            TomlTable table,
            string key)
        {
            return Assert.IsType<TomlTable>(
                table[key]);
        }

        private static TomlValue Value(
            TomlTable table,
            string key)
        {
            return Assert.IsType<TomlValue>(
                table[key]);
        }
    }
}