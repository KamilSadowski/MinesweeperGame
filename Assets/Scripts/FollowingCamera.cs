using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowingCamera : MonoBehaviour
{
    GameObject _target;
    Vector3 _position;
    float _pauseTime = 1.0f;
    Timer _pauseTimer = new Timer(0.0f);
    bool _explosion = false;
    bool _explosionReversed = false;
    float _explosionDuration = 1.0f;
    Timer _explosionTimer;
    Vector3 _explosionOffset = Vector3.up * 5.0f;
    float _speed = 800.0f;

    private void Start()
    {
        _explosionTimer = new Timer(_explosionDuration);
    }

    public void SetTarget(GameObject target)
    {
        _target = target;
    }

    // Update is called once per frame
    void Update()
    {
        if (_pauseTimer.Update(Time.deltaTime) && _target)
        {
            _position = _target.transform.position;
            _position.z = transform.position.z;
            transform.position = Vector3.MoveTowards(transform.position, _position, _speed * Time.deltaTime);
        }

        // Apply the explosion effect to the camera itself and not the origin
        if (_explosion)
        {
            if (_explosionReversed)
            {
                transform.position -= _explosionOffset;
            }
            else
            {
                transform.position += _explosionOffset;
            }
            _explosionReversed = !_explosionReversed;
            if (_explosionTimer.Update(Time.deltaTime))
            {
                _explosionTimer.Reset(_explosionDuration);
                _explosion = false;
            }
        }
    }

    // Pauses the camera in place for a small amount of time
    public void Pause()
    {
        _pauseTimer.Reset(_pauseTime);
    }

    public void Explode()
    {
        _explosion = true;
    }
}
