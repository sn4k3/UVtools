/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;

namespace UVtools.Core.Objects
{
    public class StringTag : BindableBase, IEquatable<StringTag>, IComparable<StringTag>
    {
        private string _content;
        private object _tag;

        public string Content
        {
            get => _content;
            set => RaiseAndSetIfChanged(ref _content, value);
        }

        public object Tag
        {
            get => _tag;
            set => RaiseAndSetIfChanged(ref _tag, value);
        }

        public string TagString
        {
            get => Tag?.ToString();
            set => Tag = value;
        } 

        public StringTag(object content, object tag = null)
        {
            Content = content.ToString();
            Tag = tag;
        }
        public StringTag(string content, object tag = null)
        {
            Content = content;
            Tag = tag;
        }

        public bool Equals(StringTag other)
        {
            return _content == other._content && Equals(_tag, other._tag);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StringTag) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_content != null ? _content.GetHashCode() : 0) * 397) ^ (_tag != null ? _tag.GetHashCode() : 0);
            }
        }

        private sealed class ContentEqualityComparer : IEqualityComparer<StringTag>
        {
            public bool Equals(StringTag x, StringTag y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Content == y.Content;
            }

            public int GetHashCode(StringTag obj)
            {
                return (obj.Content != null ? obj.Content.GetHashCode() : 0);
            }
        }

        public static IEqualityComparer<StringTag> ContentComparer { get; } = new ContentEqualityComparer();

        public int CompareTo(StringTag other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(Content, other.Content, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Content;
        }
    }
}
