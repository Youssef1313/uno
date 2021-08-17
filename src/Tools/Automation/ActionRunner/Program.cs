using System;
using System.IO;

namespace ActionRunner
{
	internal class Program
    {
		private const string UnoPathPrefix = "src/Uno.UI/Microsoft/UI/Xaml/Controls";
		private const string MUXPathPrefix = "microsoft-ui-xaml/dev";

		private static int Main()
        {
			if (!Directory.Exists(MUXPathPrefix))
			{
				GitHubLogger.LogError("Can't find microsoft-ui-xaml/dev directory.");
				return 1;
			}

			string[] unoResourceFiles = CollectResourcesFromUno();
			foreach (string unoPath in unoResourceFiles)
			{
				var muxPath = MapUnoToMUX(unoPath);
				GitHubLogger.LogInformation($"Mapped '{unoPath}' to '{muxPath}'.");
				if (!File.Exists(muxPath))
				{
					GitHubLogger.LogError($"Path '{muxPath}' was not found.");
				}
			}

			return 0;
        }

		private static string[] CollectResourcesFromUno()
			=> Directory.GetFiles(UnoPathPrefix, "*.resw");

		private static string MapUnoToMUX(string unoPath)
		{
			if (!unoPath.StartsWith(UnoPathPrefix))
			{
				throw new InvalidOperationException($"Expected path to start with '{UnoPathPrefix}'. Found: '{MUXPathPrefix}'.");
			}

			return Path.Combine(MUXPathPrefix, Path.GetRelativePath(relativeTo: UnoPathPrefix, unoPath));
		}
    }
}
