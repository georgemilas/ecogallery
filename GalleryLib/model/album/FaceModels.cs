using System.Data.Common;

namespace GalleryLib.model.album;

/// <summary>
/// Represents a detected face in an image with its embedding vector
/// </summary>
public record FaceEmbedding
{
    public long Id { get; set; }
    public long AlbumImageId { get; set; }
    public long? FacePersonId { get; set; }
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public int BoundingBoxX { get; set; }
    public int BoundingBoxY { get; set; }
    public int BoundingBoxWidth { get; set; }
    public int BoundingBoxHeight { get; set; }
    public float Confidence { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }

    public Rectangle BoundingBox => new(BoundingBoxX, BoundingBoxY, BoundingBoxWidth, BoundingBoxHeight);

    public static FaceEmbedding CreateFromDataReader(DbDataReader reader)
    {
        var facePersonIdOrdinal = reader.GetOrdinal("face_person_id");
        return new FaceEmbedding
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumImageId = reader.GetInt64(reader.GetOrdinal("album_image_id")),
            FacePersonId = reader.IsDBNull(facePersonIdOrdinal) ? null : reader.GetInt64(facePersonIdOrdinal),
            Embedding = reader.GetFieldValue<float[]>(reader.GetOrdinal("embedding")),
            BoundingBoxX = reader.GetInt32(reader.GetOrdinal("bounding_box_x")),
            BoundingBoxY = reader.GetInt32(reader.GetOrdinal("bounding_box_y")),
            BoundingBoxWidth = reader.GetInt32(reader.GetOrdinal("bounding_box_width")),
            BoundingBoxHeight = reader.GetInt32(reader.GetOrdinal("bounding_box_height")),
            Confidence = reader.GetFloat(reader.GetOrdinal("confidence")),
            IsConfirmed = reader.GetBoolean(reader.GetOrdinal("is_confirmed")),
            CreatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_utc")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc"))
        };
    }
}

/// <summary>
/// Represents a person (cluster of faces identified as the same individual)
/// </summary>
public record FacePerson
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public float[]? RepresentativeEmbedding { get; set; }
    public int FaceCount { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }

    public static FacePerson CreateFromDataReader(DbDataReader reader)
    {
        var nameOrdinal = reader.GetOrdinal("name");
        var embeddingOrdinal = reader.GetOrdinal("representative_embedding");
        return new FacePerson
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal),
            RepresentativeEmbedding = reader.IsDBNull(embeddingOrdinal) ? null : reader.GetFieldValue<float[]>(embeddingOrdinal),
            FaceCount = reader.GetInt32(reader.GetOrdinal("face_count")),
            CreatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_utc")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc"))
        };
    }
}

/// <summary>
/// Result from face detection containing bounding box and confidence
/// </summary>
public class FaceDetectionResult
{
    public Rectangle BoundingBox { get; set; }
    public float Confidence { get; set; }

    /// <summary>
    /// Facial landmarks (optional, used for alignment)
    /// Index: 0=left eye, 1=right eye, 2=nose, 3=left mouth, 4=right mouth
    /// </summary>
    public PointF[]? Landmarks { get; set; }
}

/// <summary>
/// Simple rectangle structure for bounding boxes
/// </summary>
public struct Rectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int Right => X + Width;
    public int Bottom => Y + Height;
    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;
}

/// <summary>
/// Simple point structure for landmarks
/// </summary>
public struct PointF
{
    public float X { get; set; }
    public float Y { get; set; }

    public PointF(float x, float y)
    {
        X = x;
        Y = y;
    }
}