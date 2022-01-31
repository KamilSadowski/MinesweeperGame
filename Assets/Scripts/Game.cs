using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private List<GameObject> EnemyPrefabs = new List<GameObject>();
    [SerializeField] private int NumberOfLives = 3;

    public Player _player { get; private set; }
    public List<Enemy> _enemies { get; private set; } = new List<Enemy>();

    private Board _board;
    private UI _ui;
    private double _gameStartTime;
    private bool _gameInProgress;
    private int _lives;

    public void OnClickedNewGame()
    {
        _lives = NumberOfLives;
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
        _player._spawnPos = _board.RandomSafePos(true);
        _player.MoveTo(_player._spawnPos);

        if (_enemies.Count == 0)
        {
            for (int i = 0; i < EnemyPrefabs.Count; ++i)
            {
                _enemies.Add(Instantiate(EnemyPrefabs[i]).GetComponent<Enemy>());
                _enemies[i]._board = _board;
                _enemies[i]._game = this;
                _enemies[i]._pathFinder = new PathFinder(_board);
                _enemies[i]._spawnPos = _board.RandomSafePos(true);
                _enemies[i].MoveTo(_enemies[i]._spawnPos);
                _enemies[i].Activate();
            }
        }
        else
        {
            for (int i = 0; i < EnemyPrefabs.Count; ++i)
            {
                _enemies[i].Activate();
                _enemies[i].MoveTo(_board.RandomSafePos(true));
            }
        }
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

        for (int i = 0; i < _enemies.Count; ++i)
        {
            _enemies[i].Deactivate();
        }
        
    }

    private void Awake()
    {
        _board = transform.parent.GetComponentInChildren<Board>();
        _board._game = this;
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
            --_lives;
            if (_lives > 0)
            {
                _player.Respawn();
                foreach (Enemy enemy in _enemies) enemy.Respawn();
            }
            else
            {
                _ui.HideGame();
                _ui.ShowResult(success: false);
                _player.Kill();
            }
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

    public void EnemyMoves(int playerMoveCost)
    {
        foreach (Enemy enemy in _enemies)
        {
            enemy.Move(playerMoveCost);
        }
    }
}
