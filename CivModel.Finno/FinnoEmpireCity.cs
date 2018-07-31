using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Finno
{
    public sealed class FinnoEmpireCity : CityBase
    {
        public static Guid ClassGuid { get; } = new Guid("300E06FD-B656-46DC-A668-BB36C75E3086");
        public override Guid Guid => ClassGuid;

        public static readonly ActorConstants Constants = new ActorConstants
        {
            MaxHP = 500,
            DefencePower = 15,
            GoldLogistics = 0,
            LaborLogistics = 0,
            MaxHealPerTurn = 20
        };

        public override void PostTurn()
        {
            if (Game.Random.Next(100) == 7)
            {
                SendUnit();
            }

            base.PostTurn();
        }

        private void SendUnit()
        {
            if (PlacedPoint is Terrain.Point thisPoint)
            {
                var creators = new Action<Terrain.Point>[] {
                    pt => new DecentralizedMilitary(Owner, pt).OnAfterProduce(null),
                    pt => new EMUHorseArcher(Owner, pt).OnAfterProduce(null),
                    pt => new ElephantCavalry(Owner, pt).OnAfterProduce(null),
                    pt => new AncientSorcerer(Owner, pt).OnAfterProduce(null),
                    pt => new JediKnight(Owner, pt).OnAfterProduce(null),
                };
                var creator = creators[Game.Random.Next(creators.Length)];

                foreach (var adjacent in thisPoint.Adjacents())
                {
                    if (adjacent is Terrain.Point pt && pt.Unit == null
                        && (pt.TileBuilding == null || pt.TileBuilding.Owner == Owner))
                    {
                        creator(pt);
                        return;
                    }
                }
            }
        }

        public override IReadOnlyList<IActorAction> SpecialActs => _specialActs;
        private readonly IActorAction[] _specialActs = new IActorAction[1];

        public FinnoEmpireCity(Player player, Terrain.Point point, bool isLoadFromFile)
            : base(player, Constants, point, null)
        {
            this.Population = 5;
            _specialActs[0] = new FinnoEmpireCityAction(this);

            if (!isLoadFromFile)
            {
                foreach (var pt in PlacedPoint.Value.Adjacents())
                {
                    if (pt.HasValue)
                        Owner.TryAddTerritory(pt.Value);
                }
            }
        }

        private class FinnoEmpireCityAction : IActorAction
        {
            private readonly FinnoEmpireCity _owner;
            public Actor Owner => _owner;

            public bool IsParametered => true;

            public int LastSkillCalled = -1;

            public FinnoEmpireCityAction(FinnoEmpireCity owner)
            {
                _owner = owner;
            }

            private Exception CheckError(Terrain.Point origin, Terrain.Point? target)
            {
                if (target == null)
                    return new ArgumentNullException(nameof(target));
                if (Owner.Owner.Game.TurnNumber == LastSkillCalled)
                    return new InvalidOperationException("Skill is not turned on");
                if (target.Value.Unit == null)
                    return new InvalidOperationException("There is no target");
                if (target.Value.Unit.Owner == Owner.Owner)
                    return new InvalidOperationException("The Unit is not hostile");
                if (!this.IsInDistance(origin, target.Value))
                    return new InvalidOperationException("Too Far to Attack");

                return null;
            }

            private bool IsInDistance(Terrain.Point origin, Terrain.Point target)
            {
                int A = origin.Position.A;
                int B = origin.Position.B;
                int C = origin.Position.C;
                int Width = origin.Terrain.Width;

                if (Math.Max(Math.Max(Math.Abs(target.Position.A - A), Math.Abs(target.Position.B - B)), Math.Abs(target.Position.C - C)) > 2)
                {
                    if (target.Position.B > B) // pt가 맵 오른쪽
                    {
                        if (Math.Max(Math.Max(Math.Abs(target.Position.B - Width - B), Math.Abs(target.Position.A + Width - A)), Math.Abs(target.Position.C - C)) > 2)
                            return false;
                    }
                    else //pt가 맵 왼쪽
                    {
                        if (Math.Max(Math.Max(Math.Abs(target.Position.B + Width - B), Math.Abs(target.Position.A - Width - A)), Math.Abs(target.Position.C - C)) > 2)
                            return false;
                    }
                }
                return true;
            }

            public ActionPoint GetRequiredAP(Terrain.Point origin, Terrain.Point? target)
            {
                if (CheckError(origin, target) != null)
                    return double.NaN;

                return 0;
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
                Owner.ConsumeAP(Ap);

                Owner.AttackTo(30, target.Value.Unit, 0, false, true);

                LastSkillCalled = Owner.Owner.Game.TurnNumber;
            }
        }

        protected override double CalculateDefencePower(double originalPower, Actor opposite, bool isMelee, bool isSkillAttack)
        {
            return originalPower + 15 * InteriorBuildings.OfType<AncientFinnoVigilant>().Count();
        }
    }

    public class FinnoEmpireCityProductionFactory : ITileObjectProductionFactory
    {
        public static FinnoEmpireCityProductionFactory Instance => _instance.Value;
        private static Lazy<FinnoEmpireCityProductionFactory> _instance
            = new Lazy<FinnoEmpireCityProductionFactory>(() => new FinnoEmpireCityProductionFactory());

        private FinnoEmpireCityProductionFactory()
        {
        }

        public Type ResultType => typeof(FinnoEmpireCity);
        public ActorConstants Constants => FinnoEmpireCity.Constants;

        public double TotalLaborCost => 200;
        public double LaborCapacityPerTurn => 20;
        public double TotalGoldCost => 300;
        public double GoldCapacityPerTurn => 50;

        public Production Create(Player owner)
        {
            return new TileObjectProduction(this, owner);
        }

        public bool IsPlacable(TileObjectProduction production, Terrain.Point point)
        {
            return point.TileBuilding == null
                && point.Unit is Pioneer pioneer
                && pioneer.Owner == production.Owner;
        }

        public TileObject CreateTileObject(Player owner, Terrain.Point point)
        {
            // remove pioneer
            point.Unit.Destroy();

            return new FinnoEmpireCity(owner, point, false);
        }
    }
}
