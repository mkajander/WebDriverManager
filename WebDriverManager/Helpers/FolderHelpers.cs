using System.IO;
using System.Reflection;

namespace WebDriverManager.Helpers
{
    public static class FolderHelpers
    {
        public static string GetProgramPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}