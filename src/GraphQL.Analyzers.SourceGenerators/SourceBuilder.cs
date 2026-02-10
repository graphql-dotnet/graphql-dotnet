using System.Text;

namespace GraphQL.Analyzers.SourceGenerators;

/// <summary>
/// A code generation helper that builds source code with automatic indentation management.
/// Uses an underlying StringBuilder to minimize allocations.
/// </summary>
internal sealed class SourceBuilder
{
    private readonly StringBuilder _sb;
    private bool _indentPending;

    /// <summary>
    /// Gets or sets the current indentation level.
    /// </summary>
    public int Indentation { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceBuilder"/> class.
    /// </summary>
    public SourceBuilder()
    {
        _sb = new StringBuilder();
        Indentation = 0;
        _indentPending = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceBuilder"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the underlying StringBuilder.</param>
    public SourceBuilder(int capacity)
    {
        _sb = new StringBuilder(capacity);
        Indentation = 0;
        _indentPending = false;
    }

    /// <summary>
    /// Increases the indentation level and returns a disposable that will decrease it when disposed.
    /// </summary>
    /// <returns>A disposable that will decrease the indentation level when disposed.</returns>
    public IndentScope Indent()
    {
        Indentation++;
        return new IndentScope(this);
    }

    /// <summary>
    /// Appends an empty line to the source code.
    /// </summary>
    public SourceBuilder AppendLine()
    {
        _sb.AppendLine();
        _indentPending = true;
        return this;
    }

    /// <summary>
    /// Appends a line with the specified text to the source code, with automatic indentation.
    /// </summary>
    /// <param name="value">The text to append.</param>
    public SourceBuilder AppendLine(string value)
    {
        WriteIndentIfPending();
        _sb.AppendLine(value);
        _indentPending = true;
        return this;
    }

    /// <summary>
    /// Appends a line with formatted text (interpolated string) to the source code, with automatic indentation.
    /// </summary>
    /// <param name="handler">The interpolated string handler that writes content directly to the StringBuilder.</param>
    public SourceBuilder AppendLine([System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] AppendLineInterpolatedStringHandler handler)
    {
        // Handler already wrote the indented content to StringBuilder
        _ = handler;
        _sb.AppendLine();
        _indentPending = true;
        return this;
    }

    /// <summary>
    /// Appends text to the source code without a line break, with automatic indentation.
    /// </summary>
    /// <param name="value">The text to append.</param>
    public SourceBuilder Append(string value)
    {
        WriteIndentIfPending();
        _sb.Append(value);
        return this;
    }

    /// <summary>
    /// Appends formatted text (interpolated string) to the source code without a line break, with automatic indentation.
    /// </summary>
    /// <param name="handler">The interpolated string handler that writes content directly to the StringBuilder.</param>
    public SourceBuilder Append([System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] AppendInterpolatedStringHandler handler)
    {
        // Handler already wrote the indented content to StringBuilder
        _ = handler;
        return this;
    }

    /// <summary>
    /// Returns the generated source code as a string.
    /// </summary>
    public override string ToString() => _sb.ToString();

    private void WriteIndentIfPending()
    {
        if (_indentPending && Indentation > 0)
        {
            _sb.Append(' ', Indentation * 4);
        }
        _indentPending = false;
    }

    /// <summary>
    /// A disposable struct that manages indentation scope.
    /// </summary>
    public struct IndentScope : IDisposable
    {
        private readonly SourceBuilder _builder;

        internal IndentScope(SourceBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// Decreases the indentation level.
        /// </summary>
        public void Dispose()
        {
            _builder.Indentation--;
        }
    }

    /// <summary>
    /// Interpolated string handler for AppendLine that writes directly to the StringBuilder.
    /// </summary>
    [System.Runtime.CompilerServices.InterpolatedStringHandler]
    public struct AppendLineInterpolatedStringHandler
    {
        private readonly StringBuilder _sb;

        public AppendLineInterpolatedStringHandler(int literalLength, int formattedCount, SourceBuilder builder)
        {
            builder.WriteIndentIfPending();
            _sb = builder._sb;
        }

        public void AppendLiteral(string value) => _sb.Append(value);
        public void AppendFormatted<T>(T value) => throw new NotImplementedException();
        public void AppendFormatted(int value) => _sb.Append(value);
        public void AppendFormatted(string? value) => _sb.Append(value);
    }

    /// <summary>
    /// Interpolated string handler for Append that writes directly to the StringBuilder.
    /// </summary>
    [System.Runtime.CompilerServices.InterpolatedStringHandler]
    public struct AppendInterpolatedStringHandler
    {
        private readonly StringBuilder _sb;

        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, SourceBuilder builder)
        {
            builder.WriteIndentIfPending();
            _sb = builder._sb;
        }

        public void AppendLiteral(string value) => _sb.Append(value);
        public void AppendFormatted<T>(T value) => throw new NotImplementedException();
        public void AppendFormatted(int value) => _sb.Append(value);
        public void AppendFormatted(string? value) => _sb.Append(value);
    }
}
