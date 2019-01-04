using UnityEngine;

using UnityEngine.UI;

// this is the visual thing that is moving around the screen
// but the board tile is comunicating with the gutter tile
public class DraggedTile : MonoBehaviour {

    Animator _animator;
    Image _image;
    SymbolType _symbolType;

    public bool IsOverDropsite
    {
        get { return _animator.GetBool("IsOverDropsite"); }
        set { _animator.SetBool("IsOverDropsite", value); }
    }

    public SymbolType Type
    {
        get { return _symbolType; }
        set
        {
            _symbolType = value;
            _image.sprite = value.Normal;
        }
    }

    public void Awake()
    {
        _image = GetComponent<Image>();
        _animator = GetComponent<Animator>();
    }

    public void Update()
    {
        if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Destroy"))
            return;

        Destroy(gameObject);
    }

    public void Drop()
    {
        _animator.SetTrigger("Dropped");
    }
}
