using System.Xml.Serialization;

namespace XboxGamingBarHelper.Settings
{
    [XmlRoot("Setting")]
    public struct Setting
    {
        [XmlElement("OnScreenDisplayProvider")]
        public int OnScreenDisplayProvider;

        [XmlElement("OnScreenDisplay")]
        public int OnScreenDisplay;

        [XmlElement("OnScreenDisplayTextSize")]
        public int OnScreenDisplayTextSize;

        public Setting(int onScreenDisplayProvider, int onScreenDisplay, int onScreenDisplayTextSize = 100)
        {
            OnScreenDisplayProvider = onScreenDisplayProvider;
            OnScreenDisplay = onScreenDisplay;
            OnScreenDisplayTextSize = onScreenDisplayTextSize > 0 ? onScreenDisplayTextSize : 100;
        }
    }
}

