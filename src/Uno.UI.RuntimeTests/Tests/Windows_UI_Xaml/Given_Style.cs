﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.UI.RuntimeTests.Helpers;
using SamplesApp.UITests;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	// The attribute is required when running WinUI. See:
	// https://github.com/microsoft/microsoft-ui-xaml/issues/4723#issuecomment-812753123
	[Bindable]
	public sealed partial class ThrowingElement : FrameworkElement
	{
		public ThrowingElement() => throw new Exception("Inner exception");
	}

	[TestClass]
	public class Given_Style
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_StyleFailsToApply()
		{
			var controlTemplate = (ControlTemplate)XamlReader.Load("""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
								 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
								 xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml">
					<local:ThrowingElement />
				</ControlTemplate>
				""");

			var style = new Style(typeof(ContentControl))
			{
				Setters =
				{
					new Setter(ContentControl.TemplateProperty, controlTemplate)
				}
			};

			// This shouldn't throw.
			_ = new ContentControl() { Style = style };
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(true)]
		[DataRow(false)]
		public void When_TargetType_Null(bool allowBadTargetTypes)
		{
			var old = FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes;
			FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes = allowBadTargetTypes;
			try
			{
				var style = new Style();

				Assert.IsNull(style.TargetType);

				var cc = new ContentControl();
				if (allowBadTargetTypes)
				{
					cc.Style = style;
				}
				else
				{
					Assert.ThrowsException<InvalidOperationException>(() => cc.Style = style);
				}
			}
			finally
			{
				FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes = old;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(true)]
		[DataRow(false)]
		public void When_TargetType_Same_Type(bool allowBadTargetTypes)
		{
			var old = FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes;
			FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes = allowBadTargetTypes;
			try
			{
				var style = new Style(typeof(ContentControl));

				Assert.AreEqual(typeof(ContentControl), style.TargetType);

				var cc = new ContentControl();
				cc.Style = style;
			}
			finally
			{
				FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes = old;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(true)]
		[DataRow(false)]
		public void When_TargetType_Base_Type(bool allowBadTargetTypes)
		{
			var old = FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes;
			FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes = allowBadTargetTypes;
			try
			{
				var style = new Style(typeof(UIElement));

				Assert.AreEqual(typeof(UIElement), style.TargetType);

				var cc = new ContentControl();
				cc.Style = style;
			}
			finally
			{
				FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes = old;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(true)]
		[DataRow(false)]
		public void When_TargetType_Derived_Type(bool allowBadTargetTypes)
		{
			var old = FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes;
			FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes = allowBadTargetTypes;
			try
			{
				var style = new Style(typeof(Button));

				Assert.AreEqual(typeof(Button), style.TargetType);
				Assert.IsTrue(typeof(Button).IsAssignableTo(typeof(ContentControl)));

				var cc = new ContentControl();

				if (allowBadTargetTypes)
				{
					cc.Style = style;
				}
				else
				{
					Assert.ThrowsException<InvalidOperationException>(() => cc.Style = style);
				}
			}
			finally
			{
				FeatureConfiguration.FrameworkElement.AllowIncompatibleStyleTargetTypes = old;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15460")]
#if __ANDROID__
		[Ignore("Doesn't pass in CI on Android")]
#endif
		public async Task When_ImplicitStyle()
		{
			var implicitStyle = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
				},
				TargetType = typeof(ContentControl),
			};

			var explicitStyle = new Style()
			{
				TargetType = typeof(ContentControl),
			};

			var cc = new ContentControl() { Width = 100, Height = 100 };

			// On Android and iOS, ContentControl fails to load if it doesn't have content.
			cc.Content = new Border() { Width = 100, Height = 100 };

			Assert.AreEqual(HorizontalAlignment.Center, cc.HorizontalContentAlignment);

			cc.Resources.Add(typeof(ContentControl), implicitStyle);
			await UITestHelper.Load(cc);

			Assert.AreEqual(HorizontalAlignment.Stretch, cc.HorizontalContentAlignment);

			cc.Style = explicitStyle;

			Assert.AreEqual(HorizontalAlignment.Left, cc.HorizontalContentAlignment);
		}
	}
}
