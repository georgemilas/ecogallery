using GalleryLib.model.configuration;
using YamlDotNet.Core;

namespace GalleryLib.service.fileProcessor;

public class CombinedProcessor : EmptyProcessor
{
    private readonly List<IFileProcessor> _processors;

    public CombinedProcessor(List<IFileProcessor> processors, PicturesDataConfiguration configuration) : base(configuration)
    {
        _processors = processors;
    }

    public static FileObserverService CreateProcessor(List<IFileProcessor> processors, PicturesDataConfiguration configuration, int degreeOfParallelism = -1)
    {
        IFileProcessor processor = new CombinedProcessor(processors, configuration);
        return new FileObserverService(processor,intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }
        
    public override async Task<int> OnFileCreated(FileData filePath, bool logIfCreated = false)
    {
        int res = 0;
        foreach (var processor in _processors)
        {
            res =  Math.Max(res, await processor.OnFileCreated(filePath, logIfCreated));
        }  
        return res;
    }
    
    public override async Task<int> OnFileDeleted(FileData filePath, bool logIfDeleted = false)
    {
        int res = 0;
        foreach (var processor in _processors)
        {
            res =  Math.Max(res, await processor.OnFileDeleted(filePath, logIfDeleted));
        }  
        return res;
    }

    public override async Task OnFileChanged(FileData filePath)
    {
        foreach (var processor in _processors)
        {
            await processor.OnFileChanged(filePath);
        }          
    }

    public override async Task OnFileRenamed(FileData oldFilePath, FileData newFilePath,  bool newValid)
    {
        foreach (var processor in _processors)
        {
            await processor.OnFileRenamed(oldFilePath, newFilePath, newValid);
        }
    }
     
    public override async Task<int> OnEnsureCleanupFile(FileData skipFilePath, bool logIfCleaned = false)
    {
        int res = 0;
        foreach (var processor in _processors)
        {
            res =  Math.Max(res, await processor.OnEnsureCleanupFile(skipFilePath, logIfCleaned));
        }  
        return res;
    }

    public override async Task OnScanStart()
    {
        foreach (var processor in _processors)
        {
            await processor.OnScanStart();
        }
    }
    public override async Task OnScanEnd()
    {
        foreach (var processor in _processors)
        {
            await processor.OnScanEnd();
        }
    }       
    
}
