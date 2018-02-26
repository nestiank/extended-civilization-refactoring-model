using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Finno
{
    public class GenghisKhan : Unit
    {
        public static Guid ClassGuid { get; } = new Guid("DCB724DA-430E-4B13-8703-9517F173FE4B");
        public override Guid Guid => ClassGuid;

        public static readonly ActorConstants Constants = new ActorConstants
        {
            MaxAP = 2,
            MaxHP = 50,
            AttackPower = 35,
            DefencePower = 10,
            GoldLogistics = 50,
            FullLaborLogistics = 3,
            BattleClassLevel = 4
        };

        private readonly IActorAction _holdingAttackAct;
        public override IActorAction HoldingAttackAct => _holdingAttackAct;

        private readonly IActorAction _movingAttackAct;
        public override IActorAction MovingAttackAct => _movingAttackAct;

        public override IReadOnlyList<IActorAction> SpecialActs => _specialActs;
        private readonly IActorAction[] _specialActs = new IActorAction[1];

        public GenghisKhan(Player owner, Terrain.Point point) : base(owner, Constants, point)
        {
            _holdingAttackAct = new AttackActorAction(this, false);
            _movingAttackAct = new AttackActorAction(this, true);
            _specialActs[0] = new GenghisKhanAction(this);
        }

        private class GenghisKhanAction : IActorAction
        {
            private readonly GenghisKhan _owner;
            public Actor Owner => _owner;

            public bool IsParametered => false;

            public GenghisKhanAction(GenghisKhan owner)
            {
                _owner = owner;
            }

            public int LastSkillCalled = -2;

            public double GetRequiredAP(Terrain.Point? pt)
            {
                if (CheckError(pt) != null)
                    return double.NaN;

                return 1;
            }

            private Exception CheckError(Terrain.Point? pt)
            {
                if (pt != null)
                    return new ArgumentException("pt is invalid");
                if (!_owner.PlacedPoint.HasValue)
                    return new InvalidOperationException("Actor is not placed yet");
                if (Owner.Owner.Game.TurnNumber <= LastSkillCalled + 1)
                    return new InvalidOperationException("Skill is not turned on");
                if (Owner.PlacedPoint.Value.TileOwner == Owner.Owner)
                    return new InvalidOperationException("You have the Territory");

                return null;
            }

            public void Act(Terrain.Point? pt)
            {
                if (CheckError(pt) is Exception e)
                    throw e;

                double Ap = GetRequiredAP(pt);
                if (!Owner.CanConsumeAP(Ap))
                    return new InvalidOperationException("Not enough Ap");


                Owner.Owner.AddTerritory(Owner.PlacedPoint.Value);
                if (Owner.PlacedPoint.Value.TileBuilding != null)
                {
                    Owner.PlacedPoint.Value.TileBuilding.Destroy();
                }

                LastSkillCalled = Owner.Owner.Game.TurnNumber;
                Owner.ConsumeAP(Ap);
            }
        }
    }

    public class GenghisKhanProductionFactory : ITileObjectProductionFactory
    {
        private static Lazy<GenghisKhanProductionFactory> _instance
            = new Lazy<GenghisKhanProductionFactory>(() => new GenghisKhanProductionFactory());
        public static GenghisKhanProductionFactory Instance => _instance.Value;
        private GenghisKhanProductionFactory()
        {
        }

        public ActorConstants ActorConstants => GenghisKhan.Constants;

        public double TotalLaborCost => 100;
        public double LaborCapacityPerTurn => 20;
        public double TotalGoldCost => 100;
        public double GoldCapacityPerTurn => 10;

        public Production Create(Player owner)
        {
            return new TileObjectProduction(this, owner);
        }
        public bool IsPlacable(TileObjectProduction production, Terrain.Point point)
        {
            return point.Unit == null
                && point.TileBuilding is CivModel.Common.CityCenter
                && point.TileBuilding.Owner == production.Owner;
        }
        public TileObject CreateTileObject(Player owner, Terrain.Point point)
        {
            return new GenghisKhan(owner, point);
        }
    }
}
