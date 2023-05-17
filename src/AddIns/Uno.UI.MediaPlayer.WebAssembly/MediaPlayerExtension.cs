﻿#nullable enable

using System;
using Uno.Media.Playback;
using Windows.Media.Core;
using Uno.Extensions;
using System.IO;
using Uno.Foundation.Logging;
using System.Collections.Generic;
using Uno;
using Uno.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System.Diagnostics.CodeAnalysis;
using Windows.ApplicationModel.Background;
using Uno.Foundation.Extensibility;
using Windows.UI.Xaml.Controls.Maps;
using System.Numerics;

[assembly: ApiExtension(typeof(IMediaPlayerExtension), typeof(Uno.UI.Media.MediaPlayerExtension))]

namespace Uno.UI.Media;

public partial class MediaPlayerExtension : IMediaPlayerExtension
{
	private static Dictionary<MediaPlayer, MediaPlayerExtension> _instances = new();

	private readonly MediaPlayer _owner;
	private HtmlMediaPlayer? _player;

	private bool _updatingPosition;
	private bool _isPlayRequested;
	private bool _isPlayerPrepared;
	private List<Uri>? _playlistItems;
	private int _playlistIndex;
	private TimeSpan _naturalDuration;
	private Uri? _uri;
	private bool _anonymousCors = FeatureConfiguration.AnonymousCorsDefault;

	public MediaPlayerExtension(object owner)
	{
		if (owner is MediaPlayer player)
		{
			_owner = player;
		}
		else
		{
			throw new InvalidOperationException($"MediaPlayerPresenterExtension must be initialized with a MediaPlayer instance");
		}

		lock (_instances)
		{
			_instances[_owner] = this;
		}
	}

	~MediaPlayerExtension()
	{
		lock (_instances)
		{
			_instances.Remove(_owner);
		}
	}

	internal static MediaPlayerExtension? GetByMediaPlayer(MediaPlayer mediaPlayer)
	{
		lock (_instances)
		{
			return _instances.TryGetValue(mediaPlayer, out var instance) ? instance : null;
		}
	}

	internal HtmlMediaPlayer? HtmlPlayer
	{
		get => _player;
		set
		{
			if (value != null)
			{
				_player = value;
				InitializePlayer();
			}
		}
	}

	void IMediaPlayerExtension.OnOptionChanged(string name, object value)
	{
		switch (name)
		{
			case "AnonymousCORS" when value is bool enabled:
				_anonymousCors = enabled;
				ApplyAnonymousCors();
				break;
		}
	}

	private void ApplyAnonymousCors()
		=> _player?.SetAnonymousCORS(_anonymousCors);

	public IMediaPlayerEventsExtension? Events { get; set; }

	private double _playbackRate;
	public double PlaybackRate
	{
		get => _playbackRate;
		set
		{
			_playbackRate = value;
			if (_player is not null)
			{
				_player.PlaybackRate = value;
			}
		}
	}

	private bool _isLoopingEnabled;
	public bool IsLoopingEnabled
	{
		get => _isLoopingEnabled;
		set
		{
			_isLoopingEnabled = value;
			if (_player is not null)
			{
				_player.SetIsLoopingEnabled(value);
			}
		}
	}

	public MediaPlayerState CurrentState { get; private set; }

	public TimeSpan NaturalDuration
	{
		get => _naturalDuration;
		internal set
		{
			_naturalDuration = value;

			Events?.NaturalDurationChanged();
		}
	}

	public bool IsProtected
		=> false;

	public double BufferingProgress
		=> 0.0;

	public bool CanPause
		=> true;

	public bool CanSeek
		=> true;

	public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }

	public MediaPlayerAudioCategory AudioCategory { get; set; }

	public TimeSpan TimelineControllerPositionOffset
	{
		get => Position;
		set => Position = value;
	}

	public bool RealTimePlayback { get; set; }

	public double AudioBalance { get; set; }

	public TimeSpan Position
	{
		get => TimeSpan.FromSeconds(_player?.CurrentPosition ?? 0);
		set
		{
			if (!_updatingPosition)
			{
				_updatingPosition = true;

				try
				{
					if (_owner.PlaybackSession.PlaybackState != MediaPlaybackState.None && _player is not null && _player.Source is not null)
					{
						_player.CurrentPosition = (int)value.TotalSeconds;
						OnSeekComplete();
					}
				}
				finally
				{
					_updatingPosition = false;
				}
			}
		}
	}

	public void Dispose()
	{
		_instances.Remove(_owner);

		TryDisposePlayer();
	}

	public void Initialize()
		=> InitializePlayer();

	private void InitializePlayer()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.InitializePlayer ({_player})");
		}

		if (_player is null)
		{
			return;
		}

		_player.OnSourceFailed -= OnError;
		_player.OnSourceLoaded -= OnPrepared;
		_player.OnSourceEnded -= OnCompletion;
		_player.OnTimeUpdate -= OnTimeUpdate;
		_player.OnSourceFailed += OnError;
		_player.OnSourceLoaded += OnPrepared;
		_player.OnSourceEnded += OnCompletion;
		_player.OnTimeUpdate += OnTimeUpdate;

		_owner.PlaybackSession.PlaybackStateChanged -= OnStatusChanged;
		_owner.PlaybackSession.PlaybackStateChanged += OnStatusChanged;

		ApplyAnonymousCors();
		ApplyVideoSource();
	}

	private void SetPlaylistItems(MediaPlaybackList playlist)
	{
		_playlistItems = playlist.Items
			.Select(i => i.Source.Uri)
			.ToList();
	}

	public void InitializeSource()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug("Enter MediaPlayerExtension.InitializeSource().");
		}

		NaturalDuration = TimeSpan.Zero;
		if (Position != TimeSpan.Zero)
		{
			Position = TimeSpan.Zero;
		}

		// Reset player
		TryDisposePlayer();

		if (_owner.Source == null)
		{
			return;
		}

		try
		{
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
			InitializePlayer();

			switch (_owner.Source)
			{
				case MediaPlaybackList playlist when playlist.Items.Count > 0 && _playlistItems is not null:
					SetPlaylistItems(playlist);
					_uri = _playlistItems[0];
					break;

				case MediaPlaybackItem item:
					_uri = item.Source.Uri;
					break;

				case MediaSource source:
					_uri = source.Uri;
					break;

				default:
					throw new InvalidOperationException("Unsupported media source type");
			}

			ApplyVideoSource();
			Events?.RaiseMediaOpened();
			Events?.RaiseSourceChanged();
		}
		catch (global::System.Exception ex)
		{

			this.Log().Debug($"MediaPlayerElementExtension.InitializeSource({ex.Message})");
			OnMediaFailed(ex);
		}
	}

	private void ApplyVideoSource()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerElementExtension.SetVideoSource({_uri})");
		}

		if (_player is not null && _uri is not null)
		{
			_player.Source = _uri.OriginalString;
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"MediaPlayerElementExtension.SetVideoSource: failed (Player is not available)");
			}
		}
	}

	public void Pause()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.Pause()");
		}

		if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
		{
			_player?.Pause();
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
		}
	}

	public void Play()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.Play()");
		}

		if (_owner.Source == null || _player == null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"MediaPlayerExtension.Play(): Failed {_owner.Source} / {_player}");
			}
			return;
		}

		try
		{
			// If we reached the end of media, we need to reset position to 0
			if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.None)
			{
				_owner.PlaybackSession.Position = TimeSpan.Zero;
			}

			_isPlayRequested = true;

			if (_isPlayerPrepared)
			{
				_player.PlaybackRate = 1;
				_player.Play();
				_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"MediaPlayerExtension.Play(): Player was not prepared");
				}
			}
		}
		catch (global::System.Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"MediaPlayerExtension.Play(): Failed {ex}");
			}
			OnMediaFailed(ex);
		}
	}

	public void SetFileSource(IStorageFile file)
		=> throw new NotSupportedException($"IStorageFile is not supported");

	public void SetMediaSource(IMediaSource source)
		=> throw new NotSupportedException($"IMediaSource is not supported");

	public void SetStreamSource(IRandomAccessStream stream)
		=> throw new NotSupportedException($"IRandomAccessStream is not supported");

	public void SetSurfaceSize(Size size)
		=> throw new NotSupportedException($"SetSurfaceSize is not supported");

	public void SetUriSource(Uri uri)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.SetUriSource({uri})");
		}

		if (_player is not null)
		{
			_player.Source = uri.OriginalString;
		}
	}

	public void StepBackwardOneFrame()
		=> throw new NotSupportedException($"StepBackwardOneFrame is not supported");

	public void StepForwardOneFrame()
		=> throw new NotSupportedException($"StepForwardOneFrame is not supported");

	public void Stop()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.Stop()");
		}

		if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
			|| _owner.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
		{
			_player?.Pause(); // Do not call stop, otherwise player will need to be prepared again
			_owner.PlaybackSession.Position = TimeSpan.Zero;
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}
	}

	public void ToggleMute()
	{
		if (_owner.IsMuted)
		{
			_player?.SetVolume(0);
		}
		else
		{
			var volume = (float)_owner.Volume / 100;
			_player?.SetVolume(volume);
		}
	}

	private void OnStatusChanged(MediaPlaybackSession? sender, object args)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnStatusChanged: {args}");
		}

		if ((MediaPlaybackState)args == MediaPlaybackState.Playing)
		{
			_player?.Play();
		}
	}

	public void OnPrepared(object? sender, object what)
	{
		if (sender is HtmlMediaPlayer mp && _player is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"OnPrepared: {_player.Duration}");
			}

			NaturalDuration = TimeSpan.FromSeconds(_player.Duration);

			if (mp.IsVideo && Events is not null)
			{
				try
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"OnPrepared: {mp.VideoWidth}x{mp.VideoHeight}");
					}

					Events.RaiseVideoRatioChanged((double)mp.VideoWidth / global::System.Math.Max(mp.VideoHeight, 1));
				}
				catch { }
			}

			if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
			{
				if (_isPlayRequested)
				{
					_player.Play();
					_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				}
				else
				{
					// To display first image of media when setting a new source. Otherwise, last image of previous source remains visible
					_player.Play();
					_player.Stop();
				}
			}

			_isPlayerPrepared = true;
		}
	}

	void OnError(object? sender, object what)
	{
		if (_owner.PlaybackSession.PlaybackState != MediaPlaybackState.None)
		{
			_player?.Stop();
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnError: {what}");
		}

		OnMediaFailed(message: $"MediaPlayer Error: {(string)what}");
	}

	public void OnCompletion(object? sender, object what)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnCompletion: {_owner.Position}");
		}

		Events?.RaiseMediaEnded();
		_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;

		// Play next item in playlist, if any
		if (_playlistItems != null && _playlistIndex < _playlistItems.Count - 1)
		{
			_uri = _playlistItems[++_playlistIndex];
			ApplyVideoSource();
		}
	}

	private void OnMediaFailed(global::System.Exception? ex = null, string? message = null)
	{
		Events?.RaiseMediaFailed(MediaPlayerError.Unknown, message ?? ex?.Message, ex);

		this.Log().Debug($"MediaPlayerElementExtension.OnMediaFailed({message})");
		_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
	}

	public void OnVolumeChanged()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnVolumeChanged: {_owner.Volume}");
		}

		var volume = (float)_owner.Volume / 100;
		_player?.SetVolume(volume);
	}

	private void OnTimeUpdate(object? sender, object what)
	{
		try
		{
			_updatingPosition = true;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnTimeUpdate: {Position}");
			}

			Events?.RaisePositionChanged();
		}
		finally
		{
			_updatingPosition = false;
		}
	}

	public void OnSeekComplete()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OnSeekComplete: {Position}");
		}

		Events?.RaiseSeekCompleted();
	}

	private void TryDisposePlayer()
	{
		if (_player != null)
		{
			_isPlayRequested = false;
			_isPlayerPrepared = false;
		}
	}
}