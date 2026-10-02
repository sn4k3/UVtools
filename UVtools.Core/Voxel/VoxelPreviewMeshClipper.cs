/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Numerics;

namespace UVtools.Core.Voxel;

/// <summary>
/// Cuts the quads of a <see cref="VoxelPreviewMesh"/> the same way the 3D preview cutaway hides them.
/// </summary>
public static class VoxelPreviewMeshClipper
{
    private const float Epsilon = 1e-4f;

    /// <summary>
    /// Removes the part of the mesh on the cut side of the cutaway plane, a face crossing the plane is shortened to it
    /// instead of being dropped, so no wall goes missing. The cut is left open.
    /// </summary>
    /// <param name="vertices">The vertices of the mesh, made of independent axis aligned quads.</param>
    /// <param name="indices">The indices of the mesh, two triangles per quad.</param>
    /// <param name="axis">The axis of the plane, <see cref="VoxelPreviewCutawayAxis.Off"/> returns the mesh as is.</param>
    /// <param name="position">The position of the plane in millimeters.</param>
    /// <param name="invert">False keeps what is below the position, true keeps what is above.</param>
    public static (VoxelPreviewVertex[] Vertices, uint[] Indices) ApplyCutaway(
        ReadOnlySpan<VoxelPreviewVertex> vertices,
        ReadOnlySpan<uint> indices,
        VoxelPreviewCutawayAxis axis,
        float position,
        bool invert)
    {
        if (axis == VoxelPreviewCutawayAxis.Off) return (vertices.ToArray(), indices.ToArray());
        if (vertices.Length % 4 != 0)
            throw new InvalidOperationException("The mesh does not contain complete quad faces.");

        var component = axis == VoxelPreviewCutawayAxis.X ? 0 : 1;
        var quadCount = vertices.Length / 4;
        var newVertices = new VoxelPreviewVertex[vertices.Length];
        var kept = 0;
        Span<Vector3> corners = stackalloc Vector3[4];

        for (var quad = 0; quad < quadCount; quad++)
        {
            var source = vertices.Slice(quad * 4, 4);
            var normal = source[0].Normal;
            var minimum = float.MaxValue;
            var maximum = float.MinValue;
            for (var corner = 0; corner < 4; corner++)
            {
                corners[corner] = source[corner].Position;
                var value = Get(corners[corner], component);
                minimum = Math.Min(minimum, value);
                maximum = Math.Max(maximum, value);
            }

            if (invert)
            {
                if (maximum <= position + Epsilon && maximum - minimum > Epsilon) continue; // Entirely cut
                if (maximum - minimum <= Epsilon && maximum < position - Epsilon) continue; // A plane facing the axis
                for (var corner = 0; corner < 4; corner++)
                {
                    corners[corner] = Set(corners[corner], component,
                        Math.Max(Get(corners[corner], component), position));
                }
            }
            else
            {
                if (minimum >= position - Epsilon && maximum - minimum > Epsilon) continue;
                if (maximum - minimum <= Epsilon && minimum > position + Epsilon) continue;
                for (var corner = 0; corner < 4; corner++)
                {
                    corners[corner] = Set(corners[corner], component,
                        Math.Min(Get(corners[corner], component), position));
                }
            }

            var destination = kept * 4;
            for (var corner = 0; corner < 4; corner++)
            {
                newVertices[destination + corner] = new VoxelPreviewVertex(corners[corner], normal);
            }

            kept++;
        }

        Array.Resize(ref newVertices, kept * 4);
        var newIndices = new uint[kept * 6];
        for (var quad = 0; quad < kept; quad++)
        {
            var vertex = (uint)(quad * 4);
            var offset = quad * 6;
            newIndices[offset] = vertex;
            newIndices[offset + 1] = vertex + 1;
            newIndices[offset + 2] = vertex + 2;
            newIndices[offset + 3] = vertex;
            newIndices[offset + 4] = vertex + 2;
            newIndices[offset + 5] = vertex + 3;
        }

        return (newVertices, newIndices);
    }

    private static float Get(Vector3 vector, int component) => component switch
    {
        0 => vector.X,
        1 => vector.Y,
        _ => vector.Z
    };

    private static Vector3 Set(Vector3 vector, int component, float value)
    {
        switch (component)
        {
            case 0: vector.X = value; break;
            case 1: vector.Y = value; break;
            default: vector.Z = value; break;
        }

        return vector;
    }
}
