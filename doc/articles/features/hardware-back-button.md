---
uid: Uno.Features.HardwareBackButton
---

# Handling hardware back button

Some devices provide a hardware back button to handle navigation within applications. `SystemNavigationManager` provides this functionality for Uno Platform applications.

## Listening to hardware back button

The `BackRequested` event of the `SystemNavigationManager` is triggered whenever the user presses the hardware back button. To subscribe to it, first get an instance of `SystemNavigationManager` via the `GetForCurrentView` method:

```csharp
var manager = SystemNavigationManager.GetForCurrentView();
manager.BackRequested += OnBackRequested;
```

The event handler should check whether the application can handle the back button press (e.g. if it is possible to navigate back within the app's UI), and in such case perform the in-app navigation and mark the event args as `Handled`:

```csharp
private void OnBackRequested(object sender, BackRequestedEventArgs e)
{
    if (this.Frame.CanGoBack)
    {
        this.Frame.GoBack();
        e.Handled = true; // Indicates that the back request has been handled
    }
}
```

When `Handled` is set to `true`, the OS will not continue processing the request. If not set or set to `false`, the OS will navigate away from the application.

Make sure to unsubscribe from the event when no longer needed.
