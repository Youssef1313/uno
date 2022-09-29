using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace UITests.Windows_UI_Xaml_Controls.ToggleSwitchControl
{
    class InverseBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language) => !(bool)value;


		public object ConvertBack(object value, Type targetType, object parameter, string language) => !(bool)value;
	}
}
