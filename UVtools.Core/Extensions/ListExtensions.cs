/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Collections.Generic;
using System;
using System.Linq;

namespace UVtools.Core.Extensions;

public static class ListExtensions
{
    public static List<T> Clone<T>(this List<T> listToClone) where T : class, ICloneable
    {
        return listToClone.Select(item => (T)item.Clone()).ToList();
    }
}