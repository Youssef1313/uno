using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.WebView
{
    [Sample("WebView")]
    public sealed partial class WebView_AppData : Page
    {
		private static Uri s_uri = new Uri("ms-appdata:///local/folder/index.html");

		public WebView_AppData()
        {
            this.InitializeComponent();
        }

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("folder", CreationCollisionOption.ReplaceExisting);
			var file = await folder.CreateFileAsync("index.html", CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(file, "<h1>Hello world</h1>");
			var webViewLocal = webView; // To simplify stepping into the method instead of the property.
			webViewLocal.Navigate(s_uri);
		}
	}
}
