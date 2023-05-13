﻿#nullable enable

using Android.Views;
using Windows.UI.Core;

namespace Uno.UI.Xaml.Core;

internal partial class RootVisual
{
	protected sealed override bool OnNativeMotionEvent(MotionEvent nativeEvent)
	{
		if (!ArePointersEnabled)
		{
			return false;
		}

		var ownerEvents = (ICoreWindowEvents)CoreWindow.GetForCurrentThread()!;
		switch (nativeEvent.ActionMasked)
		{
			// TODO: STYLUS_WITH_BARREL_DOWN, STYLUS_WITH_BARREL_MOVE, STYLUS_WITH_BARREL_UP ?
			case MotionEventActions.Down:
			case MotionEventActions.PointerDown:
				ownerEvents.RaisePointerPressed(BuildPointerArgs(nativeEvent));
				break;

			case MotionEventActions.Move:
			case MotionEventActions.HoverMove:
				ownerEvents.RaisePointerMoved(BuildPointerArgs(nativeEvent));
				break;

			case MotionEventActions.Up:
			case MotionEventActions.PointerUp:
				ownerEvents.RaisePointerReleased(BuildPointerArgs(nativeEvent));
				break;

			case MotionEventActions.Cancel:
				ownerEvents.RaisePointerCancelled(BuildPointerArgs(nativeEvent));
				break;

			case MotionEventActions.HoverEnter:
				ownerEvents.RaisePointerEntered(BuildPointerArgs(nativeEvent));
				break;

			case MotionEventActions.HoverExit:
				ownerEvents.RaisePointerExited(BuildPointerArgs(nativeEvent));
				break;
		}

		return false;
	}
}
