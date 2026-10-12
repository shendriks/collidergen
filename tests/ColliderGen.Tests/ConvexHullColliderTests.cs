using System.Numerics;
using NUnit.Framework;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(ConvexHullGenerator))]
public class ConvexHullGeneratorTests
{
    private static bool[,] Mask(int width, int height, params (int X, int Y)[] foreground)
    {
        var mask = new bool[width, height];
        foreach (var (x, y) in foreground) {
            mask[x, y] = true;
        }

        return mask;
    }

    [Test]
    public void ComputeMaskHull_DiagonalJoint_SpansBothParts()
    {
        var mask = Mask(7, 4, (3, 0), (4, 1), (5, 2), (2, 1), (1, 2));

        var hull = ConvexHullGenerator.ComputeMaskHull(mask);

        Assert.That(hull, Is.EquivalentTo(new Vector2[] { new(3, 0), new(4, 0), new(6, 2), new(6, 3), new(1, 3), new(1, 2) }));
    }

    [Test]
    public void ComputeMaskHull_EmptyMask_ReturnsEmpty()
    {
        var hull = ConvexHullGenerator.ComputeMaskHull(new bool[4, 4]);

        Assert.That(hull, Is.Empty);
    }

    [Test]
    public void ComputeMaskHull_SinglePixel_ReturnsPixelSquare()
    {
        var hull = ConvexHullGenerator.ComputeMaskHull(Mask(5, 5, (2, 3)));

        Assert.That(hull, Is.EquivalentTo(new Vector2[] { new(2, 3), new(3, 3), new(3, 4), new(2, 4) }));
    }

    [Test]
    public void ComputeMaskHull_DisconnectedRegions_SpansAllRegions()
    {
        var mask = Mask(10, 10, (1, 1), (2, 1), (1, 2), (2, 2), (8, 7));

        var hull = ConvexHullGenerator.ComputeMaskHull(mask);

        Assert.That(hull, Is.EquivalentTo(new Vector2[] { new(1, 1), new(3, 1), new(9, 7), new(9, 8), new(8, 8), new(1, 3) }));
    }

    [Test]
    public void ComputeMaskHull_HollowRing_MatchesOuterBounds()
    {
        var mask = new bool[6, 6];
        for (var i = 1; i <= 4; i++) {
            mask[i, 1] = mask[i, 4] = mask[1, i] = mask[4, i] = true;
        }

        var hull = ConvexHullGenerator.ComputeMaskHull(mask);

        Assert.That(hull, Is.EquivalentTo(new Vector2[] { new(1, 1), new(5, 1), new(5, 5), new(1, 5) }));
    }

    [Test]
    public void ComputeMaskHull_SingleRow_ReturnsRowRectangle()
    {
        var hull = ConvexHullGenerator.ComputeMaskHull(Mask(6, 3, (1, 1), (2, 1), (3, 1), (4, 1)));

        Assert.That(hull, Is.EquivalentTo(new Vector2[] { new(1, 1), new(5, 1), new(5, 2), new(1, 2) }));
    }

    [Test]
    public void ComputeMaskHull_SingleColumn_ExcludesCollinearRowCorners()
    {
        var hull = ConvexHullGenerator.ComputeMaskHull(Mask(5, 5, (2, 0), (2, 1), (2, 2), (2, 3)));

        Assert.That(hull, Is.EquivalentTo(new Vector2[] { new(2, 0), new(3, 0), new(3, 4), new(2, 4) }));
    }

    [Test]
    public void ComputeMaskHull_LShape_ExcludesReflexCorner()
    {
        var hull = ConvexHullGenerator.ComputeMaskHull(Mask(5, 5, (0, 0), (0, 1), (0, 2), (0, 3), (1, 3), (2, 3), (3, 3)));

        Assert.That(hull, Is.EqualTo(new Vector2[] { new(0, 0), new(1, 0), new(4, 3), new(4, 4), new(0, 4) }));
    }

    [Test]
    public void ComputeMaskHull_Diamond_StartsAtLeftmostTopmostAndRunsClockwiseOnScreen()
    {
        var mask = Mask(5, 5, (2, 0), (1, 1), (2, 1), (3, 1), (1, 3), (2, 3), (3, 3), (2, 4));
        for (var x = 0; x < 5; x++) {
            mask[x, 2] = true;
        }

        var hull = ConvexHullGenerator.ComputeMaskHull(mask);

        Assert.That(hull, Is.EqualTo(new Vector2[] { new(0, 2), new(2, 0), new(3, 0), new(5, 2), new(5, 3), new(3, 5), new(2, 5), new(0, 3) }));
    }
}
