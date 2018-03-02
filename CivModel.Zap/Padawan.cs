using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Zap
{
    public class Padawan : Unit
    {
        public static Guid ClassGuid { get; } = new Guid("04DBFD22-3990-48AC-BAF1-095BDAEFCB57");
        public override Guid Guid => ClassGuid;

        public static readonly ActorConstants Constants = new ActorConstants
        {
            MaxAP = 2,
            MaxHP = 15,
            AttackPower = 13,
            DefencePower = 3,
            GoldLogistics = 30,
            FullLaborForRepair = 2,
            BattleClassLevel = 3
        };

        private readonly IActorAction _holdingAttackAct;
        public override IActorAction HoldingAttackAct => _holdingAttackAct;

        private readonly IActorAction _movingAttackAct;
        public override IActorAction MovingAttackAct => _movingAttackAct;

        public Padawan(Player owner, Terrain.Point point) : base(owner, Constants, point)
        {
            _holdingAttackAct = new AttackActorAction(this, false);
            _movingAttackAct = new AttackActorAction(this, true);
        }
    }

    public class PadawanProductionFactory : ITileObjectProductionFactory
    {
        private static Lazy<PadawanProductionFactory> _instance
            = new Lazy<PadawanProductionFactory>(() => new PadawanProductionFactory());
        public static PadawanProductionFactory Instance => _instance.Value;
        private PadawanProductionFactory()
        {
        }

        public ActorConstants ActorConstants => DecentralizedMilitary.Constants;

        public double TotalLaborCost => 50;
        public double LaborCapacityPerTurn => 20;
        public double TotalGoldCost => 75;
        public double GoldCapacityPerTurn => 11;

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
            return new Padawan(owner, point);
        }
    }
}