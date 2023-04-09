using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.ContentControlTestsControl
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ContentControl_Measure_11935_Content : Page
	{
		public HomeViewModel VM { get; }

		public ContentControl_Measure_11935_Content(HomeViewModel vm)
		{
			VM = vm;
			this.InitializeComponent();
		}
	}

	public static class Helpers
	{
		public static BitmapImage ConvertByteArrayToBitmapImage(ByteArrayWrapper bytes)
		{
			using (var ms = new MemoryStream(bytes.Bytes))
			{
				var image = new BitmapImage();
				image.SetSource(ms.AsRandomAccessStream());
				return image;
			}
		}
	}
}
