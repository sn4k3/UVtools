/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Numerics;

namespace UVtools.Core.Voxel;

public class GridCube
{
    public Vector3[] Vertex { get; } = new Vector3[8];
    public float[] Value { get; } = new float[8];
}