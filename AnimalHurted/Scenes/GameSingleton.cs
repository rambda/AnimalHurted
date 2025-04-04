using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnimalHurtedLib;

public partial class GameSingleton
{
    static GameSingleton _instance;

    Deck _saveBattleDeck1;
    Deck _saveBattleDeck2;

    public Game Game { get; set; }

    public Player BuildNodePlayer { get; set; }

    public string Player1Name { get; set; }
    public string Player2Name { get; set; }
    public string AIName { get; set; }
    
    public bool Dragging { get; set; }
    
    public CardArea2D DragTarget { get; set; }
    public object DragSource { get; set; }

    public List<CardCommandQueue> FightResult { get; set; }

    public bool Sandboxing { get; set; }

    public int BattleSpeed { get; set; } = 3;

    public bool VersusAI { get; set; }

    public bool GameOverShown { get; set; }

    public static GameSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameSingleton();
            }
            return _instance;
        }
    }

    public void NewGame()
    {
        GameOverShown = false;
        Game = new Game();
        Game.Player1.Name = Player1Name;
        if (VersusAI)
            Game.Player2.Name = AIName;
        else
            Game.Player2.Name = Player2Name;
        Game.NewGame();
        BuildNodePlayer = Game.Player1; 
        // HardMode means we don't start AI thread until player 1 finishes their deck
        // so we can "see" player 1's deck before calculating best move
        if (VersusAI && !AISingleton.Instance.HardMode)
            AISingleton.Instance.StartAIThread();
    }

    public void SaveBattleDecks()
    {
        _saveBattleDeck1 = new Deck(Game.Player1, Game.BuildDeckSlots);
        Game.Player1.BattleDeck.CloneTo(_saveBattleDeck1);
        _saveBattleDeck2 = new Deck(Game.Player2, Game.BuildDeckSlots);
        Game.Player2.BattleDeck.CloneTo(_saveBattleDeck2);
    }

    public void RestoreBattleDecks()
    {
        _saveBattleDeck1.CloneTo(Game.Player1.BattleDeck);
        _saveBattleDeck2.CloneTo(Game.Player2.BattleDeck);
    }
}