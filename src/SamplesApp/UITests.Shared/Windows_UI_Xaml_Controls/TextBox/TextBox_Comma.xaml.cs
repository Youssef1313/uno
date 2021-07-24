using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[SampleControlInfo("TextBox", "TextBox_Comma", description: "Bug https://github.com/unoplatform/uno/issues/5207")]
	public sealed partial class TextBox_Comma : UserControl
	{
		public TextBox_Comma()
		{
			this.InitializeComponent();
		}
	}
}
