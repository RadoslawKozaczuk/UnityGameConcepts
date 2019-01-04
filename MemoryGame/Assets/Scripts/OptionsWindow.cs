using UnityEngine;

public class OptionsWindow : MonoBehaviour
{
    Animator _animator;
    GameManager _gameManager;

    public void Awake()
    {
        _animator = GetComponent<Animator>();
        _gameManager = GetComponentInParent<GameManager>();

        _gameManager.IsOptionsWindowOpenChanged += GameManagerOnIsOptionsWindowOpenChanged;
    }

    void GameManagerOnIsOptionsWindowOpenChanged(bool isOptionsWindowOpen)
    {
        _animator.SetBool("IsOpen", isOptionsWindowOpen);
    }
}
