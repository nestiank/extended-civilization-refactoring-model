using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Quests
{
    public class AutismBeamAmplificationCrystal : ISpecialResource
    {
        public static AutismBeamAmplificationCrystal Instance => _instance.Value;
        private static Lazy<AutismBeamAmplificationCrystal> _instance
            = new Lazy<AutismBeamAmplificationCrystal>(() => new AutismBeamAmplificationCrystal());

        private AutismBeamAmplificationCrystal() { }

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

                player.Game.TurnObservable.AddObserver(this);
            }

            public void PostTurn() { }
            public void PostPlayerSubTurn(Player playerInTurn) { }
            public void PrePlayerSubTurn(Player playerInTurn) { }
            public void PreTurn() { }
        }
    }
}
