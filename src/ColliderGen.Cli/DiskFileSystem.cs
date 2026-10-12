namespace ColliderGen.Cli;

internal sealed class DiskFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> EnumerateFilesRecursively(string directory) =>
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);

    public Stream OpenRead(string path) => File.OpenRead(path);

    public Stream Create(string path) => File.Create(path);
}
