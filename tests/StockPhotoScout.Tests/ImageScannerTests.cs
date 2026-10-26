using StockPhotoScout.Services;
using Xunit;

namespace StockPhotoScout.Tests;

public sealed class ImageScannerTests
{
    private readonly ImageScanner _scanner = new();

    [Fact]
    public void ScanFolder_FindsJpgAndJpegFiles()
    {
        using var folder = new TemporaryFolder();

        folder.CreateFile("photo1.jpg");
        folder.CreateFile("photo2.jpeg");
        folder.CreateFile("document.png");
        folder.CreateFile("notes.txt");

        var results = _scanner.ScanFolder(folder.Path);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, photo => photo.FileName == "photo1.jpg");
        Assert.Contains(results, photo => photo.FileName == "photo2.jpeg");
    }

    [Fact]
    public void ScanFolder_IgnoresFileExtensionCase()
    {
        using var folder = new TemporaryFolder();

        folder.CreateFile("photo.JPG");
        folder.CreateFile("photo.JPEG");
        folder.CreateFile("photo.png");

        var results = _scanner.ScanFolder(folder.Path);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, photo => photo.FileName == "photo.JPG");
        Assert.Contains(results, photo => photo.FileName == "photo.JPEG");
    }

    [Fact]
    public void ScanFolder_SearchesSubfoldersRecursively()
    {
        using var folder = new TemporaryFolder();

        folder.CreateFile("root.jpg");
        folder.CreateFile(Path.Combine("subfolder", "nested.jpeg"));

        var results = _scanner.ScanFolder(folder.Path);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, photo => photo.FileName == "root.jpg");
        Assert.Contains(results, photo => photo.FileName == "nested.jpeg");
    }

    [Fact]
    public void ScanFolder_ReturnsFilesInCaseInsensitivePathOrder()
    {
        using var folder = new TemporaryFolder();

        folder.CreateFile("zebra.jpg");
        folder.CreateFile("Alpha.jpeg");
        folder.CreateFile("middle.jpg");

        var results = _scanner.ScanFolder(folder.Path);

        var expected = results
            .Select(photo => photo.FilePath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expected, results.Select(photo => photo.FilePath).ToArray());
    }

    [Fact]
    public void ScanFolder_SetsInitialPhotoProperties()
    {
        using var folder = new TemporaryFolder();
        string filePath = folder.CreateFile("photo.jpg");

        var results = _scanner.ScanFolder(folder.Path);

        var photo = Assert.Single(results);

        Assert.Equal(filePath, photo.FilePath);
        Assert.Equal("photo.jpg", photo.FileName);  
        Assert.Equal(0, photo.StockPotential);
        Assert.Equal("Identifiability Unclear", photo.PeopleStatus);
        Assert.Equal("Unclear", photo.ReleaseStatus);
        Assert.Equal("Not Analyzed Yet", photo.Recommendation);
        Assert.False(photo.IsAnalyzed);
        Assert.False(photo.HasPeople);
    }

    [Fact]
    public void ScanFolder_DamagedImageDoesNotStopScan()
    {
        using var folder = new TemporaryFolder();

        folder.CreateFile("damaged.jpg", "not a valid image");
        folder.CreateFile("another-damaged.jpeg", "also not a valid image");

        var results = _scanner.ScanFolder(folder.Path);

        Assert.Equal(2, results.Count);
        Assert.All(results, photo => Assert.Null(photo.Thumbnail));
    }

    [Fact]
    public void ScanFolder_EmptyFolderReturnsEmptyList()
    {
        using var folder = new TemporaryFolder();

        var results = _scanner.ScanFolder(folder.Path);

        Assert.Empty(results);
    }

    [Fact]
    public void ScanFolder_MissingFolderThrowsDirectoryNotFoundException()
    {
        using var folder = new TemporaryFolder();
        string missingFolder = System.IO.Path.Combine(folder.Path, "missing");

        Assert.Throws<DirectoryNotFoundException>(
            () => _scanner.ScanFolder(missingFolder));
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "StockPhotoScoutTests",
            Guid.NewGuid().ToString("N"));

        public TemporaryFolder()
        {
            Directory.CreateDirectory(Path);
        }

        public string CreateFile(string relativePath, string content = "")
        {
            string fullPath = System.IO.Path.Combine(Path, relativePath);
            string? parent = System.IO.Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
