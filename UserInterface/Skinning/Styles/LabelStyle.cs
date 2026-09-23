using BrewLib.Graphics.Text;
using BrewLib.Util;
using OpenTK.Graphics;

namespace BrewLib.UserInterface.Skinning.Styles
{
    public class LabelStyle : WidgetStyle
    {
        public string FontName;
        public float FontSize;
        public BoxAlignment TextAlignment;
        public StringTrimming Trimming;
        public Color4 Color;
    }
}
