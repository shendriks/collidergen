using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColliderGen.Cli;

namespace ColliderGen.Tests;

internal sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public void Write(string path, byte[] contents) => _files[path] = contents;

    public byte[] Read(string path) => _files[path];

    public bool Exists(string path) => _files.ContainsKey(path);

    public bool DirectoryExists(string path) => _files.Keys.Any(file => IsUnder(file, path));

    public IEnumerable<string> EnumerateFilesRecursively(string directory) => [.. _files.Keys.Where(file => IsUnder(file, directory))];

    public Stream OpenRead(string path) =>
        _files.TryGetValue(path, out var contents)
            ? new MemoryStream(contents, writable: false)
            : throw new FileNotFoundException($"Could not find file '{path}'.", path);

    public Stream Create(string path) => new CommittingStream(contents => _files[path] = contents);

    private static bool IsUnder(string file, string directory) =>
        file.StartsWith(Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private sealed class CommittingStream(Action<byte[]> commit) : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                commit(ToArray());
            }

            base.Dispose(disposing);
        }
    }
}
