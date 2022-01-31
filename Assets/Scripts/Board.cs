using System;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public enum Event { ClickedBlank, ClickedNearDanger, ClickedDanger, Win };
    public enum Direction { Up, Down, Left, Right };

    // Holds data needed for creating a maze
    struct MazeCell
    {
        public int TopLeft { get; private set; }
        public int TopRight { get; private set; }
        public int BotLeft { get; private set; }
        public int BotRight { get; private set; }

        public MazeCell(int topLeft, int topRight, int botLeft, int botRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BotLeft = botLeft;
            BotRight = botRight;
        }
    }

    class MazeTreeNode
    {
        public int ThisCell { get; private set; }
        public int TopCell { get; private set; }
        public int BottomCell { get; private set; }
        public int RightCell { get; private set; }
        public int LeftCell { get; private set; }
        public bool Visited { get; private set; }   

        public MazeTreeNode(int thisCell, int topCell, int bottomCell, int rightCell, int leftCell, bool visited = false)
        {
            ThisCell = thisCell;
            TopCell = topCell;
            BottomCell = bottomCell;
            RightCell = rightCell;
            LeftCell = leftCell;
            Visited = visited;  
        }

        public void Visit()
        {
            Visited = true;
        }
    }

    [SerializeField] private Box BoxPrefab;
    [SerializeField] public const int Width = 10;
    [SerializeField] public const int Height = 10;
    [SerializeField] private const int NumberOfDangerousBoxes = 5;
    [SerializeField] private const int CostFieldChance = 5;
    
    public Game _game;
    public Box[] _grid { get; private set; }
    List<MazeCell> _gridCells = new List<MazeCell>();
    List<MazeTreeNode> _mazeTree = new List<MazeTreeNode>();
    // 2D array holding the index of each box on the grid (Makes updating fields less computationally expensive)
    private int[,] _grid2D = new int[Width, Height];
    // Holds all of the boxes that are not bombs
    private List<int> _safeBoxes = new List<int>(); 
    private List<bool> _dangerList = new List<bool>();
    private Vector2Int[] _neighbours;
    private RectTransform _rect;
    private Action<Event> _clickEvent;
    private int _bombsLeft;

    public void Setup(Action<Event> onClickEvent)
    {
        _clickEvent = onClickEvent;
        Clear();
    }

    public void Clear()
    {
        for (int row = 0; row < Height; ++row)
        {
            for (int column = 0; column < Width; ++column)
            {
                int index = row * Width + column;
                _grid[index].StandDown();
            }
        }
    }

    // Returns walkable fields
    void GenerateMaze()
    {
        // Create a new maze
        // Get edges
        _gridCells.Clear();
        _mazeTree.Clear();
        for (int row = 0; row < Height - 1; row += 2)
        {
            for (int column = 0; column < Width - 1; column += 2)
            {
                int index = row * Width + column;
                // Add edge
                _gridCells.Add(new MazeCell(row * Width + column,
                                            row * Width + column + 1,
                                            (row + 1) * Width + column,
                                            (row + 1) * Width + column + 1));
                // Reset wall
                // Top left will always be a passage
                _grid[_gridCells[_gridCells.Count - 1].TopLeft].Wall(true);
                _grid[_gridCells[_gridCells.Count - 1].TopRight].Wall(true);
                _grid[_gridCells[_gridCells.Count - 1].BotLeft].Wall(true);
                _grid[_gridCells[_gridCells.Count - 1].BotRight].Wall(true);

                int topNode = _gridCells.Count - (Width / 2) - 1;
                if (0 > topNode)
                {
                    topNode = -1;
                }

                int bottomNode = _gridCells.Count + (Width / 2) - 1;
                if (bottomNode > Width / 2 * Height / 2 - 1)
                {
                    bottomNode = -1;
                }

                int rightNode = _gridCells.Count + 1 - 1;
                if (rightNode % (Width / 2) == 0 || rightNode < 0)
                {
                    rightNode = -1;
                }

                int leftNode = _gridCells.Count - 1 - 1;
                if ((leftNode + 1) % (Width / 2) == 0 || leftNode >= Width * Height - 1)
                {
                    leftNode = -1;
                }


                MazeTreeNode thisNode = new MazeTreeNode(_gridCells.Count - 1, topNode, bottomNode, rightNode, leftNode);
                _mazeTree.Add(thisNode);
            }
        }

        // Choose a random starting point
        int startPoint = UnityEngine.Random.Range(0, _gridCells.Count);
        int indexToVisit = startPoint;
        Stack<int> visitedCells = new Stack<int>();
        int randDir = 0;
        List<int> possibleDirections = new List<int>(4);

        // Keep visiting neighbouring cells
        do
        {
            // Make a passage
            TrySetWall(_gridCells[indexToVisit].TopLeft, false);
            _mazeTree[indexToVisit].Visit();
            possibleDirections.Clear();

            // Chose which way to go
            randDir = -1;
            if (_mazeTree[indexToVisit].TopCell != -1 &&
                !_mazeTree[_mazeTree[indexToVisit].TopCell].Visited)
            {
                possibleDirections.Add((int)Direction.Up);
            }
            if (_mazeTree[indexToVisit].BottomCell != -1 &&
                !_mazeTree[_mazeTree[indexToVisit].BottomCell].Visited)
            {
                possibleDirections.Add((int)Direction.Down);
            }
            if (_mazeTree[indexToVisit].LeftCell != -1 &&
                !_mazeTree[_mazeTree[indexToVisit].LeftCell].Visited)
            {
                possibleDirections.Add((int)Direction.Left);
            }
            if (_mazeTree[indexToVisit].RightCell != -1 &&
                !_mazeTree[_mazeTree[indexToVisit].RightCell].Visited)
            {
                possibleDirections.Add((int)Direction.Right);
            }

            if (possibleDirections.Count == 0)
            {
                randDir = -1;
            }
            else randDir = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Count)];

            // Move onto the selected cell          
            if (randDir == (int)Direction.Up)
            {
                visitedCells.Push(_mazeTree[indexToVisit].TopCell);
                TrySetWall(_gridCells[_mazeTree[indexToVisit].TopCell].BotLeft, false);
                indexToVisit = _mazeTree[indexToVisit].TopCell;
            }
            else if (randDir == (int)Direction.Down)
            {
                TrySetWall(_gridCells[indexToVisit].BotLeft, false);
                visitedCells.Push(_mazeTree[indexToVisit].BottomCell);
                indexToVisit = _mazeTree[indexToVisit].BottomCell;
            }
            else if (randDir == (int)Direction.Left)
            {
                visitedCells.Push(_mazeTree[indexToVisit].LeftCell);
                TrySetWall(_gridCells[_mazeTree[indexToVisit].LeftCell].TopRight, false);
                indexToVisit = _mazeTree[indexToVisit].LeftCell;
            }
            else if (randDir == (int)Direction.Right)
            {
                TrySetWall(_gridCells[indexToVisit].TopRight, false);
                visitedCells.Push(_mazeTree[indexToVisit].RightCell);
                indexToVisit = _mazeTree[indexToVisit].RightCell;
            }

            // If all neighbouring cells visited, back up to the previous cell
            else
            {
                visitedCells.Pop();
                if (visitedCells.Count == 0)
                {
                    break;
                }
                indexToVisit = visitedCells.Peek();
            }

        } while (true);
    }

    public void RechargeBoxes()
    {
        GenerateMaze();

        // Set up bombs
        _dangerList.Clear();
        int size = Width * Height;
        for (int count = 0; count < size; ++count)
        {
            _dangerList.Add(count < NumberOfDangerousBoxes);
        }

        _bombsLeft = NumberOfDangerousBoxes;

        _dangerList.RandomShuffle();

        // Prepare the grid
        for (int row = 0; row < Height; ++row)
        {
            for (int column = 0; column < Width; ++column)
            {
                int index = row * Width + column;

                // Remove the wall if placing a bomb and then 2 random surrounding walls for access
                if (_dangerList[index] && _grid[index].IsWall())
                {
                    _grid[index].Wall(false);
                    int wallIndex = 0;
                    int[] surroundingWalls = { row - 1 * Width + column,
                                               row + 1 * Width + column, 
                                               row * Width + column - 1, 
                                               Width + column + 1};
                    surroundingWalls.RandomShuffle();

                    for (int wallsAdded = 0; wallsAdded < 2; ++wallIndex)
                    {
                        if (TrySetWall(surroundingWalls[wallIndex], false)) ++wallsAdded;
                    }
                }

                _grid[index].Charge(CountDangerNearby(_dangerList, index), _dangerList[index], OnClickedBox);

                // Randomise the cost of the field
                if (UnityEngine.Random.Range(0, 100) <= CostFieldChance)
                {
                    _grid[index].SetType(UnityEngine.Random.Range(2, Box.BoxTypeNo + 1));
                }
                else
                {
                    _grid[index].SetType(Box.BoxType.Floor);
                }

                // Add non dangerous fields to the safe box array
                if (!_grid[index].IsWall() && !_grid[index].IsDangerous())
                {
                    _safeBoxes.Add(index);
                }
            }
        }

    }

    public void ActivateSquare(Vector2Int squarePosition)
    {
        _grid[_grid2D[squarePosition.x, squarePosition.y]].Reveal();
        OnClickedBox(_grid[_grid2D[squarePosition.x, squarePosition.y]]);
    }

    public void EnemyLeftSquare(Vector2Int squarePosition)
    {
        _grid[_grid2D[squarePosition.x, squarePosition.y]].EmptyBox();
        _grid[_grid2D[squarePosition.x, squarePosition.y]].UpdateBoxAndNeighbours(false);
    }

    public void EnemyMovedToSquare(Vector2Int squarePosition)
    {
        if (_grid[_grid2D[squarePosition.x, squarePosition.y]].HasPlayer())
        {
            _clickEvent?.Invoke(Event.ClickedDanger);
        }
        else
        {
            _grid[_grid2D[squarePosition.x, squarePosition.y]].EnemyEnter();
            _grid[_grid2D[squarePosition.x, squarePosition.y]].UpdateBoxAndNeighbours(false);
        }        
    }

    public void PlayerLeftSquare(Vector2Int squarePosition)
    {
        _grid[_grid2D[squarePosition.x, squarePosition.y]].EmptyBox();
    }

    public void PlayerMovedToSquare(Vector2Int squarePosition)
    {
        int index = _grid2D[squarePosition.x, squarePosition.y];
        if (_grid[index].HasEnemy())
        {
            _clickEvent?.Invoke(Event.ClickedDanger);
        }
        else
        {
            _grid[index].PlayerEnter();
            // Enemy moves are adjusted based on the cost of the player move
            _game.EnemyMoves(_grid[index].Cost);
        }
    }

    // Returns a random grid position that is safe
    public Vector2Int RandomSafePos()
    {
        return _safeBoxes.Count > 0 ? _grid[_safeBoxes[UnityEngine.Random.Range(0, _safeBoxes.Count)]].Get2DPos() : Vector2Int.zero;
    }

    // Removes the returned random grid position from safe positions
    public Vector2Int RandomSafePos(bool removePos)
    {
        if (!removePos) { return RandomSafePos(); }
        else
        {
            if (_safeBoxes.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, _safeBoxes.Count);
                int gridPos = _safeBoxes[randomIndex];
                _safeBoxes.RemoveAt(randomIndex);
                return _grid[gridPos].Get2DPos();
            }
            else
            {
                return Vector2Int.zero;
            }
        }
    }

    public Vector2Int GetCenter()
    {
        return new Vector2Int(Width / 2, Height / 2);
    }

    public void BombDefused(int index)
    {
        --_bombsLeft;
        _dangerList[index] = false;
        if (_bombsLeft == 0)
        {
            // Game is won if no bombs left to defuse
            _clickEvent?.Invoke(Event.Win);
        }
    }

    public void BoxDangerUpdate(int index, bool revealSquares = true)
    {
        _grid[index].UpdateDanger(CountDangerNearby(_dangerList, index), revealSquares);
    }

    private void Awake()
    {
        _grid = new Box[Width * Height];
        _rect = transform as RectTransform;
        RectTransform boxRect = BoxPrefab.transform as RectTransform;

        _rect.sizeDelta = new Vector2(boxRect.sizeDelta.x * Width, boxRect.sizeDelta.y * Height);
        Vector2 startPosition = _rect.anchoredPosition - (_rect.sizeDelta * 0.5f) + (boxRect.sizeDelta * 0.5f);
        startPosition.y *= -1.0f;

        _neighbours = new Vector2Int[8]
        {
            new Vector2Int(-Width - 1, -1),
            new Vector2Int(-Width, -1),
            new Vector2Int(-Width + 1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(Width - 1, 1),
            new Vector2Int(Width, 1 ),
            new Vector2Int(Width + 1, 1)
        };

        for (int row = 0; row < Width; ++row)
        {
            GameObject rowObj = new GameObject(string.Format("Row{0}", row), typeof(RectTransform));
            RectTransform rowRect = rowObj.transform as RectTransform;
            rowRect.SetParent(transform);
            rowRect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, startPosition.y - (boxRect.sizeDelta.y * row));
            rowRect.sizeDelta = new Vector2(boxRect.sizeDelta.x * Width, boxRect.sizeDelta.y);
            rowRect.localScale = Vector2.one;

            for (int column = 0; column < Height; ++column)
            {
                int index = row * Width + column;
                _grid[index] = Instantiate(BoxPrefab, rowObj.transform);
                _grid[index].Setup(index, row, column, this);
                RectTransform gridBoxTransform = _grid[index].transform as RectTransform;
                _grid[index].name = string.Format("ID{0}, Row{1}, Column{2}", index, row, column);
                gridBoxTransform.anchoredPosition = new Vector2( startPosition.x + (boxRect.sizeDelta.x * column), 0.0f);
                _grid2D[column, row] = index;
            }
        }

        // Sanity check
        for(int count = 0; count < _grid.Length; ++count)
        {
            Debug.LogFormat("Count: {0}  ID: {1}  Row: {2}  Column: {3}", count, _grid[count].ID, _grid[count].RowIndex, _grid[count].ColumnIndex);
        }
    }

    private int CountDangerNearby(List<bool> danger, int index)
    {
        int result = 0;
        int boxRow = index / Width;

        if (!danger[index])
        {
            for (int count = 0; count < _neighbours.Length; ++count)
            {
                int neighbourIndex = index + _neighbours[count].x;
                int expectedRow = boxRow + _neighbours[count].y;
                int neighbourRow = neighbourIndex / Width;
                result += (expectedRow == neighbourRow && neighbourIndex >= 0 && neighbourIndex < danger.Count && danger[neighbourIndex]) ? 1 : 0;
            }
        }

        return result;
    }

    private void OnClickedBox(Box box)
    {
        Event clickEvent = Event.ClickedBlank;

        if(box.IsDangerous())
        {
            clickEvent = Event.ClickedDanger;
        }
        else if(box.DangerNearby > 0)
        {
            clickEvent = Event.ClickedNearDanger;
        }
        else
        {
            ClearNearbyBlanks(box);
        }

        _clickEvent?.Invoke(clickEvent);
    }

    private bool CheckForWin()
    {
        bool Result = true;

        for( int count = 0; Result && count < _grid.Length; ++count)
        {
            if(!_grid[count].IsDangerous() && _grid[count].IsActive)
            {
                Result = false;
            }
        }

        return Result;
    }

    private void ClearNearbyBlanks(Box box)
    {
        RecursiveClearBlanks(box);
    }

    private void RecursiveClearBlanks(Box box)
    {
        if (!box.IsDangerous())
        {
            box.Reveal();

            if (box.DangerNearby == 0)
            {
                for (int count = 0; count < _neighbours.Length; ++count)
                {
                    int neighbourIndex = box.ID + _neighbours[count].x;
                    int expectedRow = box.RowIndex + _neighbours[count].y;
                    int neighbourRow = neighbourIndex / Width;
                    bool correctRow = expectedRow == neighbourRow;
                    bool active = neighbourIndex >= 0 && neighbourIndex < _grid.Length && _grid[neighbourIndex].IsActive;

                    if (correctRow && active && !_grid[neighbourIndex].IsWall())
                    {
                        RecursiveClearBlanks(_grid[neighbourIndex]);
                    }
                }
            }
        }
    }

    public int GetHeight()
    {
        return Height;
    }

    public int GetWidth()
    {
        return Width;
    }

    public Box GetBox(Vector2Int position)
    {
        return _grid[GetBoxIndex(position)];
    }

    public Box GetBox(int index)
    {
        return _grid[index];
    }

    // Returns true if wall exists
    public bool TrySetWall(int index, bool wall)
    {
        if (index > -1 && index < _grid.Length && _grid[index] != null)
        {
            _grid[index].Wall(wall);
            return true;
        }
        return false;
    }

    // Returns true if within the boundaries of the board
    public bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= 0 && position.y >= 0 && position.x < Width && position.y < Height;
    }

    // Faster version with no out of range checks
    public int GetBoxIndex(Vector2Int position)
    {
        return _grid2D[position.x, position.y];
    }

    // Returns -1 if failed
    public int TryGetBoxIndex(Vector2Int position)
    {
        if (position.x < 0) { return -1; }
        else if (position.y < 0) { return -1; }
        else if (position.x >= Width) { return -1; }
        else if (position.y >= Height) { return -1; }
        return _grid2D[position.x, position.y];
    }

    public int TryGetBoxIndex(int x, int y)
    {
        if (x < 0) { return -1; }
        else if (y < 0) { return -1; }
        else if (x >= Width) { return -1; }
        else if (y >= Height) { return -1; }
        return _grid2D[x, y];
    }
}
