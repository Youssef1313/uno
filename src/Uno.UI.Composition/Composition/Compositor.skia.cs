#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp;
using Windows.ApplicationModel.Core;

namespace Microsoft.UI.Composition;

public partial class Compositor
{
	private Stopwatch _stopwatch = Stopwatch.StartNew();
	private List<CompositionAnimation> _activeAnimations = new List<CompositionAnimation>();

	internal bool? IsSoftwareRenderer { get; set; }
	internal long Timestamp => _stopwatch.ElapsedMilliseconds;

	internal void RenderRootVisual(SKSurface surface, ContainerVisual rootVisual)
	{
		var timestamp = Timestamp;
		foreach (var animation in _activeAnimations)
		{
			animation.Update(timestamp);
		}

		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		rootVisual.RenderRootVisual(surface);
	}

	partial void InvalidateRenderPartial(Visual visual)
	{
		CoreApplication.QueueInvalidateRender(visual.CompositionTarget);
	}
}
