/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections;
using System.Collections.Generic;

namespace UVtools.Core.PixelEditor;

public class PixelHistory : IEnumerable<PixelOperation>
{
    public List<PixelOperation> Items { get; } = new List<PixelOperation>();

    public int Count => Items.Count;

    #region Indexers
    public PixelOperation this[uint index] => Items[(int) index];

    public PixelOperation this[int index] => Items[index];

    public PixelOperation this[long index] => Items[(int) index];

    #endregion

    #region Numerators
    public IEnumerator<PixelOperation> GetEnumerator()
    {
        return ((IEnumerable<PixelOperation>)Items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    #region Methods

    public void Add(PixelOperation item) => Items.Add(item);
    public void Clear() => Items.Clear();

    public bool Contains(PixelOperation operation)
    {
        for (int i = 0; i < Count; i++)
        {
            if (Items[i].Location == operation.Location && 
                Items[i].OperationType == operation.OperationType &&
                Items[i].LayerIndex == operation.LayerIndex) return true;
        }

        return false;
    }

    #endregion

    public void Renumber()
    {
        for (int i = 0; i < Count; i++)
        {
            Items[i].Index = (uint) (i + 1);
        }
    }
}