namespace PicturesLib.model.configuration;

public class PicturesDataConfiguration
{
    public const string SectionName = "PicturesData";    
    public string Folder { get; set; } = string.Empty;
    public List<string> SkipSuffix { get; set; } = new List<string> { "_skip", "_pss", "_noW" };
    public List<string> SkipPrefix { get; set; } = new List<string> { "skip_", "pss_", "noW_" };
    public List<string> SkipContains { get; set; } = new List<string> { "DCIM", "EosRP", "_thumbnails" };
    public List<string> RoleSuffix { get; set; } = new List<string> { "_private", "_public", "-family", " extfamily", "_friends", "_custid" };
    public List<string> RolePrefix { get; set; } = new List<string> { "private_", "public_", "family_", "extfamily_", "friends_", "custid_" };
    public List<string> ValueBasedRoleSuffix { get; set; } = new List<string> { "_custid_{X}", "-custid-{X}", " custid {X}"}; 
    public List<string> ValueBasedRolePrefix { get; set; } = new List<string> { "custid_{X}_", "custid-{X}-", "custid {X} "};
    public List<string> FeaturePhotoSuffixOrPrefix { get; set; } = new List<string> { "label", "feature" };
    public List<string> Extensions { get; set; } = new List<string> { ".jpg", ".jpeg", ".png", ".webp", ".mp4" }; 
    public DirectoryInfo RootFolder => new DirectoryInfo(Folder);
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
        "featurePhotoSuffixOrPrefix": ["label", "feature"],
        "extensions": [ ".jpg", ".jpeg", ".png", ".webp" ]
  }
*/