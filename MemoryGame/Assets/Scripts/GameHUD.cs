using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    Animator _animator;
    GameManager _gameManager;

    public Text PointsText;
    public Text TimeText;

    public Slider PointsSlider;
    public Slider TimeSlider;

    public void Awake()
    {
        _animator = GetComponent<Animator>();
        _gameManager = GetComponentInParent<GameManager>();

        _gameManager.IsPlayingChanged += GameManagerOnIsPlayingChanged;
        _gameManager.PointsChanged += GameManagerOnPointsChanged;
        _gameManager.TimeLeftChanged += GameManagerOnTimeChanged;
        _gameManager.IsGameBoardAnimatingChanged += GameManagerOnIsGameBoardAnimating;
    }

    void GameManagerOnIsGameBoardAnimating(bool isGameBoardAnimating)
    {
        _animator.SetBool("IsDisabled", isGameBoardAnimating);
    }

    void GameManagerOnTimeChanged(float timeLeft)
    {
        if (!_gameManager.IsPlaying)
            TimeSlider.maxValue = timeLeft;
        
        TimeText.text = string.Format("{0:F2} seconds left", timeLeft);
        TimeSlider.value = timeLeft;
    }

    void GameManagerOnPointsChanged(int points)
    {
        PointsText.text = string.Format("{0} points", points);
        PointsSlider.value = points;
    }

    void GameManagerOnIsPlayingChanged(bool isPlayingChanged)
    {
        _animator.SetBool("IsPlaying", isPlayingChanged);
    }
}
