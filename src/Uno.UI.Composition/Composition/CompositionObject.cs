#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Uno.Foundation.Logging;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;

using static Microsoft.UI.Composition.SubPropertyHelpers;

namespace Microsoft.UI.Composition
{
	public partial class CompositionObject : IDisposable
	{
		private readonly ContextStore _contextStore = new ContextStore();
		private CompositionPropertySet? _properties;
		private protected Dictionary<string, (string SubProperty, CompositionAnimation Animation)>? _animations;

		internal CompositionObject()
		{
			ApiInformation.TryRaiseNotImplemented(GetType().FullName!, "The compositor constructor is not available, as the type is not implemented");
			Compositor = new Compositor();
		}

		internal CompositionObject(Compositor compositor)
		{
			Compositor = compositor;
		}

		public CompositionPropertySet Properties => _properties ??= GetProperties();

		public Compositor Compositor { get; }

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		public string? Comment { get; set; }

		private CompositionPropertySet GetProperties()
		{
			if (this is CompositionPropertySet @this)
			{
				return @this;
			}

			return new CompositionPropertySet(Compositor);
		}

		// Overrides are based on:
		// https://learn.microsoft.com/en-us/uwp/api/windows.ui.composition.compositionobject.startanimation?view=winrt-22621
		internal virtual object GetAnimatableProperty(string propertyName, string subPropertyName)
			=> TryGetFromProperties(_properties, propertyName, subPropertyName);

		internal virtual void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
			=> TryUpdateFromProperties(_properties, propertyName, subPropertyName, propertyValue);

		public void StartAnimation(string propertyName, CompositionAnimation animation)
		{
			string firstPropertyName;
			string subPropertyName;
			var firstDotIndex = propertyName.IndexOf('.');
			if (firstDotIndex > -1)
			{
				firstPropertyName = propertyName.Substring(0, firstDotIndex);
				subPropertyName = propertyName.Substring(firstDotIndex + 1);
			}
			else
			{
				firstPropertyName = propertyName;
				subPropertyName = "";
			}

			if (_animations?.ContainsKey(firstPropertyName) == true)
			{
				StopAnimation(firstPropertyName);
			}

			_animations ??= new();
			_animations[firstPropertyName] = (subPropertyName, animation);
			var animationValue = animation.Start(animation.Compositor.Timestamp, this, firstPropertyName, subPropertyName);

			try
			{
				this.SetAnimatableProperty(firstPropertyName, subPropertyName, animationValue);
			}
			catch (Exception ex)
			{
				// Important to catch the exception.
				// It can currently happen for non-implemented animations which will evaluate to null and the target animation property is value type.
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"An exception occurred while setting animation value '{animationValue}' to property '{propertyName}' for animation '{animation}'. {ex.Message}");
				}
			}
		}

		public void StopAnimation(string propertyName)
		{
			var firstDotIndex = propertyName.IndexOf('.');
			if (firstDotIndex > -1)
			{
				propertyName = propertyName.Substring(0, firstDotIndex);
			}

			if (_animations?.TryGetValue(propertyName, out var animation) == true)
			{
				animation.Animation.Stop();
				_animations.Remove(propertyName);
			}
		}

		public void Dispose() => DisposeInternal();

		private protected virtual void DisposeInternal()
		{
		}

		internal virtual void StartAnimationCore(string propertyName, CompositionAnimation animation)
		{

		}

		internal void AddContext(CompositionObject context, string? propertyName)
		{
			_contextStore.AddContext(context, propertyName);
		}

		internal void RemoveContext(CompositionObject context, string? propertyName)
		{
			_contextStore.RemoveContext(context, propertyName);
		}

		private protected void SetProperty(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref int field, int value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref float field, float value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Matrix3x2 field, Matrix3x2 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Matrix4x4 field, Matrix4x4 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Vector2 field, Vector2 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Vector3 field, Vector3 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Quaternion field, Quaternion value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Color field, Color value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetEnumProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
			where T : Enum
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
			where T : CompositionObject?
		{
			if (field == value)
			{
				return;
			}

			OnCompositionPropertyChanged(field, value, propertyName);

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetObjectProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (field?.Equals(value) ?? value == null)
			{
				return;
			}

			// This check is here for backward compatibility
			// Is this valid even for non-composition objects like interface?
			var fieldCO = field as CompositionObject;
			var valueCO = value as CompositionObject;
			if (fieldCO != null || value != null)
			{
				OnCompositionPropertyChanged(fieldCO, valueCO, propertyName);
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void OnChanged() => OnPropertyChanged(null, false);

		private protected void OnCompositionPropertyChanged(CompositionObject? oldValue, CompositionObject? newValue) => OnCompositionPropertyChanged(oldValue, newValue, null);

		private protected void OnCompositionPropertyChanged(CompositionObject? oldValue, CompositionObject? newValue, string? propertyName)
		{
			if (oldValue != null)
			{
				oldValue.RemoveContext(this, propertyName);
			}

			if (newValue != null)
			{
				newValue.AddContext(this, propertyName);
			}
		}

		private protected void OnPropertyChanged(string? propertyName, bool isSubPropertyChange)
		{
			OnPropertyChangedCore(propertyName, isSubPropertyChange);
			_contextStore.RaiseChanged();
		}

		private protected virtual void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
		}
	}
}
