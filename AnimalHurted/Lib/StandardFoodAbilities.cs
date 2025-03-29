using System;
using System.Diagnostics;
using System.Linq;

namespace AnimalHurtedLib
{
    public partial class HoneyBeeAbility : FoodAbility
    {
        public override void Fainted(CardCommandQueue queue, Card card, int index)
        {
            base.Fainted(queue, card, index);
            if (Ability.CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, 
                    typeof(ZombieBeeAbility), 1, 1).Execute());
        }
    }

    public partial class BoneAttackAbility : FoodAbility
    {
        public override void CalculatingDamage(Card card, ref int damage)
        {
            base.CalculatingDamage(card, ref damage);
            damage += 5;
        }
    }

    public partial class GarlicArmorAbility : FoodAbility
    {
        public override void Hurting(Card card, ref int damage)
        {
            base.Hurting(card, ref damage);
            damage = Math.Max(1, damage - 2);
        }
    }

    public partial class MelonArmorAbility : FoodAbility
    {
        public override void Hurting(Card card, ref int damage)
        {
            base.Hurting(card, ref damage);
            damage = Math.Max(0, damage - 20);
            card.FoodAbility = null; // remove the melon armor after first damage
        }
    }

    public partial class SplashAttackAbility : FoodAbility
    {
        public override void Attacking(CardCommandQueue queue, Card card, Card opponentCard = null)
        {
            base.Attacking(queue, card);
            var opponent = card.Deck.Player.GetOpponentPlayer();
            Card targetCard = null;
            // opponentCard may have fainted from the attack
            if (opponentCard == null)
                targetCard = opponent.BattleDeck.GetLastCard();
            else if (opponentCard.Index > 0)
                targetCard = opponent.BattleDeck[opponentCard.Index - 1];
            if (targetCard != null && targetCard.TotalHitPoints > 0)
                queue.Add(new HurtCardCommand(targetCard, 5, card.Deck, card.Index).Execute());
        }
    }

    public partial class CoconutShieldAbility : FoodAbility
    {
        public override void Hurting(Card card, ref int damage)
        {
            base.Hurting(card, ref damage);
            damage = 0;
            // remove the armor after first damage
            // gorilla will re-attach shield ability up to 3 times
            card.FoodAbility = null;
       }       
    }

    public partial class SteakAttackAbility : FoodAbility
    {
        public override void CalculatingDamage(Card card, ref int damage)
        {
            base.CalculatingDamage(card, ref damage);
            damage += 20;
            card.FoodAbility = null;
        }
    }

    public partial class ExtraLifeAbility : FoodAbility
    {
        public override void Fainted(CardCommandQueue queue, Card card, int index)
        {
            base.Fainted(queue, card, index);
            if (Ability.CanMakeRoomAt(queue, card.Deck, index, out int summonIndex))
                queue.Add(new SummonCardCommand(card, card.Deck, summonIndex, card.Ability.GetType(), 1, 1, card.Level).Execute());
        }
    }
}