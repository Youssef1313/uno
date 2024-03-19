using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Microsoft.UI.Composition;

partial class Vector3KeyFrameAnimation
{
	private SortedDictionary<float, Vector3> _keyFrames = new();
	private long? _startTimestamp;
	private Vector3 _initialValue;
	private CompositionObject _animatedObject;

	internal Vector3KeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	public void InsertKeyFrame(float normalizedProgressKey, Vector3 value, CompositionEasingFunction easingFunction)
	{
		// TODO: easingFunction parameter is unused.
		if (normalizedProgressKey < 0 || normalizedProgressKey > 1)
		{
			throw new ArgumentException($"'{nameof(normalizedProgressKey)}' must be between 0 and 1. The value '{normalizedProgressKey}' is invalid.");
		}

		_keyFrames[normalizedProgressKey] = value;
	}

	public void InsertKeyFrame(float normalizedProgressKey, Vector3 value)
	{
		_keyFrames[normalizedProgressKey] = value;
	}

	internal override object Start(long timestamp, CompositionObject animatedObject, string firstPropertyName, string subPropertyName)
	{
		_startTimestamp = timestamp;
		_animatedObject = animatedObject;
		if (_keyFrames.Count == 0)
		{
			throw new InvalidOperationException("Cannot start keyframe animation with no keyframes.");
		}

		// https://learn.microsoft.com/en-us/windows/uwp/composition/time-animations
		// At least one KeyFrame is required (the 100% or 1f keyframe).
		if (!_keyFrames.ContainsKey(1.0f))
		{
			throw new InvalidOperationException("A keyframe with progress key = 1.0f is required.");
		}

		var first = _keyFrames.First();
		_initialValue = first.Key == 0 ? first.Value : (Vector3)animatedObject.GetAnimatableProperty(firstPropertyName, subPropertyName);
		return _initialValue;
	}

	internal override void Update(long timestamp)
	{
		var currentNormalizedKey = (float)((timestamp - _startTimestamp.Value) / Duration.TotalMilliseconds);
		if (currentNormalizedKey > 1)
		{
			var finalValue = _keyFrames[1.0f];
			SetAnimationValue(finalValue);
			Stop();
			return;
		}

		if (_keyFrames.TryGetValue(currentNormalizedKey, out var currentValue))
		{
			SetAnimationValue(currentValue);
			return;
		}

		var (previousKey, previousValue) = (0.0f, _initialValue);
		var (nextKey, nextValue) = (0.0f, _initialValue);
		foreach (var (key, value) in _keyFrames)
		{
			if (key > currentNormalizedKey)
			{
				(nextKey, nextValue) = (key, value);
				break;
			}
			else
			{
				(previousKey, previousValue) = (key, value);
			}
		}

		var percent = (currentNormalizedKey - previousKey) / (nextKey - previousKey);
		currentValue = previousValue + (nextValue - previousValue) * percent;
		SetAnimationValue(currentValue);
	}

	private void SetAnimationValue(Vector3 value)
	{
		if (_animations == null)
		{
			return;
		}

		foreach (var (property, (subProperty, animation)) in _animations)
		{
			if (animation == this)
			{
				_animatedObject.SetAnimatableProperty(property, subProperty, value);
			}
		}
	}

	internal override void Stop()
	{

	}
}
