using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using WebDriverManager.Finders;
using Xunit;
using Xunit.Abstractions;

namespace WebDriverManager.Tests
{
    public class WebdriverFinderTests
    {
        private readonly ITestOutputHelper _output;

        public WebdriverFinderTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public async Task FinderShouldFindDriver()
        {
            var finder = new EdgeWebDriverFinder(NullLogger<EdgeWebDriverFinder>.Instance);
            var path = await finder.Configure(downloadDriver: true).FindAvailableDrivers().FindBrowserVersion().ReturnMatchedDriverPath();
            _output.WriteLine("Found matching driver at: " + path);
            Assert.NotNull(path);
        }
    }
}