using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel
{
    public class AttackActorAction : IActorAction
    {
        private readonly Actor _owner;
        public Actor Owner => _owner;

        public bool IsParametered => true;

        private readonly bool _isMoving;

        public AttackActorAction(Actor owner, bool isMoving)
        {
            _owner = owner ?? throw new ArgumentNullException("owner");
            _isMoving = isMoving;
        }

        public int GetRequiredAP(Terrain.Point? pt)
        {
            if (pt is Terrain.Point target && _owner.PlacedPoint is Terrain.Point origin)
            {
                Actor targetObject = GetTargetObject(target);

                if (targetObject != null && targetObject.Owner != _owner.Owner)
                {
                    if (Position.Distance(origin.Position, target.Position) == 1)
                        return _owner.GetRequiredAPToMove(target);
                }
            }

            return -1;
        }

        public void Act(Terrain.Point? pt)
        {
            int requiredAP = GetRequiredAP(pt);

            if (requiredAP == -1 || !_owner.CanConsumeAP(requiredAP))
                throw new ArgumentException("parameter is invalid");
            if (!_owner.PlacedPoint.HasValue)
                throw new InvalidOperationException("Actor is not placed yet");

            Actor targetObject = GetTargetObject(pt.Value);

            _owner.ConsumeAP(requiredAP);
            var result = _owner.AttackTo(targetObject);
            _owner.ConsumeAllAP();

            if (_isMoving && result == BattleResult.Victory)
            {
                if (pt.Value.Unit == null)
                {
                    _owner.PlacedPoint = pt;
                }
            }
        }

        private Actor GetTargetObject(Terrain.Point target)
        {
            if (target.TileBuilding != null && target.TileBuilding.RemainHP > 0)
               return target.TileBuilding;
            else
                return target.Unit;
        }
    }
}
