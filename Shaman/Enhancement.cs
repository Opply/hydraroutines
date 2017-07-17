namespace Hydra.UserRotations
{
    using System;
    using Shadowbuddy.Common;
    using Shadowbuddy.CommonBehaviors.Actions;
    using Shadowbuddy.TreeSharp;
    using Shadowbuddy.WoW.Resources.Enums;
    using Helpers;
    using Managers;

    /// <summary>
    /// The base class for building dynamic rotations.
    /// </summary>
    internal class Enhancement : UserRotation
    {
        public override WoWSpec Specialization => WoWSpec.ShamanEnhancement;

        private static bool questSelfHeal = false;

        private static void registerHotkeys()
        {
            HotkeysManager.Register(
                "EnchSelfHeal",
                Shadowbuddy.MemoryManagement.Native.Keys.F6,
                Shadowbuddy.MemoryManagement.Native.ModifierKeys.None,
                hotkey =>
                {
                    questSelfHeal = !questSelfHeal;
                    Log.Hotkey("Quest Self Heal {0}", questSelfHeal ? "Enabled" : "Disabled");
                    if (questSelfHeal)
                    {
                        OverlayManager.HotkeyEnabled("Quest Self Heal Enabled!");
                    }
                    else
                    {
                        OverlayManager.HotkeyDisabled("Quest Self Heal Disabled!");
                    }
                });
        }

        public Enhancement()
        {
            registerHotkeys();
        }

        protected override Composite HandleRotation()
        {
            return new PrioritySelector(
                SingleTarget()
                );
        }

        private static Composite SingleTarget()
        {
            return new PrioritySelector(
                Spells.Rockbiter.Cast(
                        on => Target,
                        ret => !Player.HasAura(Auras.LandSlide) ),
                //Spells.FuryOfAir.Cast(
                //        ret => !Player.HasAura("Fury of Air"))
                Spells.EnhHealingSurge.Cast(
                    ret => questSelfHeal && Player.HealthPercentage < 70 && Player.CurrentMaelstrom > 30 ),
                Spells.CrashLightning.Cast(
                        on => Target),
                Spells.Flametongue.Cast(
                        on => Target,
                        ret => !Player.HasAura(Auras.Flametongue)),
                Spells.Stormstrike.Cast(
                        on => Target,
                        ret => Player.HasAura(Auras.Stormbringer)),
                Spells.Windsong.Cast(),
                Spells.DoomWinds.Cast(
                        on => Target),
                Spells.LavaLash.Cast(
                        on => Target,
                        ret => Player.HasAura(Auras.HotHand)),
                Spells.Stormstrike.Cast(
                        on => Target),
                Spells.Rockbiter.Cast(
                        on => Target,
                        ret => Player.CurrentMaelstrom < 100 ),
                Spells.LavaLash.Cast(
                        on => Target,
                        ret => Player.CurrentMaelstrom > 100)
                );
        }

        private static class Spells
        {
            public static readonly Spell Rockbiter = new Spell(193786),
                                        CrashLightning = new Spell(187874),
                                        DoomWinds = new Spell(204945),
                                        FeralLunge = new Spell(196884),
                                        FeralSpirit = new Spell(51533),
                                        Flametongue = new Spell(193796),
                                        FrostBrand = new Spell(196834),
                                        EnhHealingSurge = new Spell(188070),
                                        LavaLash = new Spell(60103),
                                        EnhLightningBolt = new Spell(187837),
                                        SpiritWalk = new Spell(58875),
                                        Stormstrike = new Spell(17364),
                                        Windsong = new Spell(201898),
                                        EarthenSpike = new Spell(188089),
                                        Sundering = new Spell(197214),
                                        FuryOfAir = new Spell(197211),
                                        EnhRainFall = new Spell(215864),
                                        AstralShift = new Spell(108271),
                                        WindShear = new Spell(57994),
                                        EnhAscendance = new Spell(114051);

        }

        private static class Auras
        {
            public static readonly int Flametongue = 194084,
                                       Stormbringer = 201846,
                                       HotHand = 215785,
                                       LandSlide = 202004;



        }

        private static class Talents
        {
            public static readonly Talent
                Bloodtalons = new Talent(6, 1),
                BrutalSlash = new Talent(6, 2);
        }
    }
}
