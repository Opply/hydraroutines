namespace Hydra.UserRotations
{
    using System;
    using Shadowbuddy.CommonBehaviors.Actions;
    using Shadowbuddy.TreeSharp;
    using Shadowbuddy.WoW.Resources.Enums;
    using Helpers;
    using Managers;

    /// <summary>
    /// The base class for building dynamic rotations.
    /// </summary>
    internal class Feral : UserRotation
    {
        public override WoWSpec Specialization => WoWSpec.DruidFeral;

        public static TimeSpan TEN_SECS = new TimeSpan(0, 0, 10);
        public static TimeSpan ONE_SECS = new TimeSpan(0, 0, 1);

        protected override Composite HandleNGCD()
        {
            return new PrioritySelector(
                Spells.TigersFury.Cast(
                    ret => Target != null &&
                           Target.Health > 200000 &&
                           Player.EnergyPercent < 50 &&
                           (Spells.AshamanesFrenzy.CooldownTimeLeft.TotalSeconds < 0.01 ||
                           Spells.AshamanesFrenzy.CooldownTimeLeft.TotalSeconds > 10.0)));
        }

        protected override Composite HandleRotation()
        {
            return new PrioritySelector(
                Finisher(),
                AoE(),
                SingleTarget()
                );
        }

        private static Composite Finisher()
        {
            return new Decorator(
                ret => Player.ComboPoints == 5,
                new PrioritySelector(
                    Spells.Regrowth.Cast(
                        ret => Talents.Bloodtalons.IsTaken && Player.HasAura(69369)),
                    Spells.SavageRoar.Cast(
                        ret => !Player.HasAura("Savage Roar") || (Player.GetAuraByName("Savage Roar").TimeLeft < TEN_SECS && Player.EnergyPercent > 95)),
                    new Decorator(
                        ret => Player.GetAuraByName("Savage Roar").TimeLeft < TEN_SECS && Player.EnergyPercent < 90,
                        new ActionAlwaysSucceed()),
                    Spells.Rip.Cast(
                        on => Target,
                        ret => !Target.HasAura("Rip") || (Target.GetAuraByName("Rip").TimeLeft < TEN_SECS && Player.EnergyPercent > 90)),
                    new Decorator(
                        ret => Target.GetAuraByName("Rip").TimeLeft < TEN_SECS && Player.EnergyPercent < 90,
                        new ActionAlwaysSucceed()),
                    new Decorator(
                        ret => Player.EnergyPercent < 90,
                        new ActionAlwaysSucceed()),
                    Spells.FerociousBite.Cast(
                        on => Target,
                        ret => Player.EnergyPercent >= 90)));
        }

        private static Composite AoE()
        {
            return new PrioritySelector(
                Spells.Rake.Cast(
                    on => Target,
                    ret => !Target.HasMyDebuff("Rake")),
                Spells.AshamanesFrenzy.Cast(
                    on => Target),
                Spells.Trash.Cast(
                    on => Target,
                    ret => !Target.HasMyDebuff("Thrash") && TargetManager.GetEnemiesInRadius(8.0).Count > 2),
                Spells.BrutalSlash.Cast(
                    on => Target,
                    ret => Talents.BrutalSlash.IsTaken && TargetManager.GetEnemiesInRadius(8.0).Count > 2),
                Spells.Swipe.Cast(
                    on => Target,
                    ret => !Talents.BrutalSlash.IsTaken && TargetManager.GetEnemiesInRadius(8.0).Count > 9));
        }

        private static Composite SingleTarget()
        {
            return new PrioritySelector(
                Spells.Rake.Cast(
                    on => Target,
                    ret => !Target.HasMyDebuff("Rake")),
                Spells.AshamanesFrenzy.Cast(
                    on => Target),
                Spells.Trash.Cast(
                    on => Target,
                    ret => !Target.HasMyDebuff("Thrash")),
                Spells.BrutalSlash.Cast(
                   on => Target,
                   ret => Talents.BrutalSlash.IsTaken && Spells.BrutalSlash.ChargesUsed == 0),
                Spells.Shred.Cast(
                    on => Target));

        }

        private static class Spells
        {
            public static readonly Spell SavageRoar = new Spell(52610),
                                                        Rip = new Spell(1079),
                                                        FerociousBite = new Spell(231056),
                                                        Rake = new Spell(1822),
                                                        TigersFury = new Spell(5217),
                                                        AshamanesFrenzy = new Spell(210722),
                                                        Shred = new Spell(5221),
                                                        BrutalSlash = new Spell(202028),
                                                        Swipe = new Spell(106785),
                                                        Trash = new Spell(106830),
                                                        Regrowth = new Spell(8936);
        }

        private static class Talents
        {
            public static readonly Talent
                Bloodtalons = new Talent(6, 1),
                BrutalSlash = new Talent(6, 2);
        }
    }
}
