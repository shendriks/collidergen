using System.Collections.Generic;
using System.Numerics;

namespace ColliderGen.Tests;

internal static class ConvexPolygon
{
    private const float Tolerance = 1e-3f;

    internal static bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)
    {
        var hasNegative = false;
        var hasPositive = false;
        for (var i = 0; i < polygon.Count; i++) {
            var start = polygon[i];
            var end = polygon[(i + 1) % polygon.Count];
            var cross = (end.X - start.X) * (point.Y - start.Y) - (end.Y - start.Y) * (point.X - start.X);
            hasNegative |= cross < -Tolerance;
            hasPositive |= cross > Tolerance;
        }

        return !(hasNegative && hasPositive);
    }
}
