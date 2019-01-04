using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class Gutter : MonoBehaviour
{
    public UIBehaviour LeftGutter;
    public UIBehaviour RightGutter;

    public GutterTile GutterTilePrefab;

    GameManager _manager;
    SymbolDatabase _symbolDatabase;

    public void Awake()
    {
        _manager = GetComponentInParent<GameManager>();
        _symbolDatabase = GetComponentInParent<SymbolDatabase>();

        _manager.IsPlayingChanged += ManagerOnIsPlayingChanged;
    }

    void ManagerOnIsPlayingChanged(bool isPlaying)
    {
        if(isPlaying)
        {
            var symbolPivot = _symbolDatabase.AvailableSymbolsCount / 2;
            var index = 0;

            foreach(var symbol in _symbolDatabase.AvailableSymbols)
            {
                var gutter = ++index <= symbolPivot ? LeftGutter : RightGutter;
                var tile = Instantiate(GutterTilePrefab);
                tile.transform.SetParent(gutter.transform);
                tile.transform.localScale = new Vector3(1, 1, 1);
                tile.transform.localPosition = new Vector3();
                tile.Type = symbol;
            }
        }
    }

    void DestroyAllChildren()
    {
        foreach (var child in LeftGutter.transform.OfType<Transform>().ToList())
            Destroy(child.gameObject);

        foreach (var child in RightGutter.transform.OfType<Transform>().ToList())
            Destroy(child.gameObject);
    }
}
