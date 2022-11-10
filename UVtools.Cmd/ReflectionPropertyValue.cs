/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.Cmd
{
    internal sealed class ReflectionPropertyValue
    {
        public string Name { get; init; }
        public string Value { get; init; }
        public bool Found { get; set; }

        public ReflectionPropertyValue(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public void Deconstruct(out string name, out string value)
        {
            name = Name;
            value = Value;
        }

        public void SetFound() => Found = true;

        private bool Equals(ReflectionPropertyValue other)
        {
            return Name == other.Name && Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is ReflectionPropertyValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Value);
        }

        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}
