using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] private GameObject PlayerPrefab;

    private Board _board;
    private UI _ui;
    private double _gameStartTime;
    private bool _gameInProgress;
    private Player _player;

    public void OnClickedNewGame()
    {
        if (_board != null)
        {
            _board.RechargeBoxes();
        }

        if (_ui != null)
        {
            _ui.HideMenu();
            _ui.ShowGame();
        }

        if (_player == null)
        {
            _player = Instantiate(PlayerPrefab).GetComponent<Player>();
            // Setting this here instead of calling GetObjectOfType in the player is a lot faster
            _player._board = _board;
        }
        _player.Activate();
        _player.MoveTo(_board.RandomSafePos());
    }

    public void OnClickedExit()
    {
#if !UNITY_EDITOR
        Application.Quit();
#endif
    }

    public void OnClickedReset()
    {
        if (_board != null)
        {
            _board.Clear();
        }

        if (_ui != null)
        {
            _ui.HideResult();
            _ui.ShowMenu();
        }

        if (_player != null)
        {
            _player.Deactivate();
        }
    }

    private void Awake()
    {
        _board = transform.parent.GetComponentInChildren<Board>();
        _ui = transform.parent.GetComponentInChildren<UI>();
        _gameInProgress = false;
    }

    private void Start()
    {
        if (_board != null)
        {
            _board.Setup(BoardEvent);
        }

        if (_ui != null)
        {
            _ui.ShowMenu();
        }
    }

    private void Update()
    {
        if(_ui != null)
        {
            _ui.UpdateTimer(_gameInProgress ? Time.realtimeSinceStartupAsDouble - _gameStartTime : 0.0);
        }
    }

    private void BoardEvent(Board.Event eventType)
    {
        if(eventType == Board.Event.ClickedDanger && _ui != null)
        {
            _ui.HideGame();
            _ui.ShowResult(success: false);
            _player.Kill();
        }

        if (eventType == Board.Event.Win && _ui != null)
        {
            _ui.HideGame();
            _ui.ShowResult(success: true);
            _player.Win();
        }

        if (!_gameInProgress)
        {
            _gameInProgress = true;
            _gameStartTime = Time.realtimeSinceStartupAsDouble;
        }
    }
}
