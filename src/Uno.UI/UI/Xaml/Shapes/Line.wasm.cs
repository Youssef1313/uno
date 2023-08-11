using System;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Shapes
{
	partial class Line
	{
		private readonly SvgElement _line = new SvgElement("line");

		partial void InitializePartial()
		{
			SvgChildren.Add(_line);
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _line;
		}
	}
}
