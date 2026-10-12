using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(HullSimplifier))]
public class OuterFitSimplificationTests
{
    private static readonly Vector2[] Octagon = [
        new(1, 0), new(3, 0), new(4, 1), new(4, 3), new(3, 4), new(1, 4), new(0, 3), new(0, 1)
    ];

    private static readonly Vector2[] EnclosingSquare = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];

    [Test]
    public void Simplify_Triangle_ReturnsInputUnchanged()
    {
        Vector2[] triangle = [new(0, 0), new(4, 0), new(2, 4)];

        var simplified = HullSimplifier.Simplify(triangle, ColliderFit.Outer, epsilon: 100f, maxVertices: 3);

        Assert.That(simplified, Is.EqualTo(triangle));
    }

    [Test]
    public void Simplify_OctagonCappedAtFour_ExtendsSidesToEnclosingSquare()
    {
        var simplified = HullSimplifier.Simplify(Octagon, ColliderFit.Outer, epsilon: 0f, maxVertices: 4);

        Assert.That(simplified, Is.EquivalentTo(EnclosingSquare));
    }

    [Test]
    public void Simplify_AddedAreaWithinEpsilon_RemovesEdge()
    {
        var simplified = HullSimplifier.Simplify(Octagon, ColliderFit.Outer, epsilon: 0.5f);

        Assert.That(simplified, Is.EquivalentTo(EnclosingSquare));
    }

    [Test]
    public void Simplify_AddedAreaAboveEpsilon_KeepsEdge()
    {
        var simplified = HullSimplifier.Simplify(Octagon, ColliderFit.Outer, epsilon: 0.4f);

        Assert.That(simplified, Is.EqualTo(Octagon));
    }

    [Test]
    public void Simplify_RectangleCappedAtThree_StaysRectangleBecauseOppositeSidesAreParallel()
    {
        var simplified = HullSimplifier.Simplify(EnclosingSquare, ColliderFit.Outer, epsilon: 0f, maxVertices: 3);

        Assert.That(simplified, Is.EqualTo(EnclosingSquare));
    }

    [Test]
    public void TryApply_EdgeWithParallelNeighbours_LeavesRingUnchanged()
    {
        var ring = HullVertex.BuildRing(EnclosingSquare);

        var applied = OuterFitStep.TryApply(ring[0], out var affected);

        Assert.That(applied, Is.False);
        Assert.That(affected, Is.Empty);
        Assert.That(HullVertex.CollectRemaining(ring), Is.EqualTo(EnclosingSquare));
    }

    [Test]
    public void Unlink_MiddleVertex_ReturnsAndJoinsFormerNeighbours()
    {
        var ring = HullVertex.BuildRing(EnclosingSquare);

        var (prev, next) = ring[1].Unlink();

        Assert.That(prev, Is.SameAs(ring[0]));
        Assert.That(next, Is.SameAs(ring[2]));
        Assert.That(prev.Next, Is.SameAs(next));
        Assert.That(next.Prev, Is.SameAs(prev));
        Assert.That(ring[1].IsDeleted, Is.True);
    }

    [Test]
    public void Simplify_TrapezoidCappedAtThree_ExtendsLegsToApex()
    {
        Vector2[] trapezoid = [new(1, 0), new(3, 0), new(4, 2), new(0, 2)];

        var simplified = HullSimplifier.Simplify(trapezoid, ColliderFit.Outer, epsilon: 0f, maxVertices: 3);

        Assert.That(simplified, Is.EquivalentTo(new Vector2[] { new(2, -2), new(4, 2), new(0, 2) }));
    }

    [TestCase(3)]
    [TestCase(5)]
    [TestCase(8)]
    public void Simplify_RegularPolygon_EnclosesEveryOriginalVertexWithinCap(int maxVertices)
    {
        var circle = Enumerable.Range(0, 24)
            .Select(i => new Vector2(MathF.Cos(i * MathF.Tau / 24), MathF.Sin(i * MathF.Tau / 24)) * 10f)
            .ToArray();

        var simplified = HullSimplifier.Simplify(circle, ColliderFit.Outer, epsilon: 0f, maxVertices);

        Assert.That(simplified, Has.Count.EqualTo(maxVertices));
        Assert.That(circle, Has.All.Matches<Vector2>(vertex => ConvexPolygon.Contains(simplified, vertex)));
    }
}
