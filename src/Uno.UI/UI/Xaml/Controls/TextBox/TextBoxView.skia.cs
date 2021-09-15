#nullable enable

using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal class TextBoxView
	{
		private readonly ITextBoxViewExtension? _textBoxExtension;

		private readonly WeakReference<TextBox> _textBox;

		public TextBoxView(TextBox textBox)
		{
			_textBox = new WeakReference<TextBox>(textBox);
			if (!ApiExtensibility.CreateInstance(this, out _textBoxExtension))
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"No TextBoxView implementation is available " +
						"for this Skia target. Functionality will be limited.");
				}
			}
		}

		public TextBox? TextBox
		{
			get
			{
				if (_textBox.TryGetTarget(out var target))
				{
					return target;
				}
				return null;
			}
		}

		internal int GetSelectionStart() => _textBoxExtension?.GetSelectionStart() ?? 0;

		internal int GetSelectionLength() => _textBoxExtension?.GetSelectionLength() ?? 0;

		public TextBlock DisplayBlock { get; } = new TextBlock();


		internal void SetTextNative(string text)
		{
			_textBoxExtension?.SetTextNative(text);
		}

		internal void Select(int start, int length)
		{
			_textBoxExtension?.Select(start, length);
		}

		internal void OnForegroundChanged(Brush brush) => _textBoxExtension?.SetForeground(brush);

		internal void OnFocusStateChanged(FocusState focusState)
		{
			if (focusState != FocusState.Unfocused)
			{
				_textBoxExtension?.StartEntry();
			}
			else
			{
				_textBoxExtension?.EndEntry();
			}
		}

		internal void SetIsPassword(bool isPassword)
		{
			_textBoxExtension?.SetIsPassword(isPassword);
		}

		internal void UpdateTextFromNative(string newText)
		{
			var textBox = _textBox?.GetTarget();
			if (textBox != null)
			{
				var text = textBox.ProcessTextInput(newText);
				if (text != newText)
				{
					SetTextNative(text);
				}
			}
		}

		public void UpdateMaxLength() => _textBoxExtension?.UpdateNativeView();
	}
}
