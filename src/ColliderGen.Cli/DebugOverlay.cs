using System.Numerics;

namespace ColliderGen.Cli;

internal static class DebugOverlay
{
    private const int DefaultScale = 4;

    internal static SpriteImage Render(SpriteImage sprite, SpriteCollider collider, int scale = DefaultScale)
    {
        var canvas = new Canvas(collider.FrameCount * CellWidth(collider.FrameWidth, scale), sprite.Height * scale + 1);
        CopySprite(canvas, sprite, StripLayout.Resolve(sprite.Width, sprite.Height, collider.FrameWidth), scale);
        DrawHulls(canvas, collider, scale);
        return new SpriteImage(canvas.Pixels, canvas.Width, canvas.Height);
    }

    private static int CellWidth(int frameWidth, int scale) => frameWidth * scale + 1;

    private static void CopySprite(Canvas canvas, SpriteImage sprite, StripLayout layout, int scale)
    {
        var rgba = sprite.Rgba.Span;
        var cellWidth = CellWidth(layout.FrameWidth, scale);

        for (var frameIndex = 0; frameIndex < layout.FrameCount; frameIndex++) {
            for (var y = 0; y < layout.FrameHeight * scale; y++) {
                for (var x = 0; x < layout.FrameWidth * scale; x++) {
                    var sourceOffset = layout.PixelOffset(frameIndex, x / scale, y / scale);
                    var destinationOffset = (y * canvas.Width + frameIndex * cellWidth + x) * 4;
                    rgba.Slice(sourceOffset, 4).CopyTo(canvas.Pixels.AsSpan(destinationOffset, 4));
                }
            }
        }
    }

    private static void DrawHulls(Canvas canvas, SpriteCollider collider, int scale)
    {
        for (var frameIndex = 0; frameIndex < collider.FrameCount; frameIndex++) {
            var vertices = collider.Frames[frameIndex];
            var frameOffset = new Vector2(frameIndex * CellWidth(collider.FrameWidth, scale), 0);
            for (var i = 0; i < vertices.Count; i++) {
                var from = (vertices[i] + collider.Origin) * scale + frameOffset;
                var to = (vertices[(i + 1) % vertices.Count] + collider.Origin) * scale + frameOffset;
                canvas.DrawLine(from, to, 255, 0, 0);
            }

            foreach (var vertex in vertices) {
                var point = (vertex + collider.Origin) * scale + frameOffset;
                canvas.PlotPixel((int)MathF.Round(point.X), (int)MathF.Round(point.Y), 255, 255, 0);
            }
        }
    }

    private sealed class Canvas(int width, int height)
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public byte[] Pixels { get; } = new byte[width * height * 4];

        public void DrawLine(Vector2 from, Vector2 to, byte r, byte g, byte b)
        {
            var x0 = (int)MathF.Round(from.X);
            var y0 = (int)MathF.Round(from.Y);
            var x1 = (int)MathF.Round(to.X);
            var y1 = (int)MathF.Round(to.Y);

            var dx = Math.Abs(x1 - x0);
            var dy = -Math.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;

            while (true) {
                PlotPixel(x0, y0, r, g, b);
                if (x0 == x1 && y0 == y1) {
                    break;
                }

                var doubledError = 2 * error;
                if (doubledError >= dy) {
                    error += dy;
                    x0 += sx;
                }

                if (doubledError > dx) {
                    continue;
                }

                error += dx;
                y0 += sy;
            }
        }

        public void PlotPixel(int x, int y, byte r, byte g, byte b)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) {
                return;
            }

            var offset = (y * Width + x) * 4;
            Pixels[offset] = r;
            Pixels[offset + 1] = g;
            Pixels[offset + 2] = b;
            Pixels[offset + 3] = 255;
        }
    }
}
