namespace GalleryLib.service.fileProcessor;

public class FileData : IEquatable<FileData>
{
    public FileData(string filePath, object data)
    {
        this.FilePath = filePath;
        this.Data = data;
    }

    public string FilePath { get; init; }
    public object Data { get; init; }

    /// <summary>
    /// Original image dimensions set by MultipleThumbnailsProcessor to avoid redundant image loads in downstream processors
    /// </summary>
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }

    public bool Equals(FileData? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is FileData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return FilePath.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator ==(FileData? left, FileData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(FileData? left, FileData? right)
    {
        return !Equals(left, right);
    }
}
