using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Hwan
{
    public class JediKnight : Unit
    {
        public int SkillDurationTime = 0;

        public double? SkillDamage = null;

        protected override double CalculateDamage(double originalDamage, Actor attacker, Actor defender, bool isMelee, bool isSkillAttack)
        {
            if (this == defender)
            {
                if (attacker.BattleClassLevel >= 4 && isSkillAttack)
                {
                    return originalDamage;
                }
                else if (this.SkillDurationTime >= this.Owner.Game.TurnNumber)
                {
                    SkillDamage = originalDamage;
                    return 0;
                }
            }
            return originalDamage;
        }

        protected override void OnAfterDamage(double atk, double def, double attackerDamage, double defenderDamage,
            Actor attacker, Actor defender, Player atkOwner, Player defOwner, bool isMelee, bool isSkillAttack)
        {
            if (this == defender && SkillDamage.HasValue)
            {
                if (attacker.Owner != null)
                {
                    attacker.GetDamage(SkillDamage.Value, defOwner);
                }
                SkillDamage = null;
            }
        }

        private readonly IActorAction _holdingAttackAct;
        public override IActorAction HoldingAttackAct => _holdingAttackAct;

        private readonly IActorAction _movingAttackAct;
        public override IActorAction MovingAttackAct => _movingAttackAct;

        public override IReadOnlyList<IActorAction> SpecialActs => _specialActs;
        private readonly IActorAction[] _specialActs = new IActorAction[1];

        public JediKnight(Player owner, Terrain.Point point) : base(owner, typeof(JediKnight), point)
        {
            _holdingAttackAct = new AttackActorAction(this, false);
            _movingAttackAct = new AttackActorAction(this, true);
            _specialActs[0] = new JediKnightAction(this);
        }

        private class JediKnightAction : IActorAction
        {
            private readonly JediKnight _owner;
            public Actor Owner => _owner;

            public bool IsParametered => false;

            public JediKnightAction(JediKnight owner)
            {
                _owner = owner;
            }

            public int LastSkillCalled = -3;

            public ActionPoint GetRequiredAP(Terrain.Point origin, Terrain.Point? target)
            {
                if (CheckError(origin, target) != null)
                    return double.NaN;

                return 2;
            }

            private Exception CheckError(Terrain.Point origin, Terrain.Point? target)
            {
                if (target != null)
                    return new ArgumentException("pt is invalid");
                if (Owner.Owner.Game.TurnNumber <= LastSkillCalled + 2)
                    return new InvalidOperationException("Skill is not turned on");

                return null;
            }

            public void Act(Terrain.Point? target)
            {
                if (!_owner.PlacedPoint.HasValue)
                    throw new InvalidOperationException("Actor is not placed yet");
                var origin = _owner.PlacedPoint.Value;

                if (CheckError(origin, target) is Exception e)
                    throw e;

                ActionPoint Ap = GetRequiredAP(origin, target);
                if (!Owner.CanConsumeAP(Ap))
                    throw new InvalidOperationException("Not enough Ap");

                _owner.SkillDurationTime = Owner.Owner.Game.TurnNumber + 1;
                LastSkillCalled = Owner.Owner.Game.TurnNumber;
                Owner.ConsumeAP(Ap);
            }
        }
    }

    public class JediKnightProductionFactory : IActorProductionFactory
    {
        private static Lazy<JediKnightProductionFactory> _instance
            = new Lazy<JediKnightProductionFactory>(() => new JediKnightProductionFactory());
        public static JediKnightProductionFactory Instance => _instance.Value;
        private JediKnightProductionFactory()
        {
        }

        public Type ResultType => typeof(JediKnight);

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
            return new JediKnight(owner,point);
        }
    }
}
