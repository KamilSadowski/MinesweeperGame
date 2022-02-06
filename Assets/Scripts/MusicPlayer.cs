using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public enum MusicPlayerState { Off, Menu, Roaming, Chasing }
   
    [SerializeField] AudioClip[] MenuSongs;
    [SerializeField] AudioClip[] RoamingSongs;
    [SerializeField] AudioClip[] ChasingSongs;
    
    MusicPlayerState _state;
    AudioSource _audioSource;


    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        ChangeState(MusicPlayerState.Menu);
    }

    // Only changes the state if the current state is not the same as the one you are trying to change to
    public void ChangeState(MusicPlayerState state)
    {
        if (state != _state)
        {
            _state = state;
            if (_audioSource != null)
            {
                _audioSource.Stop();
                switch (_state)
                {
                    case MusicPlayerState.Menu:
                        {
                            _audioSource.clip = MenuSongs[Random.Range(0, MenuSongs.Length)];
                            _audioSource.Play();
                            break;
                        }
                    case MusicPlayerState.Roaming:
                        {
                            _audioSource.clip = RoamingSongs[Random.Range(0, RoamingSongs.Length)];
                            _audioSource.Play();
                            break;
                        }
                    case MusicPlayerState.Chasing:
                        {
                            _audioSource.clip = ChasingSongs[Random.Range(0, ChasingSongs.Length)];
                            _audioSource.Play();
                            break;
                        }
                }
                _audioSource.loop = true;
            }
        }

    }

}
