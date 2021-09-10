using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
	/// <summary>
	/// Generic base class for all variable types synchronized in Elympics snapshots.
	/// </summary>
	/// <typeparam name="T">Synchronized variable type.</typeparam>
	[Serializable]
	public abstract class ElympicsVar<T> : ElympicsVar
		where T : IEquatable<T>
	{
		public T Value
		{
			get => currentValue;
			set
			{
				if (!Equals(currentValue, value))
					ValueChanged?.Invoke(currentValue, value);
				currentValue = value;
			}
		}

		[SerializeField] private T currentValue;

		protected ElympicsVar(T value = default, bool enabledSynchronization = true) : base(enabledSynchronization) => currentValue = value;

		public delegate void ValueChangedCallback(T lastValue, T newValue);

		public event ValueChangedCallback ValueChanged;

		public override string ToString() => Value.ToString();

		public static implicit operator T(ElympicsVar<T> v) => v.Value;
	}

	/// <summary>
	/// Base class for all variable types synchronized in Elympics snapshots.
	/// </summary>
	public abstract class ElympicsVar
	{
		protected ElympicsVar(bool enabledSynchronization)
		{
			EnabledSynchronization = enabledSynchronization;
		}

		public bool EnabledSynchronization { get; }

		public abstract void Serialize(BinaryWriter bw);
		public abstract void Deserialize(BinaryReader br);
		public abstract bool Equals(BinaryReader br1, BinaryReader br2);
	}
}
