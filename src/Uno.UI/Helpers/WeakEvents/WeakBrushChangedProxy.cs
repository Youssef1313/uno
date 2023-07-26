#nullable enable

using System;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Helpers;

internal class WeakBrushChangedProxy : WeakEventProxy<Brush, Action<Brush?>>
{
	private IDisposable? _disposable;

	private void OnBrushChanged()
	{
		if (TryGetHandler(out var handler))
		{
			_ = TryGetSource(out var brush);
			handler(brush);
		}
		else
		{
			Unsubscribe();
		}
	}

	public override void Subscribe(Brush? source, Action<Brush?> handler)
	{
		_disposable?.Dispose();

		if (TryGetSource(out var s))
		{
			s.InvalidateRender -= OnBrushChanged;
		}

		handler(source);

		if (source is not null)
		{
			_disposable = source.RegisterInvalidateRenderEvent((s, _) => handler((Brush?)s));
			//source.InvalidateRender += OnBrushChanged;
			base.Subscribe(source, handler);
		}
	}

	public override void Unsubscribe()
	{
		if (TryGetSource(out var s))
		{
			s.InvalidateRender -= OnBrushChanged;
		}

		base.Unsubscribe();
	}

	~WeakBrushChangedProxy()
	{
		Unsubscribe();
	}
}
