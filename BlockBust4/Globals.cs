using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BlockBust4
{
    public static class Globals
    {
        public static int numberOfTries = 0;
        public static int bestScore = 0;
        public static GameBoard gameBoard;
        public static readonly List<Color> predefinedColors = new List<Color>
        {
            (Color)ColorConverter.ConvertFromString("#E1603C"),
            (Color)ColorConverter.ConvertFromString("#3FB5E3"),
            (Color)ColorConverter.ConvertFromString("#3FB446"),
            (Color)ColorConverter.ConvertFromString("#CF3833"),
            (Color)ColorConverter.ConvertFromString("#8D61D0"),
            (Color)ColorConverter.ConvertFromString("#E9B241"),
            (Color)ColorConverter.ConvertFromString("#4761E3")
        };
    }
}
