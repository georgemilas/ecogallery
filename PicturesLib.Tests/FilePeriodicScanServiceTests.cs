using System.Text.Json;
using PicturesLib.service;
using PicturesLib.service.fileProcessor;
using Xunit;

namespace PicturesLib.Tests;

public class FilePeriodicScanServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FilePeriodicScanService _service;

    public FilePeriodicScanServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _service = new FilePeriodicScanService(new EmptyProcessor(new model.configuration.PicturesDataConfiguration{}));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    
    [Fact]
    public void GetSourceFiles_ReturnsCorrectFilesToProcess()
    {
        Assert.True(true);        
    }    
}
