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
    public class StringTag : IComparable<StringTag>
    {
        public string Content { get; set; }

        public object Tag { get; set; }
        public string TagString => Tag.ToString();

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
