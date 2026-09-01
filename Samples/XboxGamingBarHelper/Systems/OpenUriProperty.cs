using NLog;
using Shared.Enums;
using System;
using System.Diagnostics;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class OpenUriProperty : HelperProperty<string, SystemManager>
    {
        public OpenUriProperty(SystemManager inManager) : base(string.Empty, null, Function.OpenUri, inManager)
        {
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var uriString = newValue?.ToString();
            Logger.Info($"OpenUriProperty SetValue called with URI: {uriString}");
            if (!string.IsNullOrWhiteSpace(uriString))
            {
                OpenUri(uriString);
            }
            return base.SetValue(string.Empty, updatedTime);
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);
            if (!string.IsNullOrWhiteSpace(Value))
            {
                OpenUri(Value);
                value = string.Empty;
            }
        }

        public static void OpenUri(string uriString)
        {
            try
            {
                if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    Logger.Info($"Launching external URI in default browser: {uriString}");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = uriString,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Logger.Warn($"Rejected non-HTTP(S) URI for OpenUri: {uriString}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to launch external URI: {uriString}");
            }
        }
    }
}
