
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BlockBust4
{
    public partial class MainWindow : Window
    {
        private GameBoard _gameBoard = new();
        private Block[] _currentBlocks = new Block[3];

        private Point _clickOffset;
        private Point _clickPosition;
        private double _dampingFactorY = 0.0025;
        private double _dampingFactorX = 0.005;

        private Point? _lastValidPosition;
        private bool _placementIsDisplayed;

        private bool _isDragging;
        private BlockPreviewControl _draggedControl;
        private Block _draggedBlock;
        private int _draggedIndex;

        private Point[] blockCenterPositions = new Point[3];

        private bool _isSoundOn = true;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGameGrid();
            InitializePreviewGrid();
            InitializeBlockPreviews();
            LoadNewBlocks();
            ShowMainMenu();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundMusic.Play();
            VolumeSlider.Value = BackgroundMusic.Volume;
            BackgroundMusic.MediaEnded += (s, e) => BackgroundMusic.Position = TimeSpan.Zero;
            Globals.gameBoard = _gameBoard;
            MessageBox.Show("zapni zvuk lopato");
        }

        private void InitializeBlockPreviews()
        {
            Block1.MouseDown += BlockPreview_MouseDown;
            Block2.MouseDown += BlockPreview_MouseDown;
            Block3.MouseDown += BlockPreview_MouseDown;

            blockCenterPositions[0] = new(110, 450);
            blockCenterPositions[1] = new(300, 450);
            blockCenterPositions[2] = new(490, 450);

            Score.Text = "0";
            iterationNum.Visibility = Visibility.Hidden;
        }

        private void InitializeGameGrid()
        {
            for (int i = 0; i < 64; i++)
            {
                var border = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(20, 28, 64)), // fixnout tuhle sracku
                    BorderThickness = new Thickness(0.5),
                    Background = new SolidColorBrush(Color.FromRgb(36, 44, 84)),
                };

                GameGrid.Children.Add(border);
            }
        }

        private void InitializePreviewGrid()
        {
            for (int i = 0; i < 64; i++)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0.5),
                    Background = Brushes.Transparent
                };

                PreviewGrid.Children.Add(border);
            }
        }

        private void UpdateGameGrid()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (_gameBoard.GetCellColor(row, col) != null)
                    {
                        ((Border)GameGrid.Children[row * 8 + col]).Background = _gameBoard.GetCellColor(row, col);
                    } else
                    {
                        ((Border)GameGrid.Children[row * 8 + col]).Background = new SolidColorBrush(Color.FromRgb(36, 44, 84));
                    }
                }
            }
        }

        private void UpdateScore()
        {
            Score.Text = _gameBoard.Score.ToString();
        }

        private void LoadNewBlocks()
        {
            _currentBlocks = BlockCreator.GenerateSolvableCombination(_gameBoard);

            Block1.DrawBlock(_currentBlocks[0]);
            Block2.DrawBlock(_currentBlocks[1]);
            Block3.DrawBlock(_currentBlocks[2]);

            Block1.Opacity = 1;
            Block2.Opacity = 1;
            Block3.Opacity = 1;

            Block1.IsHitTestVisible = true;
            Block2.IsHitTestVisible = true;
            Block3.IsHitTestVisible = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isDragging) return;

            var (mousePosition, mouseGridPosition) = GetMousePositions(e);

            Canvas.SetLeft(DragCanvas.Children[0], mousePosition.X);
            Canvas.SetTop(DragCanvas.Children[0], mousePosition.Y);

            var col = (int)(mouseGridPosition.X / 50);
            var row = (int)(mouseGridPosition.Y / 50);

            ShowPlacementPreview(row, col);
        }

        private (Point mousePosition, Point mouseGridPosition) GetMousePositions(MouseEventArgs e)
        {
            var mousePos = e.GetPosition(DragCanvas);
            var mouseGridPos = e.GetPosition(GameGrid);

            double deltaX = mousePos.X - _clickPosition.X;
            double deltaY = mousePos.Y - _clickPosition.Y;

            var xOffsetMultiplier = 1 - (deltaX * _dampingFactorX);
            var yOffsetMultiplier = 1 - (deltaY * _dampingFactorY);

            var offset = new Vector(_clickOffset.X * xOffsetMultiplier, _clickOffset.Y * yOffsetMultiplier);
            
            mousePos -= offset;
            mouseGridPos -= offset;

            return (mousePos, mouseGridPos);

        }

        private void BlockPreview_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is not BlockPreviewControl control) return;

            DragCanvas.Children.Clear();

            _draggedIndex = control == Block1 ? 0 : control == Block2 ? 1 : 2;
            _draggedBlock = _currentBlocks[_draggedIndex];

            if (_draggedBlock == null) return;

            PlayPickupSound();

            var dragVisual = CreateDragVisual(_draggedBlock);
            
            DragCanvas.Children.Add(dragVisual);

            _clickPosition = e.GetPosition(DragCanvas);

            _clickOffset = new(
                _clickPosition.X - blockCenterPositions[_draggedIndex].X + 25 * _draggedBlock.Width,
                _clickPosition.Y - blockCenterPositions[_draggedIndex].Y
                );

            Canvas.SetLeft(dragVisual, blockCenterPositions[_draggedIndex].X - 25 * _draggedBlock.Width);
            Canvas.SetTop(dragVisual, blockCenterPositions[_draggedIndex].Y);

            MouseUp += OnDragEnd;

            _draggedControl = control;
            control.Opacity = 0;
            control.IsHitTestVisible = false;
            control.CaptureMouse();

            _isDragging = true;
            Cursor = Cursors.Hand;
        }

        private void OnDragEnd(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            if (!_placementIsDisplayed)
            {
                CleanUpDrag();
                return;
            }

            PlayDropSound();

            var (_, mouseGridPosition) = GetMousePositions(e);
            var col = (int)(mouseGridPosition.X / 50);
            var row = (int)(mouseGridPosition.Y / 50);

            var nearestValidPosition = _lastValidPosition;
            if (nearestValidPosition.HasValue && _gameBoard.TryPlaceBlock(_draggedBlock, nearestValidPosition.Value))
            {
                UpdateScore();
                UpdateGameGrid();
            }

            _draggedControl.ReleaseMouseCapture();
            _draggedControl = null;
            _currentBlocks[_draggedIndex] = null;
            _draggedIndex = -1;

            CleanUpDrag();


            if (IsArrayOnlyNulls(_currentBlocks))
            {
                LoadNewBlocks();
                iterationNum.Text = Globals.numberOfTries.ToString();
            }
            if (HasPlayerLost())
            {
                ShowNotEnoughSpaceOverlay(true);

                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.5)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    ShowNotEnoughSpaceOverlay(false);
                    
                };
                timer.Start();
                DispatcherTimer timer2 = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.8)
                };
                timer2.Tick += (s, e) =>
                {
                    timer2.Stop();
                    PlayDoneSound();
                };
                timer2.Start();
                DispatcherTimer timer3 = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2.5)
                };
                timer3.Tick += (s, e) =>
                {
                    timer3.Stop();
                    ShowFullBlueScreenOverlay();
                    BackgroundMusic.Pause();
                };
                timer3.Start();
                Globals.bestScore = Math.Max(Globals.bestScore, _gameBoard.Score);
                BestScore.Text = Globals.bestScore.ToString();
                
                FillGameBoardAnimation();
            }
        }

        private void CleanUpDrag()
        {
            _isDragging = false;
            DragCanvas.Children.Clear();
            PreviewGrid.Children.OfType<Border>().ToList().ForEach(border => border.Background = Brushes.Transparent);

            Mouse.RemovePreviewMouseDownHandler(this, OnDragEnd);

            if (_draggedControl != null)
            {
                _draggedControl.Opacity = 1;         
                _draggedControl.IsHitTestVisible = true; 
                _draggedControl.ReleaseMouseCapture();
                _draggedControl = null;
            }

            Cursor = Cursors.Arrow;
        }

        private void ShowPlacementPreview(int row, int col)
        {
            PreviewGrid.Children.OfType<Border>().ToList().ForEach(border =>
            {
                border.Background = Brushes.Transparent;
                border.Child = null;
            });

            _placementIsDisplayed = false;

            var nearestValidPosition = FindNearestValidPosition(row, col, 0.4);
            if (nearestValidPosition.HasValue)
            {
                _lastValidPosition = nearestValidPosition;
            }
            else if (_lastValidPosition.HasValue && (Math.Abs(_lastValidPosition.Value.X - col) <= 1.5 && Math.Abs(_lastValidPosition.Value.Y - row) <= 1.5))
            {
                nearestValidPosition = _lastValidPosition;
            }

            if (nearestValidPosition.HasValue && _gameBoard.IsValidPosition(_draggedBlock, nearestValidPosition.Value))
            {
                _placementIsDisplayed = true;

                foreach (var relativePosition in _draggedBlock.RelativePositions)
                {
                    int previewCol = (int)(nearestValidPosition.Value.X + relativePosition.X);
                    int previewRow = (int)(nearestValidPosition.Value.Y + relativePosition.Y);

                    var border = (Border)PreviewGrid.Children[previewRow * 8 + previewCol];
                    border.Background = _draggedBlock.Color;
                    border.Opacity = 0.3;
                }

                bool[,] tempGrid = new bool[8, 8];
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        tempGrid[r, c] = _gameBoard.GetCellColor(r, c) != null;
                    }
                }

                foreach (var relativePosition in _draggedBlock.RelativePositions)
                {
                    int c = (int)(nearestValidPosition.Value.X + relativePosition.X);
                    int r = (int)(nearestValidPosition.Value.Y + relativePosition.Y);
                    if (r >= 0 && r < 8 && c >= 0 && c < 8)
                    {
                        tempGrid[r, c] = true;
                    }
                }

                List<int> completedRows = new List<int>();
                List<int> completedCols = new List<int>();

                for (int r = 0; r < 8; r++)
                {
                    bool isRowFull = true;
                    for (int c = 0; c < 8; c++)
                    {
                        if (!tempGrid[r, c])
                        {
                            isRowFull = false;
                            break;
                        }
                    }
                    if (isRowFull)
                        completedRows.Add(r);
                }
                for (int c = 0; c < 8; c++)
                {
                    bool isColFull = true;
                    for (int r = 0; r < 8; r++)
                    {
                        if (!tempGrid[r, c])
                        {
                            isColFull = false;
                            break;
                        }
                    }
                    if (isColFull)
                        completedCols.Add(c);
                }

                var redBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
                foreach (int r in completedRows)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        var border = (Border)PreviewGrid.Children[r * 8 + c];
                        border.Child = new Rectangle
                        {
                            Width = 30,
                            Height = 30,
                            Fill = redBrush,
                            Opacity = 2,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                    }
                }

                foreach (int c in completedCols)
                {
                    for (int r = 0; r < 8; r++)
                    {
                        var border = (Border)PreviewGrid.Children[r * 8 + c];
                        border.Child = new Rectangle
                        {
                            Width = 30,
                            Height = 30,
                            Fill = redBrush,
                            Opacity = 2,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                    }
                }
            }
        }

        private Point? FindNearestValidPosition(int row, int col, double maxDistance)
        {
            for (double d = 0; d <= maxDistance; d += 0.5)
            {
                for (double dx = -d; dx <= d; dx += 0.5)
                {
                    for (double dy = -d; dy <= d; dy += 0.5)
                    {
                        int newRow = row + (int)Math.Round(dy);
                        int newCol = col + (int)Math.Round(dx);

                        if (_gameBoard.IsValidPosition(_draggedBlock, new(newCol, newRow)))
                            return new Point(newCol, newRow);
                    }
                }
            }
            return null;
        }

        private UIElement CreateDragVisual(Block block)
        {
            var canvas = new Canvas
            {
                Effect = new DropShadowEffect
                {
                    Color = Color.FromArgb(0x2A, 0x00, 0x00, 0x00),
                    Direction = 315,
                    ShadowDepth = 60,
                    Opacity = 0.3,
                    BlurRadius = 20
                }
            };

            foreach (var relativePosition in block.RelativePositions)
            {
                var rect = new Rectangle
                {
                    Width = 50,
                    Height = 50,
                    Fill = block.Color,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(rect, relativePosition.X * 50);
                Canvas.SetTop(rect, relativePosition.Y * 50);
                canvas.Children.Add(rect);
            }

            return canvas;
        }

        private bool HasPlayerLost()
        {
            foreach (var block in _currentBlocks)
            {
                if (block == null) continue;

                for (int col = 0; col < 8; col++)
                {
                    for (int row = 0; row < 8; row++)
                    {
                        if (_gameBoard.IsValidPosition(block, new Point(col, row)))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        bool IsArrayOnlyNulls<T> (T[] array)
        {
            return array.All(a => a == null);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void ToggleSound_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Content.ToString() == "Sound: On")
                {
                    button.Content = "Sound: Off";
                    PlayButtonSound();
                    _isSoundOn = false;
                }
                else
                {
                    button.Content = "Sound: On";
                    _isSoundOn = true;
                    PlayButtonSound();
                }
            }
        }

        private void ToggleMusic_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            if (sender is Button button)
            {
                if (button.Content.ToString() == "Music: On")
                {
                    button.Content = "Music: Off";
                    BackgroundMusic.Pause();
                }
                else
                {
                    button.Content = "Music: On";
                    BackgroundMusic.Play();
                }
            }
        }

        private void ToggleDebug_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            if (sender is Button button)
            {
                if (button.Content.ToString() == "Debug: Off")
                {
                    button.Content = "Debug: On";
                    iterationNum.Visibility = Visibility.Visible;
                }
                else
                {
                    button.Content = "Debug: Off";
                    iterationNum.Visibility = Visibility.Hidden;
                }
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BackgroundMusic != null)
            {
                BackgroundMusic.Volume = VolumeSlider.Value;
            }
        }

        private void PlayPickupSound()
        {
            if (!_isSoundOn) return;

            PickupSound.Stop();
            PickupSound.Position = TimeSpan.Zero;
            PickupSound.Play();
        }

        private void PlayDoneSound()
        {
            if (!_isSoundOn) return;
            DoneSound.Stop();
            DoneSound.Position = TimeSpan.Zero;
            DoneSound.Play();
        }

        private void PlayDropSound()
        {
            if (!_isSoundOn) return;

            DropSound.Stop();
            DropSound.Position = TimeSpan.Zero;
            DropSound.Play();
        }

        private void PlayButtonSound()
        {
            if (!_isSoundOn) return;
            ButtonSound.Stop();
            ButtonSound.Position = TimeSpan.Zero;
            ButtonSound.Play();
        }

        public void PlayClearSound()
        {
            if (!_isSoundOn) return;
            ClearSound.Stop();
            ClearSound.Position = TimeSpan.Zero;
            ClearSound.Play();
        }

        private void FillGameBoardAnimation()
        {
            Storyboard storyboard = new Storyboard();

            for (int row = 7; row >= 0; row--)
            {
                for (int col = 0; col < 8; col++)
                {
                    var border = (Border)GameGrid.Children[row * 8 + col];

                    if (border.Background is SolidColorBrush brush && brush.Color == Color.FromRgb(36, 44, 84))
                    {
                        ColorAnimation colorAnimation = new ColorAnimation
                        {
                            To = GetNextColor(),
                            Duration = TimeSpan.FromSeconds(0.2),
                            BeginTime = TimeSpan.FromSeconds(1 + (7 - row) * 0.08)
                        };

                        Storyboard.SetTarget(colorAnimation, border);
                        Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Border.Background).(SolidColorBrush.Color)"));

                        storyboard.Children.Add(colorAnimation);
                    }
                }
            }
            storyboard.Begin();
        }

        private int _colorIndex = 0;

        private Color GetNextColor()
        {
            Color color = Globals.predefinedColors[_colorIndex];
            _colorIndex = (_colorIndex + 1) % Globals.predefinedColors.Count;
            return color;
        }

        private void ShowNotEnoughSpaceOverlay(bool show)
        {
            NotEnoughSpaceOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowFullBlueScreenOverlay()
        {
            FinalScore.Text = $"Score: {_gameBoard.Score}";
            FinalBestScore.Text = $"Best Score: {Globals.bestScore}";

            FullBlueScreenOverlay.Visibility = Visibility.Visible;
        }

        private void HideFullBlueScreenOverlay()
        {
            FullBlueScreenOverlay.Visibility = Visibility.Collapsed;
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            HideFullBlueScreenOverlay();
            BackgroundMusic.Play();
            _gameBoard.Reset();
            UpdateScore();
            UpdateGameGrid();
            LoadNewBlocks();
        }

        private void ShowMainMenu()
        {
            MainMenuOverlay.Visibility = Visibility.Visible;
            GameGrid.Visibility = Visibility.Collapsed;
            PreviewGrid.Visibility = Visibility.Collapsed;
            Score.Visibility = Visibility.Collapsed;
            BestScore.Visibility = Visibility.Collapsed;
            DragCanvas.Visibility = Visibility.Collapsed;
        }

        private void HideMainMenu()
        {
            MainMenuOverlay.Visibility = Visibility.Collapsed;
            GameGrid.Visibility = Visibility.Visible;
            PreviewGrid.Visibility = Visibility.Visible;
            Score.Visibility = Visibility.Visible;
            BestScore.Visibility = Visibility.Visible;
            DragCanvas.Visibility = Visibility.Visible;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonSound();
            HideMainMenu();

            _gameBoard.Reset();
            UpdateScore();
            UpdateGameGrid();
            LoadNewBlocks();
        }
    }
}
