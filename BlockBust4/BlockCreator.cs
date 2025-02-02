using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace BlockBust4
{
    public static class BlockCreator
    {
        private static readonly Random _random = new Random();
        private static readonly List<List<(List<Point> Points, int Width, int Height)>> _shapeDefinitions = new List<List<(List<Point>, int, int)>>
        {
            new List<(List<Point>, int, int)>
            {
                (new List<Point> { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) }, 2, 2)
            },

            new List<(List<Point>, int, int)>
            {
                (new List<Point> { new Point(0, 0), new Point(0, 1), new Point(0, 2),
                                   new Point(1, 0), new Point(1, 1), new Point(1, 2),
                                   new Point(2, 0), new Point(2, 1), new Point(2, 2) }, 3, 3),
                (new List<Point> { new Point(0, 0), new Point(0, 1), new Point(0, 2),
                                   new Point(1, 0), new Point(1, 1), new Point(1, 2)}, 3, 2),
                (new List<Point> { new Point(0, 0), new Point(0, 1),
                                   new Point(1, 0), new Point(1, 1),
                                   new Point(2, 0), new Point(2, 1) }, 2, 3)
            },

            new List<(List<Point>, int, int)>
            {
                (new List<Point> { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(1, 1) }, 3, 2),
                (new List<Point> { new Point(1, -1), new Point(1, 0), new Point(1, 1), new Point(2, 0) }, 2, 3),
                (new List<Point> { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) }, 3, 2),
                (new List<Point> { new Point(0, 0), new Point(1, -1), new Point(1, 0), new Point(1, 1) }, 2, 3)
            },

            new List<(List<Point>, int, int)>
            {
                (new List<Point> { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(2, 1) }, 3, 2),
                (new List<Point> { new Point(1, -1), new Point(1, 0), new Point(1, 1), new Point(2, -1) }, 2, 3),
                (new List<Point> { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) }, 3, 2),
                (new List<Point> { new Point(0, 1), new Point(1, -1), new Point(1, 0), new Point(1, 1) }, 3, 2)
            },

            new List<(List<Point>, int, int)>
            {
                (new List<Point> { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(3, 0) }, 4, 1),
                (new List<Point> { new Point(0, 0), new Point(0, 1), new Point(0, 2), new Point(0, 3) }, 1, 4),
                (new List<Point> { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(2, 1) }, 3, 2),
                (new List<Point> { new Point(1, -1), new Point(1, 0), new Point(0, 0), new Point(0, 1) }, 2, 3)
            },
            new List<(List<Point>, int, int)>
            {
                (new List<Point> { new Point(0, 0), new Point(1, 0), new Point(0, 1) }, 2, 2),
                (new List<Point> { new Point(0, 0), new Point(1, 0), new Point(1, 1) }, 2, 2),
                (new List<Point> { new Point(0, 0), new Point(0, 1), new Point(1, 1) }, 2, 2),
                (new List<Point> { new Point(1, 0), new Point(0, 1), new Point(1, 1) }, 2, 2)
            }

        };

        public static Block[] GenerateSolvableCombination(GameBoard gameBoard)
        {
            int attempts = 1000;
            Globals.numberOfTries = 0;
            while (attempts-- > 0)
            {
                var blocks = GenerateRandomBlocks();

                if (IsCombinationSolvable(gameBoard, blocks))
                {
                    ColorBlocks(blocks);
                    return blocks;
                }
            }

            //MessageBox.Show("Very unlucky. No solvable combination was found in 1000 attempts", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            var blocks1 = GenerateRandomBlocks();
            ColorBlocks(blocks1);
            return blocks1;
        }

        private static Block[] GenerateRandomBlocks()
        {
            var blocks = new Block[3];
            for (int i = 0; i < 3; i++)
            {
                var shapeList = _shapeDefinitions[_random.Next(_shapeDefinitions.Count)];
                var shape = shapeList[_random.Next(shapeList.Count)];
                blocks[i] = CreateBlockFromDefinition(shape);
                blocks[i].Color = Brushes.Black; // jinak tam bude null a nebude fungovat funkce IsCombinationSolvable
            }
            return blocks;
        }

        private static void ColorBlocks(Block[] blocks)
        {
            HashSet<int> numbers = new HashSet<int>();

            while (numbers.Count < 3)
            {
                numbers.Add(_random.Next(0, 7));
            }
            int[] nums = numbers.ToArray();

            blocks[0].Color = new SolidColorBrush(Globals.predefinedColors[nums[0]]);
            blocks[1].Color = new SolidColorBrush(Globals.predefinedColors[nums[1]]);
            blocks[2].Color = new SolidColorBrush(Globals.predefinedColors[nums[2]]);
        }

        private static Block CreateBlockFromDefinition((List<Point> points, int width, int height) definition)
        {
            return new Block(
                new List<Point>(definition.points),
                definition.width,
                definition.height
            );
        }

        private static bool IsCombinationSolvable(GameBoard gameBoard, Block[] blocks)
        {
            var clonedBoard = gameBoard.Clone();
            
            return TryPlaceBlocks(clonedBoard, blocks, 0);
        }

        private static bool TryPlaceBlocks(GameBoard gameBoard, Block[] blocks, int currentIndex)
        {
            Globals.numberOfTries += 1;
            if (currentIndex >= blocks.Length)
                return true;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (gameBoard.IsValidPosition(blocks[currentIndex], new Point(col, row)))
                    {
                        var clonedBoard = gameBoard.Clone();
                        clonedBoard.TryPlaceBlock(blocks[currentIndex], new Point(col, row));
                        if (TryPlaceBlocks(clonedBoard, blocks, currentIndex + 1))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}

