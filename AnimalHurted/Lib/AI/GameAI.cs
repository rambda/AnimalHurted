using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MonteCarlo;

namespace AnimalHurtedLib.AI
{
    public enum MoveActionEnum { Buy, BuyFood, Reorder, Roll }

    public partial class MoveAction
    {
        public virtual void Execute(Move move, Player player, List<CardCommandQueue> result)
        {

        }
    }

    public partial class BuyAction : MoveAction
    {
        public int ShopIndex { get; set; }
        public int TargetIndex { get; set; }
        Type _boughtAbilityType;

        public override void Execute(Move move, Player player, List<CardCommandQueue> result)
        {
            var shopCard = player.ShopDeck[ShopIndex];
            // AI picked a shop card that doesn't exist, so find next card in ShopDeck
            if (shopCard == null)
                shopCard = player.ShopDeck.SkipWhile((card) => card != null && card.Index <= ShopIndex).FirstOrDefault();
            if (shopCard == null)
                shopCard = player.ShopDeck.Reverse().SkipWhile((card) => card != null && card.Index >= ShopIndex).FirstOrDefault();
            if (shopCard != null)
            {
                // _boughtAbilityType is strictly for error checking. As nodes are revisited, actions are performed again,
                // and they should be performed again in the same state
                if (_boughtAbilityType == null)
                    _boughtAbilityType = shopCard.Ability.GetType();
                else if (_boughtAbilityType != shopCard.Ability.GetType())
                    throw new Exception("Previously bought card has different ability.");
                var buildCard = player.BuildDeck[TargetIndex];
                // if a card is at target location, and we can't level it up, then sell it
                if (buildCard != null && shopCard.Ability.GetType() != buildCard.Ability.GetType() &&
                // after getting gold from selling will we have enough to buy new card
                    player.Gold + buildCard.Level >= Game.PetCost)
                {
                    var queue = new CardCommandQueue();
                    buildCard.Sell();
                    buildCard.Sold(queue, TargetIndex);
                    result.AddRange(queue.CreateExecuteResult(player.Game));
                }
                if (player.Gold >= Game.PetCost)
                {
                    var queue = new CardCommandQueue();
                    player.Game.BuyFromShop(shopCard.Index, TargetIndex, player, queue);   
                    result.AddRange(queue.CreateExecuteResult(player.Game));
                }
            }
        }
    }

    public partial class BuyFoodAction : MoveAction
    {
        public int FoodIndex { get; set; }
        public int TargetIndex { get; set; }
        Type _boughtFoodType;

        public override void Execute(Move move, Player player, List<CardCommandQueue> result)
        {
            var food = player.GetShopFoodFromIndex(FoodIndex);
            var buildCard = player.BuildDeck[TargetIndex];
            if (buildCard != null && food != null && player.Gold >= food.Cost)
            {
                if (_boughtFoodType == null)
                    _boughtFoodType = food.GetType();
                else if (_boughtFoodType != food.GetType())
                    throw new Exception("Previously bought food has different type.");
                player.BuyFood(buildCard, FoodIndex);
                var queue = new CardCommandQueue();
                buildCard.Ate(queue, food);
                result.AddRange(queue.CreateExecuteResult(player.Game));
            }
        }
    }

    public partial class ReorderAction : MoveAction
    {
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }

        public override void Execute(Move move, Player player, List<CardCommandQueue> result)
        {
            var fromCard = player.BuildDeck[FromIndex];
            if (fromCard != null)
            {
                var toCard = player.BuildDeck[ToIndex];
                if (toCard == null)
                    player.BuildDeck.MoveCard(fromCard, ToIndex);
                else if (fromCard.Ability.GetType() == toCard.Ability.GetType())
                {
                    // level up
                    var saveLevel = fromCard.Level;
                    fromCard.GainXP(toCard);
                    var queue = new CardCommandQueue();
                    fromCard.GainedXP(queue, saveLevel);
                    result.AddRange(queue.CreateExecuteResult(player.Game));
                }
                else
                {
                    // swap cards
                    player.BuildDeck.Remove(ToIndex);
                    player.BuildDeck.MoveCard(fromCard, ToIndex);
                    player.BuildDeck.SetCard(toCard, FromIndex);
                }
            }
        }
    }

    public partial class RollAction : MoveAction
    {
        public override void Execute(Move move, Player player, List<CardCommandQueue> result)
        {
            if (player.Gold >= Game.RollCost)
                // roll with seed value
                player.Roll(deductGold: true, move.SeededRandom);
        }
    }

    /// A Move represents one permutation of actions the AI can perform in the build phase before the battle
    public partial class Move : MonteCarlo.IAction
    {
        List<MoveAction> _actions = new List<MoveAction>();
        List<CardCommandQueue> _result;
        static Array _enums = Enum.GetValues(typeof(MoveActionEnum));
        int _seed = GameAIState.Random.Next();
        Random _seededRandom;

        public Random SeededRandom { get { return _seededRandom; } }

        public void ExecuteActions(Player player)
        {
            _seededRandom = new Random(_seed);
            // ensure player's shop deck is the same for each subsequent call to ExecuteActions
            //TODO: this ruins Squirrel ability
            player.Roll(deductGold: false, _seededRandom);

            // if _result == null
            _result = new List<CardCommandQueue>();
            foreach (var action in _actions)
                action.Execute(this, player, _result);
            //TODO
            // should be able to just execute commands again? will investigate later
            //else
            //    foreach (var queue in _result)
            //        foreach (var command in queue)
            //            command.Execute();
        }

        // constructor
        public Move(Player player)
        {
            AddActions(player);
        }

        void AddActions(Player player)
        {
            int gold = player.Gold;
            while (gold >= 1)
            {
                double[] probabilities = new double[] 
                { 
                    0.6, // 0.6 chance of Buy 
                    0.8, // 0.2 chance of BuyFood
                    0.9, // 0.1 chance of Reorder
                    1.0  // 0.1 chance of Roll
                };

                MoveActionEnum foundEnum = MoveActionEnum.Buy;
                double rand = GameAIState.Random.NextDouble();
                foreach (var @enum in _enums)
                    if ((MoveActionEnum)@enum == MoveActionEnum.Buy)
                    {
                        if (rand <= probabilities[0])
                        {
                            foundEnum = MoveActionEnum.Buy;
                            break;
                        }
                    }
                    else if (rand >= probabilities[(int)(MoveActionEnum)@enum - 1] && 
                        rand <= probabilities[(int)(MoveActionEnum)@enum])
                    {
                        foundEnum = (MoveActionEnum)@enum;
                        break;
                    }

                switch (foundEnum)
                {
                    case MoveActionEnum.Buy:
                        if (gold >= Game.PetCost)
                        {
                            gold -= Game.PetCost;
                            var buildIndex = GameAIState.Random.Next(player.BuildDeck.Size);
                            var shopIndex = GameAIState.Random.Next(player.Game.GetShopSlotCount());
                            _actions.Add(new BuyAction() { ShopIndex = shopIndex, TargetIndex = buildIndex });
                        }                        
                        break;
                    case MoveActionEnum.BuyFood:
                        if (gold >= Game.FoodCost)
                        {
                            gold -= Game.FoodCost;
                            var buildIndex = GameAIState.Random.Next(player.BuildDeck.Size);
                            var foodIndex = GameAIState.Random.Next(2);
                            _actions.Add(new BuyFoodAction() { FoodIndex = foodIndex, TargetIndex = buildIndex });
                        }
                        break;
                    case MoveActionEnum.Reorder:
                        var buildCard = player.BuildDeck.GetRandomCard();
                        if (buildCard != null)
                        {
                            var moveTo = GameAIState.Random.Next(player.BuildDeck.Size);
                            _actions.Add(new ReorderAction() { FromIndex = buildCard.Index, ToIndex = moveTo });
                        }
                        break;
                    case MoveActionEnum.Roll:
                        gold--;
                        _actions.Add(new RollAction());
                        break;
                    default:
                        throw new Exception("Invalid enum");
                }
            }
        }
    }

    // Helper class to support IPlayer interface from the MCTS library
    public partial class GameAIPlayer : MonteCarlo.IPlayer
    {
        Player _player;

        public GameAIPlayer(Player player)
        {
            _player = player;
        }

        public Player Player { get { return _player; } }
    }

    // not used currently, since UI wants events for AI progress
    // may remove later
    public static class GameAI
    {
        public static void ExecuteBestMove(GameAIState gameAIState, Player player)
        {
            var moves = MonteCarloTreeSearch.GetTopActions<GameAIPlayer, Move>(gameAIState, 50000);
            var move = moves.FirstOrDefault();
            move?.Action.ExecuteActions(player);
        }
    }

    // state class is attached to each node in the tree
    public partial class GameAIState : MonteCarlo.IState<GameAIPlayer, Move>
    {
        bool _rootState;
        Game _game;
        GameAIPlayer _player1;
        GameAIPlayer _player2;
        GameAIPlayer _currentPlayer;
        GameAIPlayer _opponentPlayer;
        List<Move> _actions;

        public static Random Random = new Random();

        void NewCurrentPlayer()
        {
            if (_currentPlayer == _player1)
            {
                _currentPlayer = _player2;
                _opponentPlayer = _player1;
            }
            else
            {
                _currentPlayer = _player1;
                _opponentPlayer = _player2;
            }
        }

        public GameAIState(bool rootState, Game game, Player currentPlayer)
        {
            _game = game;
            _rootState = rootState;
            _player1 = new GameAIPlayer(_game.Player1);
            _player2 = new GameAIPlayer(_game.Player2);
            if (currentPlayer == _player1.Player)
            {
                _currentPlayer = _player1;
                _opponentPlayer = _player2;
            }
            else
            {
                _currentPlayer = _player2;
                _opponentPlayer = _player1;
            }
        }

        // MonteCarlo.IState interfaces
        public IList<Move> Actions
        { 
            get
            {
                if (_actions == null)
                {
                    // pick an arbitrary number of permutations of random actions to perform during build
                    // see also GameSingleton's AIMaxIterations const
                    // higher numbers increase RAM consumption considerably
                    var count = 50;
                    if (_rootState)
                        // picking a higher number of actions for root node to explore
                        count = 150;

                    _actions = new List<Move>();
                    for (int i = 1; i <= count; i++)
                    {
                        var move = new Move(_currentPlayer.Player);
                        _actions.Add(move);
                    }
                }
                return _actions;
            }
        }

        public void Rollout()
        {
            while (!_game.IsGameOver())
            {
                _currentPlayer.Player.NewBattleDeck();
                _opponentPlayer.Player.NewBattleDeck();
                _game.CreateFightResult();
                _game.NewRound();
                if (!_game.IsGameOver())
                {
                    var move1 = new Move(_currentPlayer.Player);
                    move1.ExecuteActions(_currentPlayer.Player);
                    var move2 = new Move(_opponentPlayer.Player);
                    move2.ExecuteActions(_opponentPlayer.Player);
                }
            }
        }

        public void ApplyAction(Move action)
        { 
            action.ExecuteActions(_currentPlayer.Player);
            if (_currentPlayer == _player2)
            {
                _opponentPlayer.Player.NewBattleDeck();
                _currentPlayer.Player.NewBattleDeck();
                _game.CreateFightResult();
                _game.NewRound();
            }
            NewCurrentPlayer();
        }

        public GameAIPlayer CurrentPlayer { get { return _currentPlayer; } }

        public double GetData(GameAIPlayer player)
        { 
            // ignore player param since it's from cloned state
            var currentPlayer = _player2.Player;
            Player opponentPlayer;
            opponentPlayer = currentPlayer.GetOpponentPlayer();
            if (currentPlayer.Lives > 0 && opponentPlayer.Lives == 0)
                return 1.0; 
            else if (opponentPlayer.Lives > 0 && currentPlayer.Lives == 0)
                return 0;
            else
                return 0.5;
        }

        public IState<GameAIPlayer, Move> Clone()
        { 
            Game game = new Game();
            _game.CloneTo(game);
            Player currentPlayer;
            if (_currentPlayer.Player == _game.Player1)
                currentPlayer = game.Player1;
            else
                currentPlayer = game.Player2;
            return new GameAIState(false, game, currentPlayer); 
        }
    }
}