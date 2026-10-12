namespace ColliderGen;

internal interface ISimplificationStep
{
    static abstract float Cost(HullVertex vertex);
    static abstract bool TryApply(HullVertex vertex, out IReadOnlyList<HullVertex> affected);
}
