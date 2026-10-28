using Shared.Data;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace XboxGamingBar.Data
{
    public class TrayItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public TrayItem RawItem { get; }
        public string Id => RawItem?.Id ?? string.Empty;
        public string Name => RawItem?.Name ?? string.Empty;
        public string Tooltip => RawItem?.Tooltip ?? string.Empty;
        public string ProcessName => RawItem?.ProcessName ?? string.Empty;
        public int ProcessId => RawItem?.ProcessId ?? 0;
        public int X => RawItem?.X ?? 0;
        public int Y => RawItem?.Y ?? 0;
        public bool IsHidden => RawItem?.IsHidden ?? false;

        private BitmapImage iconSource;
        public BitmapImage IconSource
        {
            get => iconSource;
            private set
            {
                iconSource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasIcon));
                OnPropertyChanged(nameof(HasNoIcon));
            }
        }

        public bool HasIcon => IconSource != null;
        public bool HasNoIcon => IconSource == null;

        public TrayItemViewModel(TrayItem item)
        {
            RawItem = item;
            if (!string.IsNullOrEmpty(item?.IconBase64))
            {
                LoadIconAsync(item.IconBase64);
            }
        }

        public void Update(TrayItem newItem)
        {
            if (newItem == null || RawItem == null) return;

            bool iconChanged = !string.Equals(RawItem.IconBase64, newItem.IconBase64, StringComparison.Ordinal);
            RawItem.Name = newItem.Name;
            RawItem.Tooltip = newItem.Tooltip;
            RawItem.ProcessName = newItem.ProcessName;
            RawItem.ProcessId = newItem.ProcessId;
            RawItem.IsHidden = newItem.IsHidden;
            RawItem.X = newItem.X;
            RawItem.Y = newItem.Y;
            RawItem.IconBase64 = newItem.IconBase64;

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Tooltip));
            OnPropertyChanged(nameof(ProcessName));
            OnPropertyChanged(nameof(ProcessId));
            OnPropertyChanged(nameof(IsHidden));
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));

            if (iconChanged && !string.IsNullOrEmpty(newItem.IconBase64))
            {
                LoadIconAsync(newItem.IconBase64);
            }
        }

        private async void LoadIconAsync(string base64)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                using (var stream = new InMemoryRandomAccessStream())
                {
                    await stream.WriteAsync(bytes.AsBuffer());
                    stream.Seek(0);
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    IconSource = bitmap;
                }
            }
            catch
            {
                IconSource = null;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
