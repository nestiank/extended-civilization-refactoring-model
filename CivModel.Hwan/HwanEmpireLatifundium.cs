using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Hwan
{
    public sealed class HwanEmpireLatifundium : TileBuilding
    {
        public override IReadOnlyList<IActorAction> SpecialActs => _specialActs;
        private readonly IActorAction[] _specialActs = new IActorAction[1];

        public override double ProvidedGold => _providedGold;
        private double _providedGold;

        public override double ProvidedLabor => _providedLabor;
        private double _providedLabor;

        private int _skillExpireTurn;

        public HwanEmpireLatifundium(Player owner, Terrain.Point point, Player donator = null)
            : base(owner, typeof(HwanEmpireLatifundium), point, donator)
        {
            _specialActs[0] = new HwanEmpireLatifundiumAction(this);

            SkillModeOff();
        }

        private void SkillModeOn()
        {
            _providedGold = 0;
            _providedLabor = 10;
            _skillExpireTurn = Game.TurnNumber + 2;
        }
        private void SkillModeOff()
        {
            _providedGold = 10;
            _providedLabor = 0;
            _skillExpireTurn = -1;
        }

        protected override void FixedPostTurn()
        {
            if (_skillExpireTurn >= Game.TurnNumber)
            {
                SkillModeOff();
            }

            base.FixedPostTurn();
        }

        private class HwanEmpireLatifundiumAction : IActorAction
        {
            private readonly HwanEmpireLatifundium _owner;
            public Actor Owner => _owner;

            public bool IsParametered => false;

            public int LastSkillCalled = -1;

            public HwanEmpireLatifundiumAction(HwanEmpireLatifundium owner)
            {
                _owner = owner;
            }

            private Exception CheckError(Terrain.Point origin, Terrain.Point? target)
            {
                if (target != null)
                    return new ArgumentException("target is invalid");
                if (Owner.Owner.Game.TurnNumber == LastSkillCalled)
                    return new InvalidOperationException("Skill is not turned on");

                return null;
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

                _owner.SkillModeOn();

                LastSkillCalled = Owner.Owner.Game.TurnNumber;
            }
        }
    }

    public class HwanEmpireLatifundiumProductionFactory : ITileBuildingProductionFactory
    {
        public static HwanEmpireLatifundiumProductionFactory Instance => _instance.Value;
        private static Lazy<HwanEmpireLatifundiumProductionFactory> _instance
            = new Lazy<HwanEmpireLatifundiumProductionFactory>(() => new HwanEmpireLatifundiumProductionFactory());
        private HwanEmpireLatifundiumProductionFactory()
        {
        }

        public Type ResultType => typeof(HwanEmpireLatifundium);

        public Production Create(Player owner)
        {
            return new TileBuildingProduction(this, owner);
        }
        public bool IsPlacable(TileObjectProduction production, Terrain.Point point)
        {
            return point.TileBuilding == null && !IsCityNeer(production, point) && production.Owner.IsAlliedWith(point.TileOwner);
        }
        public TileObject CreateTileObject(Player owner, Terrain.Point point)
        {
            return new HwanEmpireLatifundium(owner, point);
        }
        public TileBuilding CreateDonation(Player owner, Terrain.Point point, Player donator)
        {
            return new HwanEmpireLatifundium(owner, point, donator);
        }

        private bool IsCityNeer(TileObjectProduction production, Terrain.Point point)
        {
            int A = point.Position.A;
            int B = point.Position.B;
            int C = point.Position.C;


            if (RealAction(A + 1, B - 1, C, production, point))
                return true;
            else if (RealAction(A + 1, B, C - 1, production, point))
                return true;
            else if (RealAction(A, B + 1, C - 1, production, point))
                return true;
            else if (RealAction(A - 1, B + 1, C, production, point))
                return true;
            else if (RealAction(A - 1, B, C + 1, production, point))
                return true;
            else if (RealAction(A, B - 1, C + 1, production, point))
                return true;

            return false;
        }

        private bool RealAction(int A, int B, int C, TileObjectProduction production, Terrain.Point point)
        {
            if (0 <= B + (C + Math.Sign(C)) / 2 && B + (C + Math.Sign(C)) / 2 < point.Terrain.Width && 0 <= C && C < point.Terrain.Height)
            {
                if ((point.Terrain.GetPoint(A, B, C)).TileBuilding is CityBase)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
