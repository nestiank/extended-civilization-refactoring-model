using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Hwan
{
    public sealed class HwanEmpireLatifundium : TileBuilding
    {
        public static Guid ClassGuid { get; } = new Guid("10B85454-07B8-4D6A-8FF2-157870C41AF6");
        public override Guid Guid => ClassGuid;

        public override IReadOnlyList<IActorAction> SpecialActs => _specialActs;
        private readonly IActorAction[] _specialActs = new IActorAction[1];


        public static readonly ActorConstants Constants = new ActorConstants
        {
            MaxHP = 20,
            DefencePower = 0,
            GoldLogistics = 20,
            LaborLogistics = 10,
            MaxHealPerTurn = 4
        };
        private double _ProvidedGold = 10;
        public override double ProvidedGold => _ProvidedGold;

        private double _ProvidedLabor = 0;
        public override double ProvidedLabor => _ProvidedLabor;

        public int SkillDurationTime = 0;

        public override void PostTurn()
        {
            if (this.SkillDurationTime >= this.Owner.Game.TurnNumber)
            {
                _ProvidedGold = 0;
                _ProvidedLabor = 10;
            }
            else
            {
                _ProvidedGold = 10;
                _ProvidedLabor = 0;
            }
            base.PostTurn();
        }



        public HwanEmpireLatifundium(Player owner, Terrain.Point point) : base(owner, Constants, point)
        {
            _specialActs[0] = new HwanEmpireLatifundiumAction(this);
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

            private Exception CheckError(Terrain.Point? pt)
            {
                if (pt != null)
                    return new ArgumentException("pt is invalid");
                if (!_owner.PlacedPoint.HasValue)
                    return new InvalidOperationException("Actor is not placed yet");
                if (Owner.Owner.Game.TurnNumber == LastSkillCalled)
                    return new InvalidOperationException("Skill is not turned on");

                return null;
            }

            public ActionPoint GetRequiredAP(Terrain.Point? pt)
            {
                if (CheckError(pt) != null)
                    return double.NaN;

                return 0;
            }

            public void Act(Terrain.Point? pt)
            {
                if (CheckError(pt) is Exception e)
                    throw e;

                ActionPoint Ap = GetRequiredAP(pt);
                if (!Owner.CanConsumeAP(Ap))
                    throw new InvalidOperationException("Not enough Ap");

                _owner.SkillDurationTime = Owner.Owner.Game.TurnNumber + 2;

                LastSkillCalled = Owner.Owner.Game.TurnNumber;
            }
        }

        
    }

    public class HwanEmpireLatifundiumProductionFactory : ITileObjectProductionFactory
    {
        public static HwanEmpireLatifundiumProductionFactory Instance => _instance.Value;
        private static Lazy<HwanEmpireLatifundiumProductionFactory> _instance
            = new Lazy<HwanEmpireLatifundiumProductionFactory>(() => new HwanEmpireLatifundiumProductionFactory());
        private HwanEmpireLatifundiumProductionFactory()
        {
        }

        public Type ResultType => typeof(HwanEmpireLatifundium);
        public ActorConstants ActorConstants => HwanEmpireLatifundium.Constants;

        public double TotalLaborCost => 30;
        public double LaborCapacityPerTurn => 10;
        public double TotalGoldCost => 30;
        public double GoldCapacityPerTurn => 10;

        public Production Create(Player owner)
        {
            return new TileObjectProduction(this, owner);
        }
        public bool IsPlacable(TileObjectProduction production, Terrain.Point point)
        {
            return point.TileBuilding == null
                 && !IsCityNeer(production, point)
                 && (point.TileOwner == production.Owner || point.TileOwner == production.Owner.Game.Players[2] || point.TileOwner == production.Owner.Game.Players[4] || point.TileOwner == production.Owner.Game.Players[6]);
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

        public TileObject CreateTileObject(Player owner, Terrain.Point point)
        {
            return new HwanEmpireLatifundium(owner, point);
        }
    }
}
