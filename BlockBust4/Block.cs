using System.Windows;
using System.Windows.Media;

namespace BlockBust4
{
    public class Block
    {
        public Brush Color { get; set; }
        public List<Point> RelativePositions { get; }
        public int Width { get; }
        public int Height { get; }

        public Block(List<Point> relativePositions, int width, int height)
        {
            RelativePositions = relativePositions;
            Width = width;
            Height = height;
        }
    }
}
