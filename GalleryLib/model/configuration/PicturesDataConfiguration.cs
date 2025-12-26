namespace GalleryLib.model.configuration;

public class PicturesDataConfiguration
{
    public const string SectionName = "PicturesData";    
    public string Folder { get; set; } = string.Empty;
    public List<string> SkipSuffix { get; set; } = new List<string> { "_skip", "_pss", "_noW" };
    public List<string> SkipPrefix { get; set; } = new List<string> { "skip_", "pss_", "noW_" };
    public List<string> SkipContains { get; set; } = new List<string> { "EosRP", "_thumbnails" };
    public List<string> RoleSuffix { get; set; } = new List<string> { "_private", "_public", "-family", " extfamily", "_friends", "_custid" };
    public List<string> RolePrefix { get; set; } = new List<string> { "private_", "public_", "family_", "extfamily_", "friends_", "custid_" };
    public List<string> ValueBasedRoleSuffix { get; set; } = new List<string> { "_custid_{X}", "-custid-{X}", " custid {X}"}; 
    public List<string> ValueBasedRolePrefix { get; set; } = new List<string> { "custid_{X}_", "custid-{X}-", "custid {X} "};
    public List<string> FeaturePhotoSuffix { get; set; } = new List<string> { "_label", "_feature" };
    public List<string> FeaturePhotoPrefix { get; set; } = new List<string> { "label_", "feature_" };
    public List<string> ImageExtensions { get; set; } = new List<string> { ".jpg", ".jpeg", ".png", ".webp" }; 
    public List<string> MovieExtensions { get; set; } = new List<string> { ".mp4", ".mov", ".avi", ".3gp" };
    // { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".webm", ".flv", ".3gp", ".mts", ".m2ts" };

    private List<string> _extensions = new List<string>();
    public List<string> Extensions 
    { 
        get 
        { 
            if (_extensions.Count > 0)
            {
                return _extensions;
            }
            _extensions.AddRange(ImageExtensions);
            _extensions.AddRange(MovieExtensions);
            return _extensions;
        }
    }
    public DirectoryInfo RootFolder => new DirectoryInfo(Folder);

    public string ThumbnailsBase { get { return Path.Combine(RootFolder.FullName, "_thumbnails"); } }
    public  string ThumbDir(int thumbHeight) {  return Path.Combine(ThumbnailsBase, thumbHeight.ToString());  }

    /// <summary>
    /// //get the thumbnail path from the absolute path
    /// </summary>
    public virtual string GetThumbnailPath(string sourceFilePath, int thumbHeight)
    {
        if (IsMovieFile(sourceFilePath))
        {
            sourceFilePath = Path.ChangeExtension(sourceFilePath, ".jpg");
        }
        return sourceFilePath.Replace(RootFolder.FullName, ThumbDir(thumbHeight));
    }

    public bool IsMovieFile(string sourceFilePath)
    {
        var fileExt = Path.GetExtension(sourceFilePath);
        return MovieExtensions.Any(ext => ext.Equals(fileExt, StringComparison.OrdinalIgnoreCase));
        //return fileExt.Equals(".mp4", StringComparison.OrdinalIgnoreCase);
    }


    public bool IsFeatureFile(string sourceFilePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        return FeaturePhotoSuffix.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) ||
               FeaturePhotoPrefix.Any(prefix => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

}


public enum ThumbnailHeights
{
    Thumb = 400,
    /// <summary>
    /// Small HD resolution should be 720p but to save space we only created UltraHD images at 1440 height
    /// </summary>
    SmallHD = 1440,  //720
    /// <summary>
    /// HD aka Full HD resolution should be 1080p but to save space we only created UltraHD images at 1440 height
    /// </summary>
    HD = 1440,   //1080
    /// <summary>
    /// UltraHD (aka 2K) resolution 1440p 
    /// </summary>
    UHD = 1440,
    UHD_2K = 1440,
    UHD_4K = 2160,
    UHD_5K = 2880,
    UHD_6K = 3240,
    UHD_8K = 4320
} 


/*
    "PicturesData": {
        "Folder": "E:\\TotulAici\\TutuLaptop\\pictures",
        "skipSuffix": ["_skip", "_pss", "_noW"],
        "skipPrefix": ["skip_", "pss_", "noW_"],
        "skipContains": ["DCIM", "EosRP", "_thumbnails"],
        "roleSuffix":["_private", "_public", "-family", " extfamily", "_friends", "_custid"],    
        "rolePrefix":["private_", "public_", "family_", "extfamily_", "friends_", "custid_"],    
        "valueBasedRolePrefix":["custid_{X}_", "custid-{X}-", "custid {X} "],
        "valueBasedRoleSuffix":["_custid_{X}", "-custid-{X}", " custid {X}"],    
         "featurePhotoSuffix": ["_label", "_feature"],
        "featurePhotoPrefix": ["label_", "feature_"],
        "imageExtensions": [ ".jpg", ".jpeg", ".png", ".webp" ],
        "movieExtensions": [ ".mp4" ]
  }
*/