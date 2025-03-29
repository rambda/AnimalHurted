using Godot;
using AnimalHurtedLib;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

public interface IBattleNode
{
    float MaxTimePerEvent { get; set; }
    CardCommandQueueReader Reader { get; }
}

public partial class BattleNode : Node, IBattleNode
{
    bool _playingAttack;
    bool _playingBattle;
    bool _battleStopped;
    Vector2 _player1DeckPosition;
    Vector2 _player2DeckPosition;
    CardCommandQueueReader _reader;

    // IBattleNode
    // every card command event (OnHurt, OnFaint etc.) that is handled by BattleNode and DeckNode2D
    // must be finished before MaxTimePerEvent. See comments in PositionDecks
    public float MaxTimePerEvent { get; set; } = DefaultMaxTimePerEvent;
    public CardCommandQueueReader Reader { get { return _reader; } }
    // IBattleNode

    public const float DefaultMaxTimePerEvent = 0.4f;

    public DeckNode2D Player1DeckNode2D { get { return GetNode<DeckNode2D>("Player1DeckNode2D"); } }
    public DeckNode2D Player2DeckNode2D { get { return GetNode<DeckNode2D>("Player2DeckNode2D"); } }
    public AudioStreamPlayer FightPlayer { get { return GetNode<AudioStreamPlayer>("FightPlayer"); } }
    public Button ReplayButton { get { return GetNode<Button>("ReplayButton"); } }
    public Button SaveButton { get { return GetNode<Button>("SaveButton"); } }
    public TextureButton PlayOneButton { get { return GetNode<TextureButton>("PlayOneButton"); } }
    public FileDialog SaveFileDialog { get { return GetNode<FileDialog>("SaveFileDialog"); } } 

    [Signal]
    public delegate void ExecuteQueueOverSignalEventHandler();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Dispose can be called from Godot editor, and our singleton
        // may not have a Game when designing
        if (GameSingleton.Instance.Game != null)
        {
            GameSingleton.Instance.Game.AttackEvent -= _game_AttackEvent;
            GameSingleton.Instance.Game.CardHurtEvent -= _game_CardHurtEvent;
        }
    }

    public override void _Ready()
    {
        Connect(SignalName.ExecuteQueueOverSignal, Callable.From(_signal_ExecuteQueueOver),
            // important to be Deferred because it ensures that all tweens will be freed
            // before the next set of animation events fire off -- so a new "tween_completed"
            // signal can be invoked
            (uint)ConnectFlags.Deferred);

        GetNode<Slider>("SpeedSlider").Value = GameSingleton.Instance.BattleSpeed;
        SetMaxTimePerEvent();

        _player1DeckPosition = Player1DeckNode2D.Position;
        _player2DeckPosition = Player2DeckNode2D.Position;

        GameSingleton.Instance.Game.AttackEvent += _game_AttackEvent;
        GameSingleton.Instance.Game.CardHurtEvent += _game_CardHurtEvent;

        Player1DeckNode2D.RenderDeck(GameSingleton.Instance.Game.Player1.BattleDeck);
        Player2DeckNode2D.ReverseCardAreaPositions();
        Player2DeckNode2D.RenderDeck(GameSingleton.Instance.Game.Player2.BattleDeck);

        _reader = new CardCommandQueueReader(this, GameSingleton.Instance.FightResult, "ExecuteQueueOverSignal");
    }

    public override void _Input(InputEvent @event)
    {
        #if CHEATS_ENABLED
        // pressing Alt+R in battle screen will retry the battle with new random variables
        // useful for replaying battles to see if outcome would be different by chance
        if (Input.IsActionPressed("retry_battle"))
        {
            GameSingleton.Instance.RestoreBattleDecks();
            GameSingleton.Instance.SaveBattleDecks();
            GameSingleton.Instance.FightResult = GameSingleton.Instance.Game.CreateFightResult();
            // restore for rendering in next scene
            GameSingleton.Instance.RestoreBattleDecks();
            GetTree().ChangeSceneToFile("res://Scenes/BattleNode.tscn");
        }
        #endif
    } 
    
    public void _on_ContinueButton_pressed()
    {
        if (GameSingleton.Instance.Sandboxing)
            GetTree().ChangeSceneToFile("res://Scenes/SandboxNode.tscn");
        else
        {
            if (GameSingleton.Instance.Game.IsGameOver())
            {
                if (GameSingleton.Instance.GameOverShown)
                    GetTree().ChangeSceneToFile("res://Scenes/MainNode.tscn");
                else
                    GetTree().ChangeSceneToFile("res://Scenes/WinnerNode.tscn");
            }
            else
            {
                GameSingleton.Instance.BuildNodePlayer = GameSingleton.Instance.Game.Player1; 
                GetTree().ChangeSceneToFile("res://Scenes/BuildNode.tscn");
            }
        }
    }

    public void _on_ReplayButton_pressed()
    {
        BeginReplay();
    }

    public void BeginReplay()
    {
        Player1DeckNode2D.Position = _player1DeckPosition;
        Player2DeckNode2D.Position = _player2DeckPosition;

        GameSingleton.Instance.RestoreBattleDecks();

        Player1DeckNode2D.RenderDeck(GameSingleton.Instance.Game.Player1.BattleDeck);
        Player2DeckNode2D.RenderDeck(GameSingleton.Instance.Game.Player2.BattleDeck);

        _reader.Reset();
    }

    public void _on_PlayOneButton_pressed()
    {
        if (_playingBattle)
            _battleStopped = true;
        else if (!_playingAttack && !_playingBattle)
        {
            if (_reader.Finished)
                BeginReplay();
            _playingAttack = true;
            ReplayButton.Disabled = true;
            SaveButton.Disabled = true;
            _reader.Execute();
        }
    }

    public void _on_PlayButton_pressed()
    {
        if (!_playingBattle && !_playingAttack)
        {
            if (_reader.Finished)
                BeginReplay();
            
            PlayOneButton.TextureNormal = GD.Load<Texture2D>($"res://Assets/pause_button.png");
            PlayOneButton.TexturePressed = GD.Load<Texture2D>($"res://Assets/pause_button_pressed.png");

            _playingBattle = true;
            ReplayButton.Disabled = true;
            SaveButton.Disabled = true;
            _reader.Execute();
        }
    }

    public async void _signal_ExecuteQueueOver()
    {
        _playingAttack = false;
        if (_playingBattle)
        {
            if (_reader.Finished || _battleStopped)
            {
                PlayOneButton.TextureNormal = GD.Load<Texture2D>($"res://Assets/play_one_button.png");
                PlayOneButton.TexturePressed = GD.Load<Texture2D>($"res://Assets/play_one_button_pressed.png");

                _playingBattle = false;
                _battleStopped = false;
                ReplayButton.Disabled = false;
                SaveButton.Disabled = false;
            }
            else
            {
				// while every event stays within MaxTimePerEvent, the summon event must call PositionDecks
				// which waits for MaxTimePerEvent, and then spends PositionDeckMoveSpeed amount time to move decks
				// which means the true maximum time spent considering all events is MaxTimePerEvent + PositionDeckMoveSpeed
				// so we have to wait this additional amount of time before starting the next round of animations
                await ToSignal(GetTree().CreateTimer(MaxTimePerEvent + 0.1f), "timeout");
                _reader.Execute();
            }
        }
        else
        {
            ReplayButton.Disabled = false;
            SaveButton.Disabled = false;
        }
    }

    void SetMaxTimePerEvent()
    {
        if (GameSingleton.Instance.BattleSpeed > 3)
            MaxTimePerEvent = DefaultMaxTimePerEvent / ((GameSingleton.Instance.BattleSpeed - 3) * 2);
        else if (GameSingleton.Instance.BattleSpeed < 3)
            MaxTimePerEvent = DefaultMaxTimePerEvent * ((3 - GameSingleton.Instance.BattleSpeed) * 2);
        else
            MaxTimePerEvent = DefaultMaxTimePerEvent;
    }

    public void _on_SpeedSlider_value_changed(float value)
    {
        GameSingleton.Instance.BattleSpeed = (int)value;
        SetMaxTimePerEvent();
    }

    public async void _game_AttackEvent(object sender, CardCommand command)
    {
        await PositionDecks();

        var tween1 = CreateTween();
        var tween2 = CreateTween();

        float rotationTime = MaxTimePerEvent;

        var card1 = GameSingleton.Instance.Game.Player1.BattleDeck.GetLastCard();
        var cardSlot1 = Player1DeckNode2D.GetCardSlotNode2D(card1.Index + 1);
        tween1.TweenProperty(cardSlot1.CardArea2D.Sprite2D, "rotation",
            0.5, // radians; about 30 degrees
            rotationTime).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);

        var card2 = GameSingleton.Instance.Game.Player2.BattleDeck.GetLastCard();
        var cardSlot2 = Player2DeckNode2D.GetCardSlotNode2D(card2.Index + 1);
        tween2.TweenProperty(cardSlot2.CardArea2D.Sprite2D, "rotation",
            -0.5, // radians; about -30 degrees
            rotationTime).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);

        await ToSignal(tween1, "finished");

        cardSlot1.CardArea2D.Sprite2D.Rotation = 0;
        cardSlot2.CardArea2D.Sprite2D.Rotation = 0;

        cardSlot2.CardArea2D.RenderCard(card2, card2.Index);
        cardSlot1.CardArea2D.RenderCard(card1, card1.Index);

        // TODO: if a knockout, play additional knockout sound clip
        FightPlayer.Play();

        command.UserEvent?.Invoke(this, EventArgs.Empty);
    }

    public async void _game_CardHurtEvent(object sender, CardCommand command)
    {
        var hurtCommand = command as HurtCardCommand;
        // see also DeckNode2D where its _game_CardHurtEvent handles 
        // the case where source card deck is the same as the card
        if (hurtCommand.SourceDeck != hurtCommand.Deck)
        {
            DeckNode2D deckNode2D;
            DeckNode2D sourceDeckNode2D;
            if (hurtCommand.Deck.Player == GameSingleton.Instance.Game.Player1)
                deckNode2D = Player1DeckNode2D;
            else
                deckNode2D = Player2DeckNode2D;
            if (hurtCommand.SourceDeck.Player == GameSingleton.Instance.Game.Player1)
                sourceDeckNode2D = Player1DeckNode2D;
            else
                sourceDeckNode2D = Player2DeckNode2D;

            var cardSlot = deckNode2D.GetCardSlotNode2D(hurtCommand.Index + 1);
            var sourceCardSlot = sourceDeckNode2D.GetCardSlotNode2D(hurtCommand.SourceIndex + 1);

            deckNode2D.WhooshPlayer.Play();

            var damageArea2DScene = (PackedScene)ResourceLoader.Load("res://Scenes/DamageArea2D.tscn");
            Area2D damageArea2D = damageArea2DScene.Instantiate() as Area2D;
            AddChild(damageArea2D);
            damageArea2D.GlobalPosition = sourceCardSlot.GlobalPosition;

            await DeckNode2D.ThrowArea2D(this, damageArea2D, cardSlot.GlobalPosition);

            damageArea2D.QueueFree();

            cardSlot.CardArea2D.RenderCard(deckNode2D.Deck[hurtCommand.Index], hurtCommand.Index);

            command.UserEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task PositionDecks(bool hideCardSlots = true)
    {
        float moveSpeed = MaxTimePerEvent;

        await ToSignal(GetTree().CreateTimer(MaxTimePerEvent), "timeout");

        if (hideCardSlots)
        {
            Player1DeckNode2D.HideEndingCardSlots();
            Player2DeckNode2D.HideEndingCardSlots();
        }

        var tween1 = CreateTween();
        var tween2 = CreateTween();

        var lastVisibleCardSlot = Player1DeckNode2D.GetEndingVisibleCardSlot();
        Tween awaitTween = null;
        if (lastVisibleCardSlot != null)
        {
            var destination = _player1DeckPosition;
            var lastCardSlot = Player1DeckNode2D.GetCardSlotNode2D(5);
            destination.X += lastCardSlot.GlobalPosition.X - lastVisibleCardSlot.GlobalPosition.X;
            tween1.TweenProperty(Player1DeckNode2D, "position",
                destination, moveSpeed).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
            awaitTween = tween1;
        }

        lastVisibleCardSlot = Player2DeckNode2D.GetEndingVisibleCardSlot();
        if (lastVisibleCardSlot != null)
        {
            var destination = _player2DeckPosition;
            var lastCardSlot = Player2DeckNode2D.GetCardSlotNode2D(5);
            destination.X -= lastVisibleCardSlot.GlobalPosition.X - lastCardSlot.GlobalPosition.X;
            tween2.TweenProperty(Player2DeckNode2D, "position",
                destination, moveSpeed).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
            if (awaitTween == null)
                awaitTween = tween2;
        }

        if (awaitTween != null)
            await ToSignal(awaitTween, "finished");

    }

    public void _on_SaveButton_pressed()
    {
        SaveFileDialog.PopupCentered();
    }

    public void _on_FileDialog_file_selected(Godot.Path3D @string)
    {
        // restore battle decks before serializing them
        BeginReplay();
        using (var fileStream = new FileStream(ProjectSettings.GlobalizePath(SaveFileDialog.CurrentPath), FileMode.Create))
        {
            using (var writer = new BinaryWriter(fileStream))
            {
                GameSingleton.Instance.Game.Player1.BattleDeck.SaveToStream(writer);
                GameSingleton.Instance.Game.Player2.BattleDeck.SaveToStream(writer);
            }
        }
    }
}
