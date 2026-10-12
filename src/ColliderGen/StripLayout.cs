using System.Numerics;

namespace ColliderGen;

/// <summary>
/// Represents the layout of a sprite strip consisting of frames of equal dimensions arranged horizontally.
/// Provides methods for calculating image properties and accessing frame-specific data.
/// </summary>
public sealed record StripLayout(int FrameWidth, int FrameHeight, int FrameCount)
{
    private int ImageWidth => FrameWidth * FrameCount;

    public static StripLayout Resolve(int imageWidth, int imageHeight, int? requestedFrameWidth)
    {
        var frameWidth = requestedFrameWidth ?? (imageWidth > imageHeight && imageWidth % imageHeight == 0 ? imageHeight : imageWidth);
        if (frameWidth <= 0 || imageWidth % frameWidth != 0) {
            throw new ArgumentException($"Image width {imageWidth} is not a multiple of frame width {frameWidth}.");
        }

        return new StripLayout(frameWidth, imageHeight, imageWidth / frameWidth);
    }

    public Vector2 Origin(ColliderOrigin origin) => origin == ColliderOrigin.TopLeft ? Vector2.Zero : new Vector2(FrameWidth / 2f, FrameHeight / 2f);

    public int PixelOffset(int frameIndex, int x, int y) => (y * ImageWidth + frameIndex * FrameWidth + x) * 4;

    internal bool[,] SolidMask(ReadOnlySpan<byte> rgba, int frameIndex, byte alphaThreshold)
    {
        var mask = new bool[FrameWidth, FrameHeight];
        for (var y = 0; y < FrameHeight; y++) {
            for (var x = 0; x < FrameWidth; x++) {
                mask[x, y] = rgba[PixelOffset(frameIndex, x, y) + 3] >= alphaThreshold;
            }
        }

        return mask;
    }
}
