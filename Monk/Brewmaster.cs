namespace Hydra.UserRotations
{
    using System;
    using Shadowbuddy.Common;
    using Shadowbuddy.TreeSharp;
    using Shadowbuddy.WoW.Resources.Enums;
    using Shadowbuddy.WoW.Internals;
    using Helpers;
    using Managers;
    using Shadowbuddy.WoW.ObjectManager;
    using Misc;

    /// <summary>
    /// The base class for building dynamic rotations.
    /// </summary>
    internal class Brewmaster : UserRotation
    {
        public override WoWSpec Specialization => WoWSpec.MonkBrewmaster;

        private static bool hotkeyInit = false;
        private static TimeSpan TIME_TEN_SECS = new TimeSpan(0, 0, 10);
        private static TimeSpan TIME_THREE_SECS = new TimeSpan(0, 0, 3);
        private static TimeSpan TIME_ONE_SECS = new TimeSpan(0, 0, 0, 1, 300);
        private static DateTime useExplodingKeg = DateTime.Now.Subtract(TIME_TEN_SECS);

        private static int BLACKOUT_COMBO_AURA = 228563;

        private static void registerHotkeys()
        {
            HotkeysManager.Register(
                "ExplodingKeg",
                Shadowbuddy.MemoryManagement.Native.Keys.F5,
                Shadowbuddy.MemoryManagement.Native.ModifierKeys.None,
                hotkey =>
                {
                    useExplodingKeg = DateTime.Now.AddSeconds(10);
                    Log.Hotkey("Hotkey: Exploding Keg Enabled");
                    OverlayManager.HotkeyEnabled("Exploding Keg Enabled!");
                });
            hotkeyInit = true;
        }

        public Brewmaster()
        {
            registerHotkeys();
        }


        protected override Composite HandleRotation()
        {
            return new PrioritySelector(
                new Decorator(
                ret => useExplodingKeg > DateTime.Now && Spells.ExplodingKeg.CanCast(),
                Spells.ExplodingKeg.CastLocation(on => Target)),
                HandleBlackoutCombo(),
                Defensive(),
                SingleTarget()
                );
        }

        private static Composite HandleBlackoutCombo()
        {
            return new PrioritySelector(
                 Spells.KegSmash.Cast(
                    on => Target,
                    ret => Player.HasAura(BLACKOUT_COMBO_AURA)),
                Spells.BreathOfFire.Cast(
                    on => Target,
                    ret => Player.HasAura(BLACKOUT_COMBO_AURA) && Target.Distance < 8 )
                );
        }

        private static Composite Defensive()
        {
            return new PrioritySelector(
                Spells.PurifyingBrew.Cast(
                    ret => Player.HasAura("Heavy Stagger")),
                Spells.PurifyingBrew.Cast(
                    ret => Player.HealthPercentage <= 50 && Manager.Me.HasAura("Moderate Stagger")),
                Spells.ExpelHarm.Cast(
                    ret => Player.HealthPercentage <= 70),
                Spells.IronskinBrew.Cast(
                    ret => Player.HealthPercentage <= 70 && !Player.HasAura("Ironskin Brew") && getBrewCharge() <= 2),
                Spells.IronskinBrew.Cast(
                    ret => Player.HealthPercentage < 90 && getBrewCharge() == 3)
                );
        }

        private static Composite SingleTarget()
        {
            return new PrioritySelector(
                Spells.KegSmash.Cast(
                    on => Target,
                    ret => getBrewCharge() == 3),
                Spells.BlackoutStrike.Cast(
                    on => Target,
                    ret => shouldUseBlackOutStrike()),
                Spells.TigerPalm.Cast(
                    on => Target,
                    ret => Player.EnergyPercent > 65 ),
                Spells.RushingJadeWind.Cast()
                );
        }

        private static int getBrewCharge()
        {
            return getSpellCharge(115308);
        }

        private static int getSpellCharge(int spellId)
        {
            WoWSpell spell = WoWSpell.FromId(spellId);
            return spell.MaxCharges - spell.ChargesUsed;
        }

        private static bool shouldUseBlackOutStrike()
        {
            if( !Spells.KegSmash.IsOnCooldown || !Spells.BreathOfFire.IsOnCooldown )
            {
                return true;
            }
            
            if( Spells.KegSmash.CooldownTimeLeft < TIME_THREE_SECS && 
                Spells.KegSmash.CooldownTimeLeft > TIME_ONE_SECS)
            {
                return false;
            }

            if (Spells.BreathOfFire.CooldownTimeLeft < TIME_THREE_SECS && 
                Spells.BreathOfFire.CooldownTimeLeft > TIME_ONE_SECS)
            {
                return false;
            }

            return true;
        }

        private static class Spells
        {
            public static readonly Spell ExpelHarm = new Spell(115072, SpellFlag.IsHelpful),
                                         IronskinBrew = new Spell(115308, SpellFlag.IsHelpful),
                                         KegSmash = new Spell(121253),
                                         BlackoutStrike = new Spell(205523),
                                         TigerPalm = new Spell(100780),
                                         RushingJadeWind = new Spell(116847),
                                         BreathOfFire = new Spell(115181),
                                         PurifyingBrew = new Spell(119582, SpellFlag.IsHelpful),
                                         HealingElixir = new Spell(122280, SpellFlag.IsHelpful),
                                         ExplodingKeg = new Spell(214326, SpellFlag.IsAreaTargeted);
        }
    }
}
