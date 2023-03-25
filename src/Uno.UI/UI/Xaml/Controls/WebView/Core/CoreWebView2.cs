#nullable enable

using System;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Net.Http;
using Uno.UI.Xaml.Controls;
using System.Linq;
using System.Threading;
using System.Globalization;
using Windows.Foundation;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal const string BlankUrl = "about:blank";
	internal const string DataUriFormatString = "data:text/html;charset=utf-8;base64,{0}";
	internal static readonly Uri BlankUri = new Uri(BlankUrl);

	private readonly IWebView _owner;

	private INativeWebView? _nativeWebView;
	internal long _navigationId;
	private object _source = BlankUri;

	internal CoreWebView2(IWebView owner)
	{
		_owner = owner;
	}

	/// <summary>
	/// Gets the CoreWebView2Settings object contains various modifiable
	/// settings for the running WebView.
	/// </summary>
	public CoreWebView2Settings Settings = new();

	public void Navigate(string uri)
	{
		if (!VerifyWebViewAvailability())
		{
			return;
		}

		if (!Uri.TryCreate(uri, UriKind.Absolute, out var actualUri))
		{
			throw new InvalidOperationException(); //TODO:MZ: What exception does UWP throw here?
		}

		_source = actualUri;
		_nativeWebView.ProcessNavigation(actualUri);
	}

	public void NavigateToString(string htmlContent)
	{
		if (!VerifyWebViewAvailability())
		{
			return;
		}

		_source = htmlContent;
		_nativeWebView.ProcessNavigation(htmlContent);
	}

	internal void NavigateWithHttpRequestMessage(HttpRequestMessage requestMessage)
	{
		if (!VerifyWebViewAvailability())
		{
			return;
		}

		if (requestMessage?.RequestUri == null)
		{
			throw new ArgumentException("Invalid request message. It does not have a RequestUri.");
		}

		_source = requestMessage;
		_nativeWebView.ProcessNavigation(requestMessage);
	}

	public void GoBack() => _nativeWebView?.GoBack();

	public void GoForward() => _nativeWebView?.GoForward();

	public void Stop() => _nativeWebView?.Stop();

	public void Reload() => _nativeWebView?.Reload();

	public IAsyncOperation<string?> ExecuteScriptAsync(string javaScript) =>
		AsyncOperation.FromTask(ct =>
		{
			if (_nativeWebView is null)
			{
				return Task.FromResult<string?>(null);
			}

			return _nativeWebView.ExecuteScriptAsync(javaScript, ct);
		});

	internal void OnOwnerApplyTemplate()
	{
		_nativeWebView = GetNativeWebViewFromTemplate();

		//The nativate WebView already navigate to a blank page if no source is set.
		//Avoid a bug where invoke GoBack() on WebView do nothing in Android 4.4
		UpdateFromInternalSource();
	}

	internal void RaiseNavigationStarting(string uri, out bool cancel)
	{
		var newNavigationId = Interlocked.Increment(ref _navigationId);
		var args = new CoreWebView2NavigationStartingEventArgs((ulong)newNavigationId, uri);
		NavigationStarting?.Invoke(this, args);

		cancel = args.Cancel;
	}

	internal void RaiseNewWindowRequested()
	{
		NewWindowRequested?.Invoke(this, new());//TODO:MZ:
	}

	internal void RaiseNavigationCompleted(Uri? uri, bool isSuccess, int httpStatusCode, CoreWebView2WebErrorStatus errorStatus)
	{
		NavigationCompleted?.Invoke(this, new CoreWebView2NavigationCompletedEventArgs((ulong)_navigationId, uri, isSuccess, httpStatusCode, errorStatus));
	}

	internal void RaiseHistoryChanged() => HistoryChanged?.Invoke(this, null);

	internal void SetHistoryProperties(bool canGoBack, bool canGoForward)
	{
		CanGoBack = canGoBack;
		CanGoForward = canGoForward;
		RaiseHistoryChanged();
	}

	internal static bool GetIsHistoryEntryValid(string url) =>
		!url.IsNullOrWhiteSpace() &&
		!url.Equals(BlankUrl, StringComparison.OrdinalIgnoreCase);

	[MemberNotNullWhen(true, nameof(_nativeWebView))]
	private bool VerifyWebViewAvailability()
	{
		if (_nativeWebView == null)
		{
			if (_owner.IsLoaded)
			{
				_owner.Log().Warn(
					"This WebView control instance does not have a native WebView child, " +
					"the control template may be missing.");
			}

			return false;
		}

		return true;
	}

	private void UpdateFromInternalSource()
	{
		if (!VerifyWebViewAvailability())
		{
			return;
		}

		if (_source is Uri uri)
		{
			_nativeWebView.ProcessNavigation(uri);
			return;
		}

		if (_source is string html)
		{
			_nativeWebView.ProcessNavigation(html);
		}

		if (_source is HttpRequestMessage httpRequestMessage)
		{
			_nativeWebView.ProcessNavigation(httpRequestMessage);
		}
	}
}
