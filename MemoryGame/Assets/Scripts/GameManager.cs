using Assets.Scripts;
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    float _timeLeft;
    int _points;
    bool _isOptionsWindowOpen;
    bool _isPlaying;
    bool _isGameBoardAnimating;

    public event Action<float> TimeLeftChanged;
    public event Action<int> PointsChanged;
    public event Action<bool> IsOptionsWindowOpenChanged;
    public event Action<bool> IsPlayingChanged;
    public event Action<bool> IsGameBoardAnimatingChanged;
    public event Action<GameEndStatus> GameEnded;
    
    public float TimeLeft
    {
        get { return _timeLeft; }
        set
        {
            _timeLeft = value;

            if (TimeLeftChanged != null)
                TimeLeftChanged(value);
        }
    }

    public int Points
    {
        get { return _points; }
        set
        {
            _points = value;

            if (PointsChanged != null)
                PointsChanged(value);
        }
    }

    public bool IsOptionsWindowOpen
    {
        get { return _isOptionsWindowOpen; }
        set
        {
            if (IsOptionsWindowOpenChanged != null)
                IsOptionsWindowOpenChanged(value);
        }
    }

    public bool IsPlaying
    {
        get { return _isPlaying; }
        private set
        {
            _isPlaying = value;

            if (IsPlayingChanged != null)
                IsPlayingChanged(value);
        }
    }

    public bool IsGameBoardAnimating
    {
        get { return _isGameBoardAnimating; }
        set
        {
            _isGameBoardAnimating = value;

            if (IsGameBoardAnimatingChanged != null)
                IsGameBoardAnimatingChanged(value);
        }
    }

    public void StartGame()
    {
        IsPlaying = true;
    }

    public void EndGame(GameEndStatus status)
    {
        IsPlaying = false;

        if (GameEnded != null)
            GameEnded(status);
    }
}
