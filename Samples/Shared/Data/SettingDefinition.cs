using Shared.Enums;
using System;

namespace Shared.Data
{
    public class SettingDefinition<T> : ISettingDefinition<T>
    {
        public Function Function { get; }
        public string Key { get; }
        public SettingScope Scope { get; }
        public T DefaultValue { get; }
        public Type ValueType => typeof(T);
        public object DefaultRawValue => DefaultValue;

        private readonly Func<T, T> sanitizeFunc;

        public SettingDefinition(Function function, string key, SettingScope scope, T defaultValue, Func<T, T> sanitize = null)
        {
            Function = function;
            Key = key ?? function.ToString();
            Scope = scope;
            DefaultValue = defaultValue;
            sanitizeFunc = sanitize;
        }

        public T Sanitize(T value)
        {
            return sanitizeFunc != null ? sanitizeFunc(value) : value;
        }

        public object Sanitize(object value)
        {
            if (value is null)
            {
                return DefaultValue;
            }

            if (value is T typedVal)
            {
                return Sanitize(typedVal);
            }

            try
            {
                if (typeof(T).IsEnum)
                {
                    if (value is int intVal)
                        return Sanitize((T)Enum.ToObject(typeof(T), intVal));
                    if (value is string strVal)
                        return Sanitize((T)Enum.Parse(typeof(T), strVal));
                }

                if (typeof(T) == typeof(bool) && value is string sBool && bool.TryParse(sBool, out bool bVal))
                {
                    return Sanitize((T)(object)bVal);
                }

                if (typeof(T) == typeof(int) && value is string sInt && int.TryParse(sInt, out int iVal))
                {
                    return Sanitize((T)(object)iVal);
                }

                var converted = (T)Convert.ChangeType(value, typeof(T));
                return Sanitize(converted);
            }
            catch
            {
                return DefaultValue;
            }
        }
    }
}
