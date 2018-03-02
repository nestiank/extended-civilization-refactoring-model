using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel.Quests
{
    public class QuestPorjectCthulhu : Quest, ITileObjectObserver
    {
        public override string Name => "첩보 - 크툴루 계획";

        public override int PostingTurn => 5;
        public override int LimitTurn => 5;

        public override string GoalNotice => "[핀란드 제국] 영토내로 [스파이] 유닛을 옮겨 특수 스킬 [정보 수집]을 사용하세요.";
        public override string RewardNotice => " [특수 자원 : 크툴루 계획 기밀 정보] 1 획득";
        public override string CompleteNotice => @"이제 핀란드 제국은 멸망 직전까지 몰렸습니다. 그들이 모든 것을 걸었던 오티즘 빔 계획은 결국 전략적인 이점을 확보하는데는 실패하였고, 오히려 반사 어레이에 의해 핀란드 최고 사령관들 전부가 오티즘에 걸려버리게 되었습니다. 그런데도 불구하고 항복을 하지 않고 있다는 사실에 환국 사령부는 의문을 가지고 있었습니다. 이를 알아내기 위해 환국 첩보부는 작전명: Gunbangjo를 실행하고, 비록 많은 스파이들이 죽었으나 핀란드의 사악한 계획의 실체를 알게 됩니다......";

        public QuestPorjectCthulhu(Player requestee) : base(null, requestee)
        {
        }

        protected override void OnAccept()
        {
            Game.TurnObservable.AddObserver(this);
            Game.TileObjectObservable.AddObserver(this);
        }

        private void Cleanup()
        {
            Game.TurnObservable.RemoveObserver(this);
            Game.TileObjectObservable.RemoveObserver(this);
        }

        protected override void OnGiveup()
        {
            Cleanup();
        }

        protected override void OnComplete()
        {
            Requestee.SpecialResource[SpecialResourceCthulhuProjectInfo.Instance] = 1;
            foreach (var TheQuest in Requestee.Quests)
            {
                if (TheQuest is QuestEgyptKingdom)
                {
                    TheQuest.Status = QuestStatus.Deployed;
                }
            }

            Cleanup();
        }

        public void TileObjectCreated(TileObject obj) { }

        public void TileObjectPlaced(TileObject obj)
        {
            if (obj is CivModel.Hwan.Spy Spy && Spy.Owner == Requestee && Spy.PlacedPoint.Value.TileOwner != Requestee)
            {
                if (Spy.QuestFlag)
                {
                    Status = QuestStatus.Completed;
                }
            }
        }

    }
}