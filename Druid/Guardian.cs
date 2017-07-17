namespace Hydra.UserRotations
{
    using System;
    using Shadowbuddy.CommonBehaviors.Actions;
    using Shadowbuddy.TreeSharp;
    using Shadowbuddy.WoW.Resources.Enums;
    using Helpers;
    using Managers;
    using System.Diagnostics;

    /// <summary>
    /// The base class for building dynamic rotations.
    /// </summary>
    internal class Guardian : UserRotation
    {
        public override WoWSpec Specialization => WoWSpec.DruidGuardian;

        public static TimeSpan TEN_SECS = new TimeSpan(0, 0, 10);
        public static TimeSpan ONE_SECS = new TimeSpan(0, 0, 1);

        private const int GALACTIC_GUARDIAN = 213708;

        protected override Composite HandleRotation()
        {
            return new PrioritySelector(
                Defensive(),
                SingleTarget()
                );
        }

        private static Composite Defensive()
        {
            return new PrioritySelector(
                Spells.Ironfur.Cast(
                    on => Target,
                    ret => Player.RagePercent > 90),
                Spells.FrenziedRegeneration.Cast(
                    ret => Player.HealthPercentage < 80 && !Player.HasBuff("Frenzied Regeneration")),
                Spells.Barkskin.Cast(
                    ret => Player.HealthPercentage < 90),
                Spells.RageOfTheSleeper.Cast(
                    on => Target,
                    ret => Player.HealthPercentage < 60 ),
                Spells.SurvivalInstincts.Cast(
                    ret => Player.HealthPercentage < 40 && !Player.HasBuff("Survival Instincts"))
                    );
        }

        private static Composite SingleTarget()
        {
            return new PrioritySelector(
                Spells.Moonfire.Cast(
                    on => Target,
                    ret => Player.HasAura(GALACTIC_GUARDIAN)),
                Spells.Thrash.Cast(
                    on => Target),
                Spells.Mangle.Cast(
                    on => Target),
                Spells.Pulverize.Cast(
                    on => Target,
                    ret => Target.HasAura("Thrash") && Target.GetAuraStacks("Thrash") >= 2),
                Spells.Moonfire.Cast(
                    on => Target,
                    ret => !Target.HasAura("Moonfire")),
                Spells.Maul.Cast(
                    on => Target,
                    ret => Player.RagePercent > 80),
                Spells.Swipe.Cast(
                    on => Target)
                    );

        }

        private static class Spells
        {
            public static readonly Spell FrenziedRegeneration = new Spell(22842),
                                                        Ironfur = new Spell(192081),
                                                        Moonfire = new Spell(8921),
                                                        Mangle = new Spell(33917),
                                                        Thrash = new Spell(77758),
                                                        Pulverize = new Spell(80313),
                                                        Swipe = new Spell(213764),
                                                        Maul = new Spell(6807),
                                                        RageOfTheSleeper = new Spell(200851),
                                                        Barkskin = new Spell(22812),
                                                        SurvivalInstincts = new Spell(61336);
        }
    }
}
