using System.Linq;
using System.Numerics;
using ColliderGen.Cli;
using NUnit.Framework;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(DebugOverlay))]
public class DebugOverlayTests
{
    private const int Width = 8;
    private const int Height = 6;

    private static SpriteImage Transparent(int width, int height) => new(new byte[width * height * 4], width, height);

    private static SpriteImage Render(int width, int height, int frameWidth, Vector2 origin, int scale, params Vector2[][] hulls)
    {
        var collider = new SpriteCollider(frameWidth, origin, hulls);
        return DebugOverlay.Render(Transparent(width, height), collider, scale);
    }

    private static SpriteImage RenderHull(params Vector2[] hull) => Render(Width, Height, Width, Vector2.Zero, 1, hull);

    private static byte[] PixelAt(SpriteImage image, int x, int y)
    {
        var offset = (y * image.Width + x) * 4;
        return image.Rgba.Span.Slice(offset, 4).ToArray();
    }

    private static bool IsSet(SpriteImage image, int x, int y) => PixelAt(image, x, y)[3] == 255;

    private static int SetPixelCount(SpriteImage image)
    {
        var count = 0;
        var rgba = image.Rgba.Span;
        for (var i = 3; i < rgba.Length; i += 4) {
            if (rgba[i] == 255) {
                count++;
            }
        }

        return count;
    }

    private static byte[] Pixel(byte r, byte g, byte b) => [r, g, b, 255];

    private static byte[] Pixels(params byte[][] pixels) => [.. pixels.SelectMany(pixel => pixel)];

    [Test]
    public void Render_DrawsRedEdgesWithYellowVertices()
    {
        var overlay = Render(6, 6, 6, Vector2.Zero, 1, [new Vector2(1, 1), new Vector2(4, 1), new Vector2(4, 4), new Vector2(1, 4)]);

        Assert.That(PixelAt(overlay, 1, 1), Is.EqualTo(Pixel(255, 255, 0)), "a hull vertex is yellow");
        Assert.That(PixelAt(overlay, 2, 1), Is.EqualTo(Pixel(255, 0, 0)), "a hull edge is red");
        Assert.That(IsSet(overlay, 2, 2), Is.False, "the hull interior stays untouched");
    }

    [Test]
    public void Render_OffsetsEachFrameByItsCellWidth()
    {
        var overlay = Render(6, 2, 3, Vector2.Zero, 1, [new Vector2(0, 0)], [new Vector2(0, 0)]);

        Assert.That(IsSet(overlay, 0, 0), Is.True);
        Assert.That(IsSet(overlay, 4, 0), Is.True, "the second frame starts one padding pixel after the first");
    }

    [Test]
    public void Render_Scaled_PlacesPointsOnPixelEdgesAndOffsetsFramesByScaledCellWidth()
    {
        var overlay = Render(6, 3, 3, new Vector2(1.5f, 1.5f), 2, [new Vector2(-0.5f, -0.5f)], [new Vector2(-0.5f, -0.5f)]);

        Assert.That(PixelAt(overlay, 2, 2), Is.EqualTo(Pixel(255, 255, 0)));
        Assert.That(PixelAt(overlay, 9, 2), Is.EqualTo(Pixel(255, 255, 0)), "the second frame starts at 3 * 2 + 1");
    }

    [Test]
    public void Render_HorizontalEdge_SetsEveryPixelBetweenEndpointsInclusive()
    {
        var overlay = RenderHull(new Vector2(1, 3), new Vector2(5, 3));

        for (var x = 1; x <= 5; x++) {
            Assert.That(IsSet(overlay, x, 3), Is.True, $"expected ({x}, 3) set");
        }

        Assert.That(SetPixelCount(overlay), Is.EqualTo(5));
    }

    [Test]
    public void Render_VerticalEdge_SetsEveryPixelBetweenEndpointsInclusive()
    {
        var overlay = RenderHull(new Vector2(2, 0), new Vector2(2, 4));

        for (var y = 0; y <= 4; y++) {
            Assert.That(IsSet(overlay, 2, y), Is.True);
        }

        Assert.That(SetPixelCount(overlay), Is.EqualTo(5));
    }

    [Test]
    public void Render_DiagonalEdge_SetsExpectedStaircase()
    {
        var overlay = RenderHull(new Vector2(0, 0), new Vector2(4, 4));

        for (var i = 0; i <= 4; i++) {
            Assert.That(IsSet(overlay, i, i), Is.True);
        }

        Assert.That(SetPixelCount(overlay), Is.EqualTo(5));
    }

    [Test]
    public void Render_ReversedVertexOrder_SetsSamePixels()
    {
        var forward = RenderHull(new Vector2(1, 1), new Vector2(5, 3));
        var backward = RenderHull(new Vector2(5, 3), new Vector2(1, 1));

        Assert.That(SetPixelCount(backward), Is.EqualTo(SetPixelCount(forward)));
        Assert.That(IsSet(backward, 1, 1) && IsSet(backward, 5, 3), Is.True);
    }

    [Test]
    public void Render_SingleVertex_SetsExactlyOnePixel()
    {
        var overlay = RenderHull(new Vector2(3, 3));

        Assert.That(SetPixelCount(overlay), Is.EqualTo(1));
        Assert.That(IsSet(overlay, 3, 3), Is.True);
    }

    [Test]
    public void Render_EmptyHull_DrawsNothing()
    {
        Assert.That(SetPixelCount(RenderHull()), Is.Zero);
    }

    [Test]
    public void Render_EdgeFullyOutsideCanvas_SetsNothing()
    {
        Assert.That(SetPixelCount(RenderHull(new Vector2(-5, -5), new Vector2(-1, -1))), Is.Zero);
    }

    [Test]
    public void Render_EdgePartiallyOutsideCanvas_SetsOnlyInBoundsPixels()
    {
        var overlay = RenderHull(new Vector2(-2, 2), new Vector2(3, 2));

        for (var x = 0; x <= 3; x++) {
            Assert.That(IsSet(overlay, x, 2), Is.True);
        }

        Assert.That(SetPixelCount(overlay), Is.EqualTo(4));
    }

    [Test]
    public void Render_SingleFrame_AddsOneTransparentColumnAndRow()
    {
        var sprite = new SpriteImage(Pixels(Pixel(1, 1, 1), Pixel(2, 2, 2), Pixel(3, 3, 3), Pixel(4, 4, 4)), 2, 2);

        var overlay = DebugOverlay.Render(sprite, new SpriteCollider(2, Vector2.Zero, [[]]), scale: 1);

        Assert.That((overlay.Width, overlay.Height), Is.EqualTo((3, 3)));
        Assert.That(PixelAt(overlay, 1, 1), Is.EqualTo(Pixel(4, 4, 4)));
        Assert.That(PixelAt(overlay, 2, 0)[3], Is.Zero);
        Assert.That(PixelAt(overlay, 0, 2)[3], Is.Zero);
    }

    [Test]
    public void Render_Strip_PlacesEachFrameInItsOwnCellWithGap()
    {
        var sprite = new SpriteImage(Pixels(
            Pixel(1, 0, 0), Pixel(1, 0, 0), Pixel(2, 0, 0), Pixel(2, 0, 0),
            Pixel(1, 0, 0), Pixel(1, 0, 0), Pixel(2, 0, 0), Pixel(2, 0, 0)), 4, 2);

        var overlay = DebugOverlay.Render(sprite, new SpriteCollider(2, Vector2.Zero, [[], []]), scale: 1);

        Assert.That(overlay.Width, Is.EqualTo(6));
        Assert.That(PixelAt(overlay, 1, 1), Is.EqualTo(Pixel(1, 0, 0)));
        Assert.That(PixelAt(overlay, 2, 0)[3], Is.Zero);
        Assert.That(PixelAt(overlay, 3, 0), Is.EqualTo(Pixel(2, 0, 0)));
        Assert.That(PixelAt(overlay, 4, 1), Is.EqualTo(Pixel(2, 0, 0)));
        Assert.That(PixelAt(overlay, 5, 0)[3], Is.Zero);
    }

    [Test]
    public void Render_Scaled_ReplicatesEachSpritePixelIntoABlock()
    {
        var sprite = new SpriteImage(Pixels(Pixel(1, 1, 1), Pixel(2, 2, 2)), 2, 1);

        var overlay = DebugOverlay.Render(sprite, new SpriteCollider(2, Vector2.Zero, [[]]), scale: 3);

        Assert.That((overlay.Width, overlay.Height), Is.EqualTo((7, 4)));
        Assert.That(PixelAt(overlay, 2, 2), Is.EqualTo(Pixel(1, 1, 1)));
        Assert.That(PixelAt(overlay, 3, 0), Is.EqualTo(Pixel(2, 2, 2)));
        Assert.That(PixelAt(overlay, 6, 0)[3], Is.Zero);
        Assert.That(PixelAt(overlay, 0, 3)[3], Is.Zero);
    }
}
