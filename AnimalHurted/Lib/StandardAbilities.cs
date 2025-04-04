using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AnimalHurtedLib
{
    public partial class NoAbility : Ability
    {
        public NoAbility() : base()
        {

        }
    }

    public partial class AntAbility : Ability
    {
        public AntAbility() : base()
        {
            DefaultHP = 1;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Faint => Give a random friend +{0} attack and +{1} health.", 2 * card.Level, card.Level);
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                var buffCard = card.Deck.GetRandomCard(new HashSet<int>() { index });
                if (buffCard != null)
                    queue.Add(new BuffCardCommand(buffCard, index, level, 2 * level).Execute());
            });
        }
    }

    public partial class CricketAbility : Ability
    {
        public CricketAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Faint => Summon a {0}/{1} cricket.", card.Level, card.Level);
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                // cricket has fainted but we still have to search for an empty slot because it's possible that
                // another ability method has moved cards here; see comments for sheep
                if (CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                    queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, typeof(ZombieCricketAbility), level, level).Execute());
            });
        }
    }

    public partial class ZombieCricketAbility : NoAbility
    {
    }

    public partial class OtterAbility : Ability
    {
        public OtterAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Buy => Give a random friend +{0} attack and +{1} health.", card.Level, card.Level);
        }

        public override void Bought(CardCommandQueue queue, Card card)
        {
            base.Bought(queue, card);
            // find and buff a random card that is not the otter
            var buffCard = card.Deck.GetRandomCard(new HashSet<int>() { card.Index });
            if (buffCard != null)
                queue.Add(new BuffCardCommand(buffCard, card.Index, card.Level, card.Level).Execute());
        }
    }

    public partial class BeaverAbility : Ability
    {
        public BeaverAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Sell => Give two random friends +{0} health.", card.Level);
        }

        public override void Sold(CardCommandQueue queue, Card card, int index)
        {
            base.Sold(queue, card, index);
            var buffCard1 = card.Deck.GetRandomCard(new HashSet<int>() { index });
            if (buffCard1 != null)
            {
                queue.Add(new BuffCardCommand(buffCard1, index, card.Level, 0).Execute());
                // second card can't be the first card we found
                var buffCard2 = card.Deck.GetRandomCard(new HashSet<int>() { index, buffCard1.Index });
                if (buffCard2 != null)
                    queue.Add(new BuffCardCommand(buffCard2, index, card.Level, 0).Execute());
            }
        }
    }

    public partial class MosquitoAbility : Ability
    {
        public MosquitoAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Start of battle => Deal 1 damage to {0} random enemies.", card.Level);
        }

        public override void BattleStarted2(CardCommandQueue queue, Card card)
        {
            base.BattleStarted2(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                var excludingIndexes = new HashSet<int>();
                for (int i = 1; i <= level; i++)
                {
                    var randomCard = opponent.BattleDeck.GetRandomCard(excludingIndexes);
                    if (randomCard != null)
                    {
                        queue.Add(new HurtCardCommand(randomCard, 1, card.Deck, card.Index).Execute());
                        // ensures we won't pick the same pet more than once when getting the
                        // next random card
                        excludingIndexes.Add(randomCard.Index);
                    }
                }
            });
        }
    }

    public partial class PigAbility : Ability
    {
        public PigAbility() : base()
        {
            DefaultHP = 1;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Sell => Gain an extra {0} gold.", card.Level);
        }

        public override void Sold(CardCommandQueue queue, Card card, int index)
        {
            base.Sold(queue, card, index);
            card.Deck.Player.Gold += card.Level;
        }
    }

    public partial class DuckAbility : Ability
    {
        public DuckAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Sell => Give shop pets {0} health.", card.Level);
        }

        public override void Sold(CardCommandQueue queue, Card card, int index)
        {
            base.Sold(queue, card, index);
            foreach (var shopCard in card.Deck.Player.ShopDeck)
                shopCard.Buff(-1, card.Level, 0);
        }
    }

    public partial class FishAbility : Ability
    {
        public FishAbility() : base()
        {
            DefaultHP = 3;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return card.Level < 3 ? $"Level-up => Give all friends +{card.Level} health and +{card.Level} attack." : string.Empty;
        }

        public override void LeveledUp(CardCommandQueue queue, Card card)
        {
            base.LeveledUp(queue, card);
            foreach (var friendCard in card.Deck)
            {
                if (friendCard != card)
                    queue.Add(new BuffCardCommand(friendCard, card.Index, card.Level - 1, card.Level - 1).Execute());
            }
        }
    }

    public partial class HorseAbility : Ability
    {
        public HorseAbility() : base()
        {
            DefaultHP = 1;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Friend summoned => Give it +{0} attack.", card.Level);
        }

        public override void FriendSummoned(CardCommandQueue queue, Card card, Card summonedCard)
        {
            base.FriendSummoned(queue, card, summonedCard);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                queue.Add(new BuffCardCommand(summonedCard, card.Index, 0, level, 
                    // buff the build points if not fighting
                    !card.Deck.Player.Game.Fighting).Execute());
            });
        }
    }

    // Tier 2
    public partial class CrabAbility : Ability
    {
        public CrabAbility() : base()
        {
            DefaultHP = 3;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return "Buy => Copy health from the most healthy friend.";
        }

        public override void Bought(CardCommandQueue queue, Card card)
        {
            base.Bought(queue, card);
            var maxCard = card.Deck.OrderByDescending(c => c.TotalHitPoints).First();
            // if maxCard was buffed by a cupcake, then we're taking on those hit points
            // as well
            int delta = maxCard.TotalHitPoints - card.TotalHitPoints;
            if (delta > 0)
                queue.Add(new BuffCardCommand(card, card.Index, delta, 0).Execute());
        }
    }

    public partial class DodoAbility : Ability
    {
        public DodoAbility() : base()
        {
            DefaultHP = 3;
            DefaultAttack = 2;
        }

        void GetAttackPercent(Card card, int level, out int attackPoints, out int attackPercent)
        {
            switch (level)
            {
                // buff 50% of dodo's attack
                case 1:
                    // doing integer division, so adding +1 to card.AttackPoints to round up
                    attackPoints = (card.TotalAttackPoints + 1) / 2;
                    attackPercent = 50;
                    break;
                // buff 100% of dodo's attack
                case 2:
                    attackPoints = card.TotalAttackPoints;
                    attackPercent = 100;
                    break;
                // buff 150% of dodo's attack
                case 3:
                    attackPoints = card.TotalAttackPoints + ((card.TotalAttackPoints + 1) / 2);
                    attackPercent = 150;
                    break;
                default:
                    attackPercent = 0;
                    attackPoints = 0;
                    Debug.Assert(false);
                    break;
            }
        }

        public override string GetAbilityMessage(Card card)
        {
            GetAttackPercent(card, card.Level, out int attackPoints, out int attackPercent);
            return string.Format("Start of battle => Give {0}% of Dodo's attack to friend ahead.", attackPercent);
        }

        public override void BattleStarted2(CardCommandQueue queue, Card card)
        {
            base.BattleStarted2(queue, card);
            // Note that this card might have been attacked by a mosquito and about to be fainted (see Game.FightOne, after calls to BattleStarted, there is a sweep to faint cards)
            // Same is true with the card ahead. It may have taken damage and about to be fainted
            // if a Dodo's ability were to buff the hitpoints of the card ahead, then that could bring the hitpoints of that card from negative to positive again --
            // e.g. bringing that card back to life again
            PerformTigerAbility(card, card.Index, (level) =>
            {
                Card nextCard = null;
                if (card.Index < card.Deck.Size - 1)
                    nextCard = card.Deck[card.Index + 1];
                if (nextCard != null)
                {
                    GetAttackPercent(card, level, out int attackPoints, out int attackPercent);
                    queue.Add(new BuffCardCommand(nextCard, card.Index, 0, attackPoints).Execute());
                }
            });
        }
    }

    public partial class ElephantAbility : Ability
    {
        public ElephantAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Before attack => Deal {0} damage to friend behind.", card.Level);
        }

        public override void BeforeAttack(CardCommandQueue queue, Card card)
        {
            base.BeforeAttack(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                Debug.Assert(card.Index != -1);
                Card priorCard = null;
                if (card.Index > 0)
                    priorCard = card.Deck[card.Index - 1];
                if (priorCard != null)
                    queue.Add(new HurtCardCommand(priorCard, level, card.Deck, card.Index).Execute());
            });
        }
    }

    public partial class FlamingoAbility : Ability
    {
        public FlamingoAbility() : base()
        {
            DefaultHP = 1;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Faint => Give the two friends behind +{0} attack and +{1} health.", card.Level, card.Level);
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                for (int i = 1; i <= 2; i++)
                    if (index - i >= 0)
                    {
                        var priorCard = card.Deck[index - i];
                        if (priorCard != null && priorCard.TotalHitPoints > 0)
                            queue.Add(new BuffCardCommand(priorCard, index, level, level).Execute());
					}
            });
        }
    }

    public partial class HedgehogAbility : Ability
    {
        public HedgehogAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return string.Format("Faint => Deal {0} damage to all.", 2 * card.Level);
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                foreach (var c in card.Deck)
                    // checking TotalHitPoints > 0; see comments in HurtCommand
                    if (c != card && c.TotalHitPoints > 0)
                        queue.Add(new HurtCardCommand(c, 2 * level, card.Deck, index).Execute());
                if (card.Deck.Player.Game.Fighting)
                    foreach (var c in opponent.BattleDeck)
                        // checking TotalHitPoints > 0; see comments in HurtCommand
                        if (c.TotalHitPoints > 0)
                            queue.Add(new HurtCardCommand(c, 2 * level, card.Deck, index).Execute());
            });
        }
    }

    public partial class PeacockAbility : Ability
    {
        public PeacockAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Hurt => Gain 50% of attack points, {card.Level} time(s).";
        }

        public override void Hurted(CardCommandQueue queue, Card card)
        {
            base.Hurted(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {

                // not necessary to check card.TotalHitpoints > 0; see comments in HurtCommand

                // if not about to faint, then buff itself
                //if (card.TotalHitPoints > 0)

                int attackPoints = (int)Math.Round(((double)card.TotalAttackPoints / 2) * level, 
                    // if 0.5 then round up
                    MidpointRounding.AwayFromZero);
                queue.Add(new BuffCardCommand(card, card.Index, 0, attackPoints).Execute());
            });
        }
    }

    public partial class DirtyRatAbility : NoAbility
    {
        public DirtyRatAbility() : base()
        {
            DefaultHP = 1;
            DefaultAttack = 1;
        }
    }

    public partial class RatAbility : Ability
    {
        public RatAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 4;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Faint => Summon {card.Level} dirty rat(s) for the opponent.";
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            if (card.Deck.Player.Game.Fighting)
            {
                PerformTigerAbility(card, index, (level) =>
                {
                    for (int i = 1; i <= level; i++)
                    {
                        var opponent = card.Deck.Player.GetOpponentPlayer();
                        if (CanMakeRoomAt(queue, opponent.BattleDeck, opponent.BattleDeck.Size - i, out int summonIndex))
                            queue.Add(new SummonCardCommand(card, opponent.BattleDeck, summonIndex, 
                                typeof(DirtyRatAbility), 1, 1).Execute());
                    }
                });
            }
        }
    }

    public partial class ZombieBeeAbility : NoAbility
    {
        public ZombieBeeAbility()
        {
            DefaultAttack = 1;
            DefaultHP = 1;
        }
    }

    public partial class ShrimpAbility : Ability
    {
        public ShrimpAbility() : base()
        {
            DefaultHP = 3;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend sold => Give a random friend +{card.Level} health.";
        }

        public override void FriendSold(CardCommandQueue queue, Card card, Card soldCard)
        {
            base.FriendSold(queue, card, soldCard);
            var buffCard = card.Deck.GetRandomCard(new HashSet<int> { card.Index });
            if (buffCard != null)
                queue.Add(new BuffCardCommand(buffCard, card.Index, card.Level, 0).Execute());
        }
    }   

    public partial class SpiderAbility : Ability
    {
        public SpiderAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Fainted => Summon a level {card.Level} tier 3 pet as 2/2.";
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                int randIndex = card.Deck.Player.Game.Random.Next(0, AbilityList.Instance.TierThreeAbilities.Count);
                var ability = AbilityList.Instance.TierThreeAbilities[randIndex];
                // see comments for cricket and sheep
                if (CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                    queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, ability, 2, 2, level).Execute());
            });
        }
    }

    public partial class SwanAbility : Ability
    {
        public SwanAbility() : base()
        {
            DefaultHP = 3;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Start of turn => Gain {card.Level} gold.";
        }

        public override void RoundStarted(Card card)
        {
            base.RoundStarted(card);
            card.Deck.Player.Gold += card.Level;
        }
    }

    // Tier 3
    public partial class BadgerAbility : Ability
    {
        public BadgerAbility() : base()
        {
            DefaultHP = 4;
            DefaultAttack = 5;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Fainted => Deal damage - {card.Level} times attack - to adjacent pets.";
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                int damage = card.TotalAttackPoints * level;
                if (index > 0)
                {
                    //TODO: SAP will ignore a newly summoned bee as a target for attack
                    // same I think below for the nextCard damage
                    var priorCard = card.Deck[index - 1];
                    if (priorCard != null && priorCard.TotalHitPoints > 0)
                        queue.Add(new HurtCardCommand(priorCard, damage, card.Deck, index).Execute());
                }

                if (attacking)
                {
                    // using opponentCard param because if we used card.Deck.LastOrDefault, we might get
                    // a summoned bee instead of the actual opponent we just attacked
                    if (opponentCard != null && opponentCard.TotalHitPoints > 0)
                        queue.Add(new HurtCardCommand(opponentCard, damage, card.Deck, index).Execute());
                }
                else
                {
                    if (index + 1 < card.Deck.Size)
                    {
                        var nextCard = card.Deck[index + 1];
                        if (nextCard != null && nextCard.TotalHitPoints > 0)
                            queue.Add(new HurtCardCommand(nextCard, damage, card.Deck, index).Execute());
                    }
                }
            });
        }
    }

    public partial class BlowfishAbility : Ability
    {
        public BlowfishAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Hurt => Deal {card.Level * 2} damage to a random enemy.";
        }

        public override void Hurted(CardCommandQueue queue, Card card)
        {
            base.Hurted(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                if (opponent.Game.Fighting)
                {
                    var opponentCard = opponent.BattleDeck.GetRandomCard();
                    if (opponentCard != null)
                        queue.Add(new HurtCardCommand(opponentCard, level * 2, card.Deck, card.Index).Execute());
                }
            });
        }
    }

    public partial class CamelAbility : Ability
    {
        public CamelAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Hurt => Give friend behind +{card.Level} attack and +{card.Level * 2} health.";
        }

        public override void Hurted(CardCommandQueue queue, Card card)
        {
            base.Hurted(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                Card priorCard = card.Deck.LastOrDefault(c => c.Index < card.Index && c.TotalHitPoints > 0);
                if (priorCard != null)
                    queue.Add(new BuffCardCommand(priorCard, card.Index, level * 2, level).Execute());
            });
        }
    }

    public partial class DogAbility : Ability
    {
        public DogAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend summoned => Gain +{card.Level} attack or health.";
        }

        public override void FriendSummoned(CardCommandQueue queue, Card card, Card summonedCard)
        {
            base.FriendSummoned(queue, card, summonedCard);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                int hitPoints = 0;
                int attackPoints = 0;
                // 50/50 chance either 0 or 1
                if (card.Deck.Player.Game.Random.Next(0, 2) == 0)
                    hitPoints = level;
                else
                    attackPoints = level;
                queue.Add(new BuffCardCommand(card, card.Index, hitPoints, attackPoints).Execute());
            });
        }
    }

    public partial class GiraffeAbility : Ability
    {
        public GiraffeAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"End of turn => Give {card.Level} friend(s) ahead +1/+1.";
        }    

        public override void RoundEnded(CardCommandQueue queue, Card card)
        {
            base.RoundEnded(queue, card);
            for (int i = 1; i <= card.Level; i++)
            {
                if (i + card.Index >= card.Deck.Size)
                    break;
                // not checking buffCard.TotalHitPoints > 0 because we aren't in a battle
                var buffCard = card.Deck[card.Index + i];
                if (buffCard != null)
                    queue.Add(new BuffCardCommand(buffCard, card.Index, 1, 1).Execute());
            }
        }
    }

    public partial class KangarooAbility : Ability
    {
        public KangarooAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend ahead attacks => Gain +{card.Level * 2} attack and +{card.Level * 2} health.";
        }    

        public override void FriendAheadAttacks(CardCommandQueue queue, Card card)
        {
            base.FriendAheadAttacks(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                queue.Add(new BuffCardCommand(card, card.Index, level * 2, level * 2).Execute());
            });
        }
    }

    public partial class OxAbility : Ability
    {
        public OxAbility() : base()
        {
            DefaultHP = 4;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend ahead faints => Gain melon armor and +{card.Level * 2} attack.";
        }    

        public override void FriendAheadFaints(CardCommandQueue queue, Card card, int faintedIndex)
        {
            base.FriendAheadFaints(queue, card, faintedIndex);
            queue.Add(new GainFoodAbilityCommand(card, new MelonArmorAbility()).Execute());
            PerformTigerAbility(card, card.Index, (level) =>
            {
                queue.Add(new BuffCardCommand(card, card.Index, 0, level * 2).Execute());
            });
        }
    }

    public partial class RabbitAbility : Ability
    {
        public RabbitAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend eats shop food => Give it +{card.Level} health.";
        }

        public override void FriendAteFood(CardCommandQueue queue, Card card, Card friendCard)
        {
            base.FriendAteFood(queue, card, friendCard);
            // friend may have fainted from sleeping pill
            if (friendCard.Index != -1)
                queue.Add(new BuffCardCommand(friendCard, card.Index, card.Level, 0).Execute());
        }
    }

    public partial class SheepAbility : Ability
    {
        public SheepAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Faint => Summon two {card.Level * 2}/{card.Level * 2} rams.";
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                // sheep has fainted but we still have to search for an empty slot because it's possible that
                // another ability method has moved cards into the sheep's spot
                if (CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                    queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, typeof(ZombieRamAbility), 
                        level * 2, level * 2).Execute());
                if (CanMakeRoomAt(queue, card.Deck, index, out int summonIndex2))
                    queue.Add(new SummonCardCommand(card, card.Deck, summonIndex2, typeof(ZombieRamAbility), 
                        level * 2, level * 2).Execute());
            });
        }
    }

    public partial class ZombieRamAbility : NoAbility
    {
        public ZombieRamAbility()
        {
            DefaultAttack = 2;
            DefaultHP = 2;
        }
    }

    public partial class SnailAbility : Ability
    {
        public SnailAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Buy => If you lost last battle, give all friends +{card.Level}/+{card.Level}.";
        }

        public override void Bought(CardCommandQueue queue, Card card)
        {
            base.Bought(queue, card);
            if (card.Deck.Player.LostLastBattle)
                foreach (var c in card.Deck)
                    if (c != card)
                        queue.Add(new BuffCardCommand(c, card.Index, card.Level, card.Level).Execute());
        }
    }

    public partial class TurtleAbility : Ability
    {
        public TurtleAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Faint => Give {card.Level} friend(s) behind melon armor.";
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            for (int i = 1; i <= card.Level; i++)
            {
                if (index - i < 0)
                    break;
                var priorCard = card.Deck[index - i];
                if (priorCard != null)
                    queue.Add(new GainFoodAbilityCommand(priorCard, new MelonArmorAbility()).Execute());
            }
        }
    }


    // Tier 4
    public partial class BisonAbility : Ability
    {
        public override string GetAbilityMessage(Card card)
        {
            return $"End turn => If there's at least one level 3 friend, gain +{card.Level * 2}/+{card.Level * 2})";
        }

        public BisonAbility() : base()
        {
            DefaultHP = 6;
            DefaultAttack = 6;
        }

        public override void RoundEnded(CardCommandQueue queue, Card card)
        {
            base.RoundEnded(queue, card);
            if (card.Deck.Any((c) => c.Level == 3))
                queue.Add(new BuffCardCommand(card, card.Index, card.Level * 2, card.Level * 2).Execute());
        }
    }

    public partial class ZombieBusAbility : NoAbility
    {
        public ZombieBusAbility()
        {
            DefaultHP = 5;
            DefaultAttack = 5;
        }
    }

    public partial class DeerAbility : Ability
    {
        public DeerAbility() : base()
        {
            DefaultHP = 1;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Fainted => Summon a {card.Level * 5}/{card.Level * 5} bus with splash attack.)";
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                if (CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                    queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, typeof(ZombieBusAbility), 
                        level * 5, level * 5, 1, typeof(SplashAttackAbility)).Execute());
            });
        }
    }

    public partial class DolphinAbility : Ability
    {
        public DolphinAbility() : base()
        {
            DefaultHP = 6;
            DefaultAttack = 4;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Start of battle => Deal {card.Level * 5} damage to the lowest health enemy.)";
        }

        public override void BattleStarted2(CardCommandQueue queue, Card card)
        {
            base.BattleStarted2(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                if (opponent.BattleDeck.GetCardCount() > 0)
                {
                    // Tiger behind dolphin will have the dolphin target the same card twice
                    // I presume because the card will be hurt but not faint and end up with negative HP
                    // and get selected again as the lowest health enemy. so to fix this we filter out
                    // all cards with negative health before doing the Aggregate call to find lowest health card
                    var healthyCards = opponent.BattleDeck.Where((c) => c.TotalHitPoints > 0).ToList();
                    if (healthyCards.Count > 0)
                    {
                        var targetCard = healthyCards.Aggregate((minCard, nextCard) => 
                           minCard.TotalHitPoints < nextCard.TotalHitPoints ? minCard : nextCard);
                        queue.Add(new HurtCardCommand(targetCard, level * 5, card.Deck, card.Index).Execute());
                    }
                }
            });
        }
    }

    public partial class HippoAbility : Ability
    {
        public HippoAbility() : base()
        {
            DefaultHP = 7;
            DefaultAttack = 4;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Knockout => Gain +{card.Level * 2}/+{card.Level * 2}.";
        }

        public override void Knockout(CardCommandQueue queue, Card card)
        {
            base.Knockout(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                queue.Add(new BuffCardCommand(card, card.Index, level * 2, level * 2).Execute());
            });
        }
    }

    public partial class ParrotAbility : Ability
    {
        public ParrotAbility() : base()
        {
            DefaultHP = 3;
            DefaultAttack = 5;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"End of turn => Copy ability from friend ahead as level {card.Level} until the end of battle.";
        }

        public override void NewBattleDeck(Card card)
        {
            base.NewBattleDeck(card);
            if (card.Index + 1 < card.Deck.Size)
            {
                var friendCard = card.Deck[card.Index + 1];
                if (friendCard != null)
                    // card.RenderAbility will still be Parrot
                    card.Ability = Activator.CreateInstance(friendCard.Ability.GetType()) as Ability;
            }
        }
    }

    public partial class PenguinAbility : Ability
    {
        public PenguinAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"End of turn => Give other level 2 and 3 friends +{card.Level}/+{card.Level}.";
        }    

        public override void RoundEnded(CardCommandQueue queue, Card card)
        {
            base.RoundEnded(queue, card);
            foreach (var buffCard in card.Deck.Where((c) => c.Level == 2 || c.Level == 3))
            {
                // not checking buffCard.TotalHitPoints > 0 because we aren't in a battle
                queue.Add(new BuffCardCommand(buffCard, card.Index, 1, 1).Execute());
            }
        }
    }

    public partial class ZombieChickAbility : NoAbility
    {
        public ZombieChickAbility()
        {
            DefaultAttack = 1;
            DefaultHP = 1;
        }
    }

    public partial class RoosterAbility : Ability
    {
        public RoosterAbility() : base()
        {
            DefaultHP = 3;
            DefaultAttack = 5;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Faint => Summon {card.Level} chick(s) with 1 health and half of the attack.";
        }    

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                for (int i = 1; i <= level; i++)
                {
                    if (CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                    {
                        // doing integer division, so adding +1 to card.TotalAttackPoints to round up
                        int attackPoints = (card.TotalAttackPoints + 1) / 2;
                        queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, typeof(ZombieChickAbility), 
                            1, attackPoints).Execute());
                    }
                }
            });
        }
    }

    public partial class SkunkAbility : Ability
    {
        public SkunkAbility() : base()
        {
            DefaultHP = 6;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Start of battle => Reduce health of the highest health enemy by {card.Level * 33}%.";
        }

        public override void BattleStarted2(CardCommandQueue queue, Card card)
        {
            base.BattleStarted2(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                if (opponent.BattleDeck.GetCardCount() > 0)
                {
                    var targetCard = opponent.BattleDeck.Aggregate((maxCard, nextCard) => 
                        maxCard.TotalHitPoints > nextCard.TotalHitPoints ? maxCard : nextCard);
                    int damage = (int)Math.Round(targetCard.TotalHitPoints * (((double)card.Level * 33) / 100));
                    if (damage >= targetCard.TotalHitPoints)
                        damage = targetCard.TotalHitPoints - 1;
                    queue.Add(new HurtCardCommand(targetCard, damage, card.Deck, card.Index).Execute());
                }
            });
        }
    }

    public partial class SquirrelAbility : Ability
    {
        public SquirrelAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Start of turn => Discount shop food by {card.Level} gold.";
        }

        public override void RoundStarted(Card card)
        {
            base.RoundStarted(card);
            // shop food could have been frozen from earlier round, so we'd be discounting it a second time
            // but we're ensuring that the cost will never get below zero
            card.Deck.Player.ShopFood1.Cost -= Math.Min(card.Deck.Player.ShopFood1.Cost, card.Level);
            card.Deck.Player.ShopFood2.Cost -= Math.Min(card.Deck.Player.ShopFood2.Cost, card.Level);
        }
    }

    public partial class WhaleAbility : Ability
    {
        Card _friendAhead;

        public WhaleAbility() : base()
        {
            DefaultHP = 6;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Start of battle => Swallow friend ahead and release it as level {card.Level} after fainting.";
        }

        public override void BattleStarted1(CardCommandQueue queue, Card card)
        {
            base.BattleStarted1(queue, card);
            if (card.Index + 1 < card.Deck.Size)
            {
                _friendAhead = card.Deck[card.Index + 1];
                // we faint the card in BattleStarted1 because we don't want other ability methods
                // to target this card within the same queue.
                if (_friendAhead != null && _friendAhead.TotalHitPoints > 0)
                    queue.Add(new FaintCardCommand(_friendAhead, false).Execute());
            }
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            if (_friendAhead != null)
            {
                PerformTigerAbility(card, index, (level) =>
                {
                    if (CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                        queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, _friendAhead.Ability.GetType(), 
                            _friendAhead.TotalHitPoints, _friendAhead.TotalAttackPoints, level, 
                            //TODO: restore food ability on the summoned card?
                            null, 
                            // in case we swallowed a parrot
                            _friendAhead.RenderAbility.GetType()).Execute());
                });
            }
        }
    }

    public partial class WormAbility : Ability
    {
        public WormAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 2;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Eats shop food => Gain +{card.Level}/+{card.Level}.";
        }

        public override void AteFood(CardCommandQueue queue, Card card)
        {
            base.AteFood(queue, card);
            queue.Add(new BuffCardCommand(card, card.Index, card.Level, card.Level).Execute());
        }
    }


    // Tier 5
    public partial class CowAbility : Ability
    {
        public CowAbility() : base()
        {
            DefaultHP = 6;
            DefaultAttack = 4;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Buy => Replace food shop with free milk that gives +{card.Level} attack and +{card.Level * 2} health.";
        }

        public override void Bought(CardCommandQueue queue, Card card)
        {
            base.Bought(queue, card);
            card.Deck.Player.ShopFood1 = new MilkFood() { Cost = 0, AttackPoints = card.Level, HitPoints = card.Level * 2 };
            card.Deck.Player.ShopFood2 = new MilkFood() { Cost = 0, AttackPoints = card.Level, HitPoints = card.Level * 2 };
        }
    }

    public partial class CrocodileAbility : Ability
    {
        public CrocodileAbility() : base()
        {
            DefaultHP = 4;
            DefaultAttack = 8;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Start of battle => Deal {card.Level * 8} damage to the last enemy.";
        }

        public override void BattleStarted2(CardCommandQueue queue, Card card)
        {
            base.BattleStarted2(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                var firstCard = opponent.BattleDeck.FirstOrDefault((c) => c.TotalHitPoints > 0);
                if (firstCard != null)
                    queue.Add(new HurtCardCommand(firstCard, level * 8, card.Deck, card.Index).Execute());
            });
        }
    }

    public partial class MonkeyAbility : Ability
    {
        public MonkeyAbility() : base()
        {
            DefaultHP = 2;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"End of turn => Give the right-most friend +{card.Level * 2} attack and +{card.Level * 3} health.";
        }

        public override void RoundEnded(CardCommandQueue queue, Card card)
        {
            base.RoundEnded(queue, card);
            var buffCard = card.Deck[card.Deck.Size - 1];
            if (buffCard != null)
                queue.Add(new BuffCardCommand(buffCard, card.Index, card.Level * 3, card.Level * 2).Execute());
        }
    }

    public partial class RhinoAbility : Ability
    {
        public RhinoAbility() : base()
        {
            DefaultHP = 8;
            DefaultAttack = 5;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Knockout => Deal {card.Level * 4} damage to the first enemy.";
        }

        public override void Knockout(CardCommandQueue queue, Card card)
        {
            base.Knockout(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                var lastCard = opponent.BattleDeck.LastOrDefault((c) => c.TotalHitPoints > 0);
                if (lastCard != null)
                    queue.Add(new HurtCardCommand(lastCard, level * 4, card.Deck, card.Index).Execute());
            });
        }
    }

    public partial class ScorpionAbility : Ability
    {
        public ScorpionAbility() : base()
        {
            DefaultHP = 1;
            DefaultAttack = 1;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Attacked => Gives a deadly poison attack to the enemy.";
        }

        public override void Attacked(CardCommandQueue queue, Card card, int damage, Card opponentCard = null)
        {
            base.Attacked(queue, card, damage, opponentCard);
            // if opponent had melon armor then it would have set damage to zero
            // and we don't deliver the faint
            if (damage > 0 && opponentCard != null)
                queue.Add(new FaintCardCommand(opponentCard, false).Execute());
        }
    }

    public partial class SealAbility : Ability
    {
        public SealAbility() : base()
        {
            DefaultHP = 8;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Eats shop food => Give 2 random friends +{card.Level}/+{card.Level}.";
        }

        public override void AteFood(CardCommandQueue queue, Card card)
        {
            base.AteFood(queue, card);
            var friendCard = card.Deck.GetRandomCard(new HashSet<int>() { card.Index });
            if (friendCard != null)
            {
                queue.Add(new BuffCardCommand(friendCard, card.Index, card.Level, card.Level).Execute());
                // exclude the friend card in the next search
                friendCard = card.Deck.GetRandomCard(new HashSet<int>() { card.Index, friendCard.Index });
                if (friendCard != null)
                    queue.Add(new BuffCardCommand(friendCard, card.Index, card.Level, card.Level).Execute());
            }
        }
    }

    public partial class SharkAbility : Ability
    {
        public SharkAbility() : base()
        {
            DefaultHP = 4;
            DefaultAttack = 4;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend faints => Gain +{card.Level * 2} attack and +{card.Level} health.";
        }

        public override void FriendFaints(CardCommandQueue queue, Card card, int index)
        {
            base.FriendFaints(queue, card, index);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                queue.Add(new BuffCardCommand(card, card.Index, level, level * 2).Execute());
            });
        }
    }

    public partial class TurkeyAbility : Ability
    {
        public TurkeyAbility() : base()
        {
            DefaultHP = 4;
            DefaultAttack = 3;
        }
        
        public override string GetAbilityMessage(Card card)
        {
            return $"Friend summoned => Give it +{card.Level * 3}/+{card.Level * 3}.";
        }

        public override void FriendSummoned(CardCommandQueue queue, Card card, Card summonedCard)
        {
            base.FriendSummoned(queue, card, summonedCard);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                queue.Add(new BuffCardCommand(summonedCard, card.Index, level * 3, level * 3, 
                    // buff the build points if not fighting
                    !card.Deck.Player.Game.Fighting).Execute());
            });
        }
    }


    // Tier 6
    public partial class BoarAbility : Ability
    {
        public BoarAbility() : base()
        {
            DefaultHP = 6;
            DefaultAttack = 8;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Before attack => Gain +{card.Level * 2}/+{card.Level * 2}.";
        }

        public override void BeforeAttack(CardCommandQueue queue, Card card)
        {
            base.BeforeAttack(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                queue.Add(new BuffCardCommand(card, card.Index, level * 2, level * 2).Execute());
            });
        }
    }

    public partial class CatAbility : Ability
    {
        public CatAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 4;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Multiply the attack and health of effects of food by {card.Level + 1}.";
        }

        public override void Eating(Card card, Card eatingCard, ref int hitPoints, ref int attackPoints)
        {
            base.Eating(card, eatingCard, ref hitPoints, ref attackPoints);
            hitPoints *= card.Level + 1;
            attackPoints *= card.Level + 1;
        }
    }

    public partial class DragonAbility : Ability
    {
        public DragonAbility() : base()
        {
            DefaultHP = 8;
            DefaultAttack = 6;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Buy tier 1 pet => Give all friends +{card.Level}/+{card.Level}.";
        }

        public override void FriendBought(CardCommandQueue queue, Card card, Card friendCard)
        {
            base.FriendBought(queue, card, friendCard);
            if (AbilityList.Instance.TierOneAbilities.Any((ability) => ability == friendCard.Ability.GetType()))
                foreach (var c in card.Deck)
                    if (c != card)
                        queue.Add(new BuffCardCommand(c, card.Index, card.Level, card.Level).Execute());
        }
    }

    public partial class ZombieFlyAbility : NoAbility
    {
        public ZombieFlyAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 5;
        }
    }

    public partial class FlyAbility : Ability
    {
        int _summonCount;

        public FlyAbility() : base()
        {
            DefaultHP = 5;
            DefaultAttack = 5;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend faints => Summon a {card.Level * 5}/{card.Level * 5} fly in its place, three times per battle.";
        }

        public override void BattleStarted1(CardCommandQueue queue, Card card)
        {
            base.BattleStarted1(queue, card);
            _summonCount = 0;
        }

        public override void FriendFaints(CardCommandQueue queue, Card card, int index)
        {
            base.FriendFaints(queue, card, index);
            if (_summonCount < 3)
            {
                // six flies
                PerformTigerAbility(card, card.Index, (level) =>
                {
                    if (CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                    {
                        queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, typeof(ZombieFlyAbility), 
                            level * 5, level * 5).Execute());
                        _summonCount++;
                    }
                });
            }
        }
    }

    public partial class GorillaAbility : Ability
    {
        int _shieldCount;

        public GorillaAbility() : base()
        {
            DefaultHP = 9;
            DefaultAttack = 6;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Hurt => Gain a Coconut Shield, {card.Level} time(s) per battle.";
        }

        public override void BattleStarted1(CardCommandQueue queue, Card card)
        {
            base.BattleStarted1(queue, card);
            _shieldCount = 0;
        }

        public override void Hurted(CardCommandQueue queue, Card card)
        {
            base.Hurted(queue, card);
            //TODO: tiger do anything? let them invoke shield twice as much?
            if (_shieldCount < card.Level)
            {
                queue.Add(new GainFoodAbilityCommand(card, new CoconutShieldAbility()).Execute());
                _shieldCount++;
            }
        }
    }

    public partial class LeopardAbility : Ability
    {
        public LeopardAbility() : base()
        {
            DefaultHP = 4;
            DefaultAttack = 10;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Start of battle => Deal 50% of its attack damage to {card.Level} random enemy(s).";
        }

        public override void BattleStarted2(CardCommandQueue queue, Card card)
        {
            base.BattleStarted2(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                // integer division rounds up
                int damage = (card.AttackPoints + 1) / 2;
                var hashSet = new HashSet<int>();
                for (int i = 1; i <= level; i++)
                {
                    var enemyCard = opponent.BattleDeck.GetRandomCard(hashSet);
                    if (enemyCard != null)
                    {
                        queue.Add(new HurtCardCommand(enemyCard, damage, card.Deck, card.Index).Execute());
                        hashSet.Add(enemyCard.Index);
                    }
                }
            });
        }
    }

    public partial class MammothAbility : Ability
    {
        public MammothAbility() : base()
        {
            DefaultHP = 10;
            DefaultAttack = 3;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Faint => Give all friends +{card.Level * 2}/+{card.Level * 2}.";
        }

        public override void Fainted(CardCommandQueue queue, Card card, int index, bool attacking, Card opponentCard = null)
        {
            base.Fainted(queue, card, index, attacking, opponentCard);
            PerformTigerAbility(card, index, (level) =>
            {
                foreach (var friendCard in card.Deck)
                    if (friendCard.TotalHitPoints > 0)
                        queue.Add(new BuffCardCommand(friendCard, index, level * 2, level * 2).Execute());
            });
        }
    }

    public partial class SnakeAbility : Ability
    {
        public SnakeAbility() : base()
        {
            DefaultHP = 6;
            DefaultAttack = 6;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend ahead attacks => Deal {card.Level * 3} damage to a random enemy.";
        }

        public override void FriendAheadAttacks(CardCommandQueue queue, Card card)
        {
            base.FriendAheadAttacks(queue, card);
            PerformTigerAbility(card, card.Index, (level) =>
            {
                var opponent = card.Deck.Player.GetOpponentPlayer();
                var enemyCard = opponent.BattleDeck.GetRandomCard();
                if (enemyCard != null)
                    queue.Add(new HurtCardCommand(enemyCard, level * 3, card.Deck, card.Index).Execute());
            });
        }
    }

    public partial class TigerAbility : Ability
    {
        public TigerAbility() : base()
        {
            DefaultHP = 3;
            DefaultAttack = 4;
        }

        public override string GetAbilityMessage(Card card)
        {
            return $"Friend ahead repeats their ability in battle as if they are level {card.Level}.";
        }
    }
}

