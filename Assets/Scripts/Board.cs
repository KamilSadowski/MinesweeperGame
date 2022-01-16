using System;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public enum Event { ClickedBlank, ClickedNearDanger, ClickedDanger, Win };

    [SerializeField] private Box BoxPrefab;
    [SerializeField] public const int Width = 10;
    [SerializeField] public const int Height = 10;
    [SerializeField] private int NumberOfDangerousBoxes = 1;

    public Box[] _grid { get; private set; }
    private int[,] _grid2D = new int[Width, Height]; // 2D array holding the index of each box on the grid
    private List<int> _safeBoxes = new List<int>(); // Holds all of the boxes that are not bombs
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

    public void RechargeBoxes()
    {
        // Create a maze
        // Get edges
        for (int row = 0; row < Height; ++row)
        {
            for (int column = 0; column < Width; ++column)
            {

            }
        }

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
                _grid[index].Charge(CountDangerNearby(_dangerList, index), _dangerList[index], OnClickedBox);

                // Add non dangerous fields to the safe box array
                if (!_grid[index].IsDangerous)
                {
                    _safeBoxes.Add(index);
                }
            }
        }
    }

    private void CreateMaze()
    {

    }

    public void ActivateSquare(Vector2Int squarePosition)
    {
        _grid[_grid2D[squarePosition.x, squarePosition.y]].Reveal();
        OnClickedBox(_grid[_grid2D[squarePosition.x, squarePosition.y]]);
    }

    // Returns a random grid position that is safe
    public Vector2Int RandomSafePos()
    {
        return _safeBoxes.Count > 0 ? _grid[_safeBoxes[UnityEngine.Random.Range(0, _safeBoxes.Count)]].Get2DPos() : Vector2Int.zero;
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

    public void BoxDangerUpdate(int index)
    {
        _grid[index].UpdateDanger(CountDangerNearby(_dangerList, index));
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

        if(box.IsDangerous)
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
            if(!_grid[count].IsDangerous && _grid[count].IsActive)
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
        if (!box.IsDangerous)
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

                    if (correctRow && active)
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

    public int GetBoxIndex(Vector2Int position)
    {
        return _grid2D[position.x, position.y];
    }
}
