using Shared.Constants;
using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Hardware
{
    internal class HardwareTelemetryProperty : HelperProperty<string, HardwareManager>
    {
        public HardwareTelemetryProperty(string inValue, HardwareManager inManager) 
            : base(inValue, null, Function.HardwareTelemetry, inManager)
        {
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            if (Manager?.Connection == null) return;

            var request = new HelperValueSet
            {
                { nameof(Command), (int)Command.Set },
                { nameof(Function), (int)function },
                { nameof(Content), Value },
                { nameof(UpdatedTime), DateTime.UtcNow.Ticks }
            };

            try
            {
                await Manager.Connection.SendMessageAsync(request.ValueSet);
            }
            catch { }
        }
    }
}
