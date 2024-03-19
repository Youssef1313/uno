#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.UI.Composition
{
	public partial class KeyFrameAnimation : CompositionAnimation
	{
		private TimeSpan _duration = TimeSpan.FromMilliseconds(250);

		internal KeyFrameAnimation() => throw new NotSupportedException();

		internal KeyFrameAnimation(Compositor compositor) : base(compositor)
		{
		}

		public AnimationStopBehavior StopBehavior { get; set; }

		public int IterationCount { get; set; }

		public AnimationIterationBehavior IterationBehavior { get; set; }

		public TimeSpan Duration
		{
			get => _duration;
			set
			{
				if (value < TimeSpan.FromMilliseconds(250) || value > TimeSpan.FromDays(24))
				{
					throw new ArgumentException($"The minimum allowed duration is 250ms, and maximum is 24 days. The value '{value}' is not allowed.");
				}

				_duration = value;
			}
		}

		public global::System.TimeSpan DelayTime { get; set; }

		public int KeyFrameCount { get; }

		public AnimationDirection Direction { get; set; }
	}
}
