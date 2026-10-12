using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace XboxGamingBar.ViewModels
{
    /// <summary>
    /// Base class for all widget ViewModels implementing INotifyPropertyChanged
    /// with thread-safe UI dispatching for UWP CoreDispatcher.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private CoreDispatcher _dispatcher;

        public event PropertyChangedEventHandler PropertyChanged;

        public CoreDispatcher Dispatcher
        {
            get => _dispatcher ?? CoreApplication.MainView?.CoreWindow?.Dispatcher;
            set => _dispatcher = value;
        }

        protected ViewModelBase(CoreDispatcher dispatcher = null)
        {
            _dispatcher = dispatcher;
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var disp = Dispatcher;
            if (disp != null && !disp.HasThreadAccess)
            {
                _ = disp.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void RunOnUIThread(Action action)
        {
            if (action == null) return;

            var disp = Dispatcher;
            if (disp != null && !disp.HasThreadAccess)
            {
                _ = disp.RunAsync(CoreDispatcherPriority.Normal, () => action());
            }
            else
            {
                action();
            }
        }
    }
}
