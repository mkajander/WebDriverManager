using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebDriverManager.Helpers;

namespace WebDriverManager.Finders
{
    public class DriverDownloader
    {
        private static readonly ReadOnlyDictionary<DriverType, string> LatestVersionUrls
            = new ReadOnlyDictionary<DriverType, string>(
                new Dictionary<DriverType, string>()
                {
                    { DriverType.Chrome, "https://chromedriver.storage.googleapis.com/LATEST_RELEASE" },
                    { DriverType.Edge, "https://msedgedriver.azureedge.net/LATEST_STABLE" }
                }
            );
        private static readonly ReadOnlyDictionary<DriverType, string> BlobRepositoryUrls
            = new ReadOnlyDictionary<DriverType, string>(
                new Dictionary<DriverType, string>()
                {
                    { DriverType.Chrome, "https://chromedriver.storage.googleapis.com" },
                    { DriverType.Edge, "https://msedgedriver.azureedge.net" }
                }
            );        
        private static readonly ReadOnlyDictionary<DriverType, string> DownloadUrls
            = new ReadOnlyDictionary<DriverType, string>(
                new Dictionary<DriverType, string>()
                {
                    { DriverType.Chrome, "https://chromedriver.storage.googleapis.com/{0}/chromedriver_{1}.zip" },
                    { DriverType.Edge, "https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/{0}/edgedriver_{1}.zip" }
                }
            );
        


        const string DriverVersionRegex = @"([0-9]+(\.[0-9]+)+)";
        const string VersionMatchPattern = @"^\d+\.";

        const string ChromeDriverFileName = "chromedriver.zip";
        const string EdgeDriverFileName = "msedgedriver.zip";

        private static HttpClient Client = new HttpClient();

        public string DriverFolder { get; private set; }

        public DriverDownloader(string driverFolder = null)
        {
            DriverFolder = driverFolder ?? FolderHelpers.GetProgramPath();
            // Without this edgeDriver blobRepositoryUrl is 404 not found
            Client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        }

        public async Task<string> GetLatestVersion(DriverType driverType)
        {
            switch (driverType)
            {
                case DriverType.Chrome:
                    return await GetLatestChromeDriverVersion();
                case DriverType.Edge:
                    return await GetLatestEdgeDriverVersion();
                default:
                    throw new ArgumentException("Unknown driver name: " + driverType.ToString());
            }
        }


        private async Task<string> GetLatestChromeDriverVersion()
        {
            var response = await Client.GetAsync(LatestVersionUrls[DriverType.Chrome]).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> GetLatestEdgeDriverVersion()
        {
            // unlike chrome edge driver latest version comes in a blob file containing the version
            var response = await Client.GetAsync(LatestVersionUrls[DriverType.Edge]).ConfigureAwait(false);
            // get the blob file
            var blob = await response.Content.ReadAsStringAsync();
            // get the version from the blob file
            var version = Regex.Match(blob, DriverVersionRegex).Value;
            return version;
        }

        public async Task<string> DownloadDriver(DriverType driverType, string version)
        {
            var downloadUrl = string.Format(DownloadUrls[driverType], version, GetDriverArchitecture(driverType));
            var response = await Client.GetAsync(downloadUrl).ConfigureAwait(false);
            var zipPath = Path.Combine(DriverFolder, GetDriverFileName(driverType));
            using (var fileStream = File.Create(zipPath))
            {
                await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);
            }
            var driverPath=  await UnzipDriver(driverType, zipPath);

            return driverPath;
        }
        private async Task<string> UnzipDriver(DriverType driverType, string pathToZip)
        {
            string driverPath = "";
            using (ZipArchive archive = ZipFile.OpenRead(pathToZip))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                { 
                    if(entry.Name.Contains(".exe"))
                    {
                        driverPath = Path.Combine(DriverFolder, entry.Name);
                        entry.ExtractToFile(driverPath, true);
                        break;
                    }
                }
            } // dispose of the archive to free up memory
            File.Delete(pathToZip);
            return driverPath;
        }

        public async Task<string> DownloadLatestVersion(DriverType driverType)
        {
            var version = await GetLatestVersion(driverType);
            return await DownloadDriver(driverType, version);
        }
        public string GetDriverArchitecture(DriverType driverType)
        {
            // Todo: Implement this in an actually sensible manner so it works for all platforms
            // Not that anyone is going to use this for anything other than windows and maybe linux64
            switch (driverType)
            {
                case DriverType.Chrome:
                    return "win32";
                case DriverType.Edge:
                    return "win64";
                default:
                    throw new ArgumentException("Unknown driver name: " + driverType.ToString());
            }
        }
        public string GetDriverFileName(DriverType driverType)
        {
            switch (driverType)
            {
                case DriverType.Chrome:
                    return ChromeDriverFileName;
                case DriverType.Edge:
                    return EdgeDriverFileName;
                default:
                    throw new ArgumentException("Unknown driver name: " + driverType.ToString());
            }
        }

        public async Task<string> DownloadVersion(DriverType driverType, string browserVersion)
        {
            var driverVersion = await GetMatchingDriverVersion(driverType, browserVersion);
            return await DownloadDriver(driverType, driverVersion);
        }

        private async Task<string> GetMatchingDriverVersion(DriverType driverType, string browserVersion)
        {
            switch (driverType)
            {
                case DriverType.Chrome:
                    return await GetSpecificChromeDriverVersion(browserVersion);
                case DriverType.Edge:
                    return await GetSpecificEdgeDriverVersion(browserVersion);
                default:
                    throw new ArgumentException("Unknown driver name: " + driverType.ToString());
            }
        }

        private async Task<string> GetSpecificEdgeDriverVersion(string browserVersion)
        {
            //regex to trim the version number
            var version = browserVersion;
            if (browserVersion.Contains('.'))
            {
                version = Regex.Match(browserVersion, VersionMatchPattern).Value.Trim('.');
            }
            var response = await Client.GetAsync(BlobRepositoryUrls[DriverType.Edge]).ConfigureAwait(false);
            var xml = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xml);
            var availableDrivers = doc.Root.DescendantNodes().OfType<XElement>().Where(x => x.Name.LocalName == "Blob");
            foreach (var availableDriver in availableDrivers)
            {
                string name = availableDriver.Element("Name").Value;
                if (name.StartsWith(version) && name.EndsWith("win32.zip"))
                {
                    return availableDriver.Value.Split('/')[0];
            
                }
            }

            throw new Exception("Could not find a driver for version " + version);
        }

        private async Task<string> GetSpecificChromeDriverVersion(string browserVersion)
        {
            //regex to trim the version number
            var version = Regex.Match(browserVersion, VersionMatchPattern).Value.Trim('.');
            var response = await Client.GetAsync(BlobRepositoryUrls[DriverType.Chrome]).ConfigureAwait(false);
            var xml = await response.Content.ReadAsStringAsync();
            // Parse the xml
            var doc = XDocument.Parse(xml);
            var availableDrivers = doc.Root.DescendantNodes().OfType<XElement>().Where(x => x.Name.LocalName == "Key")
                .OrderByDescending(x => x.Value);
            foreach (var availableDriver in availableDrivers)
            {
                if (availableDriver.Value.StartsWith(version) && availableDriver.Value.EndsWith("win32.zip"))
                {
                    return availableDriver.Value.Split('/')[0];

                }
            }

            throw new Exception("Could not find a driver for version " + version);

        }

    }
}