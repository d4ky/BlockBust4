using BlockBust4;
using System.Text;
using System.Windows;
using System.Windows.Media;

public class GameBoard
{
    private readonly Brush?[,] _grid = new Brush[8, 8]; // row, column
    public int Score { get; private set; }

    public Brush? GetCellColor(int row, int col) => _grid[row, col];

    public void Reset()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                _grid[row, col] = null;
            }
        }
        Score = 0;
    }

    public bool TryPlaceBlock(Block block, Point position)
    {
        if (!IsValidPosition(block, position)) return false;

        PlaceBlock(block, position);
        CheckForCompletedLines(block, position);
        return true;
    }

    public bool IsValidPosition(Block block, Point position)
    {
        foreach (var relative in block.RelativePositions)
        {
            int col = (int)(position.X + relative.X);
            int row = (int)(position.Y + relative.Y);

            if (col < 0 || col >= 8 || row < 0 || row >= 8) return false;
            if (_grid[row, col] != null) return false;
        }
        return true;
    }

    private void PlaceBlock(Block block, Point position)
    {
        foreach (var relative in block.RelativePositions)
        {
            int col = (int)(position.X + relative.X);
            int row = (int)(position.Y + relative.Y);
            _grid[row, col] = block.Color;
        }
    }

    private void CheckForCompletedLines(Block block, Point position)
    {
        var rowsToCheck = new HashSet<int>();
        var colsToCheck = new HashSet<int>();

        foreach (var relative in block.RelativePositions)
        {
            rowsToCheck.Add((int)(position.Y + relative.Y));
            colsToCheck.Add((int)(position.X + relative.X));
        }

        Clear(CheckRows(rowsToCheck), CheckColumns(colsToCheck));
    }

    private List<int> CheckRows(HashSet<int> rows)
    {
        List<int> rowsToDelete = new List<int>();
        foreach (int row in rows)
        {
            if (IsRowComplete(row))
            {
                rowsToDelete.Add(row);
                Score += 80;
            }
        }
        return rowsToDelete;
    }

    private bool IsRowComplete(int row)
    {
        for (int col = 0; col < 8; col++)
        {
            if (_grid[row, col] == null) return false;
        }
        return true;
    }

    private List<int> CheckColumns(HashSet<int> columns)
    {
        List<int> colsToDelete = new List<int>();
        foreach (int col in columns)
        {
            if (IsColumnComplete(col))
            {
                colsToDelete.Add(col);
                Score += 80;
            }
        }
        return colsToDelete;
    }

    private bool IsColumnComplete(int col)
    {
        for (int row = 0; row < 8; row++)
        {
            if (_grid[row, col] == null) return false;
        }
        return true;
    }

    private void Clear(List<int> rows, List<int> cols)
    {
        foreach (var col in cols)
        {
            for (int row = 0; row < 8; row++)
            {
                if (this.Equals(Globals.gameBoard))
                {
                    ((MainWindow)Application.Current.MainWindow).PlayClearSound();
                }
                _grid[row, col] = null;
            }
        }

        foreach (var row in rows)
        {
            for (int col = 0; col < 8; col++)
            {
                if (this.Equals(Globals.gameBoard))
                {
                    ((MainWindow)Application.Current.MainWindow).PlayClearSound();
                }
                _grid[row, col] = null;
            }
        }
    }

    public GameBoard Clone()
    {
        var clone = new GameBoard();
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                clone._grid[row, col] = this._grid[row, col];
            }
        }
        clone.Score = this.Score;
        return clone;
    }
}