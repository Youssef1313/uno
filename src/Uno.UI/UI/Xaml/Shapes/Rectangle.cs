namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{

		#region RadiusY (DP)
		public static DependencyProperty RadiusYProperty { get; } = DependencyProperty.Register(
			"RadiusY",
			typeof(double),
			typeof(Rectangle),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsArrange
			)
		);

		public double RadiusY
		{
			get => (double)this.GetValue(RadiusYProperty);
			set => this.SetValue(RadiusYProperty, value);
		}
		#endregion

		#region RadiusX (DP)
		public static DependencyProperty RadiusXProperty { get; } = DependencyProperty.Register(
			"RadiusX",
			typeof(double),
			typeof(Rectangle),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsArrange
			)
		);

		public double RadiusX
		{
			get => (double)this.GetValue(RadiusXProperty);
			set => this.SetValue(RadiusXProperty, value);
		}
		#endregion

	}
}
