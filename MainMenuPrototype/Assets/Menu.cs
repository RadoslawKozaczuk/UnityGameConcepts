using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    private Animator _animator;
    private CanvasGroup _canvasGroup;

    public bool IsOpen
    {
        get { return _animator.GetBool("IsOpen"); }
        set { _animator.SetBool("IsOpen", value); }
    }

    public void Awake()
    {

        _animator = GetComponent<Animator>();
        _canvasGroup = GetComponent<CanvasGroup>();

        // we rly want to have all our menus in designer 
        // but we want to put them in the center of the canvas while playing
        // this allows to put the menus whereever we want when in design mode
        // while still having all the things consisent when in play mode
        var rect = GetComponent<RectTransform>();
        rect.offsetMax = rect.offsetMin = new Vector2(0, 0);
    }

    public void Update()
    {
        // when animation is in state 'Open' canvas is interactable 
        // otherwise it is not
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Open"))
            _canvasGroup.blocksRaycasts = _canvasGroup.interactable = true;
        else
            _canvasGroup.blocksRaycasts = _canvasGroup.interactable = false;
    }
}
