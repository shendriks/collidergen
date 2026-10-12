using System.Text.Json;
using StbImageSharp;
using StbImageWriteSharp;
using ColorComponents = StbImageSharp.ColorComponents;

namespace ColliderGen.Cli;

internal sealed record ProcessedSprite(SpriteCollider Collider, string JsonPath, string? DebugPath);

internal static class ColliderFile
{
    private const string DebugInfix = ".collider-debug";

    internal static bool IsDebugOverlay(string path) =>
        Path.GetFileNameWithoutExtension(path).EndsWith(DebugInfix, StringComparison.Ordinal);

    internal static ProcessedSprite Process(IFileSystem fileSystem, string spritePath, string? jsonPath, bool writeDebugOverlay, ColliderOptions options)
    {
        var sprite = ReadSprite(fileSystem, spritePath);
        var collider = SpriteColliderGenerator.Generate(sprite, options);

        jsonPath ??= Path.ChangeExtension(spritePath, ".collider.json");
        using (var jsonStream = fileSystem.Create(jsonPath)) {
            JsonSerializer.Serialize(jsonStream, ColliderDto.From(collider));
        }

        var debugPath = writeDebugOverlay ? WriteDebugOverlay(fileSystem, spritePath, sprite, collider) : null;
        return new ProcessedSprite(collider, jsonPath, debugPath);
    }

    private static SpriteImage ReadSprite(IFileSystem fileSystem, string spritePath)
    {
        using var stream = fileSystem.OpenRead(spritePath);
        var decoded = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        return new SpriteImage(decoded.Data, decoded.Width, decoded.Height);
    }

    private static string WriteDebugOverlay(IFileSystem fileSystem, string spritePath, SpriteImage sprite, SpriteCollider collider)
    {
        var debugPath = Path.Combine(
            Path.GetDirectoryName(spritePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(spritePath) + DebugInfix + ".png");
        var overlay = DebugOverlay.Render(sprite, collider);

        using var outputStream = fileSystem.Create(debugPath);
        new ImageWriter().WritePng(overlay.Rgba.ToArray(), overlay.Width, overlay.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, outputStream);
        return debugPath;
    }
}
