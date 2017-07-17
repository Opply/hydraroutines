namespace Hydra.UserRotations
{
    using System;
    using System.Linq;
    using System.Numerics;
    using System.Collections.Generic;
    using Helpers;
    using Managers;
    using Shadowbuddy.TreeSharp;
    using Shadowbuddy.WoW.ObjectManager;
    using Shadowbuddy.WoW.Resources.Enums;
    using Misc;
    using Shadowbuddy.WoW.Internals;

    /// <summary>
    /// The base class for building dynamic rotations.
    /// </summary>
    internal class Restoration : UserRotation
    {
        public override WoWSpec Specialization => WoWSpec.ShamanRestoration;

        private static bool useRainOnSelf = false;

        static DateTime positionCalculated = DateTime.Now;
        static TimeSpan twoSecs = new TimeSpan(0,0,2);
        static Vector3 lastCalculatedPosition = new Vector3();

        private static void registerHotkeys()
        {
            /*Shadowbuddy.Common.HotkeysManager.Register(
                "RestoDebug",
                Shadowbuddy.MemoryManagement.Native.Keys.F8,
                Shadowbuddy.MemoryManagement.Native.ModifierKeys.None,
                hotkey =>
                {
                    WoWPlayer la = LowestAlly;
                    if( la == null )
                    {
                        Log.Debug("LowestAlly == null");
                        return;
                    }

                    Log.Debug("LowestAlly Exist  - {0}", la.Exists());
                    Log.Debug("LowestAlly Name  - {0}", la.Name);
                    Log.Debug("LowestAlly HP  - {0}", la.HealthPercentage);

                    //TargetManager.TargetUnit(la);
                });*/
        }

        public Restoration()
        {
            registerHotkeys();
        }

        protected override Composite HandleRest()
        {
            return new PrioritySelector(
                new Decorator( ret => useRainOnSelf,
                Spells.HealingRain.CastLocationVec(
                        Player.Position)),
                HandleRotation()
                );
        }

        protected override Composite HandleRotation()
        {
            return new PrioritySelector(
                //Spells.PurifySpirit.Cast( on => DispelTarget, ret => DispelTarget.Exists() && DispelTarget.IsAlive && (DispelTarget.HasAuraWithDispelType(WoWDispelType.Magic) || DispelTarget.HasAuraWithDispelType(WoWDispelType.Curse))),
                new Decorator(req => CountAlliesUnderHealthPercentage(95.0) >= 1, Totems()),
                new Decorator(req => CountAlliesUnderHealthPercentage(50.0) >= 2, MultiHigh()),
                new Decorator(req => LowestAlly.Exists() && LowestAlly.HealthPercentage <= 30.0, SingleHigh()),
                new Decorator(req => CountAlliesUnderHealthPercentage(75.0) >= 2, MultiMid()),
                new Decorator(req => LowestAlly.Exists() && LowestAlly.HealthPercentage <= 65.0, SingleMid()),
                Low(),
                Offensive());
        }

        private Composite Dispell()
        {
            return new PrioritySelector(
                Spells.PurifySpirit.Cast(
                    on => DispelTarget));
        }

        private Composite Offensive()
        {
            return new PrioritySelector(
                Spells.FlameShock.Cast(
                    on => Target,
                    ret => Player.GotTarget && !Target.IsFriendly && !Target.HasMyDebuff("Flame Shock")),
                Spells.LavaBurst.Cast(
                    on => Target,
                    ret => Player.GotTarget && !Target.IsFriendly),
                Spells.LightningBolt.Cast(
                    on => Target,
                    ret => Player.GotTarget && !Target.IsFriendly));
        }

        private Composite Totems()
        {
            return new PrioritySelector(
                Spells.SpiritLinkTotem.CastLocation(
                    on => SpiritLinkTarget,
                    ret => !Player.IsMoving),
                Spells.LightningSurgeTotem.CastLocation(
                    on => StunTarget,
                    ret => !Player.IsMoving),
                Spells.CloudburstTotem.Cast(),
                Spells.HealingStreamTotem.Cast());
        }

        private Composite Riptide()
        {
            return new PrioritySelector(
                Spells.Riptide.Cast(
                    on => LowestAllyWithoutRiptide),
                Spells.Riptide.Cast(
                    on => LowestAlly));
        }

        private Composite MultiHigh()
        {
            return new PrioritySelector(
                Spells.GiftOfTheQueen.CastLocation(
                    on => getTarget(),
                    ret => !Player.IsMoving && CountAlliesUnderHealthPercentage(80) >= 2),
                Spells.ChainHeal.Cast(
                    on => LowestAlly));
        }

        private Composite MultiMid()
        {
            return new PrioritySelector(
                Spells.GiftOfTheQueen.CastLocation(
                    on => getTarget(),
                    ret => !Player.IsMoving && CountAlliesUnderHealthPercentage(80) >= 2),
                Spells.HealingRain.CastLocation(
                    on => getTarget(),
                    ret => !Player.IsMoving && CountAlliesUnderHealthPercentage(95) >= 2),
                Riptide(),
                Spells.ChainHeal.Cast(
                    on => LowestAlly));
        }

        private Composite SingleHigh()
        {
            return new PrioritySelector(
                Riptide(),
                Spells.HealingSurge.Cast(
                    on => LowestAlly,
                    ret => LowestAlly.Exists() && LowestAlly.HealthPercentage < 60),
                Spells.HealingWave.Cast(
                    on => LowestAlly));
        }

        private Composite SingleMid()
        {
            return new PrioritySelector(
                Riptide(),
                Spells.HealingSurge.Cast(
                    on => LowestAlly,
                    ret => LowestAlly.Exists() && LowestAlly.HealthPercentage < 60),
                Spells.HealingWave.Cast(
                    on => LowestAlly));
        }

        private Composite Low()
        {
            return new PrioritySelector(
                Riptide(),
                Spells.HealingRain.CastLocation(
                    on => getTarget(),
                    ret => !Player.IsMoving && CountAlliesUnderHealthPercentage(95) >= 2),
                Spells.HealingWave.Cast(
                    on => LowestAlly,
                    ret => LowestAlly.Exists() && LowestAlly.HealthPercentage < 98));
        }

        private static WoWUnit StunTarget => TargetManager.GetOptimalEnemy(
            "StunTarget",
            x => x.IsAlive && x.InCombat && x.Distance < 38 && x.HealthPercentage > 80 && x.Health > 2000000,
            x => x.Distance);

        private static WoWPlayer LowestAlly => TargetManager.GetOptimalAlly(
            "LowestAlly",
            x => x.IsAlive && x.Health < x.MaximumHealth,
            x => x.HealthPercentage);

        private static WoWPlayer LowestAllyWithoutRiptide => TargetManager.GetOptimalAlly(
            "LowestAllyWithoutRiptide",
            x => x.IsAlive && x.Health < x.MaximumHealth && !x.HasMyBuff( "Riptide" ),
            x => x.HealthPercentage);

        private static WoWPlayer DispelTarget => TargetManager.GetOptimalAlly(
            "DispelTarget",
            x => x.IsAlive
                && (x.HasAuraWithDispelType(WoWDispelType.Magic)
                    || x.HasAuraWithDispelType(WoWDispelType.Curse)),
            x => x.HealthPercentage);

        private static WoWPlayer SpiritLinkTarget => TargetManager.GetOptimalAlly(
            "SpiritLinkTarget",
            x => x.IsAlive && x.HealthPercentage < 25,
            x => x.HealthPercentage);

        private Composite DebugPrintLine(string str)
        {
            return new Shadowbuddy.TreeSharp.Action(ctx =>
            {
                Log.Debug(str);
                return RunStatus.Failure;
            });
        }

        /*private static Composite SingleTarget()
        {
            return new Decorator(
                ret => ( CountAlliesUnderHealthPercentage(95) > 0 || shouldHeal() && Target.HealthPercentage < 100 ) && !Player.IsCasting,
                new PrioritySelector(
                    Spells.Riptide.Cast(
                        ret => shouldHeal()),
                    Spells.HealingSurge.Cast(
                        ret => shouldHeal() && Target.HealthPercentage < 35),
                    Spells.CloudburstTotem.Cast(),
                    Spells.HealingStreamTotem.Cast(),
                    Spells.HealingRain.CastLocation(
                        on => getTarget(),
                        ret => !Player.IsMoving && CountAlliesUnderHealthPercentage(95) >= 2),
                    Spells.GiftOfTheQueen.CastLocation(
                        on => getTarget(),
                        ret => !Player.IsMoving && CountAlliesUnderHealthPercentage(80) >= 2),
                    Spells.ChainHeal.Cast(
                        ret => shouldHeal() && CountAlliesUnderHealthPercentage(90) > 2 ),
                    Spells.HealingSurge.Cast(
                        ret => shouldHeal() && Target.HealthPercentage < 60),
                    Spells.HealingWave.Cast(
                        ret => shouldHeal())
                ));
        }*/

        private static bool shouldHeal()
        {
            return Player.GotTarget && Target.IsFriendly && !Target.IsDead;
        }

        private static int CountAlliesUnderHealthPercentage(double healthPercentage)
        {
            return TargetManager.Group.Count( x => x.IsAlive && x.HealthPercentage <= healthPercentage && x.Distance < 40 );
        }

        private static WoWPlayer getTank()
        {
            return TargetManager.Tanks.OrderBy(x => x.HealthPercentage).FirstOrDefault();
        }
       
        private static WoWUnit getTarget()
        {
            Vector3 middle = getPositionComplex();
            List<WoWPlayer> allies = TargetManager.Group.Where(h => h.DistanceToPosition(middle) < 40.0).ToList();

            WoWUnit retval = null;
            float lastDistance = 999;
            foreach (WoWPlayer player in allies)
            {
                if (retval == null)
                {
                    retval = player;
                    lastDistance = Vector3.Distance(player.Position, middle);
                }
                else
                {
                    float distance = Vector3.Distance(player.Position, middle);
                    if (distance < lastDistance)
                    {
                        retval = player;
                        lastDistance = distance;
                    }
                }
            }

            List<WoWUnit> enemies = TargetManager.Enemies.Where(h => h.DistanceToPosition(middle) < 40.0).ToList();
            foreach (WoWUnit enemy in enemies)
            {
                if (retval == null)
                {
                    retval = enemy;
                    lastDistance = Vector3.Distance(enemy.Position, middle);
                }
                else
                {
                    float distance = Vector3.Distance(enemy.Position, middle);
                    if (distance < lastDistance)
                    {
                        retval = enemy;
                        lastDistance = distance;
                    }
                }
            }

            //Vector3 offset = Vector3.Subtract(middle, retval.Position);
            return retval;
        }

        private static Vector3 getPositionComplex()
        {
            //get all wounded allies
            List<WoWPlayer> woundedAllies = TargetManager.Group.Where( h => h.HealthPercentage < 100.0 && h.Distance < 38 ).ToList();

            if (woundedAllies.Count == 0 || !Spells.HealingRain.CanCast())
                return getTank() != null ? getTank().Position : Player.Position;

            Vector3 first = woundedAllies.ElementAt<WoWPlayer>(0).Position;

            if (woundedAllies.Count == 1)
                return first;

            if (positionCalculated.AddSeconds(2) > DateTime.Now )
            {
                return lastCalculatedPosition;
            }

            /*Log.Debug("Wounded Allies:");
            foreach (WoWPlayer p in woundedAllies)
            {
                Log.Debug("{0}: HP: {1}% Position: X: {2} Y: {3} Z: {4}", p.Name, p.HealthPercentage, p.Position.X, p.Position.Y, p.Position.Z);
            }*/

            //calculate search box
            float minX = first.X;
            float minY = first.Y;
            float maxX = first.X;
            float maxY = first.Y;

            for ( int i=1; i<woundedAllies.Count; i++ )
            {
                Vector3 current = woundedAllies.ElementAt<WoWPlayer>(i).Position;
                minX = Math.Min(minX, current.X);
                minY = Math.Min(minY, current.Y);
                maxX = Math.Max(maxX, current.X);
                maxY = Math.Max(maxY, current.Y);
            }

            //calculate steps
            float xStep = 4f;
            float yStep = 4f;

            //find best hit in search box
            List<WoWPlayer> best = new List<WoWPlayer>();
            for(float xCurrent = minX; xCurrent <= maxX; xCurrent += xStep)
            {
                for (float yCurrent = minY; yCurrent <= maxY; yCurrent += yStep)
                {
                    Vector3 pos = new Vector3(xCurrent, yCurrent, first.Z); //ignore height
                    IEnumerable<WoWPlayer> currentHit = woundedAllies.Where(g => Vector3.Distance( g.Position, pos ) < 20);
                    if( currentHit.Count<WoWPlayer>() > best.Count )
                    {
                        //Log.Debug("Found new best hit! New count: {0} (old count: {1})", currentHit.Count<WoWPlayer>(), best.Count);
                        best = currentHit.ToList();
                    }
                }
            }

            //calculate clean hit of best search box (not needed, but it would have bugged the hell out of me)
            float x = 0;
            float y = 0;
            float z = 0;

            int count = best.Count;
            foreach (WoWPlayer player in best)
            {
                x += player.Position.X;
                y += player.Position.Y;
                z += player.Position.Z;
            }
            x = x / count;
            y = y / count;
            z = z / count;

            Vector3 retval = new Vector3(x, y, z);
            lastCalculatedPosition = retval;
            positionCalculated = DateTime.Now;

            Log.Debug("Returning new vector: {0},{1},{2}", x, y, z);
            Log.Debug(" ");

            return retval;
        }

        private static class Spells
        {
            public static readonly Spell HealingWave = new Spell(77472, SpellFlag.IsHelpful),
                                        HealingSurge = new Spell(8004, SpellFlag.IsHelpful),
                                        ChainHeal = new Spell(1064, SpellFlag.IsHelpful),
                                        Riptide = new Spell(61295, SpellFlag.IsHelpful),
                                        HealingRain = new Spell(73920, SpellFlag.IsHelpful),
                                        PurifySpirit = new Spell(77130, SpellFlag.IsHelpful),
                                        GiftOfTheQueen = new Spell(207778, SpellFlag.IsHelpful),
                                        CloudburstTotem = new Spell(157153, SpellFlag.IsHelpful),
                                        HealingStreamTotem = new Spell(5394, SpellFlag.IsHelpful),
                                        FlameShock = new Spell(188838),
                                        LavaBurst = new Spell(51505),
                                        LightningBolt = new Spell(403),
                                        HealingTideTotem = new Spell(108280, SpellFlag.IsHelpful),
                                        SpiritLinkTotem = new Spell(98008),
                                        LightningSurgeTotem = new Spell(192058);

        }


    }
}
