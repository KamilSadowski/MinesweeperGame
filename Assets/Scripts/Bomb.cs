using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The bomb displays 4 letters on the screen
// The player has to cut the correct cables based on either the colour
// or the letter shown on the screen, the lights on the side indicate which one to input
public class Bomb : MonoBehaviour
{
    [SerializeField] Button[] Buttons;
    [SerializeField] Text ScreenText;
    [SerializeField] Image ColourIndicator;
    [SerializeField] Image TextIndicator;
    [SerializeField] Color OnColour;
    [SerializeField] Color OffColour;
    [SerializeField] AudioClip DefuseSound;
    [SerializeField] AudioClip WrongSound;

    enum CableColours { Blue, Yellow, Green, Red }
    string[] CableColourHex = new string[4] { "0024FF", "FBFF00", "56FF00", "FF0000" };
    char[] CableColourChar = new char[4] { 'B', 'Y', 'G', 'R' };

    public Player Player;
    const int CableNo = 4;

    AudioSource _audioSource;
    int[] _correctOrder = new int[4] { 0, 1, 2, 3 }; // Order required to solve the puzzle
    int[] _fakeOrder = new int[4] { 0, 1, 2, 3 }; // Order used to confuse the player
    int _cablesCut;
    bool _textMode; // Toggle between the text and colour mode for inputting what is on the screen

    // Start is called before the first frame update
    void Start()
    {
        Player = FindObjectOfType<Player>();
        _audioSource = GetComponent<AudioSource>();
    }

    void Awake()
    {
        _cablesCut = 0;
        ScreenText.text = "";
    }

    void FinishDefuse()
    {
        _audioSource.Stop();
        _audioSource.PlayOneShot(DefuseSound);
        if (Player == null) Player = FindObjectOfType<Player>();
        Player.FinishDefusing();
    }

    public void CancelDefusing()
    {
        _audioSource.Stop();
    }

    public void CutCable(int index)
    {
        // Correct cable
        if (_correctOrder[_cablesCut] == index)
        {
            ++_cablesCut;
            Buttons[index].interactable = false;

            // Last cable
            if (_cablesCut == CableNo)
            {
                FinishDefuse();
            }
        }
        // Restart the minigame if wrong cable
        else
        {
            _audioSource.PlayOneShot(WrongSound);
            StartDefusing();
        }
    }

    void AddLetter(int letter, int colour)
    {
        ScreenText.text += "<color=#";
        ScreenText.text += CableColourHex[colour];
        ScreenText.text += ">";
        ScreenText.text += CableColourChar[letter];
        ScreenText.text += "</color>";
    }

    // Switches between the colour and the text mode
    void ChangeMode(bool textMode)
    {
        _textMode = textMode;
        if (textMode)
        {
            TextIndicator.color = OnColour;
            ColourIndicator.color = OffColour;
        }
        else
        {
            TextIndicator.color = OffColour;
            ColourIndicator.color = OnColour;
        }
    }

    // Starts the minigame by randomising the game mode and order of letters and colours
    public void StartDefusing()
    {
        _audioSource.Play();
        _cablesCut = 0;
        foreach (Button button in Buttons) button.interactable = true;

        ChangeMode(!_textMode);
        ScreenText.text = "";
        ChangeMode(Random.Range(0, 2) == 1);

        _correctOrder.RandomShuffle();
        _fakeOrder.RandomShuffle();

        if (_textMode)
        {
            for (int i = 0; i < 4; ++i)
            {
                AddLetter(_correctOrder[i], _fakeOrder[i]);
            }
        }
        else
        {
            for (int i = 0; i < 4; ++i)
            {
                AddLetter(_fakeOrder[i], _correctOrder[i]);
            }
        }
    }
}
