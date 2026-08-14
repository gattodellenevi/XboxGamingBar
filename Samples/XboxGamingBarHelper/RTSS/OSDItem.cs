using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace XboxGamingBarHelper.RTSS
{
    internal abstract class OSDItem
    {
        private const string SingleLineSpacing = "  ";
        private const string MultipleLinesSpacing = "\t";
        private const string MultipleLinesSpacing2 = "\t\t";

        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected string name;
        protected Color baseColor;

        protected OSDItem()
        {
            name = "OSD Item";
            baseColor = Color.White;
        }

        protected OSDItem(string name, Color color)
        {
            this.name = name;
            this.baseColor = color;
        }

        public virtual string GetOSDString(int osdLevel, IColorFormatter formatter = null)
        {
            formatter = formatter ?? SDRColorFormatter.Instance;
            var osdValues = GetValues(osdLevel);

            if (osdValues == null || osdValues.Count == 0)
            {
                return string.Empty;
            }

            var valueColor = formatter.Format(Color.White);
            var osdString = $"{GetNameString(formatter)}<C={valueColor}>{(osdLevel >= 3 ? MultipleLinesSpacing2 : SingleLineSpacing)}";

            var lineBreakCounter = 0;
            for (int i = 0; i < osdValues.Count; i++)
            {
                var osdValue = osdValues[i];
                if (osdValue.Value < 0)
                {
                    osdString += $"{osdValue.Prefix}{osdValue.Unit}";
                }
                else
                {
                    if (osdValue.ShouldFloorToInt)
                    {
                        osdString += $"{osdValue.Prefix}{Math.Floor(osdValue.Value)}<S=50> {osdValue.Unit}<S>";
                    }
                    else
                    {
                        osdString += $"{osdValue.Prefix}{osdValue.Value:F1}<S=50> {osdValue.Unit}<S>";
                    }
                }

                if (i < osdValues.Count - 1)
                {
                    lineBreakCounter++;
                    if (osdLevel >= 3 && lineBreakCounter >= 2)
                    {
                        lineBreakCounter = 0;
                        osdString += "\n\t";
                    }

                    osdString += osdLevel >= 3 ? MultipleLinesSpacing : SingleLineSpacing;
                }
            }
            osdString += "<C>";

            return osdString;
        }

        protected virtual string GetNameString(IColorFormatter formatter = null)
        {
            formatter = formatter ?? SDRColorFormatter.Instance;
            return $"<C={formatter.Format(baseColor)}>{name}<C>";
        }

        protected virtual List<OSDItemValue> GetValues(int osdLevel)
        {
            return new List<OSDItemValue>();
        }
    }
}
