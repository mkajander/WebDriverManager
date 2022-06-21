using System.Threading.Tasks;
using WebDriverManager.Finders;
using Xunit;
using Xunit.Abstractions;

namespace WebDriverManager.Tests
{
    public class DriverDownloaderTests
    {
        private readonly ITestOutputHelper _output;

        public DriverDownloaderTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public async Task ShouldGetLatestChromeDriverVersion()
        {
            var driverDownloader = new DriverDownloader();
            var version = await driverDownloader.GetLatestVersion(DriverType.Chrome);
            _output.WriteLine("Latest ChromeDriverVersion {0}", version);
            Assert.NotNull(version);
        }
        
        [Fact]
        public async Task ShouldGetLatestEdgeDriverVersion()
        {
            var driverDownloader = new DriverDownloader();
            var version = await driverDownloader.GetLatestVersion(DriverType.Edge);
            _output.WriteLine("Latest EdgeDriverVersion {0}", version);
             Assert.NotNull(version);
        }
        
        [Fact]
        public async Task ShouldDownloadLatestChromeDriverVersion()
        {
            var driverDownloader = new DriverDownloader();
            var path = await driverDownloader.DownloadLatestVersion(DriverType.Chrome);
            _output.WriteLine("Downloaded and extracted latest ChromeDriver to {0}", path);
            Assert.NotNull(path);
        }
        
        [Fact]
        public async Task ShouldDownloadLatestEdgeDriverVersion()
        {
            var driverDownloader = new DriverDownloader();
            var path = await driverDownloader.DownloadLatestVersion(DriverType.Edge);
            _output.WriteLine("Downloaded and extracted latest EdgeDriver to {0}", path);
            Assert.NotNull(path);
        }
        
        [Theory]
        [InlineData("102.0.5005.61")]
        public async Task ShouldDownloadSpecificChromeDriverVersion(string version)
        {
            var driverDownloader = new DriverDownloader();
            var path = await driverDownloader.DownloadVersion(DriverType.Chrome, version );
            _output.WriteLine("Downloaded and extracted ChromeDriver version {0} to {1}", path, version);
            Assert.NotNull(path);
        }
        [Theory]
        [InlineData("100.0.1155.0")]
        public async Task ShouldDownloadSpecificEdgeDriverVersion(string version)
        {
            var driverDownloader = new DriverDownloader();
            var path = await driverDownloader.DownloadVersion(DriverType.Edge, version );
            _output.WriteLine("Downloaded and extracted ChromeDriver version {0} to {1}", path, version);
            Assert.NotNull(path);
        }
        
    }
}