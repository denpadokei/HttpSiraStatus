/* * * * *
 * A simple JSON Parser / builder
 * ------------------------------
 * 
 * It mainly has been written as a simple JSON parser. It can build a JSON string
 * from the node-tree, or generate a node tree from any valid JSON string.
 * 
 * Written by Bunny83 
 * 2012-06-09
 * 
 * Changelog now external. See Changelog.txt
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2012-2019 Markus Göbel (Bunny83)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * * * * */
#pragma warning disable IDE0044,IDE0034,IDE0059,IDE0018,IDE0031,IDE0075,IDE0041,IDE0019,IDE0038,IDE1006
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HttpSiraStatus.Util
{
    public enum JSONNodeType
    {
        Array = 1,
        Object = 2,
        String = 3,
        Number = 4,
        NullValue = 5,
        Boolean = 6,
        None = 7,
        Custom = 0xFF,
    }
    public enum JSONTextMode
    {
        Compact,
        Indent
    }

    public abstract partial class JSONNode : IDisposable
    {
        #region Enumerators
        public struct Enumerator : IDisposable
        {
            private enum Type { None, Array, Object }

            private Type type;
            private IEnumerator<KeyValuePair<string, JSONNode>> m_Object;
            private List<JSONNode>.Enumerator m_Array;
            private bool disposeValue;
            public bool IsValid => this.type != Type.None;
            public Enumerator(List<JSONNode>.Enumerator aArrayEnum)
            {
                this.disposeValue = false;
                this.type = Type.Array;
                this.m_Object = default(Dictionary<string, JSONNode>.Enumerator);
                this.m_Array = aArrayEnum;
            }
            public Enumerator(IEnumerator<KeyValuePair<string, JSONNode>> aDictEnum)
            {
                this.disposeValue = false;
                this.type = Type.Object;
                this.m_Object = aDictEnum;
                this.m_Array = default(List<JSONNode>.Enumerator);
            }
            public KeyValuePair<string, JSONNode> Current
            {
                get
                {
                    if (this.type == Type.Array) {
                        return new KeyValuePair<string, JSONNode>(string.Empty, this.m_Array.Current);
                    }
                    else if (this.type == Type.Object) {
                        return this.m_Object.Current;
                    }

                    return new KeyValuePair<string, JSONNode>(string.Empty, null);
                }
            }
            public bool MoveNext()
            {
                if (this.type == Type.Array) {
                    return this.m_Array.MoveNext();
                }
                else if (this.type == Type.Object) {
                    return this.m_Object.MoveNext();
                }

                return false;
            }

            private void Dispose(bool disposing)
            {
                if (this.disposeValue) {
                    return;
                }
                if (disposing) {
                    this.m_Object?.Dispose();
                    this.m_Object = null;
                    this.m_Array.Dispose();
                }
                this.disposeValue = true;
            }

            public void Dispose()
            {
                this.Dispose(true);
            }
        }
        public struct ValueEnumerator : IDisposable
        {
            private Enumerator m_Enumerator;
            public ValueEnumerator(List<JSONNode>.Enumerator aArrayEnum) : this(new Enumerator(aArrayEnum)) { }
            public ValueEnumerator(Dictionary<string, JSONNode>.Enumerator aDictEnum) : this(new Enumerator(aDictEnum)) { }
            public ValueEnumerator(Enumerator aEnumerator) { this.m_Enumerator = aEnumerator; }
            public JSONNode Current => this.m_Enumerator.Current.Value;
            public bool MoveNext()
            {
                return this.m_Enumerator.MoveNext();
            }

            public ValueEnumerator GetEnumerator()
            {
                return this;
            }

            public void Dispose()
            {
                ((IDisposable)this.m_Enumerator).Dispose();
            }
        }
        public struct KeyEnumerator : IDisposable
        {
            private Enumerator m_Enumerator;
            public KeyEnumerator(List<JSONNode>.Enumerator aArrayEnum) : this(new Enumerator(aArrayEnum)) { }
            public KeyEnumerator(Dictionary<string, JSONNode>.Enumerator aDictEnum) : this(new Enumerator(aDictEnum)) { }
            public KeyEnumerator(Enumerator aEnumerator) { this.m_Enumerator = aEnumerator; }
            public string Current => this.m_Enumerator.Current.Key;
            public bool MoveNext()
            {
                return this.m_Enumerator.MoveNext();
            }

            public KeyEnumerator GetEnumerator()
            {
                return this;
            }

            public void Dispose()
            {
                ((IDisposable)this.m_Enumerator).Dispose();
            }
        }

        public class LinqEnumerator : IEnumerator<KeyValuePair<string, JSONNode>>, IEnumerable<KeyValuePair<string, JSONNode>>
        {
            private JSONNode m_Node;
            private Enumerator m_Enumerator;
            private bool disposedValue;

            internal LinqEnumerator(JSONNode aNode)
            {
                this.m_Node = aNode;
                if (this.m_Node != null) {
                    this.m_Enumerator = this.m_Node.GetEnumerator();
                }
            }
            public KeyValuePair<string, JSONNode> Current => this.m_Enumerator.Current;
            object IEnumerator.Current => this.m_Enumerator.Current;
            public bool MoveNext()
            {
                return this.m_Enumerator.MoveNext();
            }

            public IEnumerator<KeyValuePair<string, JSONNode>> GetEnumerator()
            {
                return new LinqEnumerator(this.m_Node);
            }

            public void Reset()
            {
                if (this.m_Node != null) {
                    this.m_Enumerator = this.m_Node.GetEnumerator();
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new LinqEnumerator(this.m_Node);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposedValue) {
                    if (disposing) {
                        this.m_Enumerator.Dispose();
                    }
                    this.m_Node = null;
                    this.disposedValue = true;
                }
            }
            public void Dispose()
            {
                this.Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        #endregion Enumerators

        #region common interface

        public static bool forceASCII = false; // Use Unicode by default
        public static bool longAsString = false; // lazy creator creates a JSONString instead of JSONNumber
        public static bool allowLineComments = true; // allow "//"-style comments at the end of a line

        public abstract JSONNodeType Tag { get; }

        public virtual JSONNode this[int aIndex] { get => null; set { } }

        public virtual JSONNode this[string aKey] { get => null; set { } }

        public virtual string Value { get => ""; set { } }

        public virtual int Count => 0;

        public virtual bool IsNumber => false;
        public virtual bool IsString => false;
        public virtual bool IsBoolean => false;
        public virtual bool IsNull => false;
        public virtual bool IsArray => false;
        public virtual bool IsObject => false;

        public virtual bool Inline { get => false; set { } }

        public virtual void Add(string aKey, JSONNode aItem)
        {
        }
        public virtual void Add(JSONNode aItem)
        {
            this.Add("", aItem);
        }

        public virtual JSONNode Remove(string aKey)
        {
            return null;
        }

        public virtual JSONNode Remove(int aIndex)
        {
            return null;
        }

        public virtual JSONNode Remove(JSONNode aNode)
        {
            return aNode;
        }

        public virtual void Clear() { }

        public virtual JSONNode Clone()
        {
            return null;
        }

        public virtual IEnumerable<JSONNode> Children
        {
            get
            {
                yield break;
            }
        }

        public IEnumerable<JSONNode> DeepChildren
        {
            get
            {
                foreach (var C in this.Children) {
                    foreach (var D in C.DeepChildren) {
                        yield return D;
                    }
                }
            }
        }

        public virtual bool HasKey(string aKey)
        {
            return false;
        }

        public virtual JSONNode GetValueOrDefault(string aKey, JSONNode aDefault)
        {
            return aDefault;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            this.WriteToStringBuilder(sb, 0, 0, JSONTextMode.Compact);
            return sb.ToString();
        }

        public virtual string ToString(int aIndent)
        {
            var sb = new StringBuilder();
            this.WriteToStringBuilder(sb, 0, aIndent, JSONTextMode.Indent);
            return sb.ToString();
        }
        internal abstract void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode);

        public abstract Enumerator GetEnumerator();
        public IEnumerable<KeyValuePair<string, JSONNode>> Linq => new LinqEnumerator(this);
        public KeyEnumerator Keys => new KeyEnumerator(this.GetEnumerator());
        public ValueEnumerator Values => new ValueEnumerator(this.GetEnumerator());

        #endregion common interface

        #region typecasting properties

        public virtual double AsDouble
        {
            get
            {
                var v = 0.0;
                return double.TryParse(this.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0.0;
            }
            set => this.Value = value.ToString(CultureInfo.InvariantCulture);
        }

        public virtual int AsInt
        {
            get => (int)this.AsDouble;
            set => this.AsDouble = value;
        }

        public virtual float AsFloat
        {
            get => (float)this.AsDouble;
            set => this.AsDouble = value;
        }

        public virtual bool AsBool
        {
            get
            {
                var v = false;
                return bool.TryParse(this.Value, out v) ? v : !string.IsNullOrEmpty(this.Value);
            }
            set => this.Value = value ? "true" : "false";
        }

        public virtual long AsLong
        {
            get
            {
                long val = 0;
                return long.TryParse(this.Value, out val) ? val : 0L;
            }
            set => this.Value = value.ToString();
        }

        public virtual ulong AsULong
        {
            get
            {
                ulong val = 0;
                return ulong.TryParse(this.Value, out val) ? val : 0;
            }
            set => this.Value = value.ToString();
        }

        public virtual JSONArray AsArray => this as JSONArray;

        public virtual JSONObject AsObject => this as JSONObject;

        #endregion typecasting properties

        #region operators

        public static implicit operator JSONNode(string s)
        {
            return (s is null) ? JSONNull.CreateOrGet() : new JSONString(s);
        }
        public static implicit operator string(JSONNode d)
        {
            return (d == null) ? null : d.Value;
        }

        public static implicit operator JSONNode(double n)
        {
            return new JSONNumber(n);
        }
        public static implicit operator double(JSONNode d)
        {
            return (d == null) ? 0 : d.AsDouble;
        }

        public static implicit operator JSONNode(float n)
        {
            return new JSONNumber(n);
        }
        public static implicit operator float(JSONNode d)
        {
            return (d == null) ? 0 : d.AsFloat;
        }

        public static implicit operator JSONNode(int n)
        {
            return new JSONNumber(n);
        }
        public static implicit operator int(JSONNode d)
        {
            return (d == null) ? 0 : d.AsInt;
        }

        public static implicit operator JSONNode(long n)
        {
            return longAsString ? new JSONString(n.ToString()) : new JSONNumber(n);
        }
        public static implicit operator long(JSONNode d)
        {
            return (d == null) ? 0L : d.AsLong;
        }

        public static implicit operator JSONNode(ulong n)
        {
            return longAsString ? new JSONString(n.ToString()) : new JSONNumber(n);
        }
        public static implicit operator ulong(JSONNode d)
        {
            return (d == null) ? 0 : d.AsULong;
        }

        public static implicit operator JSONNode(bool b)
        {
            return new JSONBool(b);
        }
        public static implicit operator bool(JSONNode d)
        {
            return (d == null) ? false : d.AsBool;
        }

        public static implicit operator JSONNode(KeyValuePair<string, JSONNode> aKeyValue)
        {
            return aKeyValue.Value;
        }

        public static bool operator ==(JSONNode a, object b)
        {
            if (ReferenceEquals(a, b)) {
                return true;
            }

            var aIsNull = a is JSONNull || ReferenceEquals(a, null) || a is JSONLazyCreator;
            var bIsNull = b is JSONNull || ReferenceEquals(b, null) || b is JSONLazyCreator;
            return aIsNull && bIsNull ? true : !aIsNull && a.Equals(b);
        }

        public static bool operator !=(JSONNode a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion operators

        [ThreadStatic]
        private static StringBuilder m_EscapeBuilder;

        internal static StringBuilder EscapeBuilder
        {
            get
            {
                m_EscapeBuilder ??= new StringBuilder();

                return m_EscapeBuilder;
            }
        }
        internal static string Escape(string aText)
        {
            var sb = EscapeBuilder;
            sb.Length = 0;
            if (sb.Capacity < aText.Length + (aText.Length / 10)) {
                sb.Capacity = aText.Length + (aText.Length / 10);
            }

            foreach (var c in aText) {
                switch (c) {
                    case '\\':
                        _ = sb.Append("\\\\");
                        break;
                    case '\"':
                        _ = sb.Append("\\\"");
                        break;
                    case '\n':
                        _ = sb.Append("\\n");
                        break;
                    case '\r':
                        _ = sb.Append("\\r");
                        break;
                    case '\t':
                        _ = sb.Append("\\t");
                        break;
                    case '\b':
                        _ = sb.Append("\\b");
                        break;
                    case '\f':
                        _ = sb.Append("\\f");
                        break;
                    default:
                        if (c < ' ' || (forceASCII && c > 127)) {
                            ushort val = c;
                            _ = sb.Append("\\u").Append(val.ToString("X4"));
                        }
                        else {
                            _ = sb.Append(c);
                        }

                        break;
                }
            }
            var result = sb.ToString();
            sb.Length = 0;
            return result;
        }

        private static JSONNode ParseElement(string token, bool quoted)
        {
            if (quoted) {
                return token;
            }

            if (token.Length <= 5) {
                var tmp = token.ToLower();
                if (tmp == "false" || tmp == "true") {
                    return tmp == "true";
                }

                if (tmp == "null") {
                    return JSONNull.CreateOrGet();
                }
            }
            double val;
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out val) ? (JSONNode)val : (JSONNode)token;
        }

        public static JSONNode Parse(string aJSON)
        {
            var stack = new Stack<JSONNode>();
            JSONNode ctx = null;
            var i = 0;
            var Token = new StringBuilder();
            var TokenName = "";
            var QuoteMode = false;
            var TokenIsQuoted = false;
            var HasNewlineChar = false;
            while (i < aJSON.Length) {
                switch (aJSON[i]) {
                    case '{':
                        if (QuoteMode) {
                            _ = Token.Append(aJSON[i]);
                            break;
                        }
                        stack.Push(new JSONObject());
                        if (ctx != null) {
                            ctx.Add(TokenName, stack.Peek());
                        }
                        TokenName = "";
                        Token.Length = 0;
                        ctx = stack.Peek();
                        HasNewlineChar = false;
                        break;

                    case '[':
                        if (QuoteMode) {
                            _ = Token.Append(aJSON[i]);
                            break;
                        }

                        stack.Push(new JSONArray());
                        if (ctx != null) {
                            ctx.Add(TokenName, stack.Peek());
                        }
                        TokenName = "";
                        Token.Length = 0;
                        ctx = stack.Peek();
                        HasNewlineChar = false;
                        break;

                    case '}':
                    case ']':
                        if (QuoteMode) {

                            _ = Token.Append(aJSON[i]);
                            break;
                        }
                        if (stack.Count == 0) {
                            throw new Exception("JSON Parse: Too many closing brackets");
                        }

                        _ = stack.Pop();
                        if (Token.Length > 0 || TokenIsQuoted) {
                            ctx.Add(TokenName, ParseElement(Token.ToString(), TokenIsQuoted));
                        }

                        if (ctx != null) {
                            ctx.Inline = !HasNewlineChar;
                        }

                        TokenIsQuoted = false;
                        TokenName = "";
                        Token.Length = 0;
                        if (stack.Count > 0) {
                            ctx = stack.Peek();
                        }

                        break;

                    case ':':
                        if (QuoteMode) {
                            _ = Token.Append(aJSON[i]);
                            break;
                        }
                        TokenName = Token.ToString();
                        Token.Length = 0;
                        TokenIsQuoted = false;
                        break;

                    case '"':
                        QuoteMode ^= true;
                        TokenIsQuoted |= QuoteMode;
                        break;

                    case ',':
                        if (QuoteMode) {
                            _ = Token.Append(aJSON[i]);
                            break;
                        }
                        if (Token.Length > 0 || TokenIsQuoted) {
                            ctx.Add(TokenName, ParseElement(Token.ToString(), TokenIsQuoted));
                        }

                        TokenIsQuoted = false;
                        TokenName = "";
                        Token.Length = 0;
                        TokenIsQuoted = false;
                        break;

                    case '\r':
                    case '\n':
                        HasNewlineChar = true;
                        break;

                    case ' ':
                    case '\t':
                        if (QuoteMode) {
                            _ = Token.Append(aJSON[i]);
                        }

                        break;

                    case '\\':
                        ++i;
                        if (QuoteMode) {
                            var C = aJSON[i];
                            switch (C) {
                                case 't':
                                    _ = Token.Append('\t');
                                    break;
                                case 'r':
                                    _ = Token.Append('\r');
                                    break;
                                case 'n':
                                    _ = Token.Append('\n');
                                    break;
                                case 'b':
                                    _ = Token.Append('\b');
                                    break;
                                case 'f':
                                    _ = Token.Append('\f');
                                    break;
                                case 'u': {
                                        var s = aJSON.Substring(i + 1, 4);
                                        _ = Token.Append((char)int.Parse(
                                            s,
                                            System.Globalization.NumberStyles.AllowHexSpecifier));
                                        i += 4;
                                        break;
                                    }
                                default:
                                    _ = Token.Append(C);
                                    break;
                            }
                        }
                        break;
                    case '/':
                        if (allowLineComments && !QuoteMode && i + 1 < aJSON.Length && aJSON[i + 1] == '/') {
                            while (++i < aJSON.Length && aJSON[i] != '\n' && aJSON[i] != '\r') {
                                ;
                            }

                            break;
                        }
                        _ = Token.Append(aJSON[i]);
                        break;
                    case '\uFEFF': // remove / ignore BOM (Byte Order Mark)
                        break;

                    default:
                        _ = Token.Append(aJSON[i]);
                        break;
                }
                ++i;
            }
            return QuoteMode
                ? throw new Exception("JSON Parse: Quotation marks seems to be messed up.")
                : ctx ?? ParseElement(Token.ToString(), TokenIsQuoted);
        }
        public virtual void Dispose()
        {
            this.Keys.Dispose();
            this.Values.Dispose();
        }
    }
    // End of JSONNode

    public partial class JSONArray : JSONNode
    {
        private List<JSONNode> m_List = new List<JSONNode>();
        private bool inline = false;
        public override bool Inline
        {
            get => this.inline;
            set => this.inline = value;
        }

        public override JSONNodeType Tag => JSONNodeType.Array;
        public override bool IsArray => true;
        public override Enumerator GetEnumerator()
        {
            return new Enumerator(this.m_List.GetEnumerator());
        }

        public override JSONNode this[int aIndex]
        {
            get => aIndex < 0 || aIndex >= this.m_List.Count ? new JSONLazyCreator(this) : this.m_List[aIndex];
            set
            {
                if (value == null) {
                    value = JSONNull.CreateOrGet();
                }

                if (aIndex < 0 || aIndex >= this.m_List.Count) {
                    this.m_List.Add(value);
                }
                else {
                    this.m_List[aIndex] = value;
                }
            }
        }

        public override JSONNode this[string aKey]
        {
            get => new JSONLazyCreator(this);
            set
            {
                if (value == null) {
                    value = JSONNull.CreateOrGet();
                }

                this.m_List.Add(value);
            }
        }

        public override int Count => this.m_List.Count;

        public override void Add(string aKey, JSONNode aItem)
        {
            if (aItem == null) {
                aItem = JSONNull.CreateOrGet();
            }

            this.m_List.Add(aItem);
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= this.m_List.Count) {
                return null;
            }

            var tmp = this.m_List[aIndex];
            this.m_List.RemoveAt(aIndex);
            return tmp;
        }

        public override JSONNode Remove(JSONNode aNode)
        {
            _ = this.m_List.Remove(aNode);
            return aNode;
        }

        public override void Clear()
        {
            this.m_List.Clear();
        }

        public override JSONNode Clone()
        {
            var node = new JSONArray();
            node.m_List.Capacity = this.m_List.Capacity;
            foreach (var n in this.m_List.ToArray()) {
                if (n != null) {
                    node.Add(n.Clone());
                }
                else {
                    node.Add(null);
                }
            }
            return node;
        }

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (var N in this.m_List.ToArray()) {
                    yield return N;
                }
            }
        }

        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            _ = aSB.Append('[');
            var count = this.m_List.Count;
            if (this.inline) {
                aMode = JSONTextMode.Compact;
            }

            for (var i = 0; i < count; i++) {
                if (i > 0) {
                    _ = aSB.Append(',');
                }

                if (aMode == JSONTextMode.Indent) {
                    _ = aSB.AppendLine();
                }

                if (aMode == JSONTextMode.Indent) {
                    _ = aSB.Append(' ', aIndent + aIndentInc);
                }

                this.m_List[i].WriteToStringBuilder(aSB, aIndent + aIndentInc, aIndentInc, aMode);
            }
            if (aMode == JSONTextMode.Indent) {
                _ = aSB.AppendLine().Append(' ', aIndent);
            }

            _ = aSB.Append(']');
        }
    }
    // End of JSONArray

    public partial class JSONObject : JSONNode
    {
        private ConcurrentDictionary<string, JSONNode> m_Dict = new ConcurrentDictionary<string, JSONNode>();

        private bool inline = false;
        public override bool Inline
        {
            get => this.inline;
            set => this.inline = value;
        }

        public override JSONNodeType Tag => JSONNodeType.Object;
        public override bool IsObject => true;

        public override Enumerator GetEnumerator()
        {
            return new Enumerator(this.m_Dict.GetEnumerator());
        }

        public override JSONNode this[string aKey]
        {
            get => this.m_Dict.ContainsKey(aKey) ? this.m_Dict[aKey] : new JSONLazyCreator(this, aKey);
            set
            {
                if (value == null) {
                    value = JSONNull.CreateOrGet();
                }

                if (this.m_Dict.ContainsKey(aKey)) {
                    this.m_Dict[aKey] = value;
                }
                else {
                    _ = this.m_Dict.TryAdd(aKey, value);
                }
            }
        }

        public override JSONNode this[int aIndex]
        {
            get => aIndex < 0 || aIndex >= this.m_Dict.Count ? null : this.m_Dict.ElementAt(aIndex).Value;
            set
            {
                if (value == null) {
                    value = JSONNull.CreateOrGet();
                }

                if (aIndex < 0 || aIndex >= this.m_Dict.Count) {
                    return;
                }

                var key = this.m_Dict.ElementAt(aIndex).Key;
                this.m_Dict[key] = value;
            }
        }

        public override int Count => this.m_Dict.Count;

        public override void Add(string aKey, JSONNode aItem)
        {
            if (aItem == null) {
                aItem = JSONNull.CreateOrGet();
            }

            if (aKey != null) {
                if (this.m_Dict.ContainsKey(aKey)) {
                    this.m_Dict[aKey] = aItem;
                }
                else {
                    _ = this.m_Dict.TryAdd(aKey, aItem);
                }
            }
            else {
                _ = this.m_Dict.TryAdd(Guid.NewGuid().ToString(), aItem);
            }
        }

        public override JSONNode Remove(string aKey)
        {
            _ = this.m_Dict.TryRemove(aKey, out var tmp);
            return tmp;
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= this.m_Dict.Count) {
                return null;
            }

            var item = this.m_Dict.ElementAt(aIndex);
            _ = this.m_Dict.TryRemove(item.Key, out var tmp);
            return tmp;
        }

        public override JSONNode Remove(JSONNode aNode)
        {
            try {
                KeyValuePair<string, JSONNode>? item = this.m_Dict.Where(k => k.Value == aNode).FirstOrDefault();
                if (!item.HasValue) {
                    return null;
                }
                else {
                    _ = this.m_Dict.TryRemove(item.Value.Key, out var tmp);
                    return tmp;
                }
            }
            catch {
                return null;
            }
        }

        public override void Clear()
        {
            this.m_Dict.Clear();
        }

        public override JSONNode Clone()
        {
            var node = new JSONObject();
            foreach (var n in this.m_Dict.ToArray()) {
                node.Add(n.Key, n.Value.Clone());
            }
            return node;
        }

        public override bool HasKey(string aKey)
        {
            return this.m_Dict.ContainsKey(aKey);
        }

        public override JSONNode GetValueOrDefault(string aKey, JSONNode aDefault)
        {
            JSONNode res;
            return this.m_Dict.TryGetValue(aKey, out res) ? res : aDefault;
        }

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (var N in this.m_Dict.ToArray()) {
                    yield return N.Value;
                }
            }
        }

        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            _ = aSB.Append('{');
            var first = true;
            if (this.inline) {
                aMode = JSONTextMode.Compact;
            }

            try {
                foreach (var k in this.m_Dict.ToArray()) {
                    if (!first) {
                        _ = aSB.Append(',');
                    }

                    first = false;
                    if (aMode == JSONTextMode.Indent) {
                        _ = aSB.AppendLine();
                    }

                    if (aMode == JSONTextMode.Indent) {
                        _ = aSB.Append(' ', aIndent + aIndentInc);
                    }

                    _ = aSB.Append('\"').Append(Escape(k.Key)).Append('\"');
                    _ = aMode == JSONTextMode.Compact ? aSB.Append(':') : aSB.Append(" : ");

                    k.Value.WriteToStringBuilder(aSB, aIndent + aIndentInc, aIndentInc, aMode);
                }
            }
            catch {
                _ = aSB.Clear();
                return;
            }
            if (aMode == JSONTextMode.Indent) {
                _ = aSB.AppendLine().Append(' ', aIndent);
            }

            _ = aSB.Append('}');
        }
    }
    // End of JSONObject

    public partial class JSONString : JSONNode
    {
        private string m_Data;

        public override JSONNodeType Tag => JSONNodeType.String;
        public override bool IsString => true;

        public override Enumerator GetEnumerator()
        {
            return new Enumerator();
        }

        public override string Value
        {
            get => this.m_Data;
            set => this.m_Data = value;
        }

        public JSONString(string aData)
        {
            this.m_Data = aData;
        }
        public override JSONNode Clone()
        {
            return new JSONString(this.m_Data);
        }

        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            _ = aSB.Append('\"').Append(Escape(this.m_Data)).Append('\"');
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj)) {
                return true;
            }

            var s = obj as string;
            if (s != null) {
                return this.m_Data == s;
            }

            var s2 = obj as JSONString;
            return s2 != null ? this.m_Data == s2.m_Data : false;
        }
        public override int GetHashCode()
        {
            return this.m_Data.GetHashCode();
        }

        public override void Clear()
        {
            this.m_Data = "";
        }
    }
    // End of JSONString

    public partial class JSONNumber : JSONNode
    {
        private double m_Data;

        public override JSONNodeType Tag => JSONNodeType.Number;
        public override bool IsNumber => true;
        public override Enumerator GetEnumerator()
        {
            return new Enumerator();
        }

        public override string Value
        {
            get => this.m_Data.ToString(CultureInfo.InvariantCulture);
            set
            {
                double v;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) {
                    this.m_Data = v;
                }
            }
        }

        public override double AsDouble
        {
            get => this.m_Data;
            set => this.m_Data = value;
        }
        public override long AsLong
        {
            get => (long)this.m_Data;
            set => this.m_Data = value;
        }
        public override ulong AsULong
        {
            get => (ulong)this.m_Data;
            set => this.m_Data = value;
        }

        public JSONNumber(double aData)
        {
            this.m_Data = aData;
        }

        public JSONNumber(string aData)
        {
            this.Value = aData;
        }

        public override JSONNode Clone()
        {
            return new JSONNumber(this.m_Data);
        }

        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            _ = aSB.Append(this.Value);
        }

        private static bool IsNumeric(object value)
        {
            return value is int || value is uint
|| value is float || value is double
|| value is decimal
|| value is long || value is ulong
|| value is short || value is ushort
|| value is sbyte || value is byte;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) {
                return false;
            }

            if (base.Equals(obj)) {
                return true;
            }

            var s2 = obj as JSONNumber;
            return s2 != null ? this.m_Data == s2.m_Data : IsNumeric(obj) ? Convert.ToDouble(obj) == this.m_Data : false;
        }
        public override int GetHashCode()
        {
            return this.m_Data.GetHashCode();
        }

        public override void Clear()
        {
            this.m_Data = 0;
        }
    }
    // End of JSONNumber

    public partial class JSONBool : JSONNode
    {
        private bool m_Data;

        public override JSONNodeType Tag => JSONNodeType.Boolean;
        public override bool IsBoolean => true;
        public override Enumerator GetEnumerator()
        {
            return new Enumerator();
        }

        public override string Value
        {
            get => this.m_Data.ToString();
            set
            {
                bool v;
                if (bool.TryParse(value, out v)) {
                    this.m_Data = v;
                }
            }
        }
        public override bool AsBool
        {
            get => this.m_Data;
            set => this.m_Data = value;
        }

        public JSONBool(bool aData)
        {
            this.m_Data = aData;
        }

        public JSONBool(string aData)
        {
            this.Value = aData;
        }

        public override JSONNode Clone()
        {
            return new JSONBool(this.m_Data);
        }

        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            _ = aSB.Append(this.m_Data ? "true" : "false");
        }

        public override bool Equals(object obj)
        {
            return obj == null ? false : obj is bool ? this.m_Data == (bool)obj : false;
        }
        public override int GetHashCode()
        {
            return this.m_Data.GetHashCode();
        }

        public override void Clear()
        {
            this.m_Data = false;
        }
    }
    // End of JSONBool

    public partial class JSONNull : JSONNode
    {
        private static JSONNull m_StaticInstance = new JSONNull();
        public static bool reuseSameInstance = true;
        public static JSONNull CreateOrGet()
        {
            return reuseSameInstance ? m_StaticInstance : new JSONNull();
        }
        private JSONNull() { }

        public override JSONNodeType Tag => JSONNodeType.NullValue;
        public override bool IsNull => true;
        public override Enumerator GetEnumerator()
        {
            return new Enumerator();
        }

        public override string Value
        {
            get => "null";
            set { }
        }
        public override bool AsBool
        {
            get => false;
            set { }
        }

        public override JSONNode Clone()
        {
            return CreateOrGet();
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) ? true : obj is JSONNull;
        }
        public override int GetHashCode()
        {
            return 0;
        }

        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            _ = aSB.Append("null");
        }
    }
    // End of JSONNull

    internal partial class JSONLazyCreator : JSONNode
    {
        private JSONNode m_Node = null;
        private string m_Key = null;
        public override JSONNodeType Tag => JSONNodeType.None;
        public override Enumerator GetEnumerator()
        {
            return new Enumerator();
        }

        public JSONLazyCreator(JSONNode aNode)
        {
            this.m_Node = aNode;
            this.m_Key = null;
        }

        public JSONLazyCreator(JSONNode aNode, string aKey)
        {
            this.m_Node = aNode;
            this.m_Key = aKey;
        }

        private T Set<T>(T aVal) where T : JSONNode
        {
            if (this.m_Key == null) {
                this.m_Node.Add(aVal);
            }
            else {
                this.m_Node.Add(this.m_Key, aVal);
            }

            this.m_Node = null; // Be GC friendly.
            return aVal;
        }

        public override JSONNode this[int aIndex]
        {
            get => new JSONLazyCreator(this);
            set => this.Set(new JSONArray()).Add(value);
        }

        public override JSONNode this[string aKey]
        {
            get => new JSONLazyCreator(this, aKey);
            set => this.Set(new JSONObject()).Add(aKey, value);
        }

        public override void Add(JSONNode aItem)
        {
            this.Set(new JSONArray()).Add(aItem);
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            this.Set(new JSONObject()).Add(aKey, aItem);
        }

        public static bool operator ==(JSONLazyCreator a, object b)
        {
            return b == null ? true : System.Object.ReferenceEquals(a, b);
        }

        public static bool operator !=(JSONLazyCreator a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj == null ? true : System.Object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override int AsInt
        {
            get
            {
                _ = this.Set(new JSONNumber(0));
                return 0;
            }
            set => this.Set(new JSONNumber(value));
        }

        public override float AsFloat
        {
            get
            {
                _ = this.Set(new JSONNumber(0.0f));
                return 0.0f;
            }
            set => this.Set(new JSONNumber(value));
        }

        public override double AsDouble
        {
            get
            {
                _ = this.Set(new JSONNumber(0.0));
                return 0.0;
            }
            set => this.Set(new JSONNumber(value));
        }

        public override long AsLong
        {
            get
            {
                if (longAsString) {
                    this.Set(new JSONString("0"));
                }
                else {
                    this.Set(new JSONNumber(0.0));
                }

                return 0L;
            }
            set
            {
                if (longAsString) {
                    this.Set(new JSONString(value.ToString()));
                }
                else {
                    this.Set(new JSONNumber(value));
                }
            }
        }

        public override ulong AsULong
        {
            get
            {
                if (longAsString) {
                    this.Set(new JSONString("0"));
                }
                else {
                    this.Set(new JSONNumber(0.0));
                }

                return 0L;
            }
            set
            {
                if (longAsString) {
                    this.Set(new JSONString(value.ToString()));
                }
                else {
                    this.Set(new JSONNumber(value));
                }
            }
        }

        public override bool AsBool
        {
            get
            {
                _ = this.Set(new JSONBool(false));
                return false;
            }
            set => this.Set(new JSONBool(value));
        }

        public override JSONArray AsArray => this.Set(new JSONArray());

        public override JSONObject AsObject => this.Set(new JSONObject());
        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            _ = aSB.Append("null");
        }
    }
    // End of JSONLazyCreator

    public static class JSON
    {
        public static JSONNode Parse(string aJSON)
        {
            return JSONNode.Parse(aJSON);
        }
    }
}