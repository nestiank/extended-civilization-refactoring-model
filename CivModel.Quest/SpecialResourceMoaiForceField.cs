using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Quests
{
    public class SpecialResourceMoaiForceField : ISpecialResource
    {
        public static SpecialResourceMoaiForceField Instance => _instance.Value;
        private static Lazy<SpecialResourceMoaiForceField> _instance
            = new Lazy<SpecialResourceMoaiForceField>(() => new SpecialResourceMoaiForceField());
        private SpecialResourceMoaiForceField()
        {
        }

        public int MaxCount => 1;

        public object EnablePlayer(Player player)
        {
            return new DataObject(player);
        }

        private class DataObject : ITurnObserver, ITileObjectObserver
        {
            private Player _player;

            public DataObject(Player player)
            {
                _player = player;

                player.Game.TurnObservable.AddObserver(this, ObserverPriority.Model);
                player.Game.TileObjectObservable.AddObserver(this, ObserverPriority.Model);
            }

            public void PostTurn()
            {
                // TODO
            }

            public void TileObjectProduced(TileObject obj)
            {
                if (_player.SpecialResource[SpecialResourceMoaiForceField.Instance] < 1)
                    return;

                if (obj is TileBuilding)
                {
                   if(((TileBuilding)obj).Owner.Team == _player.Team)
                    {
                        if(obj is Hwan.HwanEmpireFIRFortress)
                        {
                            ((Hwan.HwanEmpireFIRFortress)obj).IsForceFieldOn = true;
                        }

                        else if(obj is Zap.FIRFortress)
                        {
                            ((Zap.FIRFortress)obj).IsForceFieldOn = true;
                        }
                    }
                }
            }
            public void TileObjectPlaced(TileObject obj) { }

            public void PreTurn() { }
            public void AfterPreTurn() { }
            public void AfterPostTurn() { }
            public void PreSubTurn(Player playerInTurn) { }
            public void AfterPreSubTurn(Player playerInTurn) { }
            public void PostSubTurn(Player playerInTurn) { }
            public void AfterPostSubTurn(Player playerInTurn) { }
        }
    }
}
