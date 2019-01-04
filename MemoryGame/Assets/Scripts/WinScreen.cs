using Assets.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class WinScreen : MonoBehaviour
{
    public Text PointsText;
    public Text TimeText;

    GameManager _gameManager;
    Animator _animator;

	// Use this for initialization
	void Start ()
    {
        var rect = GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(); // snap it to the center of the screen

        _gameManager.GetComponentInParent<GameManager>();
        gameObject.SetActive(false); // disable the object so it won't display whatsoever
	}
	
    void GameManagerOnGameEnded(GameEndStatus gameEndStatus)
    {
        if (gameEndStatus == GameEndStatus.Lost)
            return;

        gameObject.SetActive(true);

        PointsText.text = string.Format("With {0} points!", _gameManager.Points);
        TimeText.text = string.Format("{0:F2} seconds left", _gameManager.TimeLeft);

        _animator.SetBool("IsShowing", true);
    }

    public void Close()
    {
        _animator.SetBool("IsOpen", false);
    }

    public void Update()
    {
        if(_animator.GetCurrentAnimatorStateInfo(0).IsName("Hidden"))
            gameObject.SetActive(true);
    }
}
