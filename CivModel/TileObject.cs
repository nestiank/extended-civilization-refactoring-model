using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel
{
    /// <summary>
    /// The value indicating the kind of <see cref="TileObject"/>.
    /// </summary>
    public enum TileTag
    {
        /// <summary>
        /// Tag for <see cref="Unit"/> object
        /// </summary>
        Unit,
        /// <summary>
        /// Tag for <see cref="TileBuilding"/> object
        /// </summary>
        TileBuilding
    }

    /// <summary>
    /// Represents an object which can be placed on <see cref="Terrain.Point"/>.
    /// </summary>
    public abstract class TileObject : IGuidTaggedObject
    {
        /// <summary>
        /// The unique identifier of this class.
        /// </summary>
        public abstract Guid Guid { get; }

        /// <summary>
        /// The <see cref="Game"/> object
        /// </summary>
        public Game Game => _game;
        private readonly Game _game;

        /// <summary>
        /// The value indicating the kind of this object.
        /// </summary>
        public TileTag TileTag => _tileTag;
        private readonly TileTag _tileTag;

        /// <summary>
        /// The placed point of this object. <c>null</c> if not placed.
        /// </summary>
        public Terrain.Point? PlacedPoint
        {
            get => _placedPoint;
            set
            {
                if (value != _placedPoint)
                {
                    var oldPoint = _placedPoint;
                    SetPlacedPoint(value);
                    OnChangePlacedPoint(oldPoint);

                    Game.IterateTileObjectObserver(obj => obj.TileObjectPlaced(this));
                }
            }
        }
        private Terrain.Point? _placedPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="TileObject"/> class.
        /// </summary>
        /// <param name="game">The <see cref="CivModel.Game"/> object.</param>
        /// <param name="point">The tile where the object will be.</param>
        /// <param name="tileTag">The <see cref="TileTag"/> of the object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="game"/> is <c>null</c>.</exception>
        public TileObject(Game game, Terrain.Point point, TileTag tileTag)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _tileTag = tileTag;

            SetPlacedPoint(point);

            Game.IterateTileObjectObserver(obj => obj.TileObjectCreated(this));
            Game.IterateTileObjectObserver(obj => obj.TileObjectPlaced(this));
        }

        private void SetPlacedPoint(Terrain.Point? value)
        {
            if (_placedPoint.HasValue)
            {
                var p = _placedPoint.Value;
                _placedPoint = null;
                p.Terrain.UnplaceObject(this, p);
            }

            _placedPoint = value;
            if (value.HasValue)
            {
                value.Value.Terrain.PlaceObject(this);
            }
        }

        /// <summary>
        /// Process the logic to do at the creation of this actor.
        /// This method should not be called when this <see cref="Actor"/> object is created by loading a save file.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="ProcessCreation"/> has already been called</exception>
        /// <remarks>
        /// If <see cref="Actor"/> is newly created in game logic, such as <see cref="Production"/>, the creator should call this method.
        /// </remarks>
        /// <seealso cref="OnProcessCreation"/>
        public void ProcessCreation()
        {
            if (_processCreationAlreadyCalled)
                throw new InvalidOperationException("ProcessCreation has already been called");

            _processCreationAlreadyCalled = true;
            OnProcessCreation();
        }

        private bool _processCreationAlreadyCalled = false;

        /// <summary>
        /// Called when <see cref="ProcessCreation"/> is called.
        /// This method is not called when this <see cref="Actor"/> object is created by loading a save file.
        /// </summary>
        /// <seealso cref="ProcessCreation"/>
        protected virtual void OnProcessCreation()
        {
        }

        /// <summary>
        /// Called after <see cref="PlacedPoint"/> is changed.
        /// </summary>
        /// <param name="oldPoint">The old value of <see cref="PlacedPoint"/>.</param>
        protected virtual void OnChangePlacedPoint(Terrain.Point? oldPoint)
        {
        }
    }
}
