using System.Data.Common;

namespace GalleryLib.model.album;

public record AlbumParents
{
    public long Id { get; set; }
    public string AlbumName { get; set; } = string.Empty;
    public string ParentAlbum { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Depth { get; set; } = 0;


    public static AlbumParents CreateFromDataReader(DbDataReader reader)
    {
        return new AlbumParents
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumName = reader.GetString(reader.GetOrdinal("album_name")),
            ParentAlbum = reader.GetString(reader.GetOrdinal("parent_album")),
            Path = reader.GetString(reader.GetOrdinal("path")),
            Depth = reader.GetInt32(reader.GetOrdinal("depth"))
        };
    }
}


