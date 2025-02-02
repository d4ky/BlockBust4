using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace BlockBust4
{
    public partial class BlockPreviewControl : UserControl
    {
        public Point topLeft;

        public BlockPreviewControl()
        {
            InitializeComponent();
        }

        public void DrawBlock(Block? block)
        {
            BlockCanvas.Children.Clear();
            if (block == null) return;

            int xOffset = (150 - 35 * block.Width) / 2;

            var blockContainer = new Canvas
            {
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 10,
                    Opacity = 0.5,
                    BlurRadius = 8
                }
            };


            int minX = 0;
            int minY = 0;
            foreach (var pos in block.RelativePositions)
            {
                minX = Math.Min(minX, (int)pos.X);
                minY = Math.Min(minY, (int)pos.Y);
            }
            minX = -minX;
            minY = -minY;

            foreach (var pos in block.RelativePositions)
            {
                var border = new Border
                {
                    Width = 35,
                    Height = 35,
                    Background = block.Color,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1)
                };

                Canvas.SetLeft(border, (minX + pos.X) * 35);
                Canvas.SetTop(border, (minY + pos.Y) * 35);
                blockContainer.Children.Add(border);
            }

            Canvas.SetLeft(blockContainer, xOffset);
            Canvas.SetTop(blockContainer, 0);
            BlockCanvas.Children.Add(blockContainer);
        }
    }
}
