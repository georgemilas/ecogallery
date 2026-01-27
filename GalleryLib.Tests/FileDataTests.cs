using GalleryLib.service.fileProcessor;
using Xunit;

namespace GalleryLib.Tests;

/// <summary>
/// Tests for FileData equality and hashing behavior
/// </summary>
public class FileDataTests
{
    [Fact]
    public void FileData_SamePath_AreEqual()
    {
        var file1 = new FileData("/path/to/file.jpg", "data1");
        var file2 = new FileData("/path/to/file.jpg", "data2");

        Assert.Equal(file1, file2);
        Assert.True(file1 == file2);
        Assert.False(file1 != file2);
    }

    [Fact]
    public void FileData_DifferentPath_AreNotEqual()
    {
        var file1 = new FileData("/path/to/file1.jpg", "data");
        var file2 = new FileData("/path/to/file2.jpg", "data");

        Assert.NotEqual(file1, file2);
        Assert.False(file1 == file2);
        Assert.True(file1 != file2);
    }

    [Fact]
    public void FileData_CaseInsensitivePath_AreEqual()
    {
        var file1 = new FileData("/Path/To/FILE.jpg", "data1");
        var file2 = new FileData("/path/to/file.jpg", "data2");

        Assert.Equal(file1, file2);
        Assert.Equal(file1.GetHashCode(), file2.GetHashCode());
    }

    [Fact]
    public void FileData_HashSet_HandlesDuplicates()
    {
        var set = new HashSet<FileData>
        {
            new FileData("/path/to/file.jpg", "data1"),
            new FileData("/path/to/file.jpg", "data2"),
            new FileData("/PATH/TO/FILE.JPG", "data3")
        };

        Assert.Single(set);
    }

    [Fact]
    public void FileData_Null_NotEqual()
    {
        var file = new FileData("/path/to/file.jpg", "data");

        Assert.False(file.Equals(null));
        Assert.False(file == null);
        Assert.True(file != null);
    }

    [Fact]
    public void FileData_ReferenceEquality_Works()
    {
        var file = new FileData("/path/to/file.jpg", "data");

        Assert.True(file.Equals(file));
        Assert.Equal(file, file);
    }

    [Fact]
    public void FileData_ObjectEquals_Works()
    {
        var file1 = new FileData("/path/to/file.jpg", "data1");
        object file2 = new FileData("/path/to/file.jpg", "data2");
        object notFileData = "not a file data";

        Assert.True(file1.Equals(file2));
        Assert.False(file1.Equals(notFileData));
    }
}
