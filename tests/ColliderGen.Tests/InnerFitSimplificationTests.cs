using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(HullSimplifier))]
public class InnerFitSimplificationTests
{
    [Test]
    public void Simplify_ThreeOrFewerPoints_ReturnsInputUnchanged()
    {
        Vector2[] points = [new(0, 0), new(4, 0), new(2, 4)];

        var simplified = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon: 100f);

        Assert.That(simplified, Is.EqualTo(points));
    }

    [Test]
    public void Simplify_CollinearRun_CollapsesToThreeVertexFloor()
    {
        Vector2[] points = [new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0)];

        var simplified = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon: 1f);

        Assert.That(simplified, Has.Count.EqualTo(3));
    }

    [Test]
    public void Simplify_NearCollinearMidpointUnderEpsilon_IsRemoved()
    {
        Vector2[] points = [new(0, 0), new(5, 0.1f), new(10, 0), new(10, 10), new(0, 10)];

        var simplified = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon: 1f);

        Assert.That(simplified, Does.Not.Contain(new Vector2(5, 0.1f)));
        Assert.That(simplified, Does.Contain(new Vector2(10, 10)));
        Assert.That(simplified, Does.Contain(new Vector2(0, 10)));
    }

    [Test]
    public void Simplify_ZeroEpsilon_KeepsGenuineCorners()
    {
        Vector2[] points = [new(0, 0), new(5, 0), new(10, 0), new(10, 10), new(0, 10)];

        var simplified = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon: 0f);

        Assert.That(simplified, Has.Count.EqualTo(4));
        Assert.That(simplified, Does.Not.Contain(new Vector2(5, 0)));
    }

    [Test]
    public void Simplify_VeryLargeEpsilon_NeverGoesBelowThreeVertices()
    {
        Vector2[] points = [new(0, 0), new(10, 1), new(20, 0), new(15, 12), new(3, 10)];

        var simplified = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon: 1_000_000f);

        Assert.That(simplified, Has.Count.EqualTo(3));
    }

    [Test]
    public void Simplify_MaxVerticesBelowCount_ReducesToMaxEvenWhenAreasExceedEpsilon()
    {
        Vector2[] points = [new(0, 0), new(10, -1), new(20, 0), new(21, 10), new(20, 20), new(10, 21), new(0, 20), new(-1, 10)];

        var simplified = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon: 0f, maxVertices: 5);

        Assert.That(simplified, Has.Count.EqualTo(5));
    }

    [Test]
    public void Simplify_MaxVerticesAtOrAboveCount_ReturnsInputUnchanged()
    {
        Vector2[] points = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];

        var simplified = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon: 0f, maxVertices: 4);

        Assert.That(simplified, Is.EqualTo(points));
    }

    [Test]
    public void Simplify_MaxVerticesBelowThree_NeverGoesBelowThreeVertices()
    {
        Vector2[] points = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];

        var simplified = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon: 0f, maxVertices: 1);

        Assert.That(simplified, Has.Count.EqualTo(3));
    }

    [Test]
    public void Simplify_WithMaxVertices_MatchesNaiveGreedyReference()
    {
        var random = new Random(1234);
        for (var run = 0; run < 50; run++) {
            var count = random.Next(20, 80);
            var points = new List<Vector2>();
            for (var i = 0; i < count; i++) {
                var angle = MathF.Tau * i / count;
                var radius = 20f + (float)random.NextDouble() * 5f;
                points.Add(new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
            }

            var epsilon = (float)random.NextDouble() * 4f;
            var maxVertices = random.Next(3, 17);

            var expected = NaiveSimplify(points, epsilon, maxVertices);
            var actual = HullSimplifier.Simplify(points, ColliderFit.Inner, epsilon, maxVertices);

            Assert.That(actual, Is.EquivalentTo(expected), $"run {run}");
        }
    }

    private static List<Vector2> NaiveSimplify(List<Vector2> points, float epsilon, int maxVertices)
    {
        var vertices = new List<Vector2>(points);
        var capping = false;
        while (vertices.Count > 3) {
            var minArea = float.MaxValue;
            var minIndex = 0;
            for (var i = 0; i < vertices.Count; i++) {
                var a = vertices[(i - 1 + vertices.Count) % vertices.Count];
                var b = vertices[i];
                var c = vertices[(i + 1) % vertices.Count];
                var area = MathF.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) * 0.5f;
                if (area >= minArea) {
                    continue;
                }

                minArea = area;
                minIndex = i;
            }

            capping |= minArea > epsilon;
            if (capping && vertices.Count <= maxVertices) {
                break;
            }

            vertices.RemoveAt(minIndex);
        }

        return vertices;
    }
}
