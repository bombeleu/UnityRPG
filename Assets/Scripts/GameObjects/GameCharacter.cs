﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SimpleRPG2
{
    public class GameCharacter
    {
        public string name { get; set; }
        public char displayChar { get; set; }
        public CharacterType type { get; set; }
        public int x { get; set; }
        public int y { get; set; }

        public string characterSpritesheetName { get; set; }
        public int characterSpriteIndex { get; set; }

        public string portraitSpritesheetName { get; set; }
        public int portraitSpriteIndex { get; set; }

        private int _ac;
        public int ac
        {
            get { return _ac + CoreHelper.getArmorAmount(equippedArmor) + CoreHelper.getEffectAmount(new Random(), activeEffects, passiveEffects, StatType.Armor); }
            set { _ac = value; }
        }

        private int _totalHP;
        public int totalHP
        {
            get
            {
                return _totalHP + CoreHelper.getEffectAmount(new Random(), activeEffects, passiveEffects, StatType.HitPoints);
            }
            set
            {
                _totalHP = value;
            }
        }

        public int hp { get; set; }

        private int _attack;
        public int attack { get { return _attack + CoreHelper.getEffectAmount(new Random(), activeEffects, passiveEffects, StatType.Attack); } set { _attack = value; } }

       
        public int ap {get;set;}

        private int _totalAP ;
        public int totalAP { get { return _totalAP + CoreHelper.getEffectAmount(new Random(), activeEffects, passiveEffects, StatType.ActionPoints); } set { _totalAP = value; } }

        public List<Item> inventory { get; set; }
        public List<Armor> equippedArmor { get; set; }
        public Weapon weapon { get; set; }

        public ItemSet Ammo { get; set; }


        public List<ActiveEffect> activeEffects { get; set; }
        public List<PassiveEffect> passiveEffects { get; set; }

        public List<Ability> abilityList { get; set; }

        public GameCharacter()
        {
            inventory = new List<Item>();
            equippedArmor = new List<Armor>();
            activeEffects = new List<ActiveEffect>();
            passiveEffects = new List<PassiveEffect>();
            abilityList = new List<Ability>();
        }

        public bool CheckAP(int ap)
        {
            if (this.ap >= ap)
            {
                return true;
            }
            return false;
        }

        public bool SpendAP(int ap)
        {
            if (this.ap >= ap)
            {
                this.ap -= ap;
                return true;
            }
            return false;
        }

        public void ResetAP()
        {
            this.ap = totalAP;
        }

        public void AddActiveEffect(ActiveEffect a, BattleGame game)
        {
            ActivateEffect(a, game);

            a.duration--;
            if (a.duration > 0)
            {
                game.battleLog.AddEntry(string.Format("{0} was added to {1}.", a.name, this.name));

                activeEffects.Add(a);
            }
        }

        public void AddPassiveEffect(PassiveEffect pe)
        {
            passiveEffects.Add(pe);
        }

        public void RemovePassiveEffect(PassiveEffect pe)
        {
            if (passiveEffects.Contains(pe)) { passiveEffects.Remove(pe); }
        }

        public void RemoveTopActiveEffects(int num)
        {
            if (num > activeEffects.Count)
            {
                num = activeEffects.Count;
            }
            activeEffects.RemoveRange(0, num);
        }

        //Occurs once per turn
        public void RunActiveEffects(BattleGame game)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ActivateEffect(activeEffects[i], game);

                activeEffects[i].duration--;
                if (activeEffects[i].duration <= 0)
                {
                    game.battleLog.AddEntry(string.Format("{0} expired on {1}.", activeEffects[i].name, this.name));

                    activeEffects.RemoveAt(i);
                }
            }
        }

        private void ActivateEffect(ActiveEffect effect, BattleGame game)
        {
            int amt = 0;
            switch (effect.statType)
            {
                case StatType.Damage:
                     amt = game.r.Next(effect.minAmount, effect.maxAmount);

                    game.battleLog.AddEntry(string.Format("{0} was damaged by {1} for {2}", this.name, effect.name, amt.ToString()));

                    this.Damage(amt, game);
                    break;
                case StatType.Heal:
                     amt = game.r.Next(effect.minAmount, effect.maxAmount);
                     game.battleLog.AddEntry(string.Format("{0} was healed by {1} for {2}", this.name, effect.name, amt.ToString()));

                    this.Heal(amt, game);
                    break;
                case StatType.Dispell:

                    amt = game.r.Next(effect.minAmount, effect.maxAmount);
                    game.battleLog.AddEntry(string.Format("{0} removed {1} effects from {2}", effect.name, amt, this.name));

                    RemoveTopActiveEffects(amt);
                    break;
                default:
                    break;
            }
        }

        public void Damage(int amount, BattleGame game)
        {
            this.hp -= amount;
            if (this.hp <= 0)
            {
                Kill(game);
            }
        }

        public void Heal(int amount, BattleGame game)
        {
            this.hp += amount;
            if (this.hp > this.totalHP)
            {
                this.hp = this.totalHP;
            }

            game.battleLog.AddEntry(string.Format("{0} was healed for {1}", this.name, amount));

        }

        public void Kill(BattleGame game)
        {
            game.battleLog.AddEntry(string.Format("{0} was killed.", this.name));

            game.CharacterKill(this);
        }

        public Item getInventoryItembyItemID(long itemID)
        {
            var item = (from data in inventory
                        where data.ID == itemID
                        select data).FirstOrDefault();
            return item;
        }


        public void EquipWeapon(Weapon w)
        {
            if (inventory.Contains(w))
            {
                inventory.Remove(w);
                this.weapon = w;

                

                if (w.passiveEffects != null)
                {
                    foreach (var pe in w.passiveEffects)
                    {
                        AddPassiveEffect(pe);
                    }
                }
            }
        }


        public void RemoveWeapon(Weapon w)
        {
            if (w != null)
            {
                if (weapon == w)
                {
                    weapon = null;

                    inventory.Add(w);

                    if (w.passiveEffects != null)
                    {
                        foreach (var pe in w.passiveEffects)
                        {
                            RemovePassiveEffect(pe);
                        }
                    }
                }
            }
        }

        public void EquipArmor(Armor a)
        {

            if (inventory.Contains(a))
            {
                if (equippedArmor.FindAll(x => x.armorType == a.armorType).Count == 0)
                {
                    inventory.Remove(a);
                    equippedArmor.Add(a);
                    if (a.passiveEffects != null)
                    {
                        foreach (var pe in a.passiveEffects)
                        {
                            AddPassiveEffect(pe);
                        }
                    }

                }
            }
        }

        public void RemoveArmorInSlot(ArmorType type)
        {
            var armor = (from data in equippedArmor
                         where data.armorType == type
                         select data).FirstOrDefault();

            if (armor != null)
            {
                RemoveArmor(armor);
            }
        }

        public Armor getArmorInSlot(ArmorType type)
        {
            var armor = (from data in equippedArmor
                         where data.armorType == type
                         select data).FirstOrDefault();

            return armor;
        }


        public void RemoveArmor(Armor a)
        {
            if (equippedArmor.Contains(a))
            {
                equippedArmor.Remove(a);
                inventory.Add(a);
                if (a.passiveEffects != null)
                {
                    foreach (var pe in a.passiveEffects)
                    {
                        RemovePassiveEffect(pe);
                    }
                }

            }
        }

        public void EquipAmmo(Ammo a)
        {
            if (inventory.Contains(a))
            {
                this.Ammo = ItemHelper.getItemSet(inventory, a);
            }
        }

        public void RemoveAmmo()
        {
            this.Ammo = null;
        }


        public override string ToString()
        {
            string retval = "";
            //string retval = name + "\n";
            retval += string.Format("AC: {0} HP: {1}/{2} Atk: {3} AP: {4}/{5}\n", ac, hp, totalHP, attack, ap, totalAP);

            if(weapon != null)
            { 
                retval += weapon.ToString() + "\n";
            }

            foreach (var e in equippedArmor)
            {
                retval += e.ToString() + "\n";
            }

            foreach (var ae in activeEffects)
            {
                retval += ae.ToString() + "\n";
            }

            foreach (var pe in passiveEffects)
            {
                retval += pe.ToString() + "\n";
            }

            return retval;
        }
    }


    public class EnemyCharacter : GameCharacter
    {
        public EnemyType enemyType { get; set; }
        public AIActor aiActor { get; set; }

        public EnemyCharacter()
        {
            aiActor = new AIActor(this, enemyType);
        }
    }

}
