namespace GalleryLib.model.album;


public interface IUniqueDataId
{
    string DataId { get; }
    string FallbackUserDataId { get; }
    public long UserId { get; }
}

public class DataIdPrefix
{
    public const string Album = "album";
    public const string AlbumRandom = "album/random";
    public const string AlbumRecent = "album/recent";
    public const string AlbumSearch = "album/search";

    public const string Valbum = "valbum/id";
    public const string ValbumRoot = "valbum/root";
    public const string ValbumRandom = "valbum/random";
    public const string ValbumRecent = "valbum/recent";

    public const string LocationId = "locations/search/cluster/id";
    public const string LocationName = "locations/search/name";

    public const string FaceId = "faces/search/person/id";    
    public const string FaceName = "faces/search/name";    
}

public class UniqueDataId<T> : IUniqueDataId
{
    private UniqueDataId(string prefix, T artifactId, long userId, long fallbackUserId = 1)
    {
        Prefix = prefix;
        ArtifactId = artifactId;
        UserId = userId;
        FallbackUserId = fallbackUserId;
    }

    

    public string Prefix { get; set; }
    public T ArtifactId { get; set; }
    public long UserId { get; set; }
    public long FallbackUserId { get; set; }   

    public string DataId 
    { 
        get
        {
            return $"{Prefix}:{ArtifactId}:{UserId}";
        } 
    }
    public string FallbackUserDataId 
    { 
        get
        {
            return $"{Prefix}:{ArtifactId}:{FallbackUserId}";    //settings as defined by the admin user (id 1)
        } 
    }

    public static IUniqueDataId New(string prefix, T artifactId, long userId)
    {
        return new UniqueDataId<T>(prefix, artifactId, userId);  //admin fallback
    }
    public static IUniqueDataId Parse(string uniqueDataId)
    {
        var parts = uniqueDataId.Split(':');
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid uniqueDataId format: {uniqueDataId}");
        }
        var user = long.Parse(parts[2]);
        return new UniqueDataId<string>(parts[0], parts[1], user, user);  //no fallback
    }

}