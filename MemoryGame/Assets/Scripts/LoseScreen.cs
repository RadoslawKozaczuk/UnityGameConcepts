using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoseScreen : MonoBehaviour
{
    public Text PointsText;

    GameManager _gameManager;
    Animator _animator;

    // Use this for initialization
    void Start()
    {
        var rect = GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(); // snap it to the center of the screen

        _gameManager.GetComponentInParent<GameManager>();
        gameObject.SetActive(false); // disable the object so it won't display whatsoever
    }

    void GameManagerOnGameEnded(GameEndStatus gameEndStatus)
    {
        if (gameEndStatus == GameEndStatus.Won)
            return;

        gameObject.SetActive(true);

        PointsText.text = string.Format("With {0} points!", _gameManager.Points);

        _animator.SetBool("IsShowing", true);
    }

    public void Close()
    {
        _animator.SetBool("IsOpen", false);
    }

    public void Update()
    {
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Hidden"))
            gameObject.SetActive(true);
    }
}
