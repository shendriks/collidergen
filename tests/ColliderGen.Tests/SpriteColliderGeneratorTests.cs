using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(SpriteColliderGenerator))]
public class SpriteColliderGeneratorTests
{
    private static byte[] Rgba(int width, int height, Func<int, int, bool> isSolid)
    {
        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                if (isSolid(x, y)) {
                    rgba[(y * width + x) * 4 + 3] = 255;
                }
            }
        }

        return rgba;
    }

    private static byte[] Solid(int width, int height) => Rgba(width, height, (_, _) => true);

    private static ColliderOptions Top(float epsilon = 0f, int maxVertices = ColliderOptions.MaxVerticesLimit) =>
        new() { Origin = ColliderOrigin.TopLeft, Epsilon = epsilon, MaxVertices = maxVertices };

    private static SpriteCollider Generate(byte[] rgba, int width, int height, ColliderOptions options)
    {
        return SpriteColliderGenerator.Generate(new SpriteImage(rgba, width, height), options);
    }

    [Test]
    public void Generate_SolidSquare_ProducesFourVertexHullAroundCenterOrigin()
    {
        var collider = Generate(Solid(8, 8), 8, 8, new ColliderOptions { Epsilon = 0.5f });

        var hull = collider.Frames.Single();
        Assert.That(hull, Has.Count.EqualTo(4));
        Assert.That(hull, Has.All.Matches<Vector2>(v => v.X is >= -4f and <= 4f && v.Y is >= -4f and <= 4f));
    }

    [Test]
    public void Generate_ResultIsWithinVertexLimits()
    {
        var collider = Generate(Solid(8, 8), 8, 8, Top(epsilon: 0.5f));

        Assert.That(collider.Frames.Single(), Has.Count.InRange(ColliderOptions.MinVerticesLimit, ColliderOptions.MaxVerticesLimit));
    }

    [Test]
    public void Generate_EmptyFrame_ReturnsNoVertices()
    {
        var collider = Generate(new byte[8 * 8 * 4], 8, 8, Top());

        Assert.That(collider.Frames.Single(), Is.Empty);
    }

    [Test]
    public void Generate_SinglePixel_ReturnsThatPixelsSquareInLocalSpace()
    {
        var rgba = Rgba(8, 8, (x, y) => x == 3 && y == 3);

        var collider = Generate(rgba, 8, 8, new ColliderOptions());

        Assert.That(collider.Frames.Single(), Is.EquivalentTo(new[] { new Vector2(-1, -1), new Vector2(0, -1), new Vector2(0, 0), new Vector2(-1, 0) }));
    }

    [Test]
    public void Generate_ThinLine_ReturnsItsOnePixelTallRectangle()
    {
        var rgba = Rgba(8, 8, (x, y) => y == 3 && x is >= 1 and <= 5);

        var collider = Generate(rgba, 8, 8, Top());

        Assert.That(collider.Frames.Single(), Is.EquivalentTo(new[] { new Vector2(1, 3), new Vector2(6, 3), new Vector2(6, 4), new Vector2(1, 4) }));
    }

    [Test]
    public void Generate_FullFrame_HullReachesFrameEdges()
    {
        var collider = Generate(Solid(8, 8), 8, 8, new ColliderOptions());

        Assert.That(collider.Frames.Single(), Is.EquivalentTo(new[] { new Vector2(-4, -4), new Vector2(4, -4), new Vector2(4, 4), new Vector2(-4, 4) }));
    }

    [Test]
    public void Generate_CenterVersusTopLeft_ShiftsEveryVertexByTheFrameCenter()
    {
        var rgba = Rgba(8, 8, (x, y) => x is >= 1 and < 7 && y is >= 1 and < 7);

        var topLeft = Generate(rgba, 8, 8, Top()).Frames.Single();
        var centered = Generate(rgba, 8, 8, new ColliderOptions()).Frames.Single();

        Assert.That(centered, Has.Count.EqualTo(topLeft.Count));
        for (var i = 0; i < topLeft.Count; i++) {
            Assert.That(topLeft[i] - centered[i], Is.EqualTo(new Vector2(4, 4)));
        }
    }

    [Test]
    public void Generate_MaxVerticesBelowNaturalHull_CapsAndStaysValid()
    {
        var rgba = Rgba(16, 16, (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(8, 8)) <= 6f);

        var collider = Generate(rgba, 16, 16, new ColliderOptions { MaxVertices = 6 });

        Assert.That(collider.Frames.Single(), Has.Count.InRange(ColliderOptions.MinVerticesLimit, 6));
    }

    [TestCase(ColliderOptions.MinVerticesLimit - 1)]
    [TestCase(ColliderOptions.MaxVerticesLimit + 1)]
    public void Generate_MaxVerticesOutsideVertexLimits_Throws(int maxVertices)
    {
        Assert.That(
            () => Generate(Solid(4, 4), 4, 4, new ColliderOptions { MaxVertices = maxVertices }),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    public void Generate_InvalidEpsilon_Throws(float epsilon)
    {
        Assert.That(
            () => Generate(Solid(4, 4), 4, 4, new ColliderOptions { Epsilon = epsilon }),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [TestCase(0)]
    [TestCase(-4)]
    public void Generate_NonPositiveFrameWidth_Throws(int frameWidth)
    {
        Assert.That(
            () => Generate(Solid(4, 4), 4, 4, new ColliderOptions { FrameWidth = frameWidth }),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void SpriteImage_PixelBufferOfWrongSize_Throws()
    {
        Assert.That(() => new SpriteImage(new byte[10], 4, 4), Throws.ArgumentException);
    }

    [Test]
    public void Generate_Strip_ProducesOneHullPerFrameWithSharedGeometry()
    {
        var collider = Generate(Solid(18, 6), 18, 6, new ColliderOptions());

        Assert.That(collider.Frames, Has.Count.EqualTo(3));
        Assert.That(collider.FrameWidth, Is.EqualTo(6));
        Assert.That(collider.Origin, Is.EqualTo(new Vector2(3, 3)));
        Assert.That(collider.Frames, Has.All.Count.EqualTo(4));
    }

    [TestCase(180, 36, ExpectedResult = 36)]
    [TestCase(36, 36, ExpectedResult = 36)]
    [TestCase(40, 36, ExpectedResult = 40)]
    [TestCase(36, 72, ExpectedResult = 36)]
    public int FrameWidth_NotSpecified_AutoDetectsSquareFrameStrips(int imageWidth, int imageHeight) =>
        Generate(Solid(imageWidth, imageHeight), imageWidth, imageHeight, new ColliderOptions()).FrameWidth;

    [Test]
    public void FrameWidth_Specified_OverridesAutoDetection()
    {
        var collider = Generate(Solid(180, 36), 180, 36, new ColliderOptions { FrameWidth = 90 });

        Assert.That(collider.FrameWidth, Is.EqualTo(90));
        Assert.That(collider.Frames, Has.Count.EqualTo(2));
    }

    [Test]
    public void FrameWidth_WidthNotAMultipleOfFrameWidth_Throws()
    {
        Assert.That(
            () => Generate(Solid(180, 36), 180, 36, new ColliderOptions { FrameWidth = 50 }),
            Throws.ArgumentException);
    }

    [TestCase(4)]
    [TestCase(6)]
    [TestCase(ColliderOptions.MaxVerticesLimit)]
    public void Generate_OuterFit_EnclosesEveryVertexOfTheUnsimplifiedHull(int maxVertices)
    {
        var disc = Rgba(20, 20, (x, y) => (x - 9.5f) * (x - 9.5f) + (y - 9.5f) * (y - 9.5f) <= 81f);
        var hull = Generate(disc, 20, 20, Top()).Frames.Single();

        var outer = Generate(disc, 20, 20, Top(maxVertices: maxVertices) with { Fit = ColliderFit.Outer }).Frames.Single();

        Assert.That(outer, Has.Count.LessThanOrEqualTo(maxVertices));
        Assert.That(hull, Has.All.Matches<Vector2>(vertex => ConvexPolygon.Contains(outer, vertex)));
    }
}
