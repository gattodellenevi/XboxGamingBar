using Shared.Enums;
using System;

namespace Shared.Data
{
    public enum SettingScope
    {
        GlobalOnly,
        ProfileOrGlobal,
        Transient
    }

    public interface ISettingDefinition
    {
        Function Function { get; }
        string Key { get; }
        SettingScope Scope { get; }
        Type ValueType { get; }
        object DefaultRawValue { get; }
        object Sanitize(object value);
    }

    public interface ISettingDefinition<T> : ISettingDefinition
    {
        T DefaultValue { get; }
        T Sanitize(T value);
    }
}
