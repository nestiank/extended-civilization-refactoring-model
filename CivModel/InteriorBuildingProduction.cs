using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel
{
    /// <summary>
    /// The factory interface of <see cref="InteriorBuildingProduction"/>.
    /// This interface additionally provides <see cref="IsPlacable(InteriorBuildingProduction, CityBase)"/>
    ///  and <see cref="CreateInteriorBuilding(CityBase)"/> methods.
    /// </summary>
    /// <seealso cref="CivModel.IProductionFactory" />
    public interface IInteriorBuildingProductionFactory : IProductionFactory
    {
        /// <summary>
        /// Determines whether the production result is placable in the specified city.
        /// </summary>
        /// <param name="production">The production.</param>
        /// <param name="city">The city to test to place the production result.</param>
        /// <returns>
        ///   <c>true</c> if the production is placable; otherwise, <c>false</c>.
        /// </returns>
        bool IsPlacable(InteriorBuildingProduction production, CityBase city);

        /// <summary>
        /// Creates the <see cref="InteriorBuilding"/> which is the production result.
        /// </summary>
        /// <param name="city">The <see cref="CityBase"/> who will own the building.</param>
        /// <returns>the created <see cref="InteriorBuilding"/> result.</returns>
        InteriorBuilding CreateInteriorBuilding(CityBase city);
    }

    /// <summary>
    /// The <see cref="Production"/> class for <see cref="InteriorBuilding"/>
    /// </summary>
    /// <seealso cref="CivModel.Production" />
    public class InteriorBuildingProduction : Production
    {
        private readonly IInteriorBuildingProductionFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteriorBuildingProduction"/> class.
        /// </summary>
        /// <param name="factory">The factory object of this production kind.</param>
        /// <param name="owner">The <see cref="Player"/> who will own the production.</param>
        /// <exception cref="ArgumentException">
        /// TotalLaborCost is negative
        /// or
        /// TotalGoldCost is negative
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// LaborCapacityPerTurn is not in [0, TotalLaborCost]
        /// or
        /// GoldCapacityPerTurn is not in [0, TotalGoldCost]
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> is <c>null</c>
        /// or
        /// <paramref name="owner"/> is <c>null</c>
        /// </exception>
        public InteriorBuildingProduction(IInteriorBuildingProductionFactory factory, Player owner) : base(factory, owner)
        {
            _factory = factory;
        }

        /// <summary>
        /// Determines whether the production result is placable at the specified point.
        /// </summary>
        /// <param name="point">The point to test to place the production result.</param>
        /// <returns>
        ///   <c>true</c> if the production is placable; otherwise, <c>false</c>.
        /// </returns>
        protected override bool CoreIsPlacable(Terrain.Point point)
        {
            if (point.TileBuilding is CityBase city && city.Owner == Owner)
                return _factory.IsPlacable(this, city);
            return false;
        }

        /// <summary>
        /// Places the production result at the specified point.
        /// </summary>
        /// <param name="point">The point to place the production result.</param>
        /// <exception cref="InvalidOperationException">production is not completed yet</exception>
        /// <exception cref="ArgumentException">point is invalid</exception>
        /// <returns>The production result.</returns>
        protected override IProductionResult CorePlace(Terrain.Point point)
        {
            if (!IsCompleted)
                throw new InvalidOperationException("production is not completed yet");
            if (!IsPlacable(point))
                throw new ArgumentException("point is invalid");

            return _factory.CreateInteriorBuilding((CityBase)point.TileBuilding);
        }
    }
}
