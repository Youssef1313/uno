using System.Linq;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp
{

	public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
	{

		private int count = 0;

		public MainPage()
		{
			this.InitializeComponent();

			this.navigationView.ItemInvoked += this.NavigationView_ItemInvoked;
		}

		private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			NavigationViewItem saleItem = new NavigationViewItem() { IsSelected = true };

			this.navigationView.MenuItems.Add(saleItem);
			this.navigationView.SelectedItem = saleItem;
			if (++count == 2)
			{
				PageClose(this.navigationView.MenuItems.Last() as NavigationViewItem);
				PageClose(this.navigationView.MenuItems.Last() as NavigationViewItem);
			}
		}

		private void PageClose(NavigationViewItem item)
		{
			this.navigationView.MenuItems.Remove(item);
			this.navigationView.SelectedItem = this.navigationView.MenuItems.Last();
		}


	}

}
