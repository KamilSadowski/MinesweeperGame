using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Character : MonoBehaviour
{
    public Board Board; // No need to encapsulate since the board is set by the game
    public Vector2Int SpawnPos;
    public bool Active { get; protected set; }
    public bool Visible { get; protected set; }

    [SerializeField] protected Text Text;

    protected Vector2Int _2DPos;
    protected Vector2Int _newPos;
    protected SpriteRenderer _spriteRenderer;
    protected AudioSource _audioSource;
    protected ParticleSystem _deathParticles;
    protected Animator _animator;

    public Vector2Int Position() { return _2DPos; }

    public virtual void SetVisible(bool visible)
    {
        Visible = visible;
        _spriteRenderer.enabled = visible;
        Text.enabled = visible;
    }

    // To be called in awake when inherited
    protected void Initialise()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();
        _deathParticles = GetComponent<ParticleSystem>();
        _animator = GetComponent<Animator>();
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
