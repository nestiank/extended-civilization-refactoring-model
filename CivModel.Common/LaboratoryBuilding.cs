using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Common
{
    public class LaboratoryBuildingConstants : InteriorBuildingConstants
    {
        public override double ProvidedResearchIncome => 10;
    }

    public class LaboratoryBuilding : InteriorBuilding
    {
        public static Guid ClassGuid { get; } = new Guid("39C928FE-721D-4BB9-B7F4-995F631923AF");
        public override Guid Guid => ClassGuid;

        public LaboratoryBuilding(CityBase city, IInteriorBuildingConstants constants)
            : base(city, constants ?? new LaboratoryBuildingConstants())
        {
        }
    }

    public class LaboratoryBuildingProductionFactory : IInteriorBuildingProductionFactory
    {
        public static LaboratoryBuildingProductionFactory Instance => _instance.Value;
        private static Lazy<LaboratoryBuildingProductionFactory> _instance
            = new Lazy<LaboratoryBuildingProductionFactory>(() => new LaboratoryBuildingProductionFactory());
        private LaboratoryBuildingProductionFactory()
        {
        }
        public Guid Guid => LaboratoryBuilding.ClassGuid;
        public Type ProductionResultType => typeof(LaboratoryBuilding);
        public IInteriorBuildingConstants Constants { get; } = new LaboratoryBuildingConstants();
        public Production Create(Player owner)
        {
            return new InteriorBuildingProduction(this, owner, 5, 2, 5, 2);
        }
        public bool IsPlacable(InteriorBuildingProduction production, CityBase city)
        {
            return true;
        }
        public InteriorBuilding CreateInteriorBuilding(CityBase city)
        {
            return new LaboratoryBuilding(city, Constants);
        }
    }
}
