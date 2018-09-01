using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static CivModel.Finno.FinnoPlayerNumber;
using static CivModel.Hwan.HwanPlayerNumber;

namespace CivModel.Quests
{
    public class QuestConquerVictory : Quest, ITurnObserver
    {
        private const string Enemy = "enemy";

        public QuestConquerVictory(Player requestee)
            : base(null, requestee, typeof(QuestConquerVictory))
        {
        }

        public override void OnQuestDeployTime()
        {
            if (!Requestee.HasEnding)
            {
                Deploy();
                Accept();
            }
        }

        protected override void OnAccept()
        {
        }

        protected override void OnComplete()
        {
        }

        protected override void OnGiveup()
        {
        }

        public void AfterPostSubTurn(Player playerInTurn)
        {
            if (playerInTurn != Requestee)
                return;

            var enemy = Requestee.Team == 0 ? Game.GetPlayerFinno() : Game.GetPlayerHwan();

            if (Requestee.IsEliminated)
            {
                Requestee.AchieveEnding(new EliminationDefeat(Game));
                Complete();
            }
            else if (enemy.IsEliminated)
            {
                Requestee.AchieveEnding(new ConquerVictory(Game));
                Complete();
            }
        }

        public void PreTurn() { }
        public void AfterPreTurn() { }
        public void PostTurn() { }
        public void AfterPostTurn() { }
        public void PreSubTurn(Player playerInTurn) { }
        public void AfterPreSubTurn(Player playerInTurn) { }
        public void PostSubTurn(Player playerInTurn) { }
    }
}
