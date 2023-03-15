using Windows.Devices.Input;

namespace Windows.UI.Core;

internal partial class CoreWindowExtension : ICoreWindowExtension
{
	/// <inheritdoc />
	public CoreCursor PointerCursor { get; set; } = new CoreCursor(CoreCursorType.Arrow, 0);

	/// <inheritdoc />
	public void ReleasePointerCapture(PointerIdentifier pointer) { }

	/// <inheritdoc />
	public void SetPointerCapture(PointerIdentifier pointer) { }
}
