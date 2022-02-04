using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Box : MonoBehaviour
{
    // Box type will determine the cost to travel through the box
    public enum BoxType { Wall, Floor, Grass, Mud, Water };
    public const int BoxTypeNo = (int)BoxType.Water;

    [SerializeField] private Color[] DangerColors = new Color[8];
    [SerializeField] private SpriteState WallSpriteState;
    [SerializeField] private SpriteState NormalSpriteState;
    [SerializeField] private SpriteState GrassSpriteState;
    [SerializeField] private SpriteState MudSpriteState;
    [SerializeField] private SpriteState WaterSpriteState;

    [SerializeField] private Color DiscoveredColour;
    [SerializeField] private Color UnseenColour;
    

    private TMP_Text _textDisplay;
    private Button _button;
    private Action<Box> _changeCallback;
    private Board _board;
    private BoxType _type;


    public int EnemyIndex { get; private set; } = -1;
    public bool HasEnemy { get; private set; }
    public bool HasPlayer { get; private set; }
    public int RowIndex { get; private set; }
    public int ColumnIndex { get; private set; }
    public int ID { get; private set; }
    public int DangerNearby { get; private set; }
    public int Cost { get; private set; } = 1;
    public bool IsActive { get { return _button != null && _button.interactable; } }
    public bool IsDangerous { get; private set; }
    public bool IsWall() { return _type == BoxType.Wall; }

    public void PlayerEnter() { HasPlayer = true; }
    public void EnemyLeave() { HasEnemy = false; EnemyIndex = -1; }
    public void PlayerLeave() { HasPlayer = false; EnemyIndex = -1; }
    public void EnemyEnter(int enemyIndex) { HasEnemy = true; EnemyIndex = enemyIndex; }

    public void Setup(int id, int row, int column, Board board)
    {
        ID = id;
        RowIndex = row;
        ColumnIndex = column;
        _board = board;
        EnemyIndex = -1;
    }


    public void SetType(int type)
    {
        SetType((BoxType)type);
    }

    public void SetType(BoxType type) 
    { 
        if (IsWall()) { return; }
        _type = type;
        Cost = (int)_type;

        switch (_type)
        {
            case BoxType.Floor:
                {
                    _button.spriteState = NormalSpriteState;
                    _button.image.sprite = NormalSpriteState.highlightedSprite;
                    break;
                }
            case BoxType.Grass:
                {
                    _button.spriteState = GrassSpriteState;
                    _button.image.sprite = GrassSpriteState.highlightedSprite;
                    break;
                }

            case BoxType.Mud:
                {
                    _button.spriteState = MudSpriteState;
                    _button.image.sprite = MudSpriteState.highlightedSprite;
                    break;
                }

            case BoxType.Water:
                {
                    _button.spriteState = WaterSpriteState;
                    _button.image.sprite = WaterSpriteState.highlightedSprite;
                    break;
                }
        }
    }

    public void Charge(int dangerNearby, bool danger, Action<Box> onChange)
    {
        _changeCallback = onChange;
        DangerNearby = dangerNearby;
        IsDangerous = danger;
        ResetState();
    }

    public void UpdateDanger(int dangerNearby, bool revealSquare = true)
    {
        if (IsWall()) return;
        DangerNearby = dangerNearby;
        if (_textDisplay != null)
        {
            if (revealSquare) _textDisplay.enabled = true;
            if (DangerNearby > 0)
            {
                _textDisplay.text = DangerNearby.ToString("D");
                _textDisplay.color = DangerColors[DangerNearby - 1];
            }
            else
            {
                _textDisplay.text = string.Empty;
            }
        }
    }

    public void Wall(bool isWall)
    {
        if (_button != null)
        {
            if (isWall)
            {
                _type = BoxType.Wall;
                _button.spriteState = WallSpriteState;
                _button.image.sprite =  WallSpriteState.highlightedSprite;
                _button.interactable = false;
            }
            else
            {
                _type = BoxType.Floor;
                _button.spriteState = NormalSpriteState;
                _button.image.sprite = NormalSpriteState.highlightedSprite;
                _button.interactable = true;
            }
        }
    }
    public void Reveal()
    {
        if (IsWall()) return;

        if (_button != null)
        {
            _button.interactable = false;
            _button.image.color = DiscoveredColour;
        }

        if (_textDisplay != null)
        {
            _textDisplay.enabled = true;
        }

        if (HasEnemy)
        {
            _board.Game.Enemies[EnemyIndex].Visible(true);
        }
    }

    public void StandDown()
    {
        if (_button != null)
        {
            _button.interactable = false;
            _button.image.color = DiscoveredColour;
        }

        if (_textDisplay != null)
        {
            _textDisplay.enabled = false;
        }
    }

    public void OnClick()
    {
        if (IsWall()) return;

        if (HasEnemy)
        {
            _board.Game.Enemies[EnemyIndex].Visible(true);
        }

        if (_button != null)
        {
            _button.interactable = false;
        }

        else if (_textDisplay != null)
        {
            _textDisplay.enabled = true;
        }

        _changeCallback?.Invoke(this);
    }

    private void Awake()
    {
        _textDisplay = GetComponentInChildren<TMP_Text>(true);
        _button = GetComponent<Button>();

        // Reset to a non wall
        Wall(false);

        ResetState();
    }

    private void ResetState()
    {
        if (_textDisplay != null)
        {
            if (DangerNearby > 0)
            {
                _textDisplay.text = DangerNearby.ToString("D");
                _textDisplay.color = DangerColors[DangerNearby-1];
            }
            else
            {
                _textDisplay.text = string.Empty;
            }

            _textDisplay.enabled = false;
        }

        if (_button != null)
        {
            _button.interactable = true;
            _button.image.color = UnseenColour;
        }
    }

    public Vector2Int Get2DPos()
    {
        return new Vector2Int(ColumnIndex, RowIndex);
    }

    // Returns false if failed, refreshes the board if defused
    public bool Defuse()
    {
        // Defuse if a bomb
        if (IsDangerous)
        {
            IsDangerous = false;
            _board.BombDefused(ID);
            OnClick();

            // Update squares around the bomb
            UpdateBoxAndNeighbours();     

            return true;
        }

        // Clear the field if not
        OnClick();
        return false;
    }

    // Update the square and the squares around it
    public void UpdateBoxAndNeighbours(bool revealSquares = true)
    {
        // Test if out of bounds 
        bool topClear = ColumnIndex + 1 < _board.GetHeight();
        bool botClear = ColumnIndex - 1 >= 0;
        bool leftClear = RowIndex - 1 >= 0;
        bool rightClear = RowIndex + 1 < _board.GetWidth();

        if (topClear)
        {
            _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex + 1, RowIndex)), revealSquares);
            if (leftClear)
            {
                _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex + 1, RowIndex - 1)), revealSquares);
            }
            if (rightClear)
            {
                _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex + 1, RowIndex + 1)), revealSquares);
            }
        }
        if (botClear)
        {

            _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex - 1, RowIndex)), revealSquares);

            if (leftClear)
            {
                _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex - 1, RowIndex - 1)), revealSquares);
            }
            if (rightClear)
            {
                _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex - 1, RowIndex + 1)), revealSquares);
            }
        }
        if (leftClear)
        {
            _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex, RowIndex - 1)), revealSquares);
        }
        if (rightClear)
        {
            _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex, RowIndex + 1)), revealSquares);
        }

        _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex, RowIndex)), revealSquares);
    }
}
