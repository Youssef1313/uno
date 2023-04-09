using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.ContentControlTestsControl
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("AAA")]
	public sealed partial class ContentControl_Measure_11935 : Page
	{
		public ContentControl_Measure_11935()
		{
			this.InitializeComponent();
			VM.ActivePage = new ContentControl_Measure_11935_Content(VM);
		}

		public HomeViewModel VM { get; set; } = new();


#if __ANDROID__ || __IOS__
		private async void myRect_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			var captureUI = new CameraCaptureUI();

			var file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
			await AddStorageFileAsync(file);
		}

		private async Task AddStorageFileAsync(StorageFile file)
		{
			if (file != null)
			{
				using var stream = await file.OpenStreamForReadAsync();
				var bytes = new byte[(int)stream.Length];
				stream.Read(bytes, 0, (int)stream.Length);

				VM.ImagesToScan.Add(new ByteArrayWrapper(bytes));
				VM.SelectedIndex = VM.ImagesToScan.Count - 1;
			}
		}
#endif
	}

	public sealed class HomeViewModel : BindableBase
	{
		public ObservableCollection<ByteArrayWrapper> ImagesToScan { get; } = new();

		private Page _activePage;
		private int _selectedIndex;

		public Page ActivePage
		{
			get => _activePage;
			set => SetProperty(ref _activePage, value);
		}

		public int SelectedIndex
		{
			get => _selectedIndex;
			set
			{
				SetProperty(ref _selectedIndex, value);
			}
		}
	}

	public abstract class BindableBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
		{
			if (EqualityComparer<T>.Default.Equals(storage, value))
			{
				return false;
			}

			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public sealed class ByteArrayWrapper : BindableBase
	{
		public ByteArrayWrapper(byte[] bytes)
		{
			Bytes = bytes;
		}

		public byte[] Bytes { get; }
	}

}
