#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline
{
	private WeakReference<Timeline>? m_timingParent;
	private protected WeakReference<DependencyObject>? m_targetObjectWeakRef;
	private protected DependencyProperty? m_pTargetDependencyProperty;

	private void ResolveLocalTarget(CoreServices pCoreServices, Timeline? pParentTimeline)
	{
		WeakReference<DependencyObject>? targetObjectWeakRef = null;
		DependencyProperty? pTargetDependencyProperty = null;
		string strTargetName = Storyboard.GetTargetName(this);
		string strTargetProperty = Storyboard.GetTargetProperty(this);
		Timeline? pParent = null;
		Timeline? pResolveProxy = null;

		// Always clean our resolved target
		ReleaseTarget();

		// if we have a manual target object, it takes precedence
		if (_targetElement is not null)
		{
			targetObjectWeakRef = _targetElement;
		}

		// if we have a manual target DP (internal only), it takes precedence
		//if (m_pManualTargetDependencyProperty is not null)
		//{
		//	pTargetDependencyProperty = m_pManualTargetDependencyProperty;
		//}

		if (pParentTimeline != null)
		{
			// get targeting information from the parent chain
			pParent = pParentTimeline;
			while (pParent is not null)
			{
				// timelines under a dynamictimeline should use it as its proxy for resolving through templatescope.
				//if (pResolveProxy is null && pParent->OfTypeByIndex<KnownTypeIndex::DynamicTimeline>())
				//{
				//	pResolveProxy = pParent;
				//}

				// attempt to get a target name
				if (string.IsNullOrEmpty(strTargetName))
				{
					strTargetName = Storyboard.GetTargetName(pParent);
				}

				// attempt to get a property path
				if (string.IsNullOrEmpty(strTargetProperty))
				{
					strTargetProperty = Storyboard.GetTargetProperty(pParent);
				}

				// attempt to get a manually set target object
				if (targetObjectWeakRef?.TryGetTarget(out _) != true)
				{
					targetObjectWeakRef = pParent._targetElement;
				}

				// attempt to get a manually set target property
				//if (pTargetDependencyProperty is null)
				//{
				//	pTargetDependencyProperty = m_pManualTargetDependencyProperty;
				//}

				pParent = pParent.GetTimingParent();
			}
		}


		// If we don't have a target object yet, try to resolve it using name
		if (targetObjectWeakRef?.TryGetTarget(out _) != true)
		{
			DependencyObject? pTargetObject;

			// if we don't have a name specified, nothing we can do at this point
			if (string.IsNullOrEmpty(strTargetName))
			{
				return;
			}


			if (pResolveProxy is null)
			{
				// if no proxy is being used, possibly dynamictimeline needs to be used
				//if (m_pDynamicTimelineParent)
				//{
				//	pResolveProxy = m_pDynamicTimelineParent;
				//}
				//else
				{
					// still nothing than (this) is the correct object to use
					pResolveProxy = this;
				}
			}

			//
			// If the timeline was parsed as part of a ControlTemplate, then m_templatedParent will be set, as
			// will IsTemplateNamescopeMember.  If that's the case, then we use the TemplatedParent as the NamescopeOwner
			// that we pass to GetNamedObject()
			//
			//pTargetObject = this.GetContext().GetNamedObject(strTargetName,
			//	pResolveProxy.IsTemplateNamescopeMember() ? pResolveProxy.GetTemplatedParent() : pResolveProxy.GetStandardNameScopeOwner(),
			//	pResolveProxy.IsTemplateNamescopeMember() ?
			//		NameScopeType.TemplateNameScope :
			//		NameScopeType.StandardNameScope);
			// TODO: Fix pTargetObject above
			pTargetObject = null;

			// if we have a name, but it can't be found, set error with details.
			if (pTargetObject == null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Cannot resolve target for timeline with TargetName '{strTargetName}'.");
				}

				return;
			}

			// First get the named object from the value store.  This addrefs the weakref object.
			targetObjectWeakRef = new WeakReference<DependencyObject>(pTargetObject);
			pTargetObject = null;
		}

		// By this point we know we have a target object - need to resolve target property.
		var resolvedTargetObject = targetObjectWeakRef;
		if (resolvedTargetObject is not null)
		{
			// if we don't yet have a target DP, need to parse the property path
			if (pTargetDependencyProperty is null)
			{
				// if we don't have a property path, nothing we can do at this point
				if (string.IsNullOrEmpty(strTargetProperty))
				{
					return;
				}

				// Now parse the property path.  Note that this may change what we consider the
				// target object so we won't take a reference unless we succeed at parsing the
				// property path.  The refcount in the inout parameter pResolvedTargetObject should
				// stay the same.
				resolvedTargetObject.TryGetTarget(out var tempTargetObject);

				try
				{
					this.GetContext().ParsePropertyPath(
						ref tempTargetObject!,
						out pTargetDependencyProperty,
						strTargetProperty);
					resolvedTargetObject = new WeakReference<DependencyObject>(tempTargetObject);

					if (pTargetDependencyProperty is null)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError($"Couldn't find timeline's target DP for '{strTargetProperty}'.");
						}
					}
				}
				catch (Exception ex)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError($"Exception occurred while parsing property path. {ex}");
					}
				}
			}

			// By this point, we have the final resolved target object and the DP

			// Store the weak ref to the target object (add refs the final weak ref object).
			// pTargetObjectWeakRef still needs to be released, which happens in the cleanup section.
			m_targetObjectWeakRef = resolvedTargetObject;
			m_pTargetDependencyProperty = pTargetDependencyProperty;
		}
		else
		{
			// We have no target in the current tree - ignore
		}
	}

	private void SetTimingParent(Timeline parent)
	{
		m_timingParent = new WeakReference<Timeline>(parent);
	}

	private Timeline? GetTimingParent()
	{
		return m_timingParent?.TryGetTarget(out var parent) == true ? parent : null;
	}

	// Releases the WeakRef to the target object.
	private void ReleaseTarget()
	{
		//DetachDCompAnimationInstancesFromTarget();

		m_targetObjectWeakRef = null;
		m_pTargetDependencyProperty = null;
	}
}
