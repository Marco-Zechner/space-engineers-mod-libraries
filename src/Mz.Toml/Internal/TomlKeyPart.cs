namespace Mz.Toml.Internal
{
    internal sealed class TomlKeyPart
    {
        private readonly string _value;
        private readonly int _line;
        private readonly int _column;

        public TomlKeyPart(
            string value,
            int line,
            int column)
        {
            _value = value;
            _line = line;
            _column = column;
        }

        public string Value
        {
            get { return _value; }
        }

        public int Line
        {
            get { return _line; }
        }

        public int Column
        {
            get { return _column; }
        }
    }
}