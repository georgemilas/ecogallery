using GalleryLib.model.configuration;
using YamlDotNet.Core;

namespace GalleryLib.service.fileProcessor;

public class CombinedParallelProcessor : CombinedProcessor
{
    private readonly int _degreeOfParallelism;
    private ParallelOptions _configurationoptions;

    public CombinedParallelProcessor(List<IFileProcessor> processors, PicturesDataConfiguration configuration, int degreeOfParallelism = -1) : base(processors, configuration)
    {
        _degreeOfParallelism = degreeOfParallelism == -1 ? Environment.ProcessorCount : degreeOfParallelism; 
        _configurationoptions = new ParallelOptions { MaxDegreeOfParallelism = _degreeOfParallelism};
    }

    public static new FileObserverService CreateProcessor(List<IFileProcessor> processors, PicturesDataConfiguration configuration, int degreeOfParallelism = -1)
    {
        IFileProcessor processor = new CombinedParallelProcessor(processors, configuration, degreeOfParallelism);
        return new FileObserverService(processor,intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }
        
    public override async Task<int> OnFileCreated(FileData filePath, bool logIfCreated = false)
    {
        int res = 0;
        await Parallel.ForEachAsync(_processors, _configurationoptions, async (processor, ct) =>
        {
            res =  Math.Max(res, await processor.OnFileCreated(filePath, logIfCreated));
        });  
        return res;
    }
    
    public override async Task<int> OnFileDeleted(FileData filePath, bool logIfDeleted = false)
    {
        int res = 0;
        await Parallel.ForEachAsync(_processors, _configurationoptions, async (processor, ct) =>
        {
            res =  Math.Max(res, await processor.OnFileDeleted(filePath, logIfDeleted));
        }); 
        return res;
    }

    public override async Task OnFileChanged(FileData filePath)
    {
        await Parallel.ForEachAsync(_processors, _configurationoptions, async (processor, ct) =>
        {
            await processor.OnFileChanged(filePath);
        });
    }

    public override async Task OnFileRenamed(FileData oldFilePath, FileData newFilePath,  bool newValid)
    {
        await Parallel.ForEachAsync(_processors, _configurationoptions, async (processor, ct) =>
        {
            await processor.OnFileRenamed(oldFilePath, newFilePath, newValid);
        });
    }
     
    public override async Task<int> OnEnsureCleanupFile(FileData skipFilePath, bool logIfCleaned = false)
    {
        int res = 0;
        await Parallel.ForEachAsync(_processors, _configurationoptions, async (processor, ct) =>
        {
            res =  Math.Max(res, await processor.OnEnsureCleanupFile(skipFilePath, logIfCleaned));
        });  
        return res;
    }

    public override async Task OnScanStart()
    {
        await Parallel.ForEachAsync(_processors, _configurationoptions, async (processor, ct) =>
        {
            await processor.OnScanStart();
        });
    }
    public override async Task OnScanEnd()
    {
        await Parallel.ForEachAsync(_processors, _configurationoptions, async (processor, ct) =>
        {
            await processor.OnScanEnd();
        });
    }       
    
}
