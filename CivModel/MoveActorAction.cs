using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel
{
    public class MoveActorAction : IActorAction
    {
        private readonly Actor _owner;
        public Actor Owner => _owner;

        public bool IsParametered => true;

        public MoveActorAction(Actor owner)
        {
            _owner = owner;
        }

        public int GetRequiredAP(Terrain.Point? pt)
        {
            if (pt is Terrain.Point target && _owner.PlacedPoint is Terrain.Point origin)
            {
                if (target.Unit == null)
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

            _owner.ConsumeAP(requiredAP);
            _owner.PlacedPoint = pt;
        }
    }
}
