﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public sealed partial class TimelineCollection : DependencyObjectCollection<Timeline>, IList<Timeline>, IEnumerable<Timeline>
	{
		private string[] _targetedProperties;
		private Timeline m_pTimingOwner;

		public TimelineCollection()
		{
		}

		internal TimelineCollection(Timeline owner, bool isAutoPropertyInheritanceEnabled)
		{
			IsAutoPropertyInheritanceEnabled = isAutoPropertyInheritanceEnabled;
			m_pTimingOwner = owner;
			this.SetParent(owner);
		}

		private protected override void OnCollectionChanged()
		{
			base.OnCollectionChanged();

			// Clear the targeted properties cache
			_targetedProperties = null;
		}

		public new void Add(Timeline element)
		{
			base.Add(element);
			element.SetTimingParent(m_pTimingOwner);
		}

		internal string[] TargetedProperties
		{
			get
			{
				if (_targetedProperties == null)
				{
					_targetedProperties = Items
						.Select(i => i.GetTimelineTargetFullName())
						.Distinct()
						.ToArray();
				}

				return _targetedProperties;
			}
		}
	}
}
