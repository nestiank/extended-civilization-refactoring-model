using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CivModel.Common;

namespace CivModel
{
    /// <summary>
    /// Represents a building which must be built in <see cref="Common.CityCenter"/>.
    /// </summary>
    /// <seealso cref="TileBuilding"/>
    public abstract class InteriorBuilding : ITurnObserver, IGuidTaggedObject
    {
        /// <summary>
        /// The unique identifier of this class.
        /// </summary>
        public abstract Guid Guid { get; }

        /// <summary>
        /// The <see cref="Player"/> who owns this building.
        /// </summary>
        public Player Owner => _owner;
        private Player _owner;

        /// <summary>
        /// The <see cref="CityCenter"/> where this building is.
        /// </summary>
        public CityCenter City
        {
            get => _city;
            set
            {
                if (value.Owner != Owner)
                    throw new ArgumentException("the owner of city is different from the owner of building.", "City");

                if (_city != null)
                {
                    _city.RemoveBuilding(this);
                    _city = null;
                }
                if (value != null)
                {
                    value.AddBuilding(this);
                    _city = value;
                }
            }
        }
        private CityCenter _city = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteriorBuilding"/> class.
        /// </summary>
        /// <param name="owner">The <see cref="Player"/> who will own the building.</param>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/> is <c>null</c>.</exception>
        public InteriorBuilding(Player owner)
        {
            _owner = owner ?? throw new ArgumentNullException("city");
        }

        /// <summary>
        /// Called after a sub turn.
        /// </summary>
        /// <param name="playerInTurn">The player which the sub turn is dedicated to.</param>
        public virtual void PostPlayerSubTurn(Player playerInTurn)
        {
        }

        /// <summary>
        /// Called after a turn.
        /// </summary>
        public virtual void PostTurn()
        {
        }

        /// <summary>
        /// Called before a sub turn.
        /// </summary>
        /// <param name="playerInTurn">The player which the sub turn is dedicated to.</param>
        public virtual void PrePlayerSubTurn(Player playerInTurn)
        {
        }

        /// <summary>
        /// Called before a turn.
        /// </summary>
        public virtual void PreTurn()
        {
        }
    }
}