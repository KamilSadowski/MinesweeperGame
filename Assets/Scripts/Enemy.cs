using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    enum EnemyState { inactive, roaming };

    public Game _game;
    public PathFinder _pathFinder;
    EnemyState _state;
    List<Vector2Int> _path;
    int _costLeft; // How many moves is it gonna cost the enemy to move to the next grid
    int _movementPenalty = 1; // A small movement speed penalty to make escaping easier for the player

    // Start is called before the first frame update
    void Awake()
    {
        Initialise();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
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
    public override void Deactivate()
    {
        base.Deactivate();
        _state = EnemyState.inactive;
    }
    public override void Activate()
    {
        base.Activate();
        _state = EnemyState.roaming;
    }

    void FindPath(Vector2Int position)
    {
        _path = _pathFinder.PathTo(_2DPos, position);
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
        _board.EnemyLeftSquare(_2DPos);
        _2DPos = position;
        Box currentBox = _board.GetBox(position);
        transform.position = currentBox.transform.position;
        Visible(!currentBox.IsActive);
        _board.EnemyMovedToSquare(_2DPos);
    }

    // Moves the enemy based on how expensive the player move was
    public void Move(int playerMoveCost)
    {
        // Find a new path since the player has moved
        FindPath(_game._player.Position());

        Debug.Log("Player moved on a cost of: " + playerMoveCost);

        // Move according to costs left
        Debug.Log("AI moved on a cost of " + _costLeft);
        if (_costLeft > playerMoveCost)
        {
            _costLeft -= playerMoveCost;
        }
        else
        {
            while (playerMoveCost > 0)
            {
                _costLeft -= playerMoveCost;
                playerMoveCost -= _board.GetBox(_2DPos).Cost;
                PathStep();
                _costLeft += _board.GetBox(_2DPos).Cost + _movementPenalty;
            }
        }

    }

    public override void Respawn()
    {
        _board.EnemyLeftSquare(_2DPos);
        _2DPos = _spawnPos;
        transform.position = _board.GetBox(_spawnPos).transform.position;
        Box currentBox = _board.GetBox(_spawnPos);
        Visible(!currentBox.IsActive);
        _board.EnemyMovedToSquare(_spawnPos);
    }
}
