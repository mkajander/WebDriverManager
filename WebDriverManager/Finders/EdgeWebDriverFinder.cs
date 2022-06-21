using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebDriverManager.Exceptions;

namespace WebDriverManager.Finders
{
    public enum DriverType
    {
        Chrome = 0,
        Edge = 1
    }
    public interface IWebDriverFinder: IConfigure, IFindAvailableDrivers, IFindBrowserVersion
    {
    }
    public interface IConfigure
    {
        IFindAvailableDrivers FindAvailableDrivers();
    }

    public interface IFindAvailableDrivers
    {
        IFindBrowserVersion FindBrowserVersion();
    }

    public interface IFindBrowserVersion
    {
        Task<string> ReturnMatchedDriverPath();
    }


    public class EdgeWebDriverFinder : IWebDriverFinder
    {
        const string VersionMatchPattern = @"^\d+\.";
        //Adding others would obviously need some simple refactoring

        private readonly ILogger _logger;

        public EdgeWebDriverFinder(ILogger<EdgeWebDriverFinder> logger)
        {
            _logger = logger;
            // Add exception handling here for when the driver is not found
            
        }

        public DriverType DriverType { get; private set; }
        public Dictionary<string, string> AvailableDrivers { get; private set; }
        public string EdgeVersion { get; private set; }

        public string EdgePath { get; private set; } = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";

        public string DriveFolder { get; private set; }
            
        public bool ShouldDownloadDriver { get; private set; } = false;

        public IFindAvailableDrivers FindAvailableDrivers()
        {
            var drivers = new Dictionary<string, string>();
            var partialName = "msedgedriver";
            var hdDirectoryInWhichToSearch = new DirectoryInfo(DriveFolder);
            var filesInDir = hdDirectoryInWhichToSearch.GetFiles("*" + partialName + "*").ToArray();

            foreach (var foundFile in filesInDir)
            {
                var fullName = foundFile.FullName;
                var hasversionInName = foundFile.Name.Contains('-');
                var version = GetFileVersionInfo(fullName).FileVersion;
                if (hasversionInName)
                {
                    drivers.Add(version, fullName);
                    continue;
                }

                var newName = Path.Combine(foundFile.Directory.FullName,
                    $"msedgedriver-{version}{foundFile.Extension}");
                if (File.Exists(newName))
                {
                    File.Delete(newName); //if this file exists then delete it
                    drivers.Remove(version);
                }

                File.Move(foundFile.FullName, newName);
                drivers.Add(version, newName);
            }

            _logger.LogInformation("Found drivers", drivers);

            AvailableDrivers = drivers;
            return this;
        }

        public IFindBrowserVersion FindBrowserVersion()
        {
            try
            {
                EdgeVersion = GetFileVersionInfo(EdgePath).FileVersion;
                _logger.LogInformation($"Edge Version {EdgeVersion}");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError("Edge Version not Found.", ex);
            }

            return this;
        }

        public async Task<string> ReturnMatchedDriverPath()
        {
            
            foreach (var availableDriver in AvailableDrivers)
            {
                string availableDriverVersion = Regex.Match(availableDriver.Key, VersionMatchPattern).Value.Trim('.');
                string edgeVersion = Regex.Match(EdgeVersion, VersionMatchPattern).Value.Trim('.');
                if (availableDriverVersion == edgeVersion)
                {
                    _logger.LogInformation("Returning driver", availableDriver);
                    return availableDriver.Value;
                }
            }


            if (ShouldDownloadDriver)
            {
                _logger.LogInformation("Downloading driver");
                return await DownloadDriver();
            }

            throw new WebDriverFinderException("EdgeDriver not found or version not available", AvailableDrivers,
                EdgeVersion);
        }

        public IConfigure Configure(string edgePath = null, string driveFolder = null, bool downloadDriver = false,
            DriverType driverType = DriverType.Edge)
        {
            // get the directory path to current assembly
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            EdgePath = edgePath ?? EdgePath;
            ShouldDownloadDriver = downloadDriver;
            DriveFolder = driveFolder ?? assemblyPath;
            DriverType = driverType;
            return this;
        }

        private async Task<string> DownloadDriver()
        {
            var driverDownloader = new DriverDownloader();
            return await driverDownloader.DownloadVersion(DriverType, EdgeVersion);
        }
        public static FileVersionInfo GetFileVersionInfo(string filePath)
        {
            return FileVersionInfo.GetVersionInfo(filePath);
        }
    }
}