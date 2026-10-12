using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Shared.Data
{
    public interface IProperty : INotifyPropertyChanged
    {
        IProperty ParentProperty { get; }

        List<IProperty> ChildProperties { get; }

        bool SetValue(object newValue, long updatedTime);

        object GetValue();

        Task Sync();

        bool IsBatchUpdating { get; }

        IDisposable BeginBatchUpdate();
    }
}
