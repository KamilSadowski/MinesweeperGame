using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    public Board _board; // No need to encapsulate since the board is set by the game
    public Vector2Int _spawnPos;
    protected Vector2Int _2DPos;
    protected Vector2Int _newPos;
    protected SpriteRenderer _spriteRenderer;
    protected bool _visible;

    public Vector2Int Position() { return _2DPos; }

    protected void Visible(bool visible)
    {
        _visible = visible;
        _spriteRenderer.enabled = visible;
    }

    // To be called in awake when inherited
    protected void Initialise()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        Deactivate();
    }

    public virtual void Deactivate()
    {
        _spriteRenderer.enabled = false;
    }
    public virtual void Activate()
    {
        _spriteRenderer.enabled = true;
    }

    public abstract void MoveTo(Vector2Int position);

    public abstract void Respawn();
}
