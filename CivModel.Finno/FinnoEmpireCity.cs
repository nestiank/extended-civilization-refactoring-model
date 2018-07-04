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
                var creators = new Func<Terrain.Point, Unit>[] {
                    pt => new DecentralizedMilitary(Owner, pt),
                    pt => new EMUHorseArcher(Owner, pt),
                    pt => new ElephantCavalry(Owner, pt),
                    pt => new AncientSorcerer(Owner, pt),
                    pt => new JediKnight(Owner, pt),
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

        public FinnoEmpireCity(Player player, Terrain.Point point, bool isLoadFromFile) : base(player, Constants, point)
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

            public bool IsParametered => false;

            public int LastSkillCalled = -1;

            public FinnoEmpireCityAction(FinnoEmpireCity owner)
            {
                _owner = owner;
            }

            public ActionPoint GetRequiredAP(Terrain.Point? pt)
            {
                if (pt != null)
                    return double.NaN;
                if (!_owner.PlacedPoint.HasValue)
                    return double.NaN;
                if (Owner.Owner.Game.TurnNumber == LastSkillCalled)
                    return double.NaN;

                return 0;
            }

            public void Act(Terrain.Point? pt)
            {
                if (pt != null)
                    throw new ArgumentException("pt is invalid");
                if (!_owner.PlacedPoint.HasValue)
                    throw new InvalidOperationException("Actor is not placed yet");
                if (Owner.Owner.Game.TurnNumber == LastSkillCalled)
                    throw new InvalidOperationException("Skill is not turned on");

                int A = Owner.PlacedPoint.Value.Position.A;
                int B = Owner.PlacedPoint.Value.Position.B;
                int C = Owner.PlacedPoint.Value.Position.C;

                RealAction(A + 1, B - 1, C);
                RealAction(A + 1, B, C - 1);
                RealAction(A, B + 1, C - 1);
                RealAction(A - 1, B + 1, C);
                RealAction(A - 1, B, C + 1);
                RealAction(A, B - 1, C + 1);

                RealAction(A + 2, B - 2, C);
                RealAction(A + 2, B - 1, C - 1);
                RealAction(A + 2, B, C - 2);
                RealAction(A + 1, B + 1, C - 2);
                RealAction(A, B + 2, C - 2);
                RealAction(A - 1, B + 2, C - 1);
                RealAction(A - 2, B + 2, C);
                RealAction(A - 2, B + 1, C + 1);
                RealAction(A - 2, B, C + 2);
                RealAction(A - 1, B - 1, C + 2);
                RealAction(A, B - 2, C + 2);
                RealAction(A + 1, B - 2, C + 1);

                LastSkillCalled = Owner.Owner.Game.TurnNumber;
            }

            private void RealAction(int A, int B, int C)
            {
                if (0 <= B + (C + Math.Sign(C)) / 2 && B + (C + Math.Sign(C)) / 2 < Owner.PlacedPoint.Value.Terrain.Width && 0 <= C && C < Owner.PlacedPoint.Value.Terrain.Height)
                {
                    if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A, B, C)).Unit != null)
                    {
                        if ((Owner.PlacedPoint.Value.Terrain.GetPoint(A, B, C)).Unit.Owner != Owner.Owner)
                        {
                            Owner.AttackTo(30, (Owner.PlacedPoint.Value.Terrain.GetPoint(A, B, C)).Unit, 0, false, true);
                        }
                    }
                }
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
