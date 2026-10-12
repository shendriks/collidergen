using System.Numerics;

namespace ColliderGen;

internal sealed class HullVertex(Vector2 point)
{
    public Vector2 Point = point;
    public HullVertex Prev = null!;
    public HullVertex Next = null!;
    public int Version;
    public bool IsDeleted;

    public bool IsCurrent(int version) => !IsDeleted && Version == version;

    /// <summary>
    /// Marks the current vertex as deleted, updates the linked list to bypass the current vertex by linking its previous vertex to its next
    /// vertex, and returns the previous and next vertices.
    /// </summary>
    /// <returns>Tuple containing the previous and next vertices in the linked list after the current vertex has been unlinked.</returns>
    public (HullVertex Prev, HullVertex Next) Unlink()
    {
        IsDeleted = true;
        Prev.Next = Next;
        Next.Prev = Prev;
        return (Prev, Next);
    }

    public static HullVertex[] BuildRing(IEnumerable<Vector2> points)
    {
        var vertices = points.Select(point => new HullVertex(point)).ToArray();
        for (var i = 0; i < vertices.Length; i++) {
            vertices[i].Prev = vertices[(i - 1 + vertices.Length) % vertices.Length];
            vertices[i].Next = vertices[(i + 1) % vertices.Length];
        }

        return vertices;
    }

    public static List<Vector2> CollectRemaining(IEnumerable<HullVertex> vertices)
    {
        var start = vertices.First(vertex => !vertex.IsDeleted);
        var result = new List<Vector2>();
        var current = start;
        do {
            result.Add(current.Point);
            current = current.Next;
        } while (current != start);

        return result;
    }
}
