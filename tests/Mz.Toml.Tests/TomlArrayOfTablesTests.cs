using Mz.Toml;
using Xunit;

namespace Mz.Toml.Tests
{
    public sealed class TomlArrayOfTablesTests
    {
        [Fact]
        public void Repeated_Array_Table_Headers_Append_Table_Elements()
        {
            var document =
                Toml.Parse(
                    "[[people]]\n" +
                    "name = \"one\"\n" +
                    "[[people]]\n" +
                    "name = \"two\"\n");

            var people =
                Assert.IsType<TomlArray>(
                    document.Root["people"]);

            Assert.Equal(2, people.Count);

            Assert.Equal(
                "one",
                Assert.IsType<TomlValue>(
                    Assert.IsType<TomlTable>(
                        people[0])["name"])
                    .AsString());

            Assert.Equal(
                "two",
                Assert.IsType<TomlValue>(
                    Assert.IsType<TomlTable>(
                        people[1])["name"])
                    .AsString());
        }

        [Fact]
        public void Standard_Subtable_Attaches_To_Latest_Array_Element()
        {
            var document =
                Toml.Parse(
                    "[[arr]]\n" +
                    "[arr.sub]\n" +
                    "value = 1\n" +
                    "[[arr]]\n" +
                    "[arr.sub]\n" +
                    "value = 2\n");

            var arr =
                Assert.IsType<TomlArray>(
                    document.Root["arr"]);

            Assert.Equal(2, arr.Count);

            Assert.Equal(
                1,
                Assert.IsType<TomlValue>(
                    Assert.IsType<TomlTable>(
                        Assert.IsType<TomlTable>(
                            arr[0])["sub"])["value"])
                    .AsInteger());

            Assert.Equal(
                2,
                Assert.IsType<TomlValue>(
                    Assert.IsType<TomlTable>(
                        Assert.IsType<TomlTable>(
                            arr[1])["sub"])["value"])
                    .AsInteger());
        }

        [Fact]
        public void Nested_Array_Of_Tables_Uses_Most_Recent_Parent_Element()
        {
            var document =
                Toml.Parse(
                    "[[albums]]\n" +
                    "name = \"first\"\n" +
                    "[[albums.songs]]\n" +
                    "name = \"a\"\n" +
                    "[[albums.songs]]\n" +
                    "name = \"b\"\n" +
                    "[[albums]]\n" +
                    "name = \"second\"\n" +
                    "[[albums.songs]]\n" +
                    "name = \"c\"\n");

            var albums =
                Assert.IsType<TomlArray>(
                    document.Root["albums"]);

            var first =
                Assert.IsType<TomlTable>(
                    albums[0]);

            var second =
                Assert.IsType<TomlTable>(
                    albums[1]);

            Assert.Equal(
                2,
                Assert.IsType<TomlArray>(
                    first["songs"]).Count);

            Assert.Equal(
                1,
                Assert.IsType<TomlArray>(
                    second["songs"]).Count);
        }

        [Fact]
        public void Missing_Parent_Is_Implicit_Table_And_Can_Be_Opened_Later()
        {
            var document =
                Toml.Parse(
                    "[[a.b]]\n" +
                    "x = 1\n" +
                    "[a]\n" +
                    "y = 2\n");

            var a =
                Assert.IsType<TomlTable>(
                    document.Root["a"]);

            Assert.Equal(
                2,
                a.Count);

            Assert.IsType<TomlArray>(
                a["b"]);

            Assert.Equal(
                2,
                Assert.IsType<TomlValue>(
                    a["y"]).AsInteger());
        }

        [Fact]
        public void Implicit_Parent_From_Nested_Aot_Cannot_Later_Become_Aot()
        {
            Assert.False(
                Toml.TryParse(
                    "[[albums.songs]]\n" +
                    "name = \"one\"\n" +
                    "[[albums]]\n" +
                    "name = \"two\"\n")
                    .IsSuccess);
        }

        [Fact]
        public void Static_Array_Cannot_Become_Array_Of_Tables()
        {
            Assert.False(
                Toml.TryParse(
                    "fruit = []\n" +
                    "[[fruit]]\n")
                    .IsSuccess);
        }

        [Fact]
        public void Array_Of_Tables_Cannot_Become_Standard_Table()
        {
            Assert.False(
                Toml.TryParse(
                    "[[fruit]]\n" +
                    "[fruit]\n")
                    .IsSuccess);
        }

        [Fact]
        public void Standard_Table_Cannot_Become_Array_Of_Tables()
        {
            Assert.False(
                Toml.TryParse(
                    "[fruit]\n" +
                    "[[fruit]]\n")
                    .IsSuccess);
        }

        [Fact]
        public void Inline_Table_Cannot_Be_Extended_By_Array_Of_Tables()
        {
            Assert.False(
                Toml.TryParse(
                    "root = { child = {} }\n" +
                    "[[root.child]]\n")
                    .IsSuccess);
        }

        [Fact]
        public void Dotted_Key_Table_Cannot_Be_Reopened_As_Array_Of_Tables()
        {
            Assert.False(
                Toml.TryParse(
                    "[fruit]\n" +
                    "apple.color = \"red\"\n" +
                    "[[fruit.apple]]\n")
                    .IsSuccess);
        }

        [Fact]
        public void Array_Of_Tables_Cannot_Be_Extended_Through_Dotted_Key()
        {
            Assert.False(
                Toml.TryParse(
                    "[[a.b]]\n" +
                    "[a]\n" +
                    "b.y = 2\n")
                    .IsSuccess);
        }

        [Theory]
        [InlineData("[[a]\n")]
        [InlineData("[[a] x\n")]
        [InlineData("[[a\n")]
        [InlineData("[[]]\n")]
        public void Malformed_Array_Table_Headers_Are_Rejected(
            string text)
        {
            Assert.False(
                Toml.TryParse(text)
                    .IsSuccess);
        }

        [Fact]
        public void Empty_Quoted_Array_Table_Name_Is_Valid()
        {
            var document =
                Toml.Parse(
                    "[['']]\n" +
                    "x = 1\n" +
                    "[['']]\n" +
                    "x = 2\n");

            var array =
                Assert.IsType<TomlArray>(
                    document.Root[""]);

            Assert.Equal(
                2,
                array.Count);
        }

        [Fact]
        public void Writer_Emits_Array_Of_Tables_Syntax()
        {
            var document =
                Toml.Parse(
                    "[[products]]\n" +
                    "name = \"Hammer\"\n" +
                    "[[products]]\n" +
                    "name = \"Nail\"\n");

            var canonical =
                Toml.Write(document);

            Assert.Equal(
                "[[products]]\n" +
                "name = \"Hammer\"\n" +
                "\n" +
                "[[products]]\n" +
                "name = \"Nail\"\n",
                canonical);

            var reparsed =
                Toml.Parse(canonical);

            Assert.Equal(
                2,
                Assert.IsType<TomlArray>(
                    reparsed.Root["products"])
                    .Count);
        }

        [Fact]
        public void Writer_Emits_Nested_Aot_And_Subtable_Paths()
        {
            var document =
                Toml.Parse(
                    "[[a]]\n" +
                    "[[a.b]]\n" +
                    "[a.b.c]\n" +
                    "d = \"first\"\n" +
                    "[[a.b]]\n" +
                    "[a.b.c]\n" +
                    "d = \"second\"\n");

            var canonical =
                Toml.Write(document);

            var reparsed =
                Toml.Parse(canonical);

            var a =
                Assert.IsType<TomlArray>(
                    reparsed.Root["a"]);

            var firstA =
                Assert.IsType<TomlTable>(
                    a[0]);

            Assert.Equal(
                2,
                Assert.IsType<TomlArray>(
                    firstA["b"]).Count);
        }

        [Fact]
        public void Programmatic_Array_Of_Tables_Remains_Static_Array_Syntax()
        {
            var document =
                new TomlDocument();

            var array =
                new TomlArray();

            var first =
                new TomlTable();

            first.Set(
                "x",
                TomlValue.FromInteger(1));

            var second =
                new TomlTable();

            second.Set(
                "x",
                TomlValue.FromInteger(2));

            array.Add(first);
            array.Add(second);

            document.Root.Set(
                "items",
                array);

            Assert.Equal(
                "items = [{x = 1}, {x = 2}]\n",
                Toml.Write(document));
        }
    }
}