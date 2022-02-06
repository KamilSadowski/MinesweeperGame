using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Box : MonoBehaviour
{
    // Box type will determine the cost to travel through the box
    public enum BoxType { Wall, Floor, Grass, Mud, Water };
    enum ShadingLevel { Lit, Shaded, Dark };
    public const int BoxTypeNo = (int)BoxType.Water;

    [SerializeField] private Color[] DangerColors = new Color[8];
    [SerializeField] private SpriteState WallSpriteState;
    [SerializeField] private SpriteState NormalSpriteState;
    [SerializeField] private SpriteState GrassSpriteState;
    [SerializeField] private SpriteState MudSpriteState;
    [SerializeField] private SpriteState WaterSpriteState;

    [SerializeField] private Color[] Shading;
    [SerializeField] private AudioClip[] FootStepSounds; // Offset by -1 because a wall cannot be stood on
    [SerializeField] private AudioSource ExplosionSound; 

    [SerializeField] private ParticleSystem[] StepParticles; 
    [SerializeField] private ParticleSystem ExplosionParticles;

    [SerializeField] private Image BloodImage;
    [SerializeField] private Image ExplosionImage;
    [SerializeField] private Image Overlay;

    private AudioSource _audioSource;
    private TMP_Text _textDisplay;
    private Button _button;
    private Action<Box> _changeCallback;
    private Board _board;
    private BoxType _type;
    private bool _updateShading;


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

    public void PlayerEnter() { HasPlayer = true; Step(true); }
    public void EnemyLeave() { HasEnemy = false; EnemyIndex = -1; }
    public void PlayerLeave() { HasPlayer = false; EnemyIndex = -1; }
    public void EnemyEnter(int enemyIndex, bool visible) { HasEnemy = true; EnemyIndex = enemyIndex; Step(visible); }
    public void SetOverlay(Sprite newOverlay) { Overlay.sprite = newOverlay; }

    private void FixedUpdate()
    {
        // Shading is done in the next frame because that way all of the surrounding blocks would have been updated
        if (_updateShading)
        {
            UpdateShading();
            _updateShading = false;
        }
    }

    public void Setup(int id, int row, int column, Board board, Sprite newOverlay)
    {
        ID = id;
        RowIndex = row;
        ColumnIndex = column;
        _board = board;
        EnemyIndex = -1;
        Overlay.sprite = newOverlay;
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

    public void Step(bool visible)
    {
        if (_audioSource != null && _type > BoxType.Wall)
        {
            _audioSource.PlayOneShot(FootStepSounds[(int)_type - 1]);
            if (visible)
            {
                StepParticles[(int)_type - 1].Play();
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

        if (!IsActive)
        {
            _updateShading = true;
        }
    }

    void UpdateShading()
    {
        int shadingLevel = 0;

        bool topClear = ColumnIndex + 1 < _board.GetHeight();
        bool botClear = ColumnIndex - 1 >= 0;
        bool leftClear = RowIndex - 1 >= 0;
        bool rightClear = RowIndex + 1 < _board.GetWidth();

        bool AddShading(Box box)
        {
            return !box.IsActive && !box.IsWall();
        }

        if (topClear)
        {
            if (AddShading(_board.GetBox(_board.GetBoxIndex(new Vector2Int(ColumnIndex + 1, RowIndex))))) ++shadingLevel;
        }
        if (botClear)
        {

            if (AddShading(_board.GetBox(_board.GetBoxIndex(new Vector2Int(ColumnIndex - 1, RowIndex))))) ++shadingLevel;
        }
        if (leftClear)
        {
            if (AddShading(_board.GetBox(_board.GetBoxIndex(new Vector2Int(ColumnIndex, RowIndex - 1))))) ++shadingLevel;
        }
        if (rightClear)
        {
            if (AddShading(_board.GetBox(_board.GetBoxIndex(new Vector2Int(ColumnIndex, RowIndex + 1))))) ++shadingLevel;
        }

        if (shadingLevel >= 3) _button.image.color = Shading[(int)ShadingLevel.Lit];
        else if (shadingLevel >= 1) _button.image.color = Shading[(int)ShadingLevel.Shaded];
        else _button.image.color = Shading[(int)ShadingLevel.Dark];
    }

    public void Wall(bool isWall)
    {
        if (_button != null)
        {
            if (isWall)
            {
                _button.image.color = Shading[(int)ShadingLevel.Lit];
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
            _updateShading = true;
        }

        if (_textDisplay != null)
        {
            _textDisplay.enabled = true;
        }

        if (HasEnemy)
        {
            _board.Game.Enemies[EnemyIndex].SetVisible(true);
        }

    }

    public void StandDown()
    {
        if (_button != null)
        {
            _button.interactable = false;
            _button.image.color = Shading[(int)ShadingLevel.Lit];
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
            _board.Game.Enemies[EnemyIndex].SetVisible(true);
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
        _audioSource = GetComponent<AudioSource>();

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
            _button.image.color = Shading[(int)ShadingLevel.Dark];
        }

        if (BloodImage) BloodImage.enabled = false;
        if (ExplosionImage) ExplosionImage.enabled = false;
    }

    public Vector2Int Get2DPos()
    {
        return new Vector2Int(ColumnIndex, RowIndex);
    }

    // Returns false if failed, refreshes the board if defused
    public bool Defuse(bool explosion = false)
    {
        // Defuse if a bomb
        if (IsDangerous)
        {
            IsDangerous = false;
            _board.BombDefused(ID);
            OnClick();

            // Update squares around the bomb
            UpdateBoxAndNeighbours();

            // Play an explosion sound and leave a blood splatter if defused by setting off the bomb
            if (explosion)
            {
                ExplosionParticles.Play();
                ExplosionSound.Play();
                BloodImage.enabled = true;
                ExplosionImage.enabled = true;
            }

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

    public void Blood()
    {
        BloodImage.enabled = true;
    }
}
