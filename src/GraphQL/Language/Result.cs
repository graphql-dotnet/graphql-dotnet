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
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language
{
    /// <summary>
    /// Represents a parsing result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    public interface IResult<out T>
    {
        /// <summary>
        /// Gets the resulting value.
        /// </summary>
        T Value { get; }

        Position Position { get; }

        /// <summary>
        /// Gets a value indicating whether wether parsing was successful.
        /// </summary>
        bool WasSuccessful { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the parser expectations in case of error.
        /// </summary>
        IEnumerable<string> Expectations { get; }

        /// <summary>
        /// Gets the remainder of the input.
        /// </summary>
        IInput Remainder { get; }
    }

    /// <summary>
    /// Contains helper functions to create <see cref="IResult&lt;T&gt;"/> instances.
    /// </summary>
    public static class Result
    {
        public static IResult<T> Success<T>(T value, Position position, IInput remainder)
        {
            return new Result<T>(value, position, remainder);
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="remainder">The remainder of the input.</param>
        /// <param name="message">The error message.</param>
        /// <param name="expectations">The parser expectations.</param>
        /// <returns>The new <see cref="IResult&lt;T&gt;"/>.</returns>
        public static IResult<T> Failure<T>(IInput remainder, string message, IEnumerable<string> expectations)
        {
            return new Result<T>(remainder, message, expectations);
        }
    }

    internal class Result<T> : IResult<T>
    {
        private readonly T _value;
        private readonly IInput _remainder;
        private readonly bool _wasSuccessful;
        private readonly string _message;
        private readonly IEnumerable<string> _expectations;

        public Result(T value, Position position, IInput remainder)
        {
            _value = value;
            Position = position;
            _remainder = remainder;
            _wasSuccessful = true;
            _message = null;
            _expectations = Enumerable.Empty<string>();
        }

        public Result(IInput remainder, string message, IEnumerable<string> expectations)
        {
            _value = default(T);
            _remainder = remainder;
            _wasSuccessful = false;
            _message = message;
            _expectations = expectations;
        }

        public T Value
        {
            get
            {
                if (!WasSuccessful)
                    throw new InvalidOperationException("No value can be computed.");

                return _value;
            }
        }

        public Position Position { get; }

        public bool WasSuccessful { get { return _wasSuccessful; } }

        public string Message { get { return _message; } }

        public IEnumerable<string> Expectations { get { return _expectations; } }

        public IInput Remainder { get { return _remainder; } }

        public override string ToString()
        {
            if (WasSuccessful)
                return string.Format("Successful parsing of {0}.", Value);

            var expMsg = "";

            if (Expectations.Any())
                expMsg = " expected " + Expectations.Aggregate((e1, e2) => e1 + " or " + e2);

            var recentlyConsumed = CalculateRecentlyConsumed();

            return string.Format("Parsing failure: {0};{1} ({2}); recently consumed: {3}", Message, expMsg, Remainder, recentlyConsumed);
        }

        private string CalculateRecentlyConsumed()
        {
            const int windowSize = 10;

            var totalConsumedChars = Remainder.Position;
            var windowStart = totalConsumedChars - windowSize;
            windowStart = windowStart < 0 ? 0 : windowStart;

            var numberOfRecentlyConsumedChars = totalConsumedChars - windowStart;

            return Remainder.Source.Substring(windowStart, numberOfRecentlyConsumedChars);
        }
    }
}
