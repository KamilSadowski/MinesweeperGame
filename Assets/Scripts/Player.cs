using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    enum PlayerState { inactive, moving, defusing, stunned };

    PlayerState _state = PlayerState.inactive;
    Box _selectedBomb;

    void Awake()
    {
        Initialise();
    }

    // Update is called once per frame
    void Update()
    {
        switch (_state)
        {
            case PlayerState.inactive:
                {
                    break;
                }
            case PlayerState.moving:
                {
                    // Movement input
                    if (Input.GetKeyDown(KeyCode.A))
                    {
                        _newPos = _2DPos;
                        --_newPos.x;
                        MoveTo(_newPos);
                    }
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        _newPos = _2DPos;
                        ++_newPos.x;
                        MoveTo(_newPos);
                    }
                    if (Input.GetKeyDown(KeyCode.S))
                    {
                        _newPos = _2DPos;
                        ++_newPos.y;
                        MoveTo(_newPos);
                    }
                    if (Input.GetKeyDown(KeyCode.W))
                    {
                        _newPos = _2DPos;
                        --_newPos.y;
                        MoveTo(_newPos);
                    }

                    // Skip turn
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        MoveTo(_2DPos);
                    }

                    // Defusing input
                    if (Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        if (_2DPos.y + 1 < Board.GetHeight())
                        {
                            if (!StartDefusing(new Vector2Int(_2DPos.x, _2DPos.y + 1))) Stun();
                        }
                        else Stun();
                    }
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        if (_2DPos.x + 1 < Board.GetWidth())
                        {
                            if (!StartDefusing(new Vector2Int(_2DPos.x + 1, _2DPos.y))) Stun();
                        }
                        else Stun();
                    }
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        if (_2DPos.y - 1 >= 0)
                        {
                            if (!StartDefusing(new Vector2Int(_2DPos.x, _2DPos.y - 1))) Stun();
                        }
                        else Stun();
                    }
                    if (Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        if (_2DPos.x - 1 >= 0)
                        {
                            if (!StartDefusing(new Vector2Int(_2DPos.x - 1, _2DPos.y))) Stun();
                        }
                        else Stun();
                    }

                    break;
                }
            case PlayerState.stunned:
                {
                    MoveTo(_2DPos);
                    _state = PlayerState.moving;
                    break;
                }
        }

        // Reset
        if (_state != PlayerState.inactive && Input.GetKeyDown(KeyCode.R)) Board.Reset();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        _state = PlayerState.inactive;
    }

    public override void Activate()
    {
        base.Activate();
        _state = PlayerState.moving;
    }

    public void Kill(bool finalLife)
    {
        CancelDefusing();
        _deathParticles.Play();
        _audioSource.Play();
        if (finalLife)
        {
            _state = PlayerState.inactive;
        }
        else
        {
            Respawn();
        }
    }
    public void Win()
    {
        _state = PlayerState.inactive;
    }

    // Moves the player and activates the square the player lands on
    public override void MoveTo(Vector2Int position)
    {
        if (_state == PlayerState.moving)
        {
            // Check if the new position is on the map
            if (position.x >= 0 && position.x < Board.GetWidth() &&
                position.y >= 0 && position.y < Board.GetHeight())
            {
                Box box = Board.GetBox(position);
                if (!box.IsWall())
                {
                    Board.PlayerLeftSquare(_2DPos);
                    _2DPos = position;
                    transform.position = Board.GetBox(position).transform.position;
                    Board.ActivateSquare(_2DPos);
                    Board.PlayerMovedToSquare(_2DPos);
                }
            }
        }
    }

    public override void Respawn()
    {
        CancelDefusing();
        Board.PlayerLeftSquare(_2DPos);
        _2DPos = SpawnPos;
        transform.position = Board.GetBox(SpawnPos).transform.position;
        Board.PlayerMovedToSquare(SpawnPos);
    }

    // Returns true if started defusing
    bool StartDefusing(Vector2Int bombPosition)
    {
        _selectedBomb = Board.GetBox(bombPosition);
        if (_selectedBomb.IsDangerous)
        {
            _state = PlayerState.defusing;
            Board.Game.StartDefusing();
            return true;
        }
        return false;
    }

    public void FinishDefusing()
    {
        _state = PlayerState.moving;
        _selectedBomb.Defuse();
        _selectedBomb = null;
        Board.Game.FinishDefusing();
    }

    public void CancelDefusing()
    {
        _state = PlayerState.moving;
        _selectedBomb = null;
        Board.Game.CancelDefusing();
    }

    public void Stun()
    {
        _state = PlayerState.stunned;
    }

    public void SetLives(int livesLeft)
    {
        _animator.SetInteger("LivesLeft", livesLeft);
    }
}
