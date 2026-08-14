using System.Drawing;

namespace XboxGamingBarHelper.RTSS.OSDItems
{
    internal class OSDItemFPS : OSDItem
    {
        public override string GetOSDString(int osdLevel, IColorFormatter formatter = null)
        {
            formatter = formatter ?? SDRColorFormatter.Instance;
            var redColor = formatter.Format(Color.Red);
            var whiteColor = formatter.Format(Color.White);
            return $"<C={redColor}><APP><C>{(osdLevel == 1 ? " " : (osdLevel == 2 ? "  " : "\t\t"))}<C={whiteColor}><FR><S=50> FPS<S><C>";
        }
    }
}
