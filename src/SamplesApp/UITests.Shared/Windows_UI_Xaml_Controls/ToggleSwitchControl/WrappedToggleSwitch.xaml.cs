using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.ToggleSwitchControl
{
	public sealed partial class WrappedToggleSwitch : UserControl
    {
		public static readonly DependencyProperty SecondaryIsDisabledProperty =
			DependencyProperty.Register(
			nameof(SecondaryIsDisabled),
			typeof(bool),
			typeof(WrappedToggleSwitch),
			new PropertyMetadata(false));

		public bool SecondaryIsDisabled
		{
			get => (bool)GetValue(SecondaryIsDisabledProperty);
			set => SetValue(SecondaryIsDisabledProperty, value);
		}

		public WrappedToggleSwitch()
        {
            this.InitializeComponent();
        }
    }
}
