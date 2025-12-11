using System;
using UnityEngine;

/// <summary>
/// Quản lý turn của game - Singleton pattern
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    private int _currentTurn = 0;
    
    public int CurrentTurn => _currentTurn;
    public bool IsPlayerTurn => _currentTurn % 2 == 1;
    public bool IsNPCTurn => _currentTurn % 2 == 0;
    
    // Events
    public event Action<int> OnTurnChanged;
    public event Action OnPlayerTurnStart;
    public event Action OnNPCTurnStart;
    public event Action OnTurnEnd;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void Initialize()
    {
        _currentTurn = 1;
        OnTurnChanged?.Invoke(_currentTurn);
        OnPlayerTurnStart?.Invoke();
    }
    
    public void NextTurn()
    {
        OnTurnEnd?.Invoke();
        
        _currentTurn++;
        OnTurnChanged?.Invoke(_currentTurn);
        
        if (IsPlayerTurn)
        {
            OnPlayerTurnStart?.Invoke();
        }
        else
        {
            OnNPCTurnStart?.Invoke();
        }
    }
    
    public void ResetTurn()
    {
        _currentTurn = 0;
    }
}