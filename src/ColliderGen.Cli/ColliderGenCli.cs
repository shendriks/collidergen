namespace ColliderGen.Cli;

internal sealed class ColliderGenCli(TextWriter output, TextWriter error, IFileSystem fileSystem)
{
    public int Run(string[] args)
    {
        if (args.Length < 1) {
            error.WriteLine(CliOptions.Usage);
            return 1;
        }

        if (CliOptions.TryParse(args, out var options, out var parseError)) {
            return options.AllDirectory is null ? RunSingle(options) : RunAll(options.AllDirectory, options);
        }

        error.WriteLine(parseError);
        error.WriteLine(CliOptions.Usage);
        return 1;
    }

    private int RunSingle(CliOptions options) => TryProcess(options.InputPath!, options.OutputPath, options) ? 0 : 1;

    private int RunAll(string directory, CliOptions options)
    {
        if (!fileSystem.DirectoryExists(directory)) {
            error.WriteLine($"Directory not found: {directory}");
            return 1;
        }

        var pngPaths = fileSystem.EnumerateFilesRecursively(directory)
            .Where(path => Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase) && !ColliderFile.IsDebugOverlay(path))
            .ToArray();
        var processedCount = pngPaths.Count(path => TryProcess(path, null, options));

        output.WriteLine($"Processed {processedCount} sprites in {directory}");
        return processedCount == pngPaths.Length ? 0 : 1;
    }

    private bool TryProcess(string spritePath, string? jsonPath, CliOptions options)
    {
        try {
            var processed = ColliderFile.Process(fileSystem, spritePath, jsonPath, options.DebugDraw, options.Collider);
            var hulls = processed.Collider.Frames;
            output.WriteLine($"Wrote {processed.Collider.FrameCount} frame(s) ({string.Join(", ", hulls.Select(hull => hull.Count))} vertices) to {processed.JsonPath}");
            if (processed.DebugPath is not null) {
                output.WriteLine($"Wrote debug overlay to {processed.DebugPath}");
            }

            return true;
        } catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException) {
            error.WriteLine($"{spritePath}: {exception.Message}");
            return false;
        }
    }
}
