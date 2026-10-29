using System;
using System.Collections.Generic;
using UnityEngine;

namespace G3D
{
    public class Misc
    {
        public static G3DCameraMode CameraModeFromString(string mode)
        {
            switch (mode.ToUpper())
            {
                case "HEADTRACKING":
                    return G3DCameraMode.HEADTRACKING;
                case "HOLOBOX":
                    return G3DCameraMode.HOLOBOX;
                case "MULTIVIEW":
                    return G3DCameraMode.MULTIVIEW;
                default:
                    throw new System.ArgumentException($"Invalid camera mode: {mode}");
            }
        }
    }

    [Serializable]
    public class ObservableVariable<T>
    {
        // SerializeField allows Unity to display and save this value in the Inspector
        [SerializeField]
        private T _value;

        public event Action<T, T> Changed; // using Action<old, new> for Unity performance

        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    T oldValue = _value;
                    _value = value;
                    Changed?.Invoke(oldValue, value);
                }
            }
        }

        public ObservableVariable()
            : this(default) { }

        public ObservableVariable(T initialValue)
        {
            _value = initialValue;
        }

        // Implicit conversions for clean code syntax
        public static implicit operator T(ObservableVariable<T> variable)
        {
            if (variable == null)
                return default;
            return variable.Value;
        }

        // 2. Implicit conversion FROM T TO ObservableVariable<T>
        // Allows: ObservableVariable<T> observableVar = normalVar;
        public static implicit operator ObservableVariable<T>(T value)
        {
            return new ObservableVariable<T>(value);
        }

        // Overrides for standard variable parity
        public override string ToString() => _value?.ToString() ?? string.Empty;

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(_value);

        public override bool Equals(object obj)
        {
            if (obj is ObservableVariable<T> other)
                return EqualityComparer<T>.Default.Equals(_value, other._value);
            if (obj is T rawValue)
                return EqualityComparer<T>.Default.Equals(_value, rawValue);
            return false;
        }
    }

    // This is necessary for Unity to serialize the ObservableCameraMode class correctly in the Inspector.
    // This will have to be done for all other types you want to use with this in the inspector.
    [Serializable]
    public class ObservableCameraMode : ObservableVariable<G3DCameraMode>
    {
        public ObservableCameraMode()
            : base() { }

        public ObservableCameraMode(G3DCameraMode initialValue)
            : base(initialValue) { }
    }
}
