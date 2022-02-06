using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Audio;

public class UI : MonoBehaviour
{
    [SerializeField] private CanvasGroup Menu;
    [SerializeField] private CanvasGroup Game;
    [SerializeField] private CanvasGroup Result;
    [SerializeField] private CanvasGroup Bomb;
    [SerializeField] private CanvasGroup Options;
    [SerializeField] private TMP_Text TimerText;
    [SerializeField] private TMP_Text ResultText;
    [SerializeField] private TMP_Text Score;
    [SerializeField] private TMP_Text LivesLeft;
    [SerializeField] private TMP_Text MaxLives;
    [SerializeField] private TMP_Text BombsLeft;
    [SerializeField] private TMP_Text MaxBombs;
    [SerializeField] private TMP_Text HighScore;
    [SerializeField] private Image ScreenOverlay;
    [SerializeField] private Sprite[] BloodOverlays;
    [SerializeField] Slider EffectsSlider;
    [SerializeField] Slider MusicSlider;
    // Audio mixers could be moved to the game class once there is a need for sound effects to be applied
    [SerializeField] AudioMixer EffectsMixer;
    [SerializeField] AudioMixer MusicMixer;

    private static readonly string[] ResultTexts = { "Game Over!", "You Win!!" };
    private static readonly float AnimationTime = 0.5f;
    private static readonly float OverlayTransparency = 0.25f;

    private void Start()
    {
        // Check if player prefs were set, if not, give default values
        if (!PlayerPrefs.HasKey("Music"))
        {
            PlayerPrefs.SetFloat("Music", -10.0f);
        }
        if (!PlayerPrefs.HasKey("FX"))
        {
            PlayerPrefs.SetFloat("FX", -10.0f);
        }
        if (!PlayerPrefs.HasKey("HighScore"))
        {
            PlayerPrefs.SetFloat("HighScore", 0.0f);
        }

        // Update variables based on player prefs data
        MusicMixer.SetFloat("masterVolume", Mathf.Log10(PlayerPrefs.GetFloat("Music") * 20));
        EffectsMixer.SetFloat("masterVolume", Mathf.Log10(PlayerPrefs.GetFloat("FX") * 20));
        UpdateSettingsSliders();
    }

    public void ShowMenu()
    {
        StartCoroutine(ShowCanvas(Menu, 1.0f));
        UpdateScore();
    }

    public void ShowOptions()
    {
        StartCoroutine(ShowCanvas(Options, 1.0f));
        UpdateSettingsSliders();
    }

    public void ShowGame()
    {
        StartCoroutine(ShowCanvas(Game, 1.0f));
    }
    public void ShowBomb()
    {
        StartCoroutine(ShowCanvas(Bomb, 1.0f));
    }

    public void ShowResult(bool success)
    {
        if (ResultText != null)
        {
            ResultText.text = ResultTexts[success ? 1 : 0];
        }

        StartCoroutine(ShowCanvas(Result, 1.0f));
    }

    public void HideMenu()
    {
        StartCoroutine(ShowCanvas(Menu, 0.0f));
    }

    public void HideOptions()
    {
        StartCoroutine(ShowCanvas(Options, 0.0f));
    }

    public void HideGame()
    {
        StartCoroutine(ShowCanvas(Game, 0.0f));
    }

    public void HideResult()
    {
        StartCoroutine(ShowCanvas(Result, 0.0f));
    }

    public void HideBomb()
    {
        StartCoroutine(ShowCanvas(Bomb, 0.0f));
    }

    public void UpdateTimer(double gameTime)
    {
        if (TimerText != null)
        {
            TimerText.text = FormatTime(gameTime);
        }
    }

    private void Awake()
    {
        if (Menu != null)
        {
            Menu.alpha = 0.0f;
            Menu.interactable = false;
            Menu.blocksRaycasts = false;
        }

        if (Game != null)
        {
            Game.alpha = 0.0f;
            Game.interactable = false;
            Game.blocksRaycasts = false;
        }

        if (Result != null)
        {
            Result.alpha = 0.0f;
            Result.interactable = false;
            Result.blocksRaycasts = false;
        }

        if (Bomb != null)
        {
            Bomb.alpha = 0.0f;
            Bomb.interactable = false;
            Bomb.blocksRaycasts = false;
        }

        if (Options != null)
        {
            Options.alpha = 0.0f;
            Options.interactable = false;
            Options.blocksRaycasts = false;
        }
    }

    private static string FormatTime(double seconds)
    {
        float m = Mathf.Floor((int)seconds / 60);
        float s = (float)seconds - (m * 60);
        string mStr = m.ToString("00");
        string sStr = s.ToString("00.000");
        return string.Format("{0}:{1}", mStr, sStr);
    }

    private IEnumerator ShowCanvas(CanvasGroup group, float target)
    {
        if (group != null)
        {
            float startAlpha = group.alpha;
            float t = 0.0f;

            group.interactable = target >= 1.0f;
            group.blocksRaycasts = target >= 1.0f;

            while (t < AnimationTime)
            {
                t = Mathf.Clamp(t + Time.deltaTime, 0.0f, AnimationTime);
                group.alpha = Mathf.SmoothStep(startAlpha, target, t / AnimationTime);
                yield return null;
            }
        }
    }

    public void UpdateScore(int score)
    {
        Score.text = score.ToString();
        SubmitScore(score);
    }

    public void UpdateLives(int left, int max)
    {
        LivesLeft.text = left.ToString();
        MaxLives.text = max.ToString();
        if (left == max) ScreenOverlay.color = new Color(ScreenOverlay.color.r, ScreenOverlay.color.g, ScreenOverlay.color.b, 0.0f);
        else if (left < BloodOverlays.Length)
        {
            ScreenOverlay.sprite = BloodOverlays[left];
            ScreenOverlay.color = new Color(ScreenOverlay.color.r, ScreenOverlay.color.g, ScreenOverlay.color.b, OverlayTransparency);
        }
    }

    public void UpdateBombs(int left, int max)
    {
        BombsLeft.text = left.ToString();
        MaxBombs.text = max.ToString();
    }

    public void UpdateMusicSlider()
    {
        PlayerPrefs.SetFloat("Music", MusicSlider.value);
        MusicMixer.SetFloat("masterVolume", PlayerPrefs.GetFloat("Music"));
    }

    public void UpdateFXSlider()
    {
        PlayerPrefs.SetFloat("FX", EffectsSlider.value);
        EffectsMixer.SetFloat("masterVolume", PlayerPrefs.GetFloat("FX"));
    }

    public void UpdateSettingsSliders()
    {
        EffectsSlider.value = PlayerPrefs.GetFloat("FX");
        EffectsMixer.SetFloat("masterVolume", PlayerPrefs.GetFloat("FX"));
        MusicSlider.value = PlayerPrefs.GetFloat("Music");
        MusicMixer.SetFloat("masterVolume", PlayerPrefs.GetFloat("Music"));
    }

    private void UpdateScore()
    {
        HighScore.text = PlayerPrefs.GetFloat("HighScore").ToString();
    }

    void SubmitScore(int newScore)
    {
        if (PlayerPrefs.GetFloat("HighScore") < newScore)
        {
            PlayerPrefs.SetFloat("HighScore", newScore);
        }
    }
}
