using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Quests
{
    public class SpecialResourceAirspaceDomination : ISpecialResource
    {
        public static SpecialResourceAirspaceDomination Instance => _instance.Value;
        private static Lazy<SpecialResourceAirspaceDomination> _instance
            = new Lazy<SpecialResourceAirspaceDomination>(() => new SpecialResourceAirspaceDomination());
        private SpecialResourceAirspaceDomination()
        {
        }

        public int MaxCount => 1;

        public object EnablePlayer(Player player)
        {
            return new DataObject(player);
        }

        private class DataObject : ITurnObserver
        {
            private Player _player;

            public DataObject(Player player)
            {
                _player = player;

                player.Game.TurnObservable.AddObserver(this, ObserverPriority.Model);
            }

            public void BeforePostTurn()
            {
                // TODO
            }

            public void PreTurn() { }
            public void AfterPreTurn() { }
            public void PostTurn() { }
            public void PreSubTurn(Player playerInTurn) { }
            public void AfterPreSubTurn(Player playerInTurn) { }
            public void PostSubTurn(Player playerInTurn) { }
            public void BeforePostSubTurn(Player playerInTurn) { }
        }
    }
}
