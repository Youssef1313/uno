// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// corep.h, xpcore.cpp

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.Xaml.Core
{
	internal class CoreServices
	{
		private static Lazy<CoreServices> _instance = new Lazy<CoreServices>(() => new CoreServices());

		private VisualTree? _mainVisualTree;

#if UNO_HAS_ENHANCED_LIFECYCLE

		private static int _isAdditionalFrameRequested;

		public EventManager EventManager { get; private set; }
#endif

		public CoreServices()
		{
			ContentRootCoordinator = new ContentRootCoordinator(this);
#if UNO_HAS_ENHANCED_LIFECYCLE
			EventManager = EventManager.Create();
#endif
		}

#if UNO_HAS_ENHANCED_LIFECYCLE
		private static XamlRoot? GetXamlRoot()
		{
			if (CoreServices.Instance.ContentRootCoordinator.ContentRoots.Count > 0)
			{
				return CoreServices.Instance.ContentRootCoordinator.ContentRoots[0].XamlRoot;
			}

			if (CoreServices.Instance.MainVisualTree is { } mainVisualTree)
			{
				return mainVisualTree.XamlRoot;
			}

			return null;
		}

		internal static void RequestAdditionalFrame()
		{
			if (GetXamlRoot() is { Bounds: { Width: not 0, Height: not 0 } } &&
				Interlocked.CompareExchange(ref _isAdditionalFrameRequested, 1, 0) == 0)
			{
				// This lambda is intentionally static. It shouldn't capture anything to avoid allocations.
				NativeDispatcher.Main.Enqueue(static () => OnTick(), NativeDispatcherPriority.Normal);
			}
		}

		private static void OnTick()
		{
			_isAdditionalFrameRequested = 0;

			// NOTE: The below code should really be replaced with just this:
			// ----------------------------
			//if (GetXamlRoot()?.VisualTree?.RootElement is { } root)
			//{
			//	root.UpdateLayout();
			//
			//	if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
			//	{
			//		CoreServices.Instance.EventManager.RaiseLoadedEvent();
			//		root.UpdateLayout();
			//	}
			//}
			// -----------------------------
			// However, as we don't yet have XamlIslandRootCollection, we will need to enumerate the windows through ApplicationHelper.Windows.

			// This happens for Islands.
			if (GetXamlRoot() is { HostWindow: null, VisualTree.RootElement: { } xamlIsland })
			{
				xamlIsland.UpdateLayout();

				if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
				{
					CoreServices.Instance.EventManager.RaiseLoadedEvent();
					xamlIsland.UpdateLayout();
				}
			}

			foreach (var window in ApplicationHelper.WindowsInternal)
			{
				if (window.RootElement is not { } root)
				{
					continue;
				}

				root.UpdateLayout();

				if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
				{
					CoreServices.Instance.EventManager.RaiseLoadedEvent();
					root.UpdateLayout();
				}
			}
		}
#endif

		// TODO Uno: This will not be a singleton when multi-window setups are supported.
		public static CoreServices Instance => _instance.Value;

		/// <summary>
		/// Provides the content root coordinator.
		/// </summary>
		public ContentRootCoordinator ContentRootCoordinator { get; }

		/// <summary>
		/// Initialization type.
		/// </summary>
		public InitializationType InitializationType { get; internal set; } =
#if HAS_UNO_WINUI
			InitializationType.IslandsOnly;
#else
			InitializationType.MainView;
#endif

		public RootVisual? MainRootVisual => _mainVisualTree?.RootVisual;

		public PopupRoot? MainPopupRoot => _mainVisualTree?.PopupRoot;

		public Canvas? MainFocusVisualRoot => _mainVisualTree?.FocusVisualRoot;

		public FullWindowMediaRoot? MainFullWindowMediaRoot => _mainVisualTree?.FullWindowMediaRoot;

		public VisualTree? MainVisualTree => _mainVisualTree;

		public UIElement? VisualRoot => _mainVisualTree?.PublicRootVisual;

		internal void InitCoreWindowContentRoot()
		{
			if (_mainVisualTree is not null)
			{
				return;
			}

			var contentRoot = ContentRootCoordinator.CreateContentRoot(ContentRootType.CoreWindow, ThemingHelper.GetRootVisualBackground(), null);
			_mainVisualTree = contentRoot.VisualTree;

			//TODO Uno: Add input services
			//m_inputServices.attach(new CInputServices(this));

			//// While the tree is loading, delay async processing (such as downloads and drawing)
			//// until we're ready to raise the Loaded event and render the first frame.
			//if (m_pBrowserHost)
			//{
			//	m_isMainTreeLoading = TRUE;
			//}
		}

		internal bool IsXamlVisible()
		{
			// TODO Uno: This is currently highly simplified, adjust when all islands are rooted under main tree.
			return ContentRootCoordinator.CoreWindowContentRoot?.CompositionContent.IsSiteVisible ?? false;
		}

		[NotImplemented]
		internal void UIARaiseFocusChangedEventOnUIAWindow(DependencyObject sender)
		{
		}

#if UNO_HAS_ENHANCED_LIFECYCLE
		internal void RaisePendingLoadedRequests()
		{
			EventManager.RequestRaiseLoadedEventOnNextTick();
		}
#endif

		internal void ParsePropertyPath(ref DependencyObject ppTarget, out DependencyProperty? ppDP, string strPath, Dictionary<string, DependencyProperty>? resolvedPathTypes = null)
		{
			ppDP = null;

			XNAME nameClass = default;
			XNAME nameProperty;
			var pString = strPath.AsSpan();
			int nIndex = -1;
			bool bParen;
			bool bResolve;
			DependencyObject pTargetNoRef = ppTarget;
			DependencyObject? pTempDO = null;
			DependencyObject? pShareableCandidateNoRef = null;
			DependencyObject? pClone = null;
			Type pClass;
			DependencyProperty? nPropertyIndex = null;
			object? value;

			// Get the metadata for the current object.  It is error to not find it.
			pClass = pTargetNoRef.GetType();

			pString = pString.TrimStart();

			while (pString.Length > 0)
			{
				// Check for initial parenthesis

				if ('(' == pString[0])
				{
					bParen = true;
					pString = pString.Slice(1);
				}
				else
				{
					bParen = false;
				}

				// This could either be a class or a property, we won't know until we find
				// a period in the property path.  Initially we'll assume it is a property.

				NameFromString(pString, out pString, out nameProperty);

				pString = pString.TrimStart();

				// If there is a period here then the previous name was the class.

				if (pString.Length > 0 && '.' == pString[0])
				{
					pString = pString.Slice(1);

					// Make the previous name the type

					nameClass = nameProperty;

					// Read the property name

					NameFromString(pString, out pString, out nameProperty);
				}
				else
				{
					nameClass.Name = string.Empty;
				}

				pString = pString.TrimStart();

				// If there was an open parenthesis look for the closing one.

				if (bParen)
				{
					if (pString.Length == 0 || ')' != pString[0])
					{
						throw new ArgumentException();
					}

					pString = pString.Slice(1);
				}

				pString = pString.TrimStart();

				// Get the DP for this part of the path
				GetPropertyIndexForPropertyPath(
					pTargetNoRef,
					bParen,
					nameClass,
					nameProperty,
					pClass,
					out nPropertyIndex,
					resolvedPathTypes);

				bResolve = true;

				// If we are at the first step of a multi-step property path, check to
				//  see if we need to substitute a cloned copy for the actual
				//  animation.  This is so animations targeting pTargetNoRef. pdp would not
				//  affect other objects sharing the same value.
				if (pTargetNoRef == ppTarget &&
					pString.Length > 0 && ('[' == pString[0] || '.' == pString[0]))
				{
					// Retrieve that property value.
					value = pTargetNoRef.GetValue(nPropertyIndex);

					// Make sure we get an non-NULL object
					if (value == null)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError($"DP '{nPropertyIndex?.Name}' evaluated to null on target '{pTargetNoRef}'");
						}

						return;
					}

					pShareableCandidateNoRef = value as DependencyObject;

					// Can't use DoPointerCast because CNoParentShareableDependencyObject is hidden from public view
					//  and is hence absent in the class hierarchy metadata.
					if (pShareableCandidateNoRef is IShareableDependencyObject pShareable)
					{
						// If the value was already clone of a shared value, we would not need
						//  to clone it again.
						if (!pShareable.IsClone)
						{
							// See if this Sharable object implements cloning.
							pClone = pShareable.Clone();

							// If this ASSERT fails, the Clone() implementation of pShareable failed to
							//  call the correct constructor, or the constructor didn't properly call
							//  its base type's constructor.  This flag is set all the way at the end
							//  of the chain in CNoParentShareableDependencyObject(CNoParentShareableDependencyObject&,HRESULT&)
							Debug.Assert(pClone is IShareableDependencyObject { IsClone: true });

							value = pClone;
							pTargetNoRef.SetValue(nPropertyIndex, value);
						}
					}

					// Clean up
					value = null;
				}

				// Deal with indexed properties such as:
				// <... TargetProperty='RenderTransform.Children[3].X' ...>
				// Here we need to get the third object from the collection specified by
				// the property named 'Children'

				if (pString.Length > 0 && ('[' == pString[0]))
				{
					bResolve = false;
					IEnumerable? pCollectionNoRef = null;

					// Move to the new object, release the old one if it's not the original one we started with
					if (pTargetNoRef != ppTarget)
					{
						Debug.Assert(pTargetNoRef == pTempDO);
					}

					value = pTargetNoRef.GetValue(nPropertyIndex);

					// Make sure we get an non-NULL object
					if (value is null)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError($"DP '{nPropertyIndex?.Name}' evaluated to null on target '{pTargetNoRef}'");
						}

						return;
					}

					pTargetNoRef = (DependencyObject)value;

					// Get its metadata
					pClass = pTargetNoRef.GetType();

					pString = pString.Slice(1);

					bool hasValidIndexValue;
					var closeBracketIndex = pString.IndexOf(']');
					if (closeBracketIndex <= 0)
					{
						hasValidIndexValue = false;
					}
					else
					{
						hasValidIndexValue = int.TryParse(pString.Slice(0, closeBracketIndex), out nIndex);
						pString = pString.Slice(closeBracketIndex);
					}
					//var cPrevious = pString.Length;
					//SignedFromDecimalString(pString, out pString, (XINT32*)&nIndex);
					//bool hasValidIndexValue = (cString != cPrevious);

					pString = pString.TrimStart();

					// Fail on invalid index value or not finding a closing bracket.

					if (!hasValidIndexValue || nIndex < 0 || pString.Length == 0 || ']' != pString[0])
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError($"Failed to parse index in property path");
						}

						return;
					}

					pString = pString.Slice(1);

					// The target object must be a collection
					if (pTargetNoRef is not DependencyObjectCollectionBase)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError($"The target object is unexpectedly not a DOCollection");
						}

						return;
					}
					pCollectionNoRef = (IEnumerable)pTargetNoRef;

					pTargetNoRef = (DependencyObject)pCollectionNoRef.ElementAt(nIndex);

					// Every time we get a new DO, we take a reference to it.
					// This is so we are consistent with public APIs such as GetValue
					// and collection methods.
					pTempDO = pTargetNoRef;

					// Get its metadata
					pClass = pTargetNoRef.GetType();
				}

				pString = pString.TrimStart();

				// We must either be done or have a period here to indicate a sub-property

				if (pString.Length > 0)
				{
					if ('.' != pString[0])
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError($"Expected '.', but found {pString}");
						}
					}

					pString = pString.Slice(1);

					pString = pString.TrimStart();

					if (bResolve)
					{
						// Move to the new object, release the old one if it's not the original one we started with
						if (pTargetNoRef != ppTarget)
						{
							Debug.Assert(pTargetNoRef == pTempDO);
						}

						value = pTargetNoRef.GetValue(nPropertyIndex);

						// Make sure we get an non-NULL object
						if (value is null)
						{
							if (this.Log().IsEnabled(LogLevel.Error))
							{
								this.Log().LogError($"DP '{nPropertyIndex?.Name}' evaluated to null on target '{pTargetNoRef}'");
							}

							return;
						}

						pTargetNoRef = (DependencyObject)value;

						// Get its metadata
						pClass = pTargetNoRef.GetType();
					}
				}
			}

			ppTarget = pTargetNoRef;
			ppDP = nPropertyIndex;
		}

		private struct XNAME
		{
			public string Namespace;
			public string Name;
		}

		private void NameFromString(ReadOnlySpan<char> pString, out ReadOnlySpan<char> ppSuffix, out XNAME pName)
		{
			pString = pString.TrimStart();

			// We won't know whether or not there is a namespace until we parse the string.
			// Assume we don't have one for now.
			var length1 = 0;
			while (pString.Length > length1 &&
				(char.IsAsciiLetterOrDigit(pString[length1]) || pString[length1] == '_'))
			{
				length1++;
			}

			pName.Name = pString.Slice(0, length1).ToString();
			pString = pString.Slice(length1);

			// If the current character is a colon then what we guessed was the name was
			// actually the namespace instead.  Deal with it.

			if (pString.Length > 0 && ':' == pString[0])
			{
				pString = pString.Slice(1);
				pName.Namespace = pName.Name;

				var length2 = 0;
				while (pString.Length > length2 &&
					(char.IsAsciiLetterOrDigit(pString[length2]) || pString[length2] == '_'))
				{
					length2++;
				}

				pName.Name = pString.Slice(0, length2).ToString();
				pString = pString.Slice(length2);
			}
			else
			{
				pName.Namespace = string.Empty;
			}

			ppSuffix = pString;
		}

		//--------------------------------------------------------------------------------
		//
		//  GetPropertyIndexForPropertyPath
		//
		//  Given a portion of a property path, attempt to find the DP.  If it can't
		//  be found, return an error.
		//
		//--------------------------------------------------------------------------------

		private void GetPropertyIndexForPropertyPath(
			DependencyObject pTarget,
			bool bParen,
			XNAME pNameClass,
			XNAME pNameProperty,
			Type pClass,
			out DependencyProperty? pnPropertyIndex, Dictionary<string, DependencyProperty>? resolvedPathTypes = null)
		{
			var attempts = 0;
			Type? nClassID = null;
			DependencyProperty? nCustomPropertyIndex = null;
			pnPropertyIndex = null;
			DependencyProperty? pDP = null;
			StringBuilder nameBuilder;

			// Create a name builder
			nameBuilder = new StringBuilder();

			while (attempts < 2 && (pnPropertyIndex == null) && (nCustomPropertyIndex == null))
			{
				// If we didn't find the property on the first try, try again but look for custom
				// properties.

				if (attempts == 1)
				{
					nClassID = pTarget.GetType();
				}

				// If this clause of the property path was parenthetical then the class
				// and property might represent an attached property. Check for that now.

				if (bParen)
				{
					if (pNameClass.Name.Length > 0)
					{
						if (pNameClass.Namespace.Length > 0)
						{
							nameBuilder.Append(pNameClass.Name);
							nameBuilder.Append(':');
						}

						nameBuilder.Append(pNameClass.Name);
						nameBuilder.Append('.');
					}

					if (pNameProperty.Namespace.Length > 0)
					{
						nameBuilder.Append(pNameProperty.Namespace);
						nameBuilder.Append(':');
					}

					nameBuilder.Append(pNameProperty.Name);

					if (attempts == 0)
					{
						string searchDP;

						searchDP = nameBuilder.ToString();

						resolvedPathTypes?.TryGetValue(searchDP, out pDP);

						if (pDP != null)
						{
							pnPropertyIndex = pDP;
						}
						else
						{
							TryGetAttachedPropertyByName(searchDP, out pDP);
							pnPropertyIndex = pDP;
							//IFC_RETURN(DirectUI::MetadataAPI::TryGetDependencyPropertyByFullyQualifiedName(
							//	searchDP,
							//	nullptr, // XamlServiceProviderContext
							//	&pDP));
							//if (pDP != nullptr && DirectUI::MetadataAPI::IsAssignableFrom(pDP->GetTargetType(), pClass))
							//{
							//	*pnPropertyIndex = pDP->GetIndex();
							//}
						}
					}
					else
					{
						TryGetDependencyPropertyByName(
							nClassID,
							nameBuilder.ToString(),
							out pDP);
						if (pDP != null)
						{
							nCustomPropertyIndex = pDP;
						}
					}
				}

				// If it wasn't an attached property then just try to find the property in
				// the current target object.

				if ((pnPropertyIndex == null) && (nCustomPropertyIndex == null))
				{
					nameBuilder.Clear();

					if (pNameProperty.Namespace.Length > 0)
					{
						nameBuilder.Append(pNameProperty.Namespace);
						nameBuilder.Append(':'));
					}

					nameBuilder.Append(pNameProperty.Name);

					if (attempts == 0)
					{
						TryGetDependencyPropertyByName(
							pClass,
							nameBuilder.ToString(),
							out pDP);
						if (pDP != null)
						{
							pnPropertyIndex = pDP;
						}
					}
					else
					{
						TryGetDependencyPropertyByName(
							nClassID,
							nameBuilder.ToString(),
							out pDP);
						if (pDP != null)
						{
							nCustomPropertyIndex = pDP;
						}
					}
				}

				nameBuilder.Clear();
				attempts++;
			}

			if (pnPropertyIndex == null)
			{
				// Failure to find the specified property here is an error
				if (nCustomPropertyIndex == null)
				{
					throw new ArgumentException();
				}
				else
				{
					pnPropertyIndex = nCustomPropertyIndex;
				}
			}
		}

		// Tries to resolve an attached property by its name. strName should use the format ClassName.PropertyName. This should only
		// be called for built-in attached properties.
		private void TryGetAttachedPropertyByName(string strName, out DependencyProperty? ppDP)
		{
			ppDP = null;

			// The property was not found or we can't use the parser context. Assume that the property is in the default namespace.
			// The property will be of the format: ClassName.PropertyName.
			// We will extract the class name first then the property name and do the lookup using the type tables.
			var nDotIndex = strName.IndexOf('.');
			if (nDotIndex != -1)
			{
				// Try to resolve the type.
				string strClassName = strName.Substring(0, nDotIndex);
				Type pType = GetBuiltinClassInfoByName(strClassName);

				if (pType != null)
				{
					// Try to resolve the property.
					string strPropertyName = strName.Substring(nDotIndex + 1, strName.Length - strClassName.Length - 1);
					TryGetDependencyPropertyByName(pType, strPropertyName, out ppDP);
				}
			}
		}
	}
}
