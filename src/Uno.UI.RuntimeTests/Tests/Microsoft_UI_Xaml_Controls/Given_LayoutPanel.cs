using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public partial class Given_LayoutPanel
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Padding_Set_In_SizeChanged()
	{
#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
		var SUT = new LayoutPanel()
		{
			Width = 200,
			Height = 300,
			VerticalAlignment = VerticalAlignment.Top,
			Children =
			{
				new Ellipse()
				{
					Fill = new SolidColorBrush(Colors.DarkOrange)
				}
			}
		};

		SUT.SizeChanged += (sender, args) => SUT.Padding = new Thickness(0, 200, 0, 0);
#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(200, ((UIElement)VisualTreeHelper.GetChild(SUT, 0)).ActualOffset.Y);
	}
}
