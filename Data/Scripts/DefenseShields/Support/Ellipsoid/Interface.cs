﻿using System;

namespace DefenseShields.Support
{
    /// <summary>
    /// Interface for 1D objects (vector, line, ray, segment)
    /// </summary>
    public interface ILinearObject
    {
        Vector3d Direction { get; }
        bool IsOriented { get; }
    }

    /// <summary>
    /// Interface for 2D objects (plane, circle, ellipse, triangle)
    /// </summary>
    public interface IPlanarObject
    {
        Vector3d Normal { get; }
        bool IsOriented { get; }
    }
}