namespace ColliderGen.Cli;

internal interface IFileSystem
{
    bool DirectoryExists(string path);

    IEnumerable<string> EnumerateFilesRecursively(string directory);

    Stream OpenRead(string path);

    Stream Create(string path);
}
