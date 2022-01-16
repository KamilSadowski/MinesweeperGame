using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Box : MonoBehaviour
{
    [SerializeField] private Color[] DangerColors = new Color[8];
    [SerializeField] private Image Danger;
    [SerializeField] private Sprite WallSprite;
    [SerializeField] private Sprite NormalSprite;

    private TMP_Text _textDisplay;
    private Button _button;
    private Action<Box> _changeCallback;
    private Board _board;
    private Image _buttonImage;

    public int RowIndex { get; private set; }
    public int ColumnIndex { get; private set; }
    public int ID { get; private set; }
    public int DangerNearby { get; private set; }
    public bool IsDangerous { get; private set; }
    public bool IsWall { get; private set; }

    public bool IsActive { get { return _button != null && _button.interactable; } }

    public void Setup(int id, int row, int column, Board board)
    {
        ID = id;
        RowIndex = row;
        ColumnIndex = column;
        _board = board;
    }

    public void Charge(int dangerNearby, bool danger, Action<Box> onChange)
    {
        _changeCallback = onChange;
        DangerNearby = dangerNearby;
        IsDangerous = danger;
        ResetState();
    }

    public void UpdateDanger(int dangerNearby)
    {
        DangerNearby = dangerNearby;
        if (_textDisplay != null)
        {
            _textDisplay.enabled = true;
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
        IsWall = isWall;
        if (_button != null)
        {
            if (isWall) _button.image.sprite = WallSprite;
            else _button.image.sprite = NormalSprite;
        }

    }
    public void Reveal()
    {
        if (IsWall) { return; }

        if (_button != null)
        {
            _button.interactable = false;
        }

        if (_textDisplay != null)
        {
            _textDisplay.enabled = true;
        }
    }

    public void StandDown()
    {
        if (_button != null)
        {
            _button.interactable = false;
        }

        if (Danger != null)
        {
            Danger.enabled = false;
        }

        if (_textDisplay != null)
        {
            _textDisplay.enabled = false;
        }
    }

    public void OnClick()
    {
        if (IsWall) return;

        if(_button != null)
        {
            _button.interactable = false;
        }

        if(IsDangerous && Danger != null)
        {
            Danger.enabled = true;
        }
        else if(_textDisplay != null)
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
        _button.image.sprite = NormalSprite;
        IsWall = false;

        ResetState();
    }

    private void ResetState()
    {
        if (Danger != null)
        {
            Danger.enabled = false;
        }

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
            // Test if out of bounds 
            bool topClear = ColumnIndex + 1 < _board.GetHeight();
            bool botClear = ColumnIndex - 1 >= 0;
            bool leftClear = RowIndex - 1 >= 0;
            bool rightClear = RowIndex + 1 < _board.GetWidth();

            if (topClear)
            {              
                _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex + 1, RowIndex)));              
                if (leftClear)
                {
                    _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex + 1, RowIndex - 1)));
                }
                if (rightClear)
                {
                    _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex + 1, RowIndex + 1)));
                }
            }
            if (botClear)
            {
                
                _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex - 1, RowIndex)));
                
                if (leftClear)
                {
                    _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex - 1, RowIndex - 1)));
                }
                if (rightClear)
                {
                    _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex - 1, RowIndex + 1)));
                }
            }
            if (leftClear)
            {      
                _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex, RowIndex - 1)));
            }
            if (rightClear)
            {
                _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex, RowIndex + 1)));
            }

            _board.BoxDangerUpdate(_board.GetBoxIndex(new Vector2Int(ColumnIndex, RowIndex)));

            return true;
        }

        // Clear the field if not
        OnClick();
        return false;
    }
}
