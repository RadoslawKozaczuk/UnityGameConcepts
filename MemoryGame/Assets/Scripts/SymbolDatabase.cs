using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SymbolDatabase : MonoBehaviour
{
    [SerializeField] Sprite[] _normalSprites;
    [SerializeField] Sprite[] _droppedSprites;
    
    List<SymbolType> _allSymbols;
    List<SymbolType> _availableSymbols; // particular game can use only a subset of symbols

    public IEnumerable<SymbolType> AllSymbols { get { return _allSymbols; } }
    public IEnumerable<SymbolType> AvailableSymbols { get { return _availableSymbols; } }
    public int AvailableSymbolsCount { get { return _availableSymbols.Count; } }

    public void Awake()
    {
        _allSymbols = new List<SymbolType>();
        _availableSymbols = new List<SymbolType>();

        if (_normalSprites.Length != _droppedSprites.Length)
            throw new ArgumentException("Normal Sprites array should be the same size as the Dropped Sprites array");

        for(int i = 0; i < _normalSprites.Length; i++)
            _allSymbols.Add(new SymbolType(i, _normalSprites[i], _droppedSprites[i]));
    }

    public void SetAvailableSymbolCount(int availableSymbolCount)
    {
        availableSymbolCount = Mathf.Clamp(availableSymbolCount, 0, _allSymbols.Count);

        _availableSymbols.Clear();

        // shuffeling the cards
        _availableSymbols
            .AddRange(_allSymbols.OrderBy(v => UnityEngine.Random.Range(0f, 1f)) // our deck is now shuffeled
            .Take(availableSymbolCount)); // take as many as we want and put it into the variable
    }

    public SymbolType GetSymbolTypeId(int id)
    {
        return _allSymbols[id];
    }

    public SymbolType GetRandomSymbol()
    {
        return _availableSymbols[UnityEngine.Random.Range(0, _availableSymbols.Count)];
    }
}
