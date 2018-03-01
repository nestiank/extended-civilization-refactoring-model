using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Finno
{
    public class Necronomicon : ISpecialResource
    {
        public static Necronomicon Instance => _instance.Value;
        private static Lazy<Necronomicon> _instance
            = new Lazy<Necronomicon>(() => new Necronomicon());

        private Necronomicon() { }

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
