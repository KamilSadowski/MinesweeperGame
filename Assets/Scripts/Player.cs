using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    enum PlayerState { inactive, moving, defusing };

    const float TimeToDefuse = 1.0f;
    float _defuseTimer = 0.0f;
    PlayerState _state = PlayerState.inactive;
    SpriteRenderer _spriteRenderer;
    public Board _board; // No need to encapsulate since the board is set by the game
    Vector2Int _2DPos;
    Vector2Int _newPos;
    Box _selectedBomb;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        Deactivate();
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

                    // Defusing input
                    if (Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        if (_2DPos.y + 1 < _board.GetHeight())
                        {
                            _board.GetBox(new Vector2Int(_2DPos.x, _2DPos.y + 1)).Defuse();
                        }
                    }
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        if (_2DPos.x + 1 < _board.GetWidth())
                        {
                            _board.GetBox(new Vector2Int(_2DPos.x + 1, _2DPos.y)).Defuse();
                        }
                    }
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        if (_2DPos.y - 1 >= 0)
                        {
                            _board.GetBox(new Vector2Int(_2DPos.x, _2DPos.y - 1)).Defuse();
                        }
                    }
                    if (Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        if (_2DPos.x - 1 >= 0)
                        {
                            _board.GetBox(new Vector2Int(_2DPos.x - 1, _2DPos.y)).Defuse();
                        }
                    }

                    break;
                }
            case PlayerState.defusing:
                {
                    if (_defuseTimer < TimeToDefuse)
                    {
                        _defuseTimer += Time.deltaTime;
                    }
                    else
                    {
                        _defuseTimer = 0.0f;
                    }
                    _state = PlayerState.moving;
                    _selectedBomb.Defuse();


                    break;
                }
        }
    }

    public void Deactivate()
    {
        _state = PlayerState.inactive;
        _spriteRenderer.enabled = false;
    }

    public void Activate()
    {
        _state = PlayerState.moving;
        _spriteRenderer.enabled = true;
    }

    public void Kill()
    {
        _state = PlayerState.inactive;
    }
    public void Win()
    {
        _state = PlayerState.inactive;
    }

    // Moves the player and activates the square the player lands on
    public void MoveTo(Vector2Int position)
    {
        if (_state == PlayerState.moving)
        {
            // Check if the new position is on the map
            if (position.x >= 0 && position.x < _board.GetWidth() &&
                position.y >= 0 && position.y < _board.GetHeight())
            {
                Box box = _board.GetBox(position);
                if (!box.IsWall)
                {
                    _2DPos = position;
                    transform.position = _board.GetBox(position).transform.position;
                    _board.ActivateSquare(_2DPos);
                }
            }
        }
    }
}