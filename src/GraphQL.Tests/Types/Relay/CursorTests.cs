using System;
using System.Collections.Generic;
using GraphQL.Types.Relay;
using Shouldly;
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

            firstCursor.ShouldBeNull();
            lastCursor.ShouldBeNull();
        }

        [Fact]
        public void GetFirstAndLastCursor_EmptyCollection_ReturnsNull()
        {
            List<Entity> items = new List<Entity>();

            var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor(items, x => x.Property);

            firstCursor.ShouldBeNull();
            lastCursor.ShouldBeNull();
        }

        [Fact]
        public void GetFirstAndLastCursor_NullGetpropertyFunc_ThrowsArgumentNullException()
        {
            List<Entity> items = new List<Entity>();

            Should.Throw<ArgumentNullException>(() => Cursor.GetFirstAndLastCursor<Entity, int>(items, null));
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

            firstCursor.ShouldBe("YXJyYXljb25uZWN0aW9uOjE=");
            lastCursor.ShouldBe("YXJyYXljb25uZWN0aW9uOjM=");
        }

        [Fact]
        public void FromNullableCursor_NullValue_ReturnsNull()
        {
            var cursor = (string)null;

            var value = Cursor.FromNullableCursor<int>(cursor);

            value.ShouldBeNull();
        }

        [Fact]
        public void FromNullableCursor_IntValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOjU=";

            var value = Cursor.FromNullableCursor<int>(cursor);

            value.ShouldBe(5);
        }

        [Fact]
        public void FromNullableCursor_DateTimeValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOjAxLzAxLzIwMDAgMDA6MDA6MDA=";

            var value = Cursor.FromNullableCursor<DateTime>(cursor);

            value.ShouldBe(new DateTime(2000, 1, 1));
        }

        [Fact]
        public void FromCursor_NullValue_ReturnsNull()
        {
            var cursor = (string)null;

            var value = Cursor.FromCursor<string>(cursor);

            value.ShouldBe(null);
        }

        [Fact]
        public void FromCursor_IntValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOjU=";

            var value = Cursor.FromCursor<int>(cursor);

            value.ShouldBe(5);
        }

        [Fact]
        public void FromCursor_StringValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOmZvbw==";

            var value = Cursor.FromCursor<string>(cursor);

            value.ShouldBe("foo");
        }

        [Fact]
        public void FromCursor_DateTimeValue_ReturnsValueFromCursor()
        {
            var cursor = "YXJyYXljb25uZWN0aW9uOjAxLzAxLzIwMDAgMDA6MDA6MDA=";

            var value = Cursor.FromCursor<DateTime>(cursor);

            value.ShouldBe(new DateTime(2000, 1, 1));
        }

        [Fact]
        public void ToCursor_NullValue_ThrowsArgumentNullException()
        {
            var value = (string)null;

            Should.Throw<ArgumentNullException>(() => Cursor.ToCursor(value));
        }

        [Fact]
        public void ToCursor_IntValue_ReturnsBase64Cursor()
        {
            var value = 5;

            var cursor = Cursor.ToCursor(value);

            cursor.ShouldBe("YXJyYXljb25uZWN0aW9uOjU=");
        }

        [Fact]
        public void ToCursor_StringValue_ReturnsBase64Cursor()
        {
            var value = "foo";

            var cursor = Cursor.ToCursor(value);

            cursor.ShouldBe("YXJyYXljb25uZWN0aW9uOmZvbw==");
        }

        [Fact]
        public void ToCursor_DateTimeValue_ReturnsBase64Cursor()
        {
            var value = new DateTime(2000, 1, 1);

            var cursor = Cursor.ToCursor(value);

            cursor.ShouldBe("YXJyYXljb25uZWN0aW9uOjAxLzAxLzIwMDAgMDA6MDA6MDA=");
        }

        private class Entity
        {
            public int Property { get; set; }
        }
    }
}
