using System;
using System.Text;

namespace Alder.Generators.Emitters;

internal sealed class SourceWriter
{
    private readonly StringBuilder _sb = new StringBuilder();
    private int _indent;

    public void AppendLine(string line)
    {
        WriteIndent();
        _sb.AppendLine(line);
    }

    public void AppendLine()
    {
        _sb.AppendLine();
    }

    public void Append(string text)
    {
        WriteIndent();
        _sb.Append(text);
    }

    public BlockScope Block(string header)
    {
        AppendLine(header);
        AppendLine("{");
        _indent++;
        return new BlockScope(this);
    }

    public BlockScope Block()
    {
        AppendLine("{");
        _indent++;
        return new BlockScope(this);
    }

    public void Indent() => _indent++;
    public void Outdent() => _indent = Math.Max(0, _indent - 1);

    public override string ToString() => _sb.ToString();

    private void WriteIndent()
    {
        for (var i = 0; i < _indent; i++)
            _sb.Append("    ");
    }

    internal struct BlockScope : IDisposable
    {
        private SourceWriter? _writer;

        public BlockScope(SourceWriter writer) => _writer = writer;

        public void Dispose()
        {
            if (_writer == null) return;
            _writer._indent--;
            _writer.AppendLine("}");
            _writer = null;
        }
    }
}
