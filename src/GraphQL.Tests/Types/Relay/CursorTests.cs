using System;
using System.Collections.Generic;
using GraphQL.Types.Relay;
using Xunit;

namespace GraphQL.Tests.Types.Relay
{
    public class CursorTests
    {
        [Fact]
        public void GetFirstAndLastCursor_NullCollection_ReturnsNull()
        {
            List<Entity> items = null;

            var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor(items, x => x.Property);

            Assert.Null(firstCursor);
            Assert.Null(lastCursor);
        }

        [Fact]
        public void GetFirstAndLastCursor_EmptyCollection_ReturnsNull()
        {
            List<Entity> items = new List<Entity>();

            var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor(items, x => x.Property);

            Assert.Null(firstCursor);
            Assert.Null(lastCursor);
        }

        [Fact]
        public void GetFirstAndLastCursor_NullGetpropertyFunc_ThrowsArgumentNullException()
        {
            List<Entity> items = new List<Entity>();

            Assert.Throws<ArgumentNullException>(() => Cursor.GetFirstAndLastCursor<Entity, int>(items, null));
        }

        [Fact]
        public void GetFirstAndLastCursor_ThreeItems_ReturnsTheCursorForTheFirstAndLastItems()
        {
            List<Entity> items = new List<Entity>()
            {
                new Entity() { Property = 1 },
                new Entity() { Property = 2 },
                new Entity() { Property = 3 },
            };

            var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor(items, x => x.Property);

            Assert.Equal("YXJyYXljb25uZWN0aW9uOjE=", firstCursor);
            Assert.Equal("YXJyYXljb25uZWN0aW9uOjM=", lastCursor);
        }

        [Fact]
        public void FromNullableCursor_NullValue_ReturnsNull()
        {
            var cursor = (string)null;

            var value = Cursor.FromNullableCursor<int>(cursor);

            Assert.Null(value);
        }

        [Fact]
        public void FromNullableCursor_IntValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOjU=";

            var value = Cursor.FromNullableCursor<int>(cursor);

            Assert.Equal(5, value);
        }

        [Fact]
        public void FromNullableCursor_DateTimeValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOjAxLzAxLzIwMDAgMDA6MDA6MDA=";

            var value = Cursor.FromNullableCursor<DateTime>(cursor);

            Assert.Equal(new DateTime(2000, 1, 1), value);
        }

        [Fact]
        public void FromCursor_NullValue_ReturnsNull()
        {
            var cursor = (string)null;

            var value = Cursor.FromCursor<string>(cursor);

            Assert.Null(value);
        }

        [Fact]
        public void FromCursor_IntValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOjU=";

            var value = Cursor.FromCursor<int>(cursor);

            Assert.Equal(5, value);
        }

        [Fact]
        public void FromCursor_StringValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOmZvbw==";

            var value = Cursor.FromCursor<string>(cursor);

            Assert.Equal("foo", value);
        }

        [Fact]
        public void FromCursor_DateTimeValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOjAxLzAxLzIwMDAgMDA6MDA6MDA=";

            var value = Cursor.FromCursor<DateTime>(cursor);

            Assert.Equal(new DateTime(2000, 1, 1), value);
        }

        [Fact]
        public void ToCursor_NullValue_ThrowsArgumentNullException()
        {
            var value = (string)null;

            Assert.Throws<ArgumentNullException>(() => Cursor.ToCursor(value));
        }

        [Fact]
        public void ToCursor_IntValue_ReturnsBase64Cursor()
        {
            var value = 5;

            var cursor = Cursor.ToCursor(value);

            Assert.Equal("YXJyYXljb25uZWN0aW9uOjU=", cursor);
        }

        [Fact]
        public void ToCursor_StringValue_ReturnsBase64Cursor()
        {
            var value = "foo";

            var cursor = Cursor.ToCursor(value);

            Assert.Equal("YXJyYXljb25uZWN0aW9uOmZvbw==", cursor);
        }

        [Fact]
        public void ToCursor_DateTimeValue_ReturnsBase64Cursor()
        {
            var value = new DateTime(2000, 1, 1);

            var cursor = Cursor.ToCursor(value);

            Assert.Equal("YXJyYXljb25uZWN0aW9uOjAxLzAxLzIwMDAgMDA6MDA6MDA=", cursor);
        }

        private class Entity
        {
            public int Property { get; set; }
        }
    }
}
