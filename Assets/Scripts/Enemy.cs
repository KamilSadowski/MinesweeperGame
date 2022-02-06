using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    // In the roaming state, the enemy will move around the map in straight lines and choose a direction
    // at random as soon as it encounters an edge or a wall
    // As soon as the player is within line of sight, the enemy will enter the chasing state
    // In the chasing state, the enemy will follow the player until the player is either killed or gets away
    // In the fleeing state the enemy will run from the player, the player can kill the enemy if hit
    // The fleeing state is activated by bomb defusal
    // Respawning state is used when the player is standing on the box the enemy was going to respawn on to avoid instant death
    enum EnemyState { inactive, roaming, chasing, fleeing, respawning };

    const int FleeingMoves = 20;
    static readonly int TextDuration = 3; // How many turns the text will be visible for

    public Game Game;
    public PathFinder PathFinder;
    public int ID;

    [SerializeField] AudioClip EnemySpottedNoise;
    [SerializeField] AudioClip EnemyChasingNoise;
    [SerializeField] int MovementPenalty = 1; // A small movement speed penalty to make escaping easier for the player
    [SerializeField] int SightDistance = 6; // How far the enemy can see
    [SerializeField] int HearingDistance = 2; // How far the enemy can hear
    [SerializeField] int ChasingDistance = 6; // If further than this distance, the enemy will give up on the chase

    [SerializeField] Color NormalColour = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] Color FleeingColour = new Color(0.0f, 0.0f, 0.5f);
    int _fleeingMovesLeft = FleeingMoves;
    int _textDuration = TextDuration;

    EnemyState _state;
    List<Vector2Int> _path;

    Globals.Direction _roamingDirection; // Currently selected roaming direction
    int _costLeft; // How many moves is it going to cost the enemy to move to the next grid

    public bool IsFleeing() { return _state == EnemyState.fleeing; }

    public bool IsChasing() { return _state == EnemyState.chasing; }

    // Start is called before the first frame update
    void Awake()
    {
        Initialise();
        _spriteRenderer.color = NormalColour;
        _state = EnemyState.inactive;
        SetVisible(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                _newPos = _2DPos;
                --_newPos.x;
                MoveTo(_newPos);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                _newPos = _2DPos;
                ++_newPos.x;
                MoveTo(_newPos);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _newPos = _2DPos;
                ++_newPos.y;
                MoveTo(_newPos);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _newPos = _2DPos;
                --_newPos.y;
                MoveTo(_newPos);
            }
        }
    }

    // Enemy becoming visible will play a sound
    public override void SetVisible(bool visible)
    {
        base.SetVisible(visible);
        if (visible != Visible)
        {
            _audioSource.PlayOneShot(EnemySpottedNoise);
            // The chase state is only triggered when the enemy becomes visible
            if ((_state == EnemyState.chasing || _state == EnemyState.fleeing)) Board.EnemyChaseStart();
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();
        _state = EnemyState.inactive;
    }
    public override void Activate()
    {
        Active = true;
        StartRoaming();
    }

    public void FindPathTo(Vector2Int position)
    {
        _path = PathFinder.PathTo(_2DPos, position);
    }

    public void FindPathAwayFrom(Vector2Int position)
    {
        _path = PathFinder.PathAwayFrom(_2DPos, position);
    }

    void PathStep()
    {
        // First index is the current position
        if (_path.Count > 1)
        {
            Debug.Log(_path.Count);
            MoveTo(_path[1]);
            _path.RemoveAt(0);
        }
    }

    public override void MoveTo(Vector2Int position)
    {
        if (Board.GetBox(position).HasEnemy) return;
        Board.EnemyLeftSquare(_2DPos);
        if (IsChasing() || IsFleeing()) Board.EnemyChaseStart();
        _2DPos = position;
        Box currentBox = Board.GetBox(position);
        transform.position = currentBox.transform.position;
        SetVisible(!currentBox.IsActive);
        Board.EnemyMovedToSquare(_2DPos, ID);
        _costLeft = Board.GetBox(_2DPos).Cost + MovementPenalty;
    }

    // Moves the enemy
    public void Move(int steps)
    {
        UpdateText();
        while (steps > 0)
        {
            --steps;
            --_costLeft;

            // Move according to costs left
            if (_costLeft <= 0)
            {
                _costLeft = 0;
                // Choose how to move based on the current enemy state----------------------------------------------------------------------------------
                switch (_state)
                {
                    case EnemyState.inactive:
                        {
                            SetText("");
                            break;
                        }
                    case EnemyState.roaming:
                        {
                            Vector2Int playerPos = Board.Game.Player.Position();
                            int yDist = Math.Abs(playerPos.y - _2DPos.y);
                            int xDist = Math.Abs(playerPos.x - _2DPos.x);

                            // Enemy can only see in straight lines and the sight is blocked by walls
                            // To prevent the player not being noticed when close
                            // A hearing radius is implemented

                            // Check if player is within sight
                            if (playerPos.y == _2DPos.y && xDist <= SightDistance)
                            {
                                Vector2Int direction;
                                if (playerPos.x > _2DPos.x)
                                {
                                    direction = new Vector2Int(1, 0);
                                }
                                else
                                {
                                    direction = new Vector2Int(-1, 0);
                                }
                                if (LineOfSight(direction, xDist))
                                {
                                    // Refund steps and start chasing
                                    ++steps;
                                    ++_costLeft;
                                    StartChasing();
                                }
                            }
                            else if (playerPos.x == _2DPos.x && yDist <= SightDistance)
                            {
                                Vector2Int direction;
                                if (playerPos.y > _2DPos.y)
                                {
                                    direction = new Vector2Int(0, 1);
                                }
                                else
                                {
                                    direction = new Vector2Int(0, -1);
                                }

                                if (LineOfSight(direction, yDist))
                                {
                                    // Refund steps and start chasing
                                    ++steps;
                                    ++_costLeft;
                                    StartChasing();
                                }
                            }

                            // Check if player was heard
                            else if (xDist <= HearingDistance && yDist <= HearingDistance)
                            {
                                // Refund steps and start chasing
                                ++steps;
                                ++_costLeft;
                                StartChasing();
                            }


                            switch (_roamingDirection)
                            {
                                // Move in the current direction
                                case Globals.Direction.North:
                                    {
                                        Vector2Int newPos = new Vector2Int(_2DPos.x, _2DPos.y + 1);
                                        // Check if the direction needs to be changed
                                        if (newPos.y < Board.Height && !Board.GetBox(newPos).IsWall())
                                        {
                                            MoveTo(newPos);
                                        }
                                        else
                                        {
                                            // Exclude the current direction from the random directions and refund the cost
                                            ++steps;
                                            ++_costLeft;
                                            _roamingDirection = (Globals.Direction)UnityEngine.Random.Range(1, 4);
                                        }
                                        
                                        break;
                                    }
                                case Globals.Direction.East:
                                    {
                                        Vector2Int newPos = new Vector2Int(_2DPos.x + 1, _2DPos.y);
                                        // Check if the direction needs to be changed
                                        if (newPos.x < Board.Width && !Board.GetBox(newPos).IsWall())
                                        {
                                            MoveTo(newPos);
                                        }
                                        else
                                        {
                                            // Exclude the current direction from the random directions
                                            ++steps;
                                            ++_costLeft;
                                            _roamingDirection = (Globals.Direction)UnityEngine.Random.Range(1, 4);
                                            if (_roamingDirection == Globals.Direction.East) _roamingDirection = Globals.Direction.North;
                                        }
                                        break;
                                    }
                                case Globals.Direction.South:
                                    {
                                        Vector2Int newPos = new Vector2Int(_2DPos.x, _2DPos.y - 1);
                                        // Check if the direction needs to be changed
                                        if (newPos.y >= 0 && !Board.GetBox(newPos).IsWall())
                                        {
                                            MoveTo(newPos);
                                        }
                                        else
                                        {
                                            // Exclude the current direction from the random directions
                                            ++steps;
                                            ++_costLeft;
                                            _roamingDirection = (Globals.Direction)UnityEngine.Random.Range(1, 4);
                                            if (_roamingDirection == Globals.Direction.South) _roamingDirection = Globals.Direction.North;
                                        }
                                        break;
                                    }
                                case Globals.Direction.West:
                                    {
                                        Vector2Int newPos = new Vector2Int(_2DPos.x - 1, _2DPos.y);
                                        // Check if the direction needs to be changed
                                        if (newPos.x >= 0 && !Board.GetBox(newPos).IsWall())
                                        {
                                            MoveTo(newPos);
                                        }
                                        else
                                        {
                                            // Exclude the current direction from the random directions
                                            ++steps;
                                            ++_costLeft;
                                            _roamingDirection = (Globals.Direction)UnityEngine.Random.Range(0, 3);
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                    case EnemyState.chasing:
                        {
                            FindPathTo(Game.Player.Position());
                            PathStep();

                            // Lose interest if the player is too far
                            Vector2Int playerPos = Board.Game.Player.Position();
                            int yDist = Math.Abs(playerPos.y - _2DPos.y);
                            int xDist = Math.Abs(playerPos.x - _2DPos.x);

                            if (yDist + xDist > ChasingDistance)
                            {
                                StartRoaming();
                            }

                            break;
                        }
                    case EnemyState.fleeing:
                        {
                            FindPathAwayFrom(Game.Player.Position());
                            PathStep();

                            --_fleeingMovesLeft;
                            if (_fleeingMovesLeft <= 0)
                            {
                                StartRoaming();
                                _spriteRenderer.color = NormalColour;
                            }
                            break;
                        }
                    case EnemyState.respawning:
                        {
                            if (!Board.GetBox(SpawnPos).HasPlayer)
                            {
                                SetVisible(true);
                                Respawn();
                            }
                            break;
                        }
                } // <-------------------------------------------------------------------------------------------------End of state switch
            }
        }
    }

    // Check if no walls are blocking the sight
    bool LineOfSight(Vector2Int direction, int distance)
    {
        Vector2Int currentPos = _2DPos;
        int index;

        // Check if there is no blocking walls
        for (int i = 1; i < distance; ++i)
        {
            currentPos += direction;
            index = Board.TryGetBoxIndex(currentPos);
            if (index == -1)
            {
                return false;
            }
            if (Board.GetBox(index).IsWall())
            {
                return false;
            }
        }
        return true;
    }

    public override void Respawn()
    {
        if (!Board.GetBox(SpawnPos).HasPlayer)
        {
            Board.EnemyLeftSquare(_2DPos);
            _2DPos = SpawnPos;
            transform.position = Board.GetBox(SpawnPos).transform.position;
            Box currentBox = Board.GetBox(SpawnPos);
            SetVisible(!currentBox.IsActive);
            Board.EnemyMovedToSquare(SpawnPos, ID);
            _spriteRenderer.color = NormalColour;
            MoveTo(_2DPos);
            StartRoaming();
        }
        else
        {
            SetVisible(false);
            _state = EnemyState.respawning;
        }
    }

    void StartRoaming()
    {
        SetText("...");
        Board.EnemyChaseEnd();
        _state = EnemyState.roaming;
        _roamingDirection = (Globals.Direction)UnityEngine.Random.Range(0, 4);
        _fleeingMovesLeft = 0;
        _spriteRenderer.color = NormalColour;
    }

    void StartChasing()
    {
        if (Visible)
        {
            Board.EnemyChaseStart();
            _audioSource.PlayOneShot(EnemyChasingNoise);
            SetText("!?");
        }
        _state = EnemyState.chasing;
        _fleeingMovesLeft = 0;
        _spriteRenderer.color = NormalColour;
    }

    public void StartFleeing()
    {
        if (Visible) Board.EnemyChaseStart();
        SetText("><.><");
        _fleeingMovesLeft = FleeingMoves;
        _state = EnemyState.fleeing;
        _spriteRenderer.color = FleeingColour;
        _roamingDirection = (Globals.Direction)UnityEngine.Random.Range(0, 4);
    }

    public void Kill()
    {
        StartRoaming();
        _audioSource.Play();
        _deathParticles.Play();
    }

    void SetText(string text)
    {
        Text.text = text;
        _textDuration = TextDuration;
    }

    void UpdateText()
    {
        if (_textDuration > 0)
        {
            --_textDuration;
        }
        else
        {
            Text.text = "";
        }
    }
}
