using System;
using HttpSiraStatus.Util;
using Xunit;

namespace HttpSiraStatus.Tests.Util
{
    public class SimpleJSONTests
    {
        [Fact]
        public void Parse_Object_ExposesValues()
        {
            var node = JSONNode.Parse("{\"event\":\"songStart\",\"time\":1700000000000,\"ok\":true,\"none\":null}");

            Assert.True(node.IsObject);
            Assert.Equal(4, node.Count);
            Assert.Equal("songStart", node["event"].Value);
            Assert.Equal(1700000000000L, node["time"].AsLong);
            Assert.True(node["ok"].AsBool);
            Assert.True(node["none"].IsNull);
        }

        [Fact]
        public void Parse_NestedArray_ExposesItemsInOrder()
        {
            var node = JSONNode.Parse("{\"saberDir\":[1.5,-2,3]}");
            var array = node["saberDir"];

            Assert.True(array.IsArray);
            Assert.Equal(3, array.Count);
            Assert.Equal(1.5, array[0].AsDouble);
            Assert.Equal(-2, array[1].AsInt);
            Assert.Equal(3, array[2].AsInt);
        }

        [Fact]
        public void Parse_UnescapesStringValues()
        {
            var node = JSONNode.Parse("{\"text\":\"line1\\nline2\\t\\\"quoted\\\"\\\\backslash\"}");

            Assert.Equal("line1\nline2\t\"quoted\"\\backslash", node["text"].Value);
        }

        [Fact]
        public void ToString_EscapesControlAndQuoteCharacters()
        {
            var text = "quote \" backslash \\ newline \n tab \t control \u0001";
            var json = new JSONString(text).ToString();

            Assert.StartsWith("\"", json);
            Assert.EndsWith("\"", json);
            Assert.Contains("\\\"", json);
            Assert.Contains("\\\\", json);
            Assert.Contains("\\n", json);
            Assert.Contains("\\t", json);
            Assert.Contains("\\u0001", json);
            Assert.Equal(text, JSONNode.Parse(json).Value);
        }

        [Fact]
        public void Array_SerializesInInsertionOrder()
        {
            var array = new JSONArray();
            array.Add(1);
            array.Add("two");
            array.Add(true);

            Assert.Equal("[1,\"two\",true]", array.ToString());
        }

        [Fact]
        public void Object_SerializesSingleKey()
        {
            var obj = new JSONObject();
            obj["answer"] = 42;

            Assert.Equal("{\"answer\":42}", obj.ToString());
        }

        [Fact]
        public void Number_IsSerializedWithInvariantCulture()
        {
            Assert.Equal("0.5", new JSONNumber(0.5).ToString());
            Assert.Equal("123", new JSONNumber(123.0).ToString());
        }

        [Fact]
        public void Clone_IsDeepCopy()
        {
            var original = JSONNode.Parse("{\"game\":{\"scene\":\"Song\"},\"scores\":[1,2]}");
            var clone = original.Clone();

            clone["game"]["scene"] = "Menu";
            clone["scores"][0] = 9;

            Assert.Equal("Song", original["game"]["scene"].Value);
            Assert.Equal(1, original["scores"][0].AsInt);
            Assert.Equal("Menu", clone["game"]["scene"].Value);
            Assert.Equal(9, clone["scores"][0].AsInt);
        }

        [Fact]
        public void MissingKey_BehavesAsNull()
        {
            var obj = new JSONObject();

            Assert.True(obj["missing"] == null);
            Assert.False(obj.HasKey("missing"));
            Assert.Equal("", obj["missing"].Value);
        }

        [Fact]
        public void GetValueOrDefault_ReturnsValueOrFallback()
        {
            var obj = new JSONObject();
            obj["present"] = 5;
            var fallback = JSONNull.CreateOrGet();

            Assert.Equal(5, obj.GetValueOrDefault("present", fallback).AsInt);
            Assert.Same(fallback, obj.GetValueOrDefault("absent", fallback));
        }

        [Fact]
        public void Parse_LineComment_IsIgnored()
        {
            var node = JSONNode.Parse("{\"a\":1} // trailing comment");

            Assert.Equal(1, node["a"].AsInt);
        }

        [Fact]
        public void Parse_TooManyClosingBrackets_Throws()
        {
            Assert.ThrowsAny<Exception>(() => JSONNode.Parse("}"));
        }
    }
}
