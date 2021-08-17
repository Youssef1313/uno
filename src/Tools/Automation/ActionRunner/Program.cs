using System;
using System.IO;

namespace Automation.ActionRunner
{
	class Program
    {
		private static void Main()
        {
			if (!Directory.Exists("microsoft-ui-xaml/dev"))
			{
				throw new Exception("Can't find microsoft-ui-xaml/dev directory.")
			}
        }
    }
}
