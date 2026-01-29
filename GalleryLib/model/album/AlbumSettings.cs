using System.Data.Common;

namespace GalleryLib.model.album;

public record AlbumSettings
{
    public long Id { get; set; }
    public long AlbumId { get; set; }
    public string? SearchId { get; set; }  // hash of search expression (for search result preferences)
    public long UserId { get; set; } = 0;
    public bool IsVirtual { get; set; } = false;
    public int BannerPositionY { get; set; } = 38;
    public string AlbumSort { get; set; } = "name-asc";        //name or timestamp & asc or desc
    public string ImageSort { get; set; } = "timestamp-desc";
    public DateTimeOffset LastUpdatedUtc { get; set; }


    public static AlbumSettings CreateFromDataReader(DbDataReader reader)
    {
        var searchIdOrdinal = reader.GetOrdinal("search_id");
        return new AlbumSettings
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumId = reader.GetInt64(reader.GetOrdinal("album_id")),
            SearchId = reader.IsDBNull(searchIdOrdinal) ? null : reader.GetString(searchIdOrdinal),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            BannerPositionY = reader.GetInt32(reader.GetOrdinal("banner_position_y")),
            AlbumSort = reader.GetString(reader.GetOrdinal("album_sort")),
            ImageSort = reader.GetString(reader.GetOrdinal("image_sort")),
            IsVirtual = reader.GetBoolean(reader.GetOrdinal("is_virtual")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc"))
        };
    }
}

