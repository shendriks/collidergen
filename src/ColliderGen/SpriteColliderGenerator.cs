using System.Numerics;

namespace ColliderGen;

public static class SpriteColliderGenerator
{
    public static SpriteCollider Generate(SpriteImage image, ColliderOptions options)
    {
        if (!options.TryValidate(out var optionsError)) {
            throw new ArgumentOutOfRangeException(nameof(options), optionsError);
        }

        var layout = StripLayout.Resolve(image.Width, image.Height, options.FrameWidth);
        var origin = layout.Origin(options.Origin);
        var frames = new Vector2[layout.FrameCount][];
        for (var frameIndex = 0; frameIndex < layout.FrameCount; frameIndex++) {
            var mask = layout.SolidMask(image.Rgba.Span, frameIndex, options.AlphaThreshold);
            frames[frameIndex] = FrameHull(mask, origin, options);
        }

        return new SpriteCollider(layout.FrameWidth, origin, frames);
    }

    private static Vector2[] FrameHull(bool[,] mask, Vector2 origin, ColliderOptions options)
    {
        var hull = ConvexHullGenerator.ComputeMaskHull(mask);
        var simplified = HullSimplifier.Simplify(hull, options.Fit, options.Epsilon, options.MaxVertices);
        return [.. simplified.Select(vertex => vertex - origin)];
    }
}
