using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Data
{
    public abstract class Property : IProperty
    {
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Preserved as fallback for backward compatibility; prefer BeginBatchUpdate()
        public static bool SuppressNotification { get; set; } = false;

        private int batchUpdateCount = 0;

        public bool IsBatchUpdating => batchUpdateCount > 0 || SuppressNotification;

        public IDisposable BeginBatchUpdate()
        {
            Interlocked.Increment(ref batchUpdateCount);
            return new BatchUpdateToken(() => Interlocked.Decrement(ref batchUpdateCount));
        }

        private sealed class BatchUpdateToken : IDisposable
        {
            private Action onDispose;
            public BatchUpdateToken(Action onDispose)
            {
                this.onDispose = onDispose;
            }
            public void Dispose()
            {
                Interlocked.Exchange(ref onDispose, null)?.Invoke();
            }
        }

        private readonly IProperty parentProperty;
        public IProperty ParentProperty
        {
            get { return parentProperty; }
        }

        private readonly List<IProperty> childProperties;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual async void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<IProperty> ChildProperties
        {
            get { return childProperties; }
        }

        protected Property()
        {
            parentProperty = null;
            childProperties = new List<IProperty>();
        }

        protected Property(IProperty inParentProperty)
        {
            parentProperty = inParentProperty;
            childProperties = new List<IProperty>();
            if (parentProperty != null && !parentProperty.ChildProperties.Contains(this))
            {
                parentProperty.ChildProperties.Add(this);
            }
        }

        //public abstract bool TryGetValue<OutValueType>(out OutValueType value);

        //public abstract bool TrySetValue<InValueType>(InValueType newValue, long updatedTime);

        public abstract bool SetValue(object newValue, long updatedTime);

        public abstract object GetValue();

        public abstract Task Sync();
    }
}
