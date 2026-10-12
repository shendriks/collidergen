using System.Numerics;

namespace ColliderGen;

internal static class ConvexHullGenerator
{
    /// <summary>Computes the convex hull of a binary mask, returning a list of 2D points that define the boundary of the shape.</summary>
    /// <param name="mask">Two-dimensional boolean array representing the binary mask where foreground pixels are marked as true.</param>
    /// <returns>Read-only list of <see cref="Vector2"/> points defining the convex hull of the binary mask in clockwise order (y down).</returns>
    internal static IReadOnlyList<Vector2> ComputeMaskHull(bool[,] mask)
    {
        var (leftBoundary, rightBoundary) = TraceRowBoundaries(mask);
        if (leftBoundary.Count == 0) {
            return [];
        }

        var leftChain = ConvexChain(leftBoundary, inwardTurnSign: 1);
        var rightChain = ConvexChain(rightBoundary, inwardTurnSign: -1);

        leftChain.Reverse();
        rightChain.AddRange(leftChain);
        return [.. StartingAtLeftmostTopmost(rightChain).Select(corner => corner.ToVector2())];
    }

    private static (List<PixelCorner> Left, List<PixelCorner> Right) TraceRowBoundaries(bool[,] mask)
    {
        var width = mask.GetLength(0);
        var height = mask.GetLength(1);
        var leftBoundary = new List<PixelCorner>();
        var rightBoundary = new List<PixelCorner>();

        for (var y = 0; y < height; y++) {
            var left = 0;
            while (left < width && !mask[left, y]) {
                left++;
            }

            if (left == width) {
                continue;
            }

            var right = width - 1;
            while (!mask[right, y]) {
                right--;
            }

            AppendKeepingOutermost(leftBoundary, new(left, y), Math.Min);
            AppendKeepingOutermost(leftBoundary, new(left, y + 1), Math.Min);
            AppendKeepingOutermost(rightBoundary, new(right + 1, y), Math.Max);
            AppendKeepingOutermost(rightBoundary, new(right + 1, y + 1), Math.Max);
        }

        return (leftBoundary, rightBoundary);
    }

    private static void AppendKeepingOutermost(List<PixelCorner> boundary, PixelCorner corner, Func<int, int, int> outermost)
    {
        if (boundary.Count > 0 && boundary[^1].Y == corner.Y) {
            boundary[^1] = corner with { X = outermost(boundary[^1].X, corner.X) };
            return;
        }

        boundary.Add(corner);
    }

    private static List<PixelCorner> ConvexChain(List<PixelCorner> boundary, int inwardTurnSign)
    {
        var chain = new List<PixelCorner>(boundary.Count);
        foreach (var corner in boundary) {
            while (chain.Count >= 2 && Cross(chain[^2], chain[^1], corner) * inwardTurnSign >= 0) {
                chain.RemoveAt(chain.Count - 1);
            }

            chain.Add(corner);
        }

        return chain;
    }

    private static List<PixelCorner> StartingAtLeftmostTopmost(List<PixelCorner> hull)
    {
        var start = 0;
        for (var i = 1; i < hull.Count; i++) {
            if (hull[i].X < hull[start].X || (hull[i].X == hull[start].X && hull[i].Y < hull[start].Y)) {
                start = i;
            }
        }

        return [.. hull[start..], .. hull[..start]];
    }

    private static int Cross(PixelCorner origin, PixelCorner a, PixelCorner b)
    {
        return (a.X - origin.X) * (b.Y - origin.Y) - (a.Y - origin.Y) * (b.X - origin.X);
    }

    private readonly record struct PixelCorner(int X, int Y)
    {
        public Vector2 ToVector2() => new(X, Y);
    }
}
