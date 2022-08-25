﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image")]
public sealed partial class SvgImageSource_Basic : Page
{
	private SvgSource _selectedSource;
	private string _imageWidth = "100";
	private string _rasterizedWidth = "";
	private string _imageHeight = "100";
	private string _rasterizedHeight = "";
	private string _selectedStretch = "None";

	public SvgImageSource_Basic()
	{
		this.InitializeComponent();

		_selectedSource = Sources[0];
		OnPropertyChanged();

		this.Loaded += SvgImageSource_Basic_Loaded;
	}

	private async void SvgImageSource_Basic_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
	{
		await CopySourcesToAppDataAsync();
	}

	private async Task CopySourcesToAppDataAsync()
	{
		var appDataSvgs = new string[] { "chef.svg", "bookstack.svg" };
		var localFolder = ApplicationData.Current.LocalFolder;
		var folder = await localFolder.CreateFolderAsync("svg", CreationCollisionOption.OpenIfExists);
		TempLog("Created/opened svg folder");

		foreach (var appDataSvg in appDataSvgs)
		{
			TempLog("Processing " + appDataSvg);
			var item = await folder.TryGetItemAsync(appDataSvg);
			TempLog("Svg " + appDataSvg + " exists: " + (item != null));
			if (item is null)
			{
				var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/Formats/{appDataSvg}"));
				await file.CopyAsync(folder);
				TempLog("Svg " + appDataSvg + " copied: " + ((await folder.TryGetItemAsync(appDataSvg)) != null));
			}
		}
		TempLog("Done copying appdata svgs.");
	}

	private void TempLog(string log)
	{
		Console.WriteLine(log);
		global::System.Diagnostics.Debug.WriteLine(log);
		this.Log().LogError(log);
	}

	public SvgSource[] Sources { get; } = new SvgSource[]
	{
		new("Uno Overalls (ms-appx)", new Uri("ms-appx:///Assets/Formats/uno-overalls.svg")),
		new("Couch (ms-appx)", new Uri("ms-appx:///Assets/Formats/couch.svg")),
		new("Heliocentric (relative)", new Uri("/Assets/Formats/heliocentric.svg", UriKind.Relative)),
		new("Heart (relative)", new Uri("/Assets/Formats/heart.svg", UriKind.Relative)),
		new("Chef (app-data)", new Uri("ms-appdata:///Local/svg/chef.svg")),
		new("Bookstack (app-data)", new Uri("ms-appdata:///Local/svg/bookstack.svg")),
		new("Apple (web)", new Uri("https://upload.wikimedia.org/wikipedia/commons/2/22/UnaManzana1.svg")),
		new("Road crossing (web)", new Uri("https://upload.wikimedia.org/wikipedia/commons/c/cc/Road_Crossing_Sign_in_Grey_Scale_01.svg"))
	};

	public string[] Stretches { get; } = Enum.GetNames(typeof(Stretch)).ToArray();

	public SvgSource SelectedSource
	{
		get => _selectedSource;
		set
		{
			_selectedSource = value;
			OnPropertyChanged();
		}
	}

	public string SelectedStretch
	{
		get => _selectedStretch;
		set
		{
			_selectedStretch = value;
			OnPropertyChanged();
		}
	}

	public string ImageWidth
	{
		get => _imageWidth;
		set
		{
			_imageWidth = value;
			OnPropertyChanged();
		}
	}

	public string ImageHeight
	{
		get => _imageHeight;
		set
		{
			_imageHeight = value;
			OnPropertyChanged();
		}
	}

	public string RasterizedWidth
	{
		get => _rasterizedWidth;
		set
		{
			_rasterizedWidth = value;
			OnPropertyChanged();
		}
	}

	public string RasterizedHeight
	{
		get => _rasterizedHeight;
		set
		{
			_rasterizedHeight = value;
			OnPropertyChanged();
		}
	}

	private void OnPropertyChanged()
	{
		if (ImageElement.Source is not SvgImageSource svgImageSource)
		{
			svgImageSource = new SvgImageSource();
			ImageElement.Source = svgImageSource;
		}

		if (svgImageSource.UriSource is null || svgImageSource.UriSource != SelectedSource.Uri)
		{
			svgImageSource.UriSource = SelectedSource.Uri;
		}

		if (Enum.TryParse(SelectedStretch, out Stretch stretch))
		{
			ImageElement.Stretch = stretch;
		}

		if (double.TryParse(ImageWidth, out var width))
		{
			ImageElement.Width = width;
		}
		else
		{
			ImageElement.Width = double.NaN;
		}

		if (double.TryParse(ImageHeight, out var height))
		{
			ImageElement.Height = height;
		}
		else
		{
			ImageElement.Height = double.NaN;
		}

		if (double.TryParse(RasterizedWidth, out var rasterizedWidth))
		{
			svgImageSource.RasterizePixelWidth = rasterizedWidth;
		}
		else
		{
			//svgImageSource.RasterizePixelWidth = double.PositiveInfinity;
		}

		if (double.TryParse(RasterizedHeight, out var rasterizedHeight))
		{
			svgImageSource.RasterizePixelHeight = rasterizedHeight;
		}
		else
		{
			//svgImageSource.RasterizePixelHeight = double.PositiveInfinity;
		}
	}
}

public class SvgSource
{
	public SvgSource(string name, Uri uri)
	{
		Name = name;
		Uri = uri;
	}

	public string Name { get; }

	public Uri Uri { get; }
}