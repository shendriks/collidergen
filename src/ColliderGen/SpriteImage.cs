namespace ColliderGen;

public sealed class SpriteImage
{
    public SpriteImage(ReadOnlyMemory<byte> rgba, int width, int height)
    {
        if (width <= 0 || height <= 0 || rgba.Length != (long)width * height * 4) {
            throw new ArgumentException($"Expected {width}x{height} RGBA pixels ({(long)width * height * 4} bytes), got {rgba.Length} bytes.", nameof(rgba));
        }

        Rgba = rgba;
        Width = width;
        Height = height;
    }

    public ReadOnlyMemory<byte> Rgba { get; }
    public int Width { get; }
    public int Height { get; }
}
