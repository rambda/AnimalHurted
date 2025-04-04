using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using AnimalHurtedLib;
using System.Runtime.CompilerServices;

public interface ICardSelectHost
{
    void SelectionChanged(CardSlotNode2D cardSlot);
}

public partial class DeckNode2D : Node2D, IDragParent, ICardSlotDeck, ICardSelectHost
{
    Deck _deck;

    public AudioStreamPlayer ThumpPlayer { get { return GetNode<AudioStreamPlayer>("ThumpPlayer"); } }
    public AudioStreamPlayer GulpPlayer { get { return GetNode<AudioStreamPlayer>("GulpPlayer"); } }
    public AudioStreamPlayer WhooshPlayer { get { return GetNode<AudioStreamPlayer>("WhooshPlayer"); } }
    public AudioStreamPlayer SummonPlayer { get { return GetNode<AudioStreamPlayer>("SummonPlayer"); } }

    public Deck Deck { get { return _deck; } }
    public IBattleNode BattleNode { get { return GetParent() as IBattleNode; } }

    public bool CanDragDropLevelUp { get; set; } = true;

    public CardSlotNode2D GetCardSlotNode2D(int index)
    {
        return GetNode<CardSlotNode2D>(string.Format("CardSlotNode2D_{0}", index));
    }

    public void RenderDeck(Deck deck)
    {
        _deck = deck;
        for (int i = 0; i < deck.Size; i++)
        {
            var cardSlot = GetCardSlotNode2D(i + 1);
            cardSlot.CardArea2D.RenderCard(deck[i], i);
            // during battle cardSlot can be hidden; so restoring to visible
            cardSlot.Show();
        }
    }

    // ICardSelectHost
    public void SelectionChanged(CardSlotNode2D cardSlot)
    {
        if (GetParent() is SandboxNode)
            GetParent().EmitSignal("CardSelectionChangedSignal", cardSlot.CardArea2D.CardIndex);

        if (cardSlot.Selected)
        {
            for (int i = 1; i <= 5; i++)
            {
                var tempCardSlot = GetCardSlotNode2D(i);
                if (tempCardSlot != cardSlot)
                    tempCardSlot.Selected = false;
            }
        }
    }
    // ICardSelectHost

    public CardSlotNode2D GetSelectedCardSlotNode2D()
    {
        for (int i = 1; i <= 5; i++)
        {
            var cardSlot = GetCardSlotNode2D(i);
            if (cardSlot.Selected)
                return cardSlot;
        }
        return null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Dispose can be called from Godot editor, and our singleton
        // may not have a Game when designing
        if (GameSingleton.Instance.Game != null)
        {
            GameSingleton.Instance.Game.CardFaintedEvent -= _game_CardFaintedEvent;
            GameSingleton.Instance.Game.CardSummonedEvent -= _game_CardSummonedEvent;
            GameSingleton.Instance.Game.CardBuffedEvent -= _game_CardBuffedEvent;
            GameSingleton.Instance.Game.CardHurtEvent -= _game_CardHurtEvent;
            GameSingleton.Instance.Game.CardGainedFoodAbilityEvent -= _game_CardGainedFoodAbilityEvent;
            GameSingleton.Instance.Game.CardsMovedEvent -= _game_CardsMoved;
        }
    }

    public override void _Ready()
    {
        if (GameSingleton.Instance.Game != null)
        {
            GameSingleton.Instance.Game.CardFaintedEvent += _game_CardFaintedEvent;
            GameSingleton.Instance.Game.CardSummonedEvent += _game_CardSummonedEvent;
            GameSingleton.Instance.Game.CardBuffedEvent += _game_CardBuffedEvent;
            GameSingleton.Instance.Game.CardHurtEvent += _game_CardHurtEvent;
            GameSingleton.Instance.Game.CardGainedFoodAbilityEvent += _game_CardGainedFoodAbilityEvent;
            GameSingleton.Instance.Game.CardsMovedEvent += _game_CardsMoved;
        }
    }

    public void PlayThump()
    {
        ThumpPlayer.Play();
    }

    public void ReverseCardAreaPositions()
    {
        var cardSlot = GetCardSlotNode2D(1);
        var savePosition = cardSlot.Position;
        cardSlot.Position = GetCardSlotNode2D(5).Position;
        GetCardSlotNode2D(5).Position = savePosition;

        cardSlot = GetCardSlotNode2D(2);
        savePosition = cardSlot.Position;
        cardSlot.Position = GetCardSlotNode2D(4).Position;
        GetCardSlotNode2D(4).Position = savePosition;

        // flip the sprite to face other direction
        for (int i = 1; i <= 5; i++)
            GetCardSlotNode2D(i).CardArea2D.Sprite2D.FlipH = true;
    }

    public void HideEndingCardSlots()
    {
        for (int i = 5; i >= 1; i--)
        {
            var cardSlot = GetCardSlotNode2D(i);
            if (cardSlot.CardArea2D.Sprite2D.Visible)
                break;
            else
                cardSlot.Hide();
        }
    }

    public CardSlotNode2D GetEndingVisibleCardSlot()
    {
        for (int i = 5; i >= 1; i--)
        {
            var cardSlot = GetCardSlotNode2D(i);
            if (cardSlot.Visible)
                return cardSlot;
        }
        return null;
    }

    public static Task ToTask(SignalAwaiter signalAwaiter)
    {
        var tcs = new TaskCompletionSource();
        signalAwaiter.OnCompleted(() => tcs.SetResult());
        return tcs.Task;
    }

    public static async Task ThrowArea2D(Node parent, Area2D area2D, Vector2 toPosition)
    {
        var tweenPosX = parent.CreateTween();
        var tweenPosY_Up = parent.CreateTween();
        var tweenPosY_Down = parent.CreateTween();
        var tweenRotate = parent.CreateTween();

        float throwSpeed = (parent as IBattleNode).MaxTimePerEvent;

        // pick a somewhat random height to throw, to minimize other objects from having
        // the same trajectory
        int yDelta = GameSingleton.Instance.Game.Random.Next(0, 100);
        if (GameSingleton.Instance.Game.Random.Next(0, 2) == 1)
            yDelta *= -1;
        int arcY = 200 + yDelta;

        tweenPosX.TweenProperty(area2D, "position:x",
            toPosition.X, throwSpeed).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.In);

        tweenPosY_Up.TweenProperty(area2D, "position:y",
            area2D.GlobalPosition.Y - arcY, throwSpeed / 2).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        tweenPosY_Down.TweenProperty(area2D, "position:y",
            area2D.GlobalPosition.Y, throwSpeed / 2).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

        tweenRotate.TweenProperty(area2D, "rotation",
            6f, throwSpeed).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.OutIn);

        // await Task.WhenAll(
        //     tweenPosX.ToSignal(tweenPosX, Tween.SignalName.Finished),
        //     tweenPosY_Up.ToSignal(tweenPosY_Up, Tween.SignalName.Finished),
        //     tweenPosY_Down.ToSignal(tweenPosY_Down, Tween.SignalName.Finished),
        //     tweenRotate.ToSignal(tweenRotate, Tween.SignalName.Finished)
        // );


        await Task.WhenAll(
            ToTask(parent.ToSignal(tweenPosX, Tween.SignalName.Finished)),
            ToTask(parent.ToSignal(tweenPosY_Up, Tween.SignalName.Finished)),
            ToTask(parent.ToSignal(tweenPosY_Down, Tween.SignalName.Finished)),
            ToTask(parent.ToSignal(tweenRotate, Tween.SignalName.Finished))
        );
    }

    // IDragParent
    public void DragDropped()
    {
        if (GameSingleton.Instance.DragTarget != null && GameSingleton.Instance.DragSource is CardArea2D)
        {
            var sourceCardArea2D = GameSingleton.Instance.DragSource as CardArea2D;
            var sourceDeck = sourceCardArea2D.CardSlotNode2D.CardSlotDeck;
            var targetCardArea2D = GameSingleton.Instance.DragTarget;
            var targetDeck = targetCardArea2D.CardSlotNode2D.CardSlotDeck;
            if (targetDeck == this && sourceDeck == this)
            {
                sourceCardArea2D.CardSlotNode2D.Selected = false;
                // if dropping on an empty slot
                if (_deck[targetCardArea2D.CardIndex] == null)
                {
                    // moving a card does not invoke any abilities otherwise this
                    // would need to be done with a queue
                    _deck.MoveCard(_deck[sourceCardArea2D.CardIndex], targetCardArea2D.CardIndex);
                }
                else if (CanDragDropLevelUp)
                {
                    var targetCard = _deck[targetCardArea2D.CardIndex];
                    var sourceCard = sourceDeck.Deck[sourceCardArea2D.CardIndex];
                    if ((targetCardArea2D.CardIndex != sourceCardArea2D.CardIndex) && 
                        targetCard.Ability.GetType() == sourceCard.Ability.GetType())
                    {
                        int oldLevel = targetCard.Level;
                        targetCard.GainXP(sourceCard);
                        targetCardArea2D.RenderCard(targetCard, targetCard.Index);
                        var queue = new CardCommandQueue();
                        var savedDeck = (GetParent() as BuildNode).CreateSaveDeck();
                        GameSingleton.Instance.Game.BeginUpdate();
                        targetCard.GainedXP(queue, oldLevel);
                        GameSingleton.Instance.Game.EndUpdate();
                        // show animations from abilities, like Fish
                        (GetParent() as BuildNode).ExecuteQueue(queue, savedDeck);
                    }
                }

                targetCardArea2D.CardSlotNode2D.Selected = true;
                PlayThump();   
            }
        }
        RenderDeck(_deck);
    }

    public void DragReorder(CardArea2D atCardArea2D)
    {
        // we're either drag/dropping from the Shop scene or we are
        // drag/dropping in the build deck -- reordering cards in the same deck
        Card sourceCard = null;
        CardArea2D sourceCardArea2D = GameSingleton.Instance.DragSource as CardArea2D;
        // if reordering cards within the same deck 
        if (sourceCardArea2D.CardSlotNode2D.CardSlotDeck == this)
        {
            //.. remove source card immediately
            sourceCard = _deck[sourceCardArea2D.CardIndex];
            _deck.Remove(sourceCardArea2D.CardIndex);
        }
        if (_deck.MakeRoomAt(atCardArea2D.CardIndex))
        {
            if (sourceCardArea2D.CardSlotNode2D.CardSlotDeck == this)
                // ...place in its new position
                _deck.SetCard(sourceCard, atCardArea2D.CardIndex);
            // redisplay cards that have been moved
            RenderDeck(_deck);
            if (sourceCardArea2D.CardSlotNode2D.CardSlotDeck == this)
                // sourceCardArea2D is now associated with a different card
                // so restore its drag position and assign a new drag source card
                sourceCardArea2D.ReplaceDragSource(atCardArea2D);
            atCardArea2D.CardSlotNode2D.Selected = false;
        }
    }

    public bool GetCanDrag()
    {
        return GetParent() is BuildNode || GetParent() is SandboxNode;
    }

    public async void _game_CardFaintedEvent(object sender, CardCommand command)
    {
        if (command.Deck == this._deck)
        {
            var tween = CreateTween();

            float faintTime = BattleNode.MaxTimePerEvent;

            var cardSlot = GetCardSlotNode2D(command.Index + 1);
            tween.TweenProperty(cardSlot.CardArea2D.Sprite2D, "modulate:a",
                0.0, faintTime).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.In);

            await ToSignal(tween, "finished");

            // Restore modulate, even though we're about to hide the sprite
            var color = cardSlot.CardArea2D.Sprite2D.Modulate;
            cardSlot.CardArea2D.Sprite2D.Modulate = new Color(color.R, color.G, color.B, 1);
            cardSlot.CardArea2D.RenderCard(null, command.Index);

            command.UserEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public void _game_CardsMoved(object sender, CardCommand command)
    {
        if (command.Deck == _deck)
        {
            RenderDeck(_deck);  

            // don't invoke because we're expecting a summon event, which is likely the last command
            // in the current queue
            //command.UserEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void _game_CardSummonedEvent(object sender, CardCommand command)
    {
        var summonedCommand = command as SummonCardCommand;
        if (summonedCommand.SummonedCard.Deck == this._deck)
        {
            var cardSlot = GetCardSlotNode2D(summonedCommand.AtIndex + 1);
            if (!cardSlot.Visible)
            {
                for (int i = summonedCommand.AtIndex + 1; i >= 0; i--)
                {
                    var hiddenSlot = GetCardSlotNode2D(i);
                    if (!hiddenSlot.Visible)
                        hiddenSlot.Show();
                    else
                        break;
                }
                if (GetParent() is BattleNode)
                    await (GetParent() as BattleNode).PositionDecks(false);
            }

            SummonPlayer.Play();

            cardSlot.CardArea2D.RenderCard(_deck[summonedCommand.AtIndex], summonedCommand.AtIndex);

            var tween = CreateTween();

            float summonTime = BattleNode.MaxTimePerEvent;

            tween.TweenProperty(cardSlot.CardArea2D.Sprite2D, "scale",
                new Vector2(1.3f, 1.3f), summonTime).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.Out);

            await ToSignal(tween, "finished");

            cardSlot.CardArea2D.Sprite2D.Scale = new Vector2(1.0f, 1.0f);

            BattleNode.Reader.Signal.Release();

            command.UserEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void _game_CardBuffedEvent(object sender, CardCommand command)
    {
        if (command.Deck == this._deck)
        {
            var cardSlot = GetCardSlotNode2D(command.Index + 1);
            var sourceCardSlot = GetCardSlotNode2D((command as BuffCardCommand).SourceIndex + 1);

            var buffArea2DScene = (PackedScene)ResourceLoader.Load("res://Scenes/BuffArea2D.tscn");
            Area2D buffArea2D = buffArea2DScene.Instantiate() as Area2D;
            GetParent().AddChild(buffArea2D);
            buffArea2D.GlobalPosition = sourceCardSlot.GlobalPosition;

            await ThrowArea2D(GetParent(), buffArea2D, cardSlot.GlobalPosition);

            buffArea2D.QueueFree();

            GulpPlayer.Play();
            cardSlot.CardArea2D.RenderCard(_deck[command.Index], command.Index);

            command.UserEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void _game_CardHurtEvent(object sender, CardCommand command)
    {
        var hurtCommand = command as HurtCardCommand;
        // see also BattleNode where its _game_CardHurtEvent handles 
        // the case where source card deck is an opponent
        // in which case we have to animate from the opponent's DeckNodeScene
        if (hurtCommand.Deck == this._deck && hurtCommand.SourceDeck == hurtCommand.Deck)
        {
            var cardSlot = GetCardSlotNode2D(hurtCommand.Index + 1);
            var sourceCardSlot = GetCardSlotNode2D(hurtCommand.SourceIndex + 1);

            WhooshPlayer.Play();

            var damageArea2DScene = (PackedScene)ResourceLoader.Load("res://Scenes/DamageArea2D.tscn");
            Area2D damageArea2D = damageArea2DScene.Instantiate() as Area2D;
            GetParent().AddChild(damageArea2D);
            damageArea2D.GlobalPosition = sourceCardSlot.GlobalPosition;

            await DeckNode2D.ThrowArea2D(GetParent(), damageArea2D, cardSlot.GlobalPosition);

            damageArea2D.QueueFree();

            cardSlot.CardArea2D.RenderCard(_deck[hurtCommand.Index], hurtCommand.Index);

            command.UserEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void _game_CardGainedFoodAbilityEvent(object sender, CardCommand command)
    {
        if (command.Deck == this._deck)
        {
            var cardSlot = GetCardSlotNode2D(command.Index + 1);
            /*var sourceCardSlot = GetCardSlotNode2D(sourceIndex + 1);

            var buffArea2DScene = (PackedScene)ResourceLoader.Load("res://Scenes/BuffArea2D.tscn");
            Area2D buffArea2D = buffArea2DScene.Instance() as Area2D;
            GetParent().AddChild(buffArea2D);
            buffArea2D.GlobalPosition = sourceCardSlot.GlobalPosition;

            await DeckNode2D.ThrowArea2D(GetParent(), buffArea2D, cardSlot.GlobalPosition);

            buffArea2D.QueueFree();*/

            await ToSignal(GetTree().CreateTimer(BattleNode.MaxTimePerEvent), "timeout"); //TODO remove

            GulpPlayer.Play();
            cardSlot.CardArea2D.RenderCard(_deck[command.Index], command.Index);

            command.UserEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
