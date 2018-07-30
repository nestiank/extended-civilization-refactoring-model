using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Finno
{
    public class Spy : Unit
    {
        public static Guid ClassGuid { get; } = new Guid("B4F8293E-EBF2-47A0-A1BB-DF6A6ECCD766");
        public override Guid Guid => ClassGuid;

        public static readonly ActorConstants Constants = new ActorConstants
        {
            MaxAP = 2,
            MaxHP = 25,
            AttackPower = 5,
            DefencePower = 1,
            GoldLogistics = 10,
            FullLaborForRepair = 2,
            BattleClassLevel = 1
        };

        public bool QuestFlag = false;

        private readonly IActorAction _holdingAttackAct;
        public override IActorAction HoldingAttackAct => _holdingAttackAct;

        private readonly IActorAction _movingAttackAct;
        public override IActorAction MovingAttackAct => _movingAttackAct;

        public override IReadOnlyList<IActorAction> SpecialActs => _specialActs;
        private readonly IActorAction[] _specialActs = new IActorAction[1];

        public Spy(Player owner, Terrain.Point point) : base(owner, Constants, point)
        {
            _holdingAttackAct = new AttackActorAction(this, false);
            _movingAttackAct = new AttackActorAction(this, true);
            _specialActs[0] = new SpyAction(this);

            this.IsCloacking = true;
        }

        private class SpyAction : IActorAction
        {
            private readonly Spy _owner;
            public Actor Owner => _owner;

            public bool IsParametered => false;

            public SpyAction(Spy owner)
            {
                _owner = owner;
            }


            public ActionPoint GetRequiredAP(Terrain.Point? pt)
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
                if (_owner.Owner.IsAlliedWithOrNull(_owner.PlacedPoint.Value.TileOwner))
                    return new InvalidOperationException("Actor is not placed in Hostile");

                return null;
            }

            public void Act(Terrain.Point? pt)
            {
                if (CheckError(pt) is Exception e)
                    throw e;

                ActionPoint Ap = GetRequiredAP(pt);
                if (!Owner.CanConsumeAP(Ap))
                    throw new InvalidOperationException("Not enough Ap");

                Owner.ConsumeAP(Ap);
            }
        }
    }

    public class SpyProductionFactory : IActorProductionFactory
    {
        private static Lazy<SpyProductionFactory> _instance
            = new Lazy<SpyProductionFactory>(() => new SpyProductionFactory());
        public static SpyProductionFactory Instance => _instance.Value;
        private SpyProductionFactory()
        {
        }

        public Type ResultType => typeof(Spy);
        public ActorConstants ActorConstants => Spy.Constants;

        public double TotalLaborCost => 32;
        public double LaborCapacityPerTurn => 8;
        public double TotalGoldCost => 60;
        public double GoldCapacityPerTurn => 15;

        public Production Create(Player owner)
        {
            return new TileObjectProduction(this, owner);
        }
        public bool IsPlacable(TileObjectProduction production, Terrain.Point point)
        {
            return point.Unit == null
                && point.TileBuilding is CityBase
                && point.TileBuilding.Owner == production.Owner;
        }
        public TileObject CreateTileObject(Player owner, Terrain.Point point)
        {
            return new Spy(owner, point);
        }
    }
}
