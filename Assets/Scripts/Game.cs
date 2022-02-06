using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private List<GameObject> EnemyPrefabs = new List<GameObject>();
    [SerializeField] private Canvas GameCanvas;
    [SerializeField] private MusicPlayer MusicPlayer;
    [SerializeField] private FollowingCamera Camera;
    [SerializeField] private int KillScore = 10;
    [SerializeField] private int DefuseScore = 25;
    [SerializeField] private AudioClip VictorySound;
    [SerializeField] private AudioClip DefeatSound;
    [SerializeField] private AudioSource AudioSource;
    [SerializeField] private int MaxAllowedSpawnDistance = 10; // How many blocks away can enemies spawn from the player

    private const int NumberOfLives = 3;

    public Player Player { get; private set; }
    public List<Enemy> Enemies { get; private set; } = new List<Enemy>();

    private Timer _movementTimer = new Timer(2.5f); // If the player does not move within this time, the enemy gets a free turn
    private Board _board;
    private Bomb _bomb;
    private UI _ui;
    private double _gameStartTime;
    private bool _gameInProgress;
    private int _lives = NumberOfLives;
    private int _score;

    public void OnClickedNewGame()
    {
        MusicPlayer.ChangeState(MusicPlayer.MusicPlayerState.Roaming);

        UpdateLives(NumberOfLives);
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

        if (Player == null)
        {
            Player = Instantiate(PlayerPrefab).GetComponent<Player>();
            Player.transform.parent = GameCanvas.transform;
            // Setting this here instead of calling GetObjectOfType in the player is a lot faster
            Player.Board = _board;
            Camera.SetTarget(Player.gameObject);
        }
        Player.Activate();
        Player.SpawnPos = _board.RandomSafePos(true);
        Player.MoveTo(Player.SpawnPos);

        if (Enemies.Count == 0)
        {
            for (int i = 0; i < EnemyPrefabs.Count; ++i)
            {
                Enemies.Add(Instantiate(EnemyPrefabs[i]).GetComponent<Enemy>());
                Enemies[i].transform.parent = GameCanvas.transform;
                Enemies[i].Board = _board;
                Enemies[i].Game = this;
                Enemies[i].ID = i;
                Enemies[i].PathFinder = new PathFinder(_board);
                // Make sure the enemies spawn away from the player
                while ((Enemies[i].SpawnPos.x + Enemies[i].SpawnPos.y) < MaxAllowedSpawnDistance)
                {
                    Enemies[i].SpawnPos = _board.RandomSafePos(true);
                }
                Enemies[i].MoveTo(Enemies[i].SpawnPos);
                Enemies[i].Activate();
            }
        }
        else
        {
            for (int i = 0; i < Enemies.Count; ++i)
            {
                // Make sure the enemies spawn away from the player
                while ((Enemies[i].SpawnPos.x + Enemies[i].SpawnPos.y) < MaxAllowedSpawnDistance)
                {
                    Enemies[i].SpawnPos = _board.RandomSafePos(true);
                }
                Enemies[i].MoveTo(Enemies[i].SpawnPos);
                Enemies[i].Activate();
            }
        }
    }

    public void OnClickedExit()
    {
#if !UNITY_EDITOR
        Application.Quit();
#endif
    }

    public void OnClickedOptions()
    {
        _ui.HideMenu();
        _ui.ShowOptions();
    }

    public void OnClickedMenu()
    {
        _ui.HideOptions();
        _ui.ShowMenu();
    }

    public void OnClickedReset()
    {
        MusicPlayer.ChangeState(MusicPlayer.MusicPlayerState.Menu);

        if (_board != null)
        {
            _board.Clear();
        }

        if (_ui != null)
        {
            _ui.HideResult();
            _ui.HideOptions();
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
        _ui = FindObjectOfType<UI>();
        _gameInProgress = false;
        Player = Instantiate(PlayerPrefab).GetComponent<Player>();
        Player.transform.parent = GameCanvas.transform;
        // Setting this here instead of calling GetObjectOfType in the player is a lot faster
        Player.Board = _board;
        Camera.SetTarget(Player.gameObject);
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

        SetScore(0);
    }

    private void Update()
    {
        if (_ui != null)
        {
            _ui.UpdateTimer(_gameInProgress ? Time.realtimeSinceStartupAsDouble - _gameStartTime : 0.0);
        }

        if (_gameInProgress)
        {
            if (_movementTimer.Update(Time.deltaTime))
            {
                EnemyMoves();
            }
        }

        if (Input.GetKey(KeyCode.Escape)) OnClickedReset();
    }

    private void BoardEvent(Board.Event eventType)
    {
        if (eventType == Board.Event.ClickedDanger && _ui != null)
        {
            UpdateLives(_lives - 1);
            // Respawn player if lives left
            if (_lives > 0)
            {
                foreach (Enemy enemy in Enemies) enemy.Respawn();
                Player.Kill(false);
                Camera.Pause();
            }
            else
            {
                _ui.HideGame();
                _ui.ShowResult(success: false);
                Player.Kill(true);
                SetScore(0);
                AudioSource.PlayOneShot(DefeatSound);
                DeactivateEnemies();
            }
        }

        if (eventType == Board.Event.Win && _ui != null)
        {
            _ui.HideGame();
            _ui.ShowResult(success: true);
            AudioSource.PlayOneShot(VictorySound);
            Player.Win();
            DeactivateEnemies();
        }

        if (!_gameInProgress)
        {
            _gameInProgress = true;
            _gameStartTime = Time.realtimeSinceStartupAsDouble;
        }

        // Music player events
        if (MusicPlayer != null)
        {
            if (eventType == Board.Event.ChaseStarted)
            {
                MusicPlayer.ChangeState(MusicPlayer.MusicPlayerState.Chasing);
            }
            else if (eventType == Board.Event.ChaseEnded)
            {
                foreach (Enemy enemy in Enemies)
                {
                    if (enemy.IsFleeing() || enemy.IsChasing())
                    {
                        MusicPlayer.ChangeState(MusicPlayer.MusicPlayerState.Roaming);
                        break;
                    }
                }
            }
        }
    }

    public void EnemyMoves(int steps = 1)
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
            SetScore(_score + DefuseScore);
        }
    }

    public void CancelDefusing()
    {
        _ui.HideBomb();
    }

    public void Explode()
    {
        Camera.Explode();
    }

    public void SetScore(int score)
    {
        _score = score;
        _ui.UpdateScore(_score);
    }

    public void UpdateLives(int lives)
    {
        _lives = lives;
        _ui.UpdateLives(_lives, NumberOfLives);
        Player.SetLives(_lives);
    }

    public void UpdateBombs(int bombsLeft, int maxBombs)
    {
        _ui.UpdateBombs(bombsLeft, maxBombs);
    }

    public void EnemyKilled()
    {
        SetScore(_score + KillScore);
    }
}
