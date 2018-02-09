﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Common
{
    public class ElephantCavalry : Unit
    {
        public static Guid ClassGuid { get; } = new Guid("8AC71759-3B58-4637-9F09-4F483EB0F4B8");
        public override Guid Guid => ClassGuid;

        public override int MaxAP => 2;

        public override double MaxHP => 50;

        public override double AttackPower => 17;
        public override double DefencePower => 5;

        public override int BattleClassLevel => 2;

        private readonly IActorAction _holdingAttackAct;
        public override IActorAction HoldingAttackAct => _holdingAttackAct;

        private readonly IActorAction _movingAttackAct;
        public override IActorAction MovingAttackAct => _movingAttackAct;

        public override IReadOnlyList<IActorAction> SpecialActs => _specialActs;
        private readonly IActorAction[] _specialActs = new IActorAction[1];

        public ElephantCavalry(Player owner, Terrain.Point point) : base(owner, point)
        {
            _holdingAttackAct = new AttackActorAction(this, false);
            _movingAttackAct = new AttackActorAction(this, true);
            _specialActs[0] = new ElephantCavalryAction(this);
        }

        private class ElephantCavalryAction : IActorAction
        {
            private readonly ElephantCavalry _owner;
            public Actor Owner => _owner;

            public bool IsParametered => true;

            public ElephantCavalryAction(ElephantCavalry owner)
            {
                _owner = owner;
            }

            public int LastSkillCalled = -2;

            public int GetRequiredAP(Terrain.Point? pt)
            {
                if (pt == null)
                    return -1;
                if (!_owner.PlacedPoint.HasValue)
                    return -1;
                if (Owner.Owner.Game.TurnNumber <= LastSkillCalled + 1)
                    return -1;
                if (pt.Value.Unit != null)
                    return -1;
                if (pt.Value.Position.A != Owner.PlacedPoint.Value.Position.A && pt.Value.Position.B != Owner.PlacedPoint.Value.Position.B && pt.Value.Position.C != Owner.PlacedPoint.Value.Position.C)
                    return -1;
                if (Math.Max(Math.Max(Math.Abs(pt.Value.Position.A - Owner.PlacedPoint.Value.Position.A), Math.Abs(pt.Value.Position.B - Owner.PlacedPoint.Value.Position.B)), Math.Abs(pt.Value.Position.C - Owner.PlacedPoint.Value.Position.C)) != 3)
                    return -1;


                return 1;
            }

            public void Act(Terrain.Point? pt)
            {
                if (pt != null)
                    throw new ArgumentException("pt is invalid");
                if (!_owner.PlacedPoint.HasValue)
                    throw new InvalidOperationException("Actor is not placed yet");
                if (Owner.Owner.Game.TurnNumber <= LastSkillCalled + 1)
                    throw new InvalidOperationException("Skill is not turned on");
                if (pt.Value.Unit != null)
                    throw new InvalidOperationException("Can't go that way");
                if (pt.Value.Position.A != Owner.PlacedPoint.Value.Position.A && pt.Value.Position.B != Owner.PlacedPoint.Value.Position.B && pt.Value.Position.C != Owner.PlacedPoint.Value.Position.C)
                    throw new InvalidOperationException("Can't go that way");
                if (Math.Max(Math.Max(Math.Abs(pt.Value.Position.A - Owner.PlacedPoint.Value.Position.A), Math.Abs(pt.Value.Position.B - Owner.PlacedPoint.Value.Position.B)), Math.Abs(pt.Value.Position.C - Owner.PlacedPoint.Value.Position.C)) != 3)
                    throw new InvalidOperationException("Can't go that way");

                Owner.PlacedPoint = pt;

                int A = Owner.PlacedPoint.Value.Position.A;
                int B = Owner.PlacedPoint.Value.Position.B;
                int C = Owner.PlacedPoint.Value.Position.C;

                if (pt.Value.Position.A == Owner.PlacedPoint.Value.Position.A)
                {
                    if (pt.Value.Position.B < Owner.PlacedPoint.Value.Position.B)
                    {
                        if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A, B - 1, C + 1)).Unit != null)
                        {
                            if((Owner.PlacedPoint.Value.Terrain.GetPoint(A, B - 1, C + 1)).Unit.Owner != Owner.Owner)
                            {
                                double Damage = (Owner.PlacedPoint.Value.Terrain.GetPoint(A, B - 1, C + 1)).Unit.MaxHP * 0.2;
                                Owner.AttackTo(Damage, (Owner.PlacedPoint.Value.Terrain.GetPoint(A, B - 1, C + 1)).Unit, 0, false, true);
                            }
                        }
                    }

                    if (pt.Value.Position.B > Owner.PlacedPoint.Value.Position.B)
                    {
                        if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A, B + 1, C - 1)).Unit != null)
                        {
                            if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A, B + 1, C - 1)).Unit.Owner != Owner.Owner)
                            {
                                double Damage = (Owner.PlacedPoint.Value.Terrain.GetPoint(A, B + 1, C - 1)).Unit.MaxHP * 0.2;
                                Owner.AttackTo(Damage, (Owner.PlacedPoint.Value.Terrain.GetPoint(A, B + 1, C - 1)).Unit, 0, false, true);
                            }
                        }
                    }
                }

                if (pt.Value.Position.B == Owner.PlacedPoint.Value.Position.B)
                {
                    if (pt.Value.Position.A < Owner.PlacedPoint.Value.Position.A)
                    {
                        if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A - 1, B, C + 1)).Unit != null)
                        {
                            if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A - 1, B, C + 1)).Unit.Owner != Owner.Owner)
                            {
                                double Damage = (Owner.PlacedPoint.Value.Terrain.GetPoint(A - 1, B, C + 1)).Unit.MaxHP * 0.2;
                                Owner.AttackTo(Damage, (Owner.PlacedPoint.Value.Terrain.GetPoint(A - 1, B, C + 1)).Unit, 0, false, true);
                            }
                        }
                    }

                    if (pt.Value.Position.A > Owner.PlacedPoint.Value.Position.A)
                    {
                        if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A + 1, B, C - 1)).Unit != null)
                        {
                            if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A + 1, B, C - 1)).Unit.Owner != Owner.Owner)
                            {
                                double Damage = (Owner.PlacedPoint.Value.Terrain.GetPoint(A + 1, B, C - 1)).Unit.MaxHP * 0.2;
                                Owner.AttackTo(Damage, (Owner.PlacedPoint.Value.Terrain.GetPoint(A + 1, B, C - 1)).Unit, 0, false, true);
                            }
                        }
                    }
                }

                if (pt.Value.Position.C == Owner.PlacedPoint.Value.Position.C)
                {
                    if (pt.Value.Position.A < Owner.PlacedPoint.Value.Position.A)
                    {
                        if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A - 1, B + 1, C)).Unit != null)
                        {
                            if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A - 1, B + 1, C)).Unit.Owner != Owner.Owner)
                            {
                                double Damage = (Owner.PlacedPoint.Value.Terrain.GetPoint(A - 1, B + 1, C)).Unit.MaxHP * 0.2;
                                Owner.AttackTo(Damage, (Owner.PlacedPoint.Value.Terrain.GetPoint(A - 1, B + 1, C)).Unit, 0, false, true);
                            }
                        }
                    }

                    if (pt.Value.Position.A > Owner.PlacedPoint.Value.Position.A)
                    {
                        if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A + 1, B - 1, C)).Unit != null)
                        {
                            if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A + 1, B - 1, C)).Unit.Owner != Owner.Owner)
                            {
                                double Damage = (Owner.PlacedPoint.Value.Terrain.GetPoint(A + 1, B - 1, C)).Unit.MaxHP * 0.2;
                                Owner.AttackTo(Damage, (Owner.PlacedPoint.Value.Terrain.GetPoint(A + 1, B - 1, C)).Unit, 0, false, true);
                            }
                        }
                    }
                }

                LastSkillCalled = Owner.Owner.Game.TurnNumber;

            }
        }
    }

    public class ElephantCavalryProductionFactory : ITileObjectProductionFactory
    {
        private static Lazy<ElephantCavalryProductionFactory> _instance
            = new Lazy<ElephantCavalryProductionFactory>(() => new ElephantCavalryProductionFactory());
        public static ElephantCavalryProductionFactory Instance => _instance.Value;
        private ElephantCavalryProductionFactory()
        {
        }
        public Production Create(Player owner)
        {
            return new TileObjectProduction(this, owner, 50, 15, 25, 5);
        }
        public bool IsPlacable(TileObjectProduction production, Terrain.Point point)
        {
            return point.Unit == null
                && point.TileBuilding is CityCenter
                && point.TileBuilding.Owner == production.Owner;
        }
        public TileObject CreateTileObject(Player owner, Terrain.Point point)
        {
            return new ElephantCavalry(owner, point);
        }
    }
}
