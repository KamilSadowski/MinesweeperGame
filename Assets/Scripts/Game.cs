using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private List<GameObject> EnemyPrefabs = new List<GameObject>();
    [SerializeField] private int NumberOfLives = 5;
    [SerializeField] private Canvas Canvas;

    public Player Player { get; private set; }
    public List<Enemy> Enemies { get; private set; } = new List<Enemy>();

    private Timer _movementTimer = new Timer(5.0f); // If the player does not move within this time, the enemy gets a free turn
    private Board _board;
    private Bomb _bomb;
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

        if (_bomb == null)
        {
            _bomb = FindObjectOfType<Bomb>();
        }

        if (_ui != null)
        {
            _ui.HideMenu();
            _ui.ShowGame();
            _ui.HideBomb();
        }

        if (Enemies.Count == 0)
        {
            for (int i = 0; i < EnemyPrefabs.Count; ++i)
            {
                Enemies.Add(Instantiate(EnemyPrefabs[i]).GetComponent<Enemy>());
                Enemies[i].transform.parent = Canvas.transform;
                Enemies[i].Board = _board;
                Enemies[i].Game = this;
                Enemies[i].ID = i;
                Enemies[i].PathFinder = new PathFinder(_board);
                Enemies[i].SpawnPos = _board.RandomSafePos(true);
                Enemies[i].MoveTo(Enemies[i].SpawnPos);
                Enemies[i].Activate();
            }
        }
        else
        {
            for (int i = 0; i < EnemyPrefabs.Count; ++i)
            {
                Enemies[i].Activate();
                Enemies[i].MoveTo(_board.RandomSafePos(true));
            }
        }

        if (Player == null)
        {
            Player = Instantiate(PlayerPrefab).GetComponent<Player>();
            Player.transform.parent = Canvas.transform;
            // Setting this here instead of calling GetObjectOfType in the player is a lot faster
            Player.Board = _board;
        }
        Player.Activate();
        Player.SpawnPos = _board.RandomSafePos(true);
        Player.MoveTo(Player.SpawnPos);
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
            _ui.HideBomb();
        }

        if (Player != null)
        {
            Player.Deactivate();
        }

        for (int i = 0; i < Enemies.Count; ++i)
        {
            Enemies[i].Deactivate();
        }
        
    }

    private void Awake()
    {
        _board = transform.parent.GetComponentInChildren<Board>();
        _board.Game = this;
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

        if (_gameInProgress)
        {
            if (_movementTimer.Update(Time.deltaTime))
            {
                EnemyMoves(1);
            }
        }
    }

    private void BoardEvent(Board.Event eventType)
    {
        if(eventType == Board.Event.ClickedDanger && _ui != null)
        {
            --_lives;
            // Respawn player if lives left
            if (_lives > 0)
            {
                Player.Respawn();
                foreach (Enemy enemy in Enemies) enemy.Respawn();
            }
            else
            {
                _ui.HideGame();
                _ui.ShowResult(success: false);
                Player.Kill();
                DeactivateEnemies();
            }
        }

        if (eventType == Board.Event.Win && _ui != null)
        {
            _ui.HideGame();
            _ui.ShowResult(success: true);
            Player.Win();
            DeactivateEnemies();
        }

        if (!_gameInProgress)
        {
            _gameInProgress = true;
            _gameStartTime = Time.realtimeSinceStartupAsDouble;
        }
    }

    public void EnemyMoves(int steps)
    {
        foreach (Enemy enemy in Enemies)
        {
            if (enemy.Active)
            {
                enemy.Move(steps);
                _movementTimer.Reset();
            }
        }
    }

    void DeactivateEnemies()
    {
        foreach (Enemy enemy in Enemies)
        {
            enemy.Deactivate();
        }
    }

    public void StartDefusing()
    {
        _ui.ShowBomb();
        _bomb.StartDefusing();
    }

    public void FinishDefusing()
    {
        _ui.HideBomb();
        foreach (Enemy enemy in Enemies)
        {
            enemy.StartFleeing();
        }
    }

    public void CancelDefusing()
    {
        _ui.HideBomb();
    }
}
