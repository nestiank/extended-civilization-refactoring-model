using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Common
{
    public class TestQuest : Quest, IBattleObserver
    {
        public override string Name => "개꿀잼 퀘스트";

        public override int PostingTurn => -1;
        public override int LimitTurn => -1;

        public override string GoalNotice => "제다이 기사로 적을 처치하십시오";
        public override string RewardNotice => "배치 가능한 제다이 기사 생산 6개";
        public override string CompleteNotice => "개꿀잼 ㅇㅈ ㅆㅇㅈ";

        public TestQuest(Player requestee) : base(null, requestee)
        {
        }

        protected override void OnAccept()
        {
            Game.BattleObservable.AddObserver(this);
        }

        private void Cleanup()
        {
            Game.BattleObservable.RemoveObserver(this);
        }

        protected override void OnGiveup()
        {
            Cleanup();
        }

        protected override void OnComplete()
        {
            for (int i = 0; i < 6; ++i)
            {
                var production = JediKnightProductionFactory.Instance.Create(Requestee);
                production.IsCompleted = true;
                Requestee.Deployment.AddLast(production);
            }

            Cleanup();
        }

        public void OnBattle(Actor attacker, Actor defender, BattleResult result)
        {
            if (attacker.Owner == Requestee && attacker is JediKnight)
            {
                Status = QuestStatus.Completed;
            }
        }
    }
}