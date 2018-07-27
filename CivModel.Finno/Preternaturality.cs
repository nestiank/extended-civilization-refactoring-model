using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Finno
{
    public sealed class Preternaturality : TileBuilding
    {
        public static Guid ClassGuid { get; } = new Guid("C6BE46D2-578B-4B50-9DF5-54F683CEE45E");
        public override Guid Guid => ClassGuid;

        public static readonly ActorConstants Constants = new ActorConstants
        {
            MaxHP = 300,
            DefencePower = 20,
            GoldLogistics = 60,
            LaborLogistics = 30,
            MaxHealPerTurn = 20
        };

        public Preternaturality(Player owner, Terrain.Point point, Player donator = null)
            : base(owner, Constants, point, donator)
        {
        }

        public override double ProvidedHappiness => 5;
        public override double ProvidedGold => 200;
    }

    public class PreternaturalityProductionFactory : ITileBuildingProductionFactory
    {
        public static PreternaturalityProductionFactory Instance => _instance.Value;
        private static Lazy<PreternaturalityProductionFactory> _instance
            = new Lazy<PreternaturalityProductionFactory>(() => new PreternaturalityProductionFactory());
        private PreternaturalityProductionFactory()
        {
        }

        public Type ResultType => typeof(Preternaturality);
        public ActorConstants ActorConstants => Preternaturality.Constants;

        public double TotalLaborCost => 800;
        public double LaborCapacityPerTurn => 80;
        public double TotalGoldCost => 3000;
        public double GoldCapacityPerTurn => 300;

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
            return new Preternaturality(owner, point);
        }
        public TileBuilding CreateDonation(Player owner, Terrain.Point point, Player donator)
        {
            return new Preternaturality(owner, point, donator);
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

            else if (RealAction(A + 2, B - 2, C, production, point))
                return true;
            else if (RealAction(A + 2, B - 1, C - 1, production, point))
                return true;
            else if (RealAction(A + 2, B, C - 2, production, point))
                return true;
            else if (RealAction(A + 1, B + 1, C - 2, production, point))
                return true;
            else if (RealAction(A, B + 2, C - 2, production, point))
                return true;
            else if (RealAction(A - 1, B + 2, C - 1, production, point))
                return true;
            else if (RealAction(A - 2, B + 2, C, production, point))
                return true;
            else if (RealAction(A - 2, B + 1, C + 1, production, point))
                return true;
            else if (RealAction(A - 2, B, C + 2, production, point))
                return true;
            else if (RealAction(A - 1, B - 1, C + 2, production, point))
                return true;
            else if (RealAction(A, B - 2, C + 2, production, point))
                return true;
            else if (RealAction(A + 1, B - 2, C + 1, production, point))
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
