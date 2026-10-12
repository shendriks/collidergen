using System.Numerics;
using ScoredQueue = System.Collections.Generic.PriorityQueue<(ColliderGen.HullVertex Vertex, int Version), float>;

namespace ColliderGen;

internal static class HullSimplifier
{
    /// <summary>Simplifies a given convex polygon hull by reducing the number of vertices.</summary>
    /// <param name="hull">2D points representing the convex polygon hull.</param>
    /// <param name="fit">The fit type</param>
    /// <param name="epsilon">A non-negative value specifying the tolerance for vertex removal.</param>
    /// <param name="maxVertices">The maximum number of vertices allowed in the simplified hull. Defaults to <see cref="int.MaxValue"/></param>
    /// <returns>Read-only collection of 2D points representing the simplified hull.</returns>
    internal static IReadOnlyList<Vector2> Simplify(
        IReadOnlyList<Vector2> hull,
        ColliderFit fit,
        float epsilon,
        int maxVertices = int.MaxValue
    ) =>
        fit switch {
            ColliderFit.Outer => Simplify<OuterFitStep>(hull, epsilon, maxVertices),
            _ => Simplify<InnerFitStep>(hull, epsilon, maxVertices)
        };

    private static IReadOnlyList<Vector2> Simplify<TStep>(IReadOnlyList<Vector2> hull, float epsilon, int maxVertices) where TStep : ISimplificationStep
    {
        if (hull.Count <= ColliderOptions.MinVerticesLimit) {
            return hull;
        }

        var vertices = HullVertex.BuildRing(hull);
        var queue = new ScoredQueue();
        RescoreAll<TStep>(vertices, queue);

        var remainingCount = vertices.Length;
        var capping = false;
        while (remainingCount > ColliderOptions.MinVerticesLimit && TryDequeueCurrent(queue, out var cheapest, out var cost)) {
            capping |= cost > epsilon;
            if (capping && remainingCount <= maxVertices) {
                break;
            }

            if (TryRemove<TStep>(cheapest, queue)) {
                remainingCount--;
            }
        }

        return HullVertex.CollectRemaining(vertices);
    }

    private static bool TryDequeueCurrent(ScoredQueue queue, out HullVertex vertex, out float cost)
    {
        while (queue.TryDequeue(out var entry, out cost)) {
            if (!entry.Vertex.IsCurrent(entry.Version)) {
                continue;
            }

            vertex = entry.Vertex;
            return true;
        }

        vertex = null!;
        return false;
    }

    private static bool TryRemove<TStep>(HullVertex vertex, ScoredQueue queue) where TStep : ISimplificationStep
    {
        if (!TStep.TryApply(vertex, out var affectedVertices)) {
            return false;
        }

        RescoreAll<TStep>(affectedVertices.Distinct(), queue);
        return true;
    }

    private static void RescoreAll<TStep>(IEnumerable<HullVertex> vertices, ScoredQueue queue) where TStep : ISimplificationStep
    {
        foreach (var vertex in vertices) {
            Rescore<TStep>(vertex, queue);
        }
    }

    private static void Rescore<TStep>(HullVertex vertex, ScoredQueue queue) where TStep : ISimplificationStep
    {
        vertex.Version++;
        var cost = TStep.Cost(vertex);
        if (float.IsFinite(cost)) {
            queue.Enqueue((vertex, vertex.Version), cost);
        }
    }
}
