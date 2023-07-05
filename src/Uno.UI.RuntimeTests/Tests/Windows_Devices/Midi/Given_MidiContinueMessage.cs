#nullable disable

using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.Midi;

namespace Uno.UI.RuntimeTests.Tests.Windows_Devices.Midi
{
	[TestClass]
	public class Given_MidiContinueMessage
	{
		[TestMethod]
		public void When_RawData()
		{
			var message = new MidiContinueMessage();
			var data = message.RawData.ToArray();
			CollectionAssert.AreEqual(new byte[] { 251 }, data);
		}
	}
}
