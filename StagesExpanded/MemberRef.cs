using System;
using System.Reflection;
using HarmonyLib;

namespace StagesExpanded
{
    /// A wrapper reference around a field or property. Useful for more easily iterating over stuff like the values of `PhaseResult`.
    public class MemberRef<T>
    {
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        private readonly Action onChange;
        
        public T Get()
        {
            return getter();
        }
        public void Set(T value)
        {
            if (!(Get()?.Equals(value) ?? false))
            {
                setter(value);
                onChange?.Invoke();
            }
        }

        public MemberRef(Func<T> getter, Action<T> setter, Action onChange = null)
        {
            this.getter = getter;
            this.setter = setter;
            this.onChange = onChange;
        }

        public static MemberRef<T> FromProperty<O>(O owner, string name, Action onChange = null)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            PropertyInfo info = typeof(O).GetProperty(name, AccessTools.all);
            
            if (info == null)
                throw new ArgumentException($"Property '{name}' not found on {typeof(O).Name}.");
            if (!info.CanRead || !info.CanWrite)
                throw new ArgumentException($"Property '{name}' must have both get and set accessors.");


            return new MemberRef<T>
            (
                () => (T) info.GetValue(owner),
                value => info.SetValue(owner, value),
                onChange
            );
        }

        public static MemberRef<T> FromField<O>(O owner, string name, Action onChange = null)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            FieldInfo info = typeof(O).GetField(name, AccessTools.all);
            
            if (info == null)
                throw new ArgumentException($"Property '{name}' not found on {typeof(O).Name}.");

            return new MemberRef<T>
            (
                () => (T) info.GetValue(owner),
                value => info.SetValue(owner, value),
                onChange
            );
        }
    }
}