using System.Linq;
using System.Numerics;
using NUnit.Framework;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(StripLayout))]
public class StripLayoutTests
{
    private static byte[] SolidRgba(int width, int height, byte alpha)
    {
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++) {
            rgba[i * 4 + 3] = alpha;
        }

        return rgba;
    }

    private static bool[,] SingleMask(byte[] rgba, int width, int height, byte alphaThreshold)
    {
        return new StripLayout(width, height, FrameCount: 1).SolidMask(rgba, frameIndex: 0, alphaThreshold);
    }

    [Test]
    public void Extract_FullyTransparent_MaskIsAllFalse()
    {
        var rgba = SolidRgba(4, 4, alpha: 0);

        var mask = SingleMask(rgba, 4, 4, alphaThreshold: 128);

        for (var y = 0; y < 4; y++) {
            for (var x = 0; x < 4; x++) {
                Assert.That(mask[x, y], Is.False);
            }
        }
    }

    [Test]
    public void Extract_FullyOpaque_MaskIsAllTrue()
    {
        var rgba = SolidRgba(4, 4, alpha: 255);

        var mask = SingleMask(rgba, 4, 4, alphaThreshold: 128);

        for (var y = 0; y < 4; y++) {
            for (var x = 0; x < 4; x++) {
                Assert.That(mask[x, y], Is.True);
            }
        }
    }

    [Test]
    public void Extract_SingleOpaquePixel_OnlyThatPixelIsTrue()
    {
        var rgba = SolidRgba(3, 3, alpha: 0);
        rgba[(1 * 3 + 1) * 4 + 3] = 255;

        var mask = SingleMask(rgba, 3, 3, alphaThreshold: 128);

        Assert.That(mask[1, 1], Is.True);
        Assert.That(mask[0, 0], Is.False);
        Assert.That(mask[2, 2], Is.False);
    }

    [Test]
    public void Extract_ZeroThreshold_MaskIsAllTrue()
    {
        var rgba = SolidRgba(3, 3, alpha: 0);

        var mask = SingleMask(rgba, 3, 3, alphaThreshold: 0);

        for (var y = 0; y < 3; y++) {
            for (var x = 0; x < 3; x++) {
                Assert.That(mask[x, y], Is.True);
            }
        }
    }

    [Test]
    public void Extract_MaxThreshold_OnlyFullyOpaquePixelsAreTrue()
    {
        var rgba = SolidRgba(2, 2, alpha: 254);
        rgba[(0 * 2 + 0) * 4 + 3] = 255;

        var mask = SingleMask(rgba, 2, 2, alphaThreshold: 255);

        Assert.That(mask[0, 0], Is.True);
        Assert.That(mask[1, 0], Is.False);
        Assert.That(mask[0, 1], Is.False);
        Assert.That(mask[1, 1], Is.False);
    }

    [Test]
    public void Extract_PixelExactlyAtThreshold_IsInclusive()
    {
        var rgba = SolidRgba(2, 2, alpha: 0);
        rgba[(0 * 2 + 1) * 4 + 3] = 128;

        var mask = SingleMask(rgba, 2, 2, alphaThreshold: 128);

        Assert.That(mask[1, 0], Is.True);
    }

    [Test]
    public void Extract_NonSquareImage_UsesRowMajorIndexing()
    {
        var rgba = SolidRgba(4, 2, alpha: 0);
        rgba[(1 * 4 + 3) * 4 + 3] = 255;

        var mask = SingleMask(rgba, 4, 2, alphaThreshold: 128);

        Assert.That(mask[3, 1], Is.True);
        Assert.That(mask[1, 0], Is.False);
        Assert.That(mask.GetLength(0), Is.EqualTo(4));
        Assert.That(mask.GetLength(1), Is.EqualTo(2));
    }

    [Test]
    public void SolidMask_HorizontalStrip_SlicesTheRequestedFrame()
    {
        var rgba = SolidRgba(6, 2, alpha: 0);
        rgba[(1 * 6 + 4) * 4 + 3] = 255;
        var layout = new StripLayout(FrameWidth: 3, FrameHeight: 2, FrameCount: 2);

        var first = layout.SolidMask(rgba, frameIndex: 0, alphaThreshold: 128);
        var second = layout.SolidMask(rgba, frameIndex: 1, alphaThreshold: 128);

        Assert.That(first.GetLength(0), Is.EqualTo(3));
        Assert.That(first.Cast<bool>(), Has.None.True);
        Assert.That(second[1, 1], Is.True);
        Assert.That(second.Cast<bool>().Count(opaque => opaque), Is.EqualTo(1));
    }

    [Test]
    public void PixelOffset_SecondFrame_SkipsTheFirstFramesColumns()
    {
        var layout = new StripLayout(FrameWidth: 3, FrameHeight: 2, FrameCount: 2);

        Assert.That(layout.PixelOffset(frameIndex: 1, x: 1, y: 1), Is.EqualTo((1 * 6 + 4) * 4));
    }

    [TestCase(180, 36, ExpectedResult = 5)]
    [TestCase(36, 36, ExpectedResult = 1)]
    [TestCase(40, 36, ExpectedResult = 1)]
    public int Resolve_NoRequestedWidth_CountsSquareFrames(int imageWidth, int imageHeight) =>
        StripLayout.Resolve(imageWidth, imageHeight, requestedFrameWidth: null).FrameCount;

    [Test]
    public void Resolve_WidthNotAMultipleOfRequestedWidth_Throws()
    {
        Assert.That(() => StripLayout.Resolve(180, 36, requestedFrameWidth: 50), Throws.ArgumentException);
    }

    [TestCase(ColliderOrigin.Center, 3f, 1f)]
    [TestCase(ColliderOrigin.TopLeft, 0f, 0f)]
    public void Origin_IsMeasuredWithinOneFrame(ColliderOrigin origin, float expectedX, float expectedY)
    {
        var layout = new StripLayout(FrameWidth: 6, FrameHeight: 2, FrameCount: 3);

        Assert.That(layout.Origin(origin), Is.EqualTo(new Vector2(expectedX, expectedY)));
    }
}
