#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Windows.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	[SampleControlInfo]
	public sealed partial class TextBox_Foreground_Changing : UserControl
	{
		public TextBox_Foreground_Changing()
		{
			this.InitializeComponent();
		}

		private void ChangeForeground(object sender, object e)
		{
			TextBoxForegroundBrush.Color = Colors.Blue;
			StatusTextBlock.Text = "Changed";
		}
	}
}
