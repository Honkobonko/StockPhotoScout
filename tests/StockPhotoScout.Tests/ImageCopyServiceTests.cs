using StockPhotoScout.Services;
using StockPhotoScout.Models;
using Xunit;

namespace StockPhotoScout.Tests;

public sealed class ImageCopyServiceTests
{
    private readonly ImageCopyService _service = new();

    [Fact]
    public void CopyFiles_CopiesFileToDestination()
    {
        using var folder = new TemporaryFolder();
        string sourcePath = folder.CreateFile("source/photo.jpg", "image data");
        string destination = folder.CreateDirectory("destination");

        var photos = new[]
        {
            CreatePhoto(sourcePath, "photo.jpg")
        };

        CopyResult result = _service.CopyFiles(photos, destination);

        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(
            "image data",
            File.ReadAllText(Path.Combine(destination, "photo.jpg")));
    }

    [Fact]
    public void CopyFiles_DoesNotOverwriteExistingFile()
    {
        using var folder = new TemporaryFolder();
        string sourcePath = folder.CreateFile("source/photo.jpg", "new data");
        string destination = folder.CreateDirectory("destination");
        folder.CreateFile("destination/photo.jpg", "existing data");

        var photos = new[]
        {
            CreatePhoto(sourcePath, "photo.jpg")
        };

        CopyResult result = _service.CopyFiles(photos, destination);

        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(
            "existing data",
            File.ReadAllText(Path.Combine(destination, "photo.jpg")));
        Assert.Equal(
            "new data",
            File.ReadAllText(Path.Combine(destination, "photo_2.jpg")));
    }

    [Fact]
    public void CopyFiles_UsesNextAvailableUniqueFileName()
    {
        using var folder = new TemporaryFolder();
        string sourcePath = folder.CreateFile("source/photo.jpg", "new data");
        string destination = folder.CreateDirectory("destination");

        folder.CreateFile("destination/photo.jpg", "original");
        folder.CreateFile("destination/photo_2.jpg", "second");

        var photos = new[]
        {
            CreatePhoto(sourcePath, "photo.jpg")
        };

        CopyResult result = _service.CopyFiles(photos, destination);

        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(
            "new data",
            File.ReadAllText(Path.Combine(destination, "photo_3.jpg")));
    }

    [Fact]
    public void CopyFiles_CopiesMultipleFiles()
    {
        using var folder = new TemporaryFolder();

        string firstSource = folder.CreateFile("source/first.jpg", "first");
        string secondSource = folder.CreateFile("source/second.jpeg", "second");
        string destination = folder.CreateDirectory("destination");

        var photos = new[]
        {
            CreatePhoto(firstSource, "first.jpg"),
            CreatePhoto(secondSource, "second.jpeg")
        };

        CopyResult result = _service.CopyFiles(photos, destination);

        Assert.Equal(2, result.CopiedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal("first", File.ReadAllText(Path.Combine(destination, "first.jpg")));
        Assert.Equal("second", File.ReadAllText(Path.Combine(destination, "second.jpeg")));
    }

    [Fact]
    public void CopyFiles_ContinuesWhenOneSourceFileIsMissing()
    {
        using var folder = new TemporaryFolder();

        string validSource = folder.CreateFile("source/valid.jpg", "valid data");
        string missingSource = Path.Combine(folder.Path, "source", "missing.jpg");
        string destination = folder.CreateDirectory("destination");

        var photos = new[]
        {
            CreatePhoto(missingSource, "missing.jpg"),
            CreatePhoto(validSource, "valid.jpg")
        };

        CopyResult result = _service.CopyFiles(photos, destination);

        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.True(File.Exists(Path.Combine(destination, "valid.jpg")));
        Assert.False(File.Exists(Path.Combine(destination, "missing.jpg")));
    }

    [Fact]
    public void CopyFiles_EmptyCollectionReturnsZeroCounts()
    {
        using var folder = new TemporaryFolder();
        string destination = folder.CreateDirectory("destination");

        CopyResult result = _service.CopyFiles(
            Array.Empty<PhotoItem>(),
            destination);

        Assert.Equal(0, result.CopiedCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public void CopyFiles_MissingDestinationCountsFilesAsFailed()
    {
        using var folder = new TemporaryFolder();

        string sourcePath = folder.CreateFile("source/photo.jpg", "image data");
        string missingDestination = Path.Combine(folder.Path, "does-not-exist");

        var photos = new[]
        {
            CreatePhoto(sourcePath, "photo.jpg")
        };

        CopyResult result = _service.CopyFiles(photos, missingDestination);

        Assert.Equal(0, result.CopiedCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public void CopyFiles_PreservesExtensionWhenGeneratingUniqueName()
    {
        using var folder = new TemporaryFolder();

        string sourcePath = folder.CreateFile("source/image.jpeg", "image data");
        string destination = folder.CreateDirectory("destination");
        folder.CreateFile("destination/image.jpeg", "existing");

        var photos = new[]
        {
            CreatePhoto(sourcePath, "image.jpeg")
        };

        CopyResult result = _service.CopyFiles(photos, destination);

        Assert.Equal(1, result.CopiedCount);
        Assert.True(File.Exists(Path.Combine(destination, "image_2.jpeg")));
    }

    private static PhotoItem CreatePhoto(string filePath, string fileName)
    {
        return new PhotoItem
        {
            FilePath = filePath,
            FileName = fileName
        };
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

        public string CreateDirectory(string relativePath)
        {
            string fullPath = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(fullPath);
            return fullPath;
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
