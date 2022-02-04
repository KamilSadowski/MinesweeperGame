using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Character : MonoBehaviour
{
    public Board Board; // No need to encapsulate since the board is set by the game
    public Vector2Int SpawnPos;
    public bool Active { get; protected set; }

    [SerializeField] protected Text Text;

    protected Vector2Int _2DPos;
    protected Vector2Int _newPos;
    protected SpriteRenderer _spriteRenderer;
    protected bool _visible;

    public Vector2Int Position() { return _2DPos; }

    public void Visible(bool visible)
    {
        _visible = visible;
        _spriteRenderer.enabled = visible;
        Text.enabled = visible;
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
        Text.text = "";
        Active = false;
    }
    public virtual void Activate()
    {
        _spriteRenderer.enabled = true;
        Active = true;
    }

    public abstract void MoveTo(Vector2Int position);

    public abstract void Respawn();

}
