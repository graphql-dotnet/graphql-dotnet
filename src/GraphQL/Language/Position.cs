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
    /// Represents a position in the input.
    /// </summary>
    public class Position : IEquatable<Position>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Position" /> class.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="line">The line number.</param>
        /// <param name="column">The column.</param>
        public Position(int pos, int line, int column)
        {
            Pos = pos;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Creates an new <see cref="Position"/> instance from a given <see cref="IInput"/> object.
        /// </summary>
        /// <param name="input">The current input.</param>
        /// <returns>A new <see cref="Position"/> instance.</returns>
        public static Position FromInput(IInput input)
        {
            return new Position(input.Position, input.Line, input.Column);
        }

        /// <summary>
        /// Gets the current positon.
        /// </summary>
        public int Pos
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current line number.
        /// </summary>
        public int Line
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current column.
        /// </summary>
        public int Column
        {
            get;
            private set;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="Position" />.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="Position" />; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return Equals(obj as Position);
        }

        /// <summary>
        /// Indicates whether the current <see cref="Position" /> is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Position other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Pos == other.Pos
                && Line == other.Line
                && Column == other.Column;
        }

        /// <summary>
        /// Indicates whether the left <see cref="Position" /> is equal to the right <see cref="Position" />.
        /// </summary>
        /// <param name="left">The left <see cref="Position" />.</param>
        /// <param name="right">The right <see cref="Position" />.</param>
        /// <returns>true if both objects are equal.</returns>
        public static bool operator ==(Position left, Position right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Indicates whether the left <see cref="Position" /> is not equal to the right <see cref="Position" />.
        /// </summary>
        /// <param name="left">The left <see cref="Position" />.</param>
        /// <param name="right">The right <see cref="Position" />.</param>
        /// <returns>true if the objects are not equal.</returns>
        public static bool operator !=(Position left, Position right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="Position" />.
        /// </returns>
        public override int GetHashCode()
        {
            var h = 31;
            h = h * 13 + Pos;
            h = h * 13 + Line;
            h = h * 13 + Column;
            return h;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Line {0}, Column {1}", Line, Column);
        }
    }
}
