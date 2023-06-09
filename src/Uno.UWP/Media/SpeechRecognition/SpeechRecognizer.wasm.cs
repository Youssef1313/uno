﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Foundation;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

using NativeMethods = __Windows.Media.SpeechRecognition.SpeechRecognizer.NativeMethods;
#endif

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizer
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.Media.SpeechRecognizer";
#endif

		private readonly static ConcurrentDictionary<string, SpeechRecognizer> _instances =
			new ConcurrentDictionary<string, SpeechRecognizer>();

		private readonly Guid _instanceId = Guid.NewGuid();

		private TaskCompletionSource<SpeechRecognitionResult> _currentCompletionSource;

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static int DispatchStatus(string instanceId, string state)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				if (Enum.TryParse<SpeechRecognizerState>(state, true, out var stateEnum))
				{
					speechRecognizer.OnStateChanged(stateEnum);
				}
			}
			return 0;
		}

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static int DispatchError(string instanceId, string error)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				if (speechRecognizer._currentCompletionSource != null)
				{
					speechRecognizer._currentCompletionSource.SetException(
						new InvalidOperationException($"Speech recognition failed with '{error}'"));
				}
				else
				{
					if (typeof(SpeechRecognizer).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
					{
						typeof(SpeechRecognizer).Log().LogError($"Speech recognition failed with '{error}'");
					}
				}
			}
			return 0;
		}

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static int DispatchHypothesis(string instanceId, string hypothesis)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				speechRecognizer.OnHypothesisGenerated(hypothesis);
			}
			return 0;
		}

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static int DispatchResult(string instanceId, string result, double confidence)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				speechRecognizer.OnStateChanged(SpeechRecognizerState.Idle);
				var recognitionResult = new SpeechRecognitionResult()
				{
					Text = result,
					RawConfidence = confidence
				};
				speechRecognizer?._currentCompletionSource.SetResult(recognitionResult);
			}
			return 0;
		}

		public IAsyncOperation<SpeechRecognitionResult> RecognizeAsync() =>
			RecognizeTaskAsync().AsAsyncOperation();

		private async Task<SpeechRecognitionResult> RecognizeTaskAsync()
		{
			var existingTask = _currentCompletionSource?.Task;
			if (existingTask != null)
			{
				return await existingTask;
			}

			_currentCompletionSource = new TaskCompletionSource<SpeechRecognitionResult>();

#if NET7_0_OR_GREATER
			var recognizeResult = NativeMethods.Recognize(_instanceId.ToString());
#else
			var command = $"{JsType}.recognize('{_instanceId}')";
			var recognizeResult = bool.Parse(WebAssemblyRuntime.InvokeJS(command));
#endif

			if (!recognizeResult)
			{
				throw new InvalidOperationException(
					"Speech recognizer is not available on this device.");
			}

			var result = await _currentCompletionSource.Task;
			_currentCompletionSource = null;
			return result;
		}

		public void Dispose()
		{
			_currentCompletionSource?.SetCanceled();

#if NET7_0_OR_GREATER
			NativeMethods.RemoveInstance(_instanceId.ToString());
#else
			var removeInstanceCommand = $"{JsType}.removeInstance('{_instanceId}')";
			WebAssemblyRuntime.InvokeJS(removeInstanceCommand);
#endif
		}

		private void InitializeSpeechRecognizer()
		{
#if NET7_0_OR_GREATER
			NativeMethods.Initialize(_instanceId.ToString(), CurrentLanguage.LanguageTag);
#else
			var command = $"{JsType}.initialize('{_instanceId}','{CurrentLanguage.LanguageTag}')";
			WebAssemblyRuntime.InvokeJS(command);
#endif

			_instances.GetOrAdd(_instanceId.ToString(), this);
		}
	}
}
