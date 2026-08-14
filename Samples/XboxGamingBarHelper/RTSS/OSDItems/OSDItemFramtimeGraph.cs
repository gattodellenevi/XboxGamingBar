using System.Drawing;

namespace XboxGamingBarHelper.RTSS.OSDItems
{
    internal class OSDItemFramtimeGraph : OSDItem
    {
        public override string GetOSDString(int osdLevel, IColorFormatter formatter = null)
        {
            if (osdLevel < 2)
                return string.Empty;

            formatter = formatter ?? SDRColorFormatter.Instance;
            var greenColor = formatter.Format(Color.Lime);

            if (osdLevel >= 3)
                return $"<C={greenColor}><G=<FT>,-25,-2><C>";
            else
                return $"<C={greenColor}><G=<FT>,-20,-1><C>";
        }
    }
}
