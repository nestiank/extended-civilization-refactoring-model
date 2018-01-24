﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Common
{
    public class UnicornOrder : Unit
    {
        public override int MaxAP => 2;

        public override double MaxHP => 50;

        public override double AttackPower => 17;
        public override double DefencePower => 5;

        private readonly IActorAction _holdingAttackAct;
        public override IActorAction HoldingAttackAct => _holdingAttackAct;

        private readonly IActorAction _movingAttackAct;
        public override IActorAction MovingAttackAct => _movingAttackAct;

        public UnicornOrder(Player owner) : base(owner)
        {
            _holdingAttackAct = new AttackActorAction(this, false);
            _movingAttackAct = new AttackActorAction(this, true);
        }
    }

    public class UnicornOrderProductionFactory : ITileObjectProductionFactory
    {
        private static Lazy<UnicornOrderProductionFactory> _instance
            = new Lazy<UnicornOrderProductionFactory>(() => new UnicornOrderProductionFactory());
        public static UnicornOrderProductionFactory Instance => _instance.Value;
        private UnicornOrderProductionFactory()
        {
        }
        public Production Create(Player owner)
        {
            return new TileObjectProduction(this, owner, 50, 20);
        }
        public bool IsPlacable(TileObjectProduction production, Terrain.Point point)
        {
            return point.Unit == null
                && point.TileBuilding is CityCenter
                && point.TileBuilding.Owner == production.Owner;
        }
        public TileObject CreateTileObject(Player owner)
        {
            return new UnicornOrder(owner);
        }
    }
}