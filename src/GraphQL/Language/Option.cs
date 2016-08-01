/*
https://github.com/sprache/Sprache

The MIT License

Copyright(c) 2011 Nicholas Blumhardt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;

namespace GraphQL.Language
{
    /// <summary>
    /// Represents an optional result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    public interface IOption<out T>
    {
        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is defined.
        /// </summary>
        bool IsDefined { get; }

        /// <summary>
        /// Gets the matched result or a default value.
        /// </summary>
        /// <returns></returns>
        T GetOrDefault();

        /// <summary>
        /// Gets the matched result.
        /// </summary>
        T Get();
    }

    /// <summary>
    /// Extensions for <see cref="IOption&lt;T&gt;"/>.
    /// </summary>
    public static class OptionExtensions
    {
        /// <summary>
        /// Gets the value or else returns a default value.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="option"></param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static T GetOrElse<T>(this IOption<T> option, T defaultValue)
        {
            if (option == null) throw new ArgumentNullException("option");
            return option.IsEmpty ? defaultValue : option.Get();
        }
    }

    internal abstract class AbstractOption<T> : IOption<T>
    {
        public abstract bool IsEmpty { get; }

        public bool IsDefined
        {
            get { return !IsEmpty; }
        }

        public T GetOrDefault()
        {
            return IsEmpty ? default(T) : Get();
        }

        public abstract T Get();
    }

    internal sealed class Some<T> : AbstractOption<T>
    {
        private readonly T _value;

        public Some(T value)
        {
            _value = value;
        }

        public override bool IsEmpty
        {
            get { return false; }
        }

        public override T Get()
        {
            return _value;
        }
    }

    internal sealed class None<T> : AbstractOption<T>
    {
        public override bool IsEmpty
        {
            get { return true; }
        }

        public override T Get()
        {
            throw new InvalidOperationException("Cannot get value from None.");
        }
    }
}
