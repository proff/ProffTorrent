using System;

using MonoTorrent.BEncoding;

namespace MonoTorrent
{
    public sealed class SpanStringList : IEquatable<SpanStringList>, IEquatable<string>
    {
        public SpanStringList (string value, int start, int length) : this (value, start, length, null)
        {
            Value = value;
            Start = start;
            Length = length;
        }

        public SpanStringList (string value, int start, int length, SpanStringList? prev)
        {
            Value = value;
            Start = start;
            Length = length;
            Prev = prev;
        }

        public SpanStringList (string value) : this (value, 0, value.Length, null)
        {
        }

        string Value { get; }

        int Start { get; }

        int Length { get; }

        SpanStringList? Prev { get; }

        public SpanStringList Append (string value)
        {
            return Append (value, 0, value.Length);
        }

        public SpanStringList Append (string value, int start, int length)
        {
            return new SpanStringList (value, start, length, this);
        }

        public override string ToString ()
        {
            SpanStringList? current;
            if (Prev is null) {
                current = this;
                if (current.Start == 0 && current.Length == current.Value.Length)
                    return current.Value;
                return current.Value.AsSpan ().Slice (current.Start, current.Length).ToString ();
            }

            var length = 0;
            current = this;
            do {
                length += current.Length;
                current = current.Prev;
            } while (current is not null);
#if NET5_0_OR_GREATER
            var result = string.Create (length, this, FillSpan);
#else
            var chars = new char[length];
            var span = chars.AsSpan ();
            FillSpan (span, this);
            var result = new string (chars);
#endif
            return result;
        }

        static void FillSpan (Span<char> span, SpanStringList spanList)
        {
            var offset = span.Length;
            var current = spanList;
            do {
                var toCopy = current.Value.AsSpan ().Slice (current.Start, current.Length);
                var copyTo = span.Slice (offset - toCopy.Length, toCopy.Length);
                toCopy.CopyTo (copyTo);
                offset -= toCopy.Length;
                current = current.Prev;
            } while (current is not null);
        }

        public static implicit operator SpanStringList (string value)
        {
            return new SpanStringList (value);
        }

        public static implicit operator string (SpanStringList value)
        {
            return value.ToString ();
        }

        public static explicit operator BEncodedString (SpanStringList value)
        {
            return value.ToString ();
        }

        public override bool Equals (object? obj)
        {
            return ReferenceEquals (this, obj) || obj is SpanStringList other && Equals (other);
        }

        public override int GetHashCode ()
        {
            var result = 397;
            var item = this;
            do {
                var span = item.Value.AsSpan (item.Start, item.Length);
                for (int i = span.Length - 1; i >= 0; i--) {
                    result ^= (span[i].GetHashCode () * 397);
                }

                item = item.Prev;
            } while (item is not null);

            return result;
        }

        public static bool operator == (SpanStringList? left, SpanStringList? right)
        {
            return Equals (left, right);
        }

        public static bool operator != (SpanStringList? left, SpanStringList? right)
        {
            return !Equals (left, right);
        }

        public static bool operator == (SpanStringList? left, string? right)
        {
            if (left is null && right is null) return true;
            if (left is null && right is not null) return false;
            return left!.Equals (right);
        }

        public static bool operator != (SpanStringList? left, string? right)
        {
            if (left is null && right is null)
                return false;
            if (left is null && right is not null)
                return true;
            return !left!.Equals (right);
        }

        public static bool operator == (string? left, SpanStringList? right)
        {
            if (right is null && left is null)
                return true;
            if (right is null && left is not null)
                return false;
            return right!.Equals (left);
        }

        public static bool operator != (string? left, SpanStringList? right)
        {
            if (right is null && left is null)
                return false;
            if (right is null && left is not null)
                return true;
            return !right!.Equals (left);
        }

        public bool Equals (string? other)
        {
            if (other is null)
                return false;
            if (Prev is null) {
                if (Start == 0 && Length == Value.Length)
                    return other == Value;
                return Value.AsSpan ().Slice (Start, Length).Equals (other, StringComparison.Ordinal);
            }

            var offset = 0;
            var item = this;
            do {
                if (item.Length > other.Length - offset)
                    return false;
                var otherSpan = other.AsSpan ().Slice (other.Length - offset - item.Length, item.Length);
                var thisSpan = item.Value.AsSpan (item.Start, item.Length);
                if (!thisSpan.Equals (otherSpan, StringComparison.Ordinal))
                    return false;
                offset += item.Length;
                item = item.Prev;
            } while (item is not null);

            if (offset != other.Length)
                return false;
            return true;
        }

        public bool Equals (SpanStringList? other)
        {
            if (other is null)
                return false;
            if (Prev is null && other.Prev is null) {
                if (Start == 0 && Length == Value.Length && other.Start == 0 && other.Length == other.Value.Length)
                    return other.Value == Value;
                return Value.AsSpan ().Slice (Start, Length).Equals (other.Value.AsSpan ().Slice (other.Start, other.Length), StringComparison.Ordinal);
            }

            var thisOffset = 0;
            var otherOffset = 0;
            var thisItem = this;
            var otherItem = other;
            do {
                if (thisItem is null || otherItem is null)
                    return false;
                var thisLength = thisItem.Length - thisOffset;
                var otherLength = otherItem.Length - otherOffset;
                if (thisLength < otherLength) {
                    var thisSpan = thisItem.Value.AsSpan ().Slice (thisItem.Start, thisItem.Length).Slice (0, thisLength);
                    var otherSpan = otherItem.Value.AsSpan ().Slice (otherItem.Start, otherItem.Length).Slice (otherLength - thisLength, thisLength);
                    if (!thisSpan.Equals (otherSpan, StringComparison.Ordinal))
                        return false;
                    otherOffset += thisLength;
                    thisItem = thisItem.Prev;
                    thisOffset = 0;
                } else if (thisLength > otherLength) {
                    var otherSpan = otherItem.Value.AsSpan ().Slice (otherItem.Start, otherItem.Length).Slice (0, otherLength);
                    var thisSpan = thisItem.Value.AsSpan ().Slice (thisItem.Start, thisItem.Length).Slice (thisLength - otherLength, otherLength);
                    if (!otherSpan.Equals (thisSpan, StringComparison.Ordinal))
                        return false;
                    thisOffset += otherLength;
                    otherItem = otherItem.Prev;
                    otherOffset = 0;
                } else {
                    var otherSpan = otherItem.Value.AsSpan ().Slice (otherItem.Start, otherLength);
                    var thisSpan = thisItem.Value.AsSpan ().Slice (thisItem.Start, thisLength);
                    if (!otherSpan.Equals (thisSpan, StringComparison.Ordinal))
                        return false;
                    thisOffset = 0;
                    otherOffset = 0;
                    thisItem = thisItem.Prev;
                    otherItem = otherItem.Prev;
                }
            } while (thisItem is not null || otherItem is not null);

            return true;
        }
    }
}
