using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Finno
{
    public sealed class AncientFinnoFineDustFactory : TileBuilding
    {
        public static Guid ClassGuid { get; } = new Guid("26F24220-2B77-4E81-A985-77F3BBC77832");
        public override Guid Guid => ClassGuid;

        public static readonly ActorConstants Constants = new ActorConstants
        {
            MaxHP = 20,
            GoldLogistics = 20,
            FullLaborLogistics = 10
        };


        public AncientFinnoFineDustFactory(Player owner, Terrain.Point point) : base(owner, Constants, point) { }

        public override void PostTurn()
        {
            this.RemainHP = Math.Min(20, (this.RemainHP + 4));
        }
    }

    public class AncientFinnoFineDustFactoryProductionFactory : ITileObjectProductionFactory
    {
        public static AncientFinnoFineDustFactoryProductionFactory Instance => _instance.Value;
        private static Lazy<AncientFinnoFineDustFactoryProductionFactory> _instance
            = new Lazy<AncientFinnoFineDustFactoryProductionFactory>(() => new AncientFinnoFineDustFactoryProductionFactory());
        private AncientFinnoFineDustFactoryProductionFactory()
        {
        }

        public ActorConstants ActorConstants => AncientFinnoFineDustFactory.Constants;

        public double TotalLaborCost => 20;
        public double LaborCapacityPerTurn => 10;
        public double TotalGoldCost => 20;
        public double GoldCapacityPerTurn => 10;

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
            return new AncientFinnoFineDustFactory(owner, point);
        }
    }
}
