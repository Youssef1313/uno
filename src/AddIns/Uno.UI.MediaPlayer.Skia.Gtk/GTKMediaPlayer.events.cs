#nullable enable

using Windows.UI.Core;
using LibVLCSharp.GTK;
using LibVLCSharp.Shared;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using Windows.UI.Xaml;
using System.Threading;
using System.Linq;
using Windows.UI.Notifications;
using Uno.Extensions;
using Uno.Logging;
using Pango;
using Windows.UI.Xaml.Media;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Globalization;
using Windows.Media.Playback;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Media;

public partial class GtkMediaPlayer
{
	private Task? _initializationTask;
	private MediaPlayerElement? _mpe;

	public event EventHandler<object>? OnSourceFailed;
	public event EventHandler<object>? OnSourceEnded;
	public event EventHandler<object>? OnMetadataLoaded;
	public event EventHandler<object>? OnTimeUpdate;
	public event EventHandler<object>? OnSourceLoaded;

	private async Task Initialize()
	{
		if (_initializationTask is null)
		{
			_initializationTask = InitializeInner();
		}

		await _initializationTask;
	}

	private async Task InitializeInner()
	{
		await Task.Run(() =>
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Creating libvlc");
			}

			_libvlc = new LibVLC(enableDebugLogs: false);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Creating player");
			}

			_mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libvlc);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Creating VideoView");
			}

			_videoView = new LibVLCSharp.GTK.VideoView();
			_videoView.VideoSurfaceInteraction += OnVideoViewVideoSurfaceInteraction;
		});

		await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			_videoContainer = new ContentPresenter
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch,
			};

			if (_videoView != null && _mediaPlayer != null && _videoContainer != null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Set MediaPlayer");
				}

				_videoView.Visible = true;
				_videoView.MediaPlayer = _mediaPlayer;

				_mediaPlayer.TimeChanged += OnMediaPlayerTimeChange;
				_mediaPlayer.TimeChanged += OnMediaPlayerTimeChangeIsMediaParse;
				_mediaPlayer.MediaChanged += MediaPlayerMediaChanged;
				_mediaPlayer.Stopped += OnMediaPlayerStopped;

				_videoContainer.Content = _videoView;
				AddChild(_videoContainer);
				UpdateVideoStretch();

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Created player");
				}
			}

			UpdateMedia();
		});
	}

	private void OnVideoViewVideoSurfaceInteraction(object? sender, EventArgs e)
	{
		UpdateMediaPlayerElementReference();

		if (_mpe is not null)
		{
			_mpe.TransportControls.Show();
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Unable to find a MediaPlayerElement instance to show the transport controls");
			}
		}
	}

	private void UpdateMediaPlayerElementReference()
		=> _mpe ??= _owner.FindFirstParent<MediaPlayerElement>();

	private void OnSourceVideoLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = true;
		}

		UpdateVideoStretch();
		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
	}

	private static void OnSourceChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
	{
		if (source is GtkMediaPlayer player)
		{
			string encodedSource = (string)args.NewValue;

			player._mediaPath = new Uri(encodedSource);
			player.UpdateMedia();
		}
	}

	private void UpdateMedia()
	{
		if (_mediaPlayer != null && _libvlc != null && _mediaPath != null)
		{
			string[] options = new string[1];
			var media = new LibVLCSharp.Shared.Media(_libvlc, _mediaPath, options);

			media.Parse(MediaParseOptions.ParseNetwork);
			_mediaPlayer.Media = media;
			OnSourceLoaded?.Invoke(this, EventArgs.Empty);

			UpdateVideoStretch();
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to update the media, the player is not ready yet");
			}
		}
	}

	private void AddMediaEvents()
	{
		if (_mediaPlayer?.Media is { IsParsed: true } media)
		{
			media.DurationChanged -= DurationChanged;
			media.MetaChanged -= MetaChanged;
			media.StateChanged -= StateChanged;
			media.ParsedChanged -= ParsedChanged;
			_mediaPlayer.TimeChanged -= OnMediaPlayerTimeChangeIsMediaParse;

			media.DurationChanged += DurationChanged;
			media.MetaChanged += MetaChanged;
			media.StateChanged += StateChanged;
			media.ParsedChanged += ParsedChanged;

			Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);
			OnSourceLoaded?.Invoke(this, EventArgs.Empty);

			UpdateVideoStretch();
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to add media events, the player is not ready yet");
			}
		}
	}

	private void ParsedChanged(object? sender, EventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"ParsedChanged");
		}

		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
		OnGtkSourceLoaded(sender, el);
	}

	private void DurationChanged(object? sender, EventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"DurationChanged");
		}

		Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);
	}

	private void StateChanged(object? sender, MediaStateChangedEventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"StateChanged");
		}

		switch (el.State)
		{
			case VLCState.Opening:
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Opening");
				}
				OnGtkSourceLoaded(sender, el);
				break;

			case VLCState.Ended:
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Error");
				}
				if (!_isEnding)
				{
					OnEndReached();
				}
				break;

			case VLCState.Error:
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Error");
				}
				OnGtkSourceFailed(sender, el);
				break;

			case VLCState.NothingSpecial:
			case VLCState.Buffering:
			case VLCState.Playing:
			case VLCState.Paused:
			case VLCState.Stopped:
				break;

			default:
				break;
		}
	}

	private void MetaChanged(object? sender, EventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"MetaChanged");
		}

		OnGtkMetadataLoaded(sender, el);
	}

	private void OnMediaPlayerStopped(object? sender, EventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayer Stopped");
		}

		if (_videoView != null)
		{
			if (!_isEnding)
			{
				_videoView.Visible = false;
			}
		}
	}

	private void MediaPlayerMediaChanged(object? sender, MediaPlayerMediaChangedEventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"MediaChanged");
		}

		OnGtkSourceLoaded(sender, el);
	}

	private void OnMediaPlayerTimeChange(object? sender, MediaPlayerTimeChangedEventArgs el)
	{
		var time = el is LibVLCSharp.Shared.MediaPlayerTimeChangedEventArgs e
			? TimeSpan.FromMilliseconds(e.Time)
			: TimeSpan.Zero;

		OnTimeUpdate?.Invoke(this, time);
	}

	private void OnMediaPlayerTimeChangeIsMediaParse(object? sender, MediaPlayerTimeChangedEventArgs el)
	{
		AddMediaEvents();
	}

	private void OnEndReached()
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnEndReached");
		}

		if (_videoView != null && _mediaPlayer != null)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				_isEnding = true;
				UpdateMedia();

				OnSourceEnded?.Invoke(this, EventArgs.Empty);
				if (_isLoopingEnabled)
				{
					_mediaPlayer.Play();
				}
				_videoView.Visible = true;
				_isEnding = false;
			});
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to process end reched, the player is not ready yet");
			}
		}
	}

	private void OnGtkMetadataLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null && _mediaPlayer != null && _mediaPlayer.Media != null)
		{
			Duration = (double)_mediaPlayer.Media.Duration / 1000;
			UpdateVideoStretch();

			OnMetadataLoaded?.Invoke(this, Duration);
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to process metadata loaded, the player is not ready yet");
			}
		}
	}

	private void OnGtkSourceLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = true;
			UpdateVideoStretch();

			Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);

			if (Duration > 0)
			{
				OnSourceLoaded?.Invoke(this, EventArgs.Empty);
			}
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to process source loaded, the player is not ready yet");
			}
		}
	}

	private void OnGtkSourceFailed(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = false;
		}

		OnSourceFailed?.Invoke(this, EventArgs.Empty);
	}
}
