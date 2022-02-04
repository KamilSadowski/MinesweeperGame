using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer
{
    public float TimerDuration { get; private set; }
    public float TimePassed { get; private set; }


    public Timer(float duration)
    {
        TimePassed = 0.0f;
        TimerDuration = duration;
    }

    // Returns true when finished
    public bool Update(float timePassed)
    {
        TimePassed += timePassed;
        return TimePassed >= TimerDuration;
    }

    public void Reset()
    {
        TimePassed = 0.0f;
    }

    public void Reset(float newDuration)
    {
        TimerDuration = newDuration;
        Reset();
    }
}
