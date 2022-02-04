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

    public Game Game;
    public PathFinder PathFinder;
    public int ID;

    Color _normalColour = new Color(1.0f, 1.0f, 1.0f);
    Color _fleeingColour = new Color(0.0f, 0.0f, 0.5f);
    int _fleeingMovesLeft = FleeingMoves;

    EnemyState _state;
    List<Vector2Int> _path;

    Globals.Direction _roamingDirection; // Currently selected roaming direction
    int _costLeft; // How many moves is it going to cost the enemy to move to the next grid
    int _movementPenalty = 1; // A small movement speed penalty to make escaping easier for the player
    int _sightDistance = 4; // How far the enemy can see
    int _hearingDistance = 2; // How far the enemy can hear
    int _chasingDistance = 6; // If further than this distance, the enemy will give up on the chase

    public bool IsFleeing() { return _state == EnemyState.fleeing; }

    // Start is called before the first frame update
    void Awake()
    {
        Initialise();
        _spriteRenderer.color = _normalColour;
        _state = EnemyState.inactive;
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

    public override void Deactivate()
    {
        base.Deactivate();
        _state = EnemyState.inactive;
    }
    public override void Activate()
    {
        base.Activate();
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
            MoveTo(_path[1]);
            _path.RemoveAt(0);
        }
    }

    public override void MoveTo(Vector2Int position)
    {
        Board.EnemyLeftSquare(_2DPos);
        _2DPos = position;
        Box currentBox = Board.GetBox(position);
        transform.position = currentBox.transform.position;
        Visible(!currentBox.IsActive);
        Board.EnemyMovedToSquare(_2DPos, ID);
        _costLeft = Board.GetBox(_2DPos).Cost + _movementPenalty;
    }

    // Moves the enemy
    public void Move(int steps)
    {
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
                            Text.text = "";
                            break;
                        }
                    case EnemyState.roaming:
                        {
                            Text.text = "hmm";

                            Vector2Int playerPos = Board.Game.Player.Position();
                            int yDist = Math.Abs(playerPos.y - _2DPos.y);
                            int xDist = Math.Abs(playerPos.x - _2DPos.x);

                            // Enemy can only see in straight lines and the sight is blocked by walls
                            // To prevent the player not being noticed when close
                            // A hearing radius is implemented

                            // Check if player is within sight
                            if (playerPos.x == _2DPos.x && xDist <= _sightDistance)
                            {
                                // Refund steps and start chasing
                                ++steps;
                                ++_costLeft;
                                _state = EnemyState.chasing;
                            }
                            else if (playerPos.y == _2DPos.y && yDist <= _sightDistance)
                            {
                                // Refund steps and start chasing
                                ++steps;
                                ++_costLeft;
                                _state = EnemyState.chasing;
                            }

                            // Check if player was heard
                            else if (xDist >= _hearingDistance && yDist >= _hearingDistance)
                            {
                                // Refund steps and start chasing
                                ++steps;
                                ++_costLeft;
                                _state = EnemyState.chasing;
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
                            Text.text = "!?";

                            FindPathTo(Game.Player.Position());
                            PathStep();

                            // Lose interest if the player is too far
                            Vector2Int playerPos = Board.Game.Player.Position();
                            int yDist = Math.Abs(playerPos.y - _2DPos.y);
                            int xDist = Math.Abs(playerPos.x - _2DPos.x);

                            if (yDist + xDist > _chasingDistance)
                            {
                                StartRoaming();
                            }

                            break;
                        }
                    case EnemyState.fleeing:
                        {
                            Text.text = "X.X";

                            FindPathAwayFrom(Game.Player.Position());
                            PathStep();

                            --_fleeingMovesLeft;
                            if (_fleeingMovesLeft <= 0)
                            {
                                StartRoaming();
                                _spriteRenderer.color = _normalColour;
                            }
                            break;
                        }
                    case EnemyState.respawning:
                        {
                            if (!Board.GetBox(SpawnPos).HasPlayer)
                            {
                                Visible(true);
                                Respawn();
                            }
                            break;
                        }
                } // <-------------------------------------------------------------------------------------------------End of state switch
            }
        }
    }

    public override void Respawn()
    {
        if (!Board.GetBox(SpawnPos).HasPlayer)
        {
            Board.EnemyLeftSquare(_2DPos);
            _2DPos = SpawnPos;
            transform.position = Board.GetBox(SpawnPos).transform.position;
            Box currentBox = Board.GetBox(SpawnPos);
            Visible(!currentBox.IsActive);
            Board.EnemyMovedToSquare(SpawnPos, ID);
            _state = EnemyState.roaming;
            _spriteRenderer.color = _normalColour;
            StartRoaming();
        }
        else
        {
            Visible(false);
            _state = EnemyState.respawning;
        }
    }

    void StartRoaming()
    {
        _state = EnemyState.roaming;
        _roamingDirection = (Globals.Direction)UnityEngine.Random.Range(0, 4);
    }

    public void StartFleeing()
    {
        _fleeingMovesLeft = FleeingMoves;
        _state = EnemyState.fleeing;
        _spriteRenderer.color = _fleeingColour;
        _roamingDirection = (Globals.Direction)UnityEngine.Random.Range(0, 4);
    }
}
