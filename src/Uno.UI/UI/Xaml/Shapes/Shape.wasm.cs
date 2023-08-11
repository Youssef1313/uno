using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Disposables;
using Uno;

using RadialGradientBrush = Microsoft.UI.Xaml.Media.RadialGradientBrush;

namespace Windows.UI.Xaml.Shapes
{
	[Markup.ContentProperty(Name = "SvgChildren")]
	partial class Shape
	{
		private readonly SerialDisposable _fillBrushSubscription = new SerialDisposable();
		private readonly SerialDisposable _strokeBrushSubscription = new SerialDisposable();

		private DefsSvgElement _defs;

		public UIElementCollection SvgChildren { get; }

		protected Shape() : base("svg", isSvg: true)
		{
			SvgChildren = new UIElementCollection(this);
			SvgChildren.CollectionChanged += OnSvgChildrenChanged;
		}

		protected abstract SvgElement GetMainSvgElement();

		private static readonly NotifyCollectionChangedEventHandler OnSvgChildrenChanged = (object sender, NotifyCollectionChangedEventArgs args) =>
		{
			if (sender is UIElementCollection children && children.Owner is Shape shape)
			{
				shape.OnChildrenChanged();
			}
		};

		protected virtual void OnChildrenChanged()
		{
		}

		private protected override void OnHitTestVisibilityChanged(HitTestability oldValue, HitTestability newValue)
		{
			// We don't invoke the base, so we stay at the default "pointer-events: none" defined in Uno.UI.css in class svg.uno-uielement.
			// This is required to avoid this SVG element (which is actually only a collection) to stoll pointer events.
		}

		/// <summary>
		/// Gets host for non-visual elements
		/// </summary>
		private UIElementCollection GetDefs()
		{
			if (_defs == null)
			{
				_defs = new DefsSvgElement();
				SvgChildren.Add(_defs);
			}

			return _defs.Defs;
		}
	}
}
