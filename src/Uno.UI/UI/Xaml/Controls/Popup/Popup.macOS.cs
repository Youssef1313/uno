using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Uno.Extensions;
using AppKit;
using System.Linq;
using System.Drawing;
using Windows.UI.Xaml.Input;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private NSView _mainWindowContent;

	public NSView MainWindowContent
	{
		get
		{
			if (_mainWindowContent == null)
			{
				_mainWindowContent = NSApplication.SharedApplication.KeyWindow?.ContentView;
			}

			return _mainWindowContent;
		}
	}

	partial void OnPopupPanelChangedPartial(PopupPanel previousPanel, PopupPanel newPanel)
	{
		if (previousPanel?.Superview != null)
		{
			// Remove the current child, if any.
			previousPanel.Children.Clear();

			previousPanel.RemoveFromSuperview();
		}

		if (newPanel != null)
		{
			if (Child != null)
			{
				// Make sure that the child does not find itself without a TemplatedParent
				if (newPanel.TemplatedParent == null)
				{
					newPanel.TemplatedParent = TemplatedParent;
				}

				newPanel.AddSubview(Child);
			}

			newPanel.Background = GetPanelBackground();

			RegisterPopupPanel();
		}
	}

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		RegisterPopupPanel();
	}

	private void RegisterPopupPanel()
	{
		if (PopupPanel == null)
		{
			PopupPanel = new PopupPanel(this);
		}

		if (PopupPanel.Superview == null)
		{
			MainWindowContent?.AddSubview(PopupPanel);
		}
	}

	partial void OnUnloadedPartial()
	{
		PopupPanel?.RemoveFromSuperview();
	}

	partial void OnChildChangedPartialNative(UIElement oldChild, UIElement newChild)
	{
		if (PopupPanel != null)
		{
			if (oldChild != null)
			{
				PopupPanel.RemoveChild(oldChild);
			}

			if (newChild != null)
			{
				PopupPanel.AddSubview(newChild);
			}
		}
	}

	partial void OnIsOpenChangedPartialNative(bool oldIsOpen, bool newIsOpen)
	{
		RegisterPopupPanel();

		UpdateLightDismissLayer(newIsOpen);

		// this is necessary as during initial measure the panel was Collapsed
		// and got 0 available size from its parent (root Window element, usually a Frame)
		// this will ensure the size of its child will be calculated properly
		PopupPanel.OnInvalidateMeasure();

		EnsureForward();
	}

	partial void OnIsLightDismissEnabledChangedPartialNative(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
	{
		RegisterPopupPanel();

		if (PopupPanel != null)
		{
			PopupPanel.Background = GetPanelBackground();
		}
	}

	private void UpdateLightDismissLayer(bool newIsOpen)
	{
		var popupPanel = PopupPanel;
		if (popupPanel != null && popupPanel.Superview != null)
		{
			if (newIsOpen)
			{
				if (popupPanel.Bounds != MainWindowContent.Bounds)
				{
					// If the Bounds are different, the screen has probably been rotated.
					// We always want the light dismiss layer to have the same bounds (and frame) as the window.
					popupPanel.Frame = MainWindowContent.Frame;
					popupPanel.Bounds = MainWindowContent.Bounds;
				}

				popupPanel.Visibility = Visibility.Visible;
			}
			else
			{
				popupPanel.Visibility = Visibility.Collapsed;
			}
		}
	}

	/// <summary>
	/// Ensure that Popup panel is forward-most in the window. This ensures it isn't hidden behind the main content, which can happen when
	/// the Popup is created during initial launch.
	/// </summary>
	private void EnsureForward()
	{
		//macOS does not have BringSubviewToFront,
		//solution based on https://stackoverflow.com/questions/4236304/os-x-version-of-bringsubviewtofront
		var popupPanel = PopupPanel;
		if (popupPanel.Layer.SuperLayer is { } superlayer)
		{
			popupPanel.Layer.RemoveFromSuperLayer();
			superlayer.AddSublayer(popupPanel.Layer);
		}
		else if (popupPanel.Superview is { } superview)
		{
			popupPanel.RemoveFromSuperview();
			superview.AddSubview(popupPanel);
		}
	}
}
