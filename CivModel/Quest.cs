using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel
{
    /// <summary>
    /// The status of <see cref="Quest"/>.
    /// </summary>
    /// <seealso cref="Quest.Status"/>
    public enum QuestStatus
    {
        /// <summary>
        /// <see cref="Quest"/> is disabled.
        /// </summary>
        Disabled,
        /// <summary>
        /// <see cref="Quest"/> is deployed.
        /// </summary>
        Deployed,
        /// <summary>
        /// <see cref="Quest"/> is accepted.
        /// </summary>
        Accepted,
        /// <summary>
        /// <see cref="Quest"/> is completed.
        /// </summary>
        Completed,
    }

    /// <summary>
    /// Represents a quest.
    /// </summary>
    public abstract class Quest : ITurnObserver
    {
        /// <summary>
        /// The requester of this quest. <c>null</c> if not exists.
        /// </summary>
        public Player Requester => Requester;
        private readonly Player _requester;

        /// <summary>
        /// The requestee of this quest.
        /// </summary>
        public Player Requestee => _requestee;
        private readonly Player _requestee;

        /// <summary>
        /// The <see cref="Game"/> object.
        /// </summary>
        public Game Game => Requestee.Game;

        /// <summary>
        /// [퀘스트 이름].
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// [퀘스트 게시 기간]. <c>-1</c> if forever.
        /// </summary>
        public abstract int PostingTurn { get; }

        /// <summary>
        /// [퀘스트 제한 기간]. <c>-1</c> if forever.
        /// </summary>
        public abstract int LimitTurn { get; }

        /// <summary>
        /// [퀘스트 조건].
        /// </summary>
        public abstract string GoalNotice { get; }

        /// <summary>
        /// [퀘스트 보상].
        /// </summary>
        public abstract string RewardNotice { get; }

        /// <summary>
        /// [교육용 알림].
        /// </summary>
        public abstract string CompleteNotice { get; }

        /// <summary>
        /// The left turn. <c>-1</c> if this value is invalid.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value become 0 and <c><see cref="Status"/> == <see cref="QuestStatus.Accepted"/></c>,
        ///  <see cref="Status"/> becomes <see cref="QuestStatus.Deployed"/>.<br/>
        /// If this value become 0 and <c><see cref="Status"/> == <see cref="QuestStatus.Deployed"/></c>,
        ///  <see cref="Status"/> becomes <see cref="QuestStatus.Disabled"/>.
        /// </para>
        /// <para>
        /// This value is invalid iff
        ///  <c><see cref="Status"/> == <see cref="QuestStatus.Accepted"/> || <see cref="Status"/> == <see cref="QuestStatus.Deployed"/></c>.
        /// </para>
        /// </remarks>
        public int LeftTurn { get; private set; } = -1;

        /// <summary>
        /// <see cref="QuestStatus"/> of this quest.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// cannot mark <see cref="QuestStatus.Completed"/> quest as <see cref="QuestStatus.Disabled"/>
        /// or
        /// cannot mark <see cref="QuestStatus.Completed"/> quest as <see cref="QuestStatus.Deployed"/>
        /// or
        /// cannot mark <see cref="QuestStatus.Disabled"/> quest as <see cref="QuestStatus.Accepted"/>
        /// or
        /// cannot mark <see cref="QuestStatus.Completed"/> quest as <see cref="QuestStatus.Accepted"/>
        /// or
        /// cannot mark <see cref="QuestStatus.Disabled"/> quest as <see cref="QuestStatus.Completed"/>
        /// or
        /// cannot mark <see cref="QuestStatus.Deployed"/> quest as <see cref="QuestStatus.Completed"/>
        /// </exception>
        public QuestStatus Status
        {
            get => _status;
            set
            {
                switch (value)
                {
                    case QuestStatus.Disabled:
                        Disable();
                        break;

                    case QuestStatus.Deployed:
                        Deploy();
                        break;

                    case QuestStatus.Accepted:
                        Accept();
                        break;

                    case QuestStatus.Completed:
                        Complete();
                        break;
                }
            }
        }
        private QuestStatus _status = QuestStatus.Disabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="Quest"/> class.
        /// </summary>
        /// <param name="requester">The requester of this quest. <c>null</c> if not exists.</param>
        /// <param name="requestee">The requestee of this quest.</param>
        /// <exception cref="ArgumentNullException"><paramref name="requestee"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="requester"/> and <paramref name="requestee"/> do not involve in the same game</exception>
        public Quest(Player requester, Player requestee)
        {
            _requester = requester;
            _requestee = requestee ?? throw new ArgumentNullException(nameof(requestee));

            if (requester != null && requester.Game != requestee.Game)
                throw new ArgumentException("requester and requestee do not involve in the same game");

            requestee.AddQuestToList(this);

            Game.TurnObservable.AddObserver(this);
        }

        /// <summary>
        /// set <see cref="Status"/> to <see cref="QuestStatus.Disabled"/>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">cannot mark <see cref="QuestStatus.Completed"/> quest as <see cref="QuestStatus.Disabled"/></exception>
        /// <seealso cref="Status"/>
        public void Disable()
        {
            var prev = _status;

            if (prev == QuestStatus.Disabled)
                return;
            if (prev == QuestStatus.Completed)
                throw new InvalidOperationException("cannot mark completed quest as disabled");

            _status = QuestStatus.Disabled;
            LeftTurn = -1;

            if (prev == QuestStatus.Accepted)
            {
                OnGiveup();
            }
        }

        /// <summary>
        /// set <see cref="Status"/> to <see cref="QuestStatus.Deployed"/>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">cannot mark <see cref="QuestStatus.Completed"/> quest as <see cref="QuestStatus.Deployed"/></exception>
        /// <seealso cref="Status"/>
        public void Deploy()
        {
            var prev = _status;

            if (prev == QuestStatus.Deployed)
                return;
            if (prev == QuestStatus.Completed)
                throw new InvalidOperationException("cannot mark completed quest as deployed");

            _status = QuestStatus.Deployed;
            LeftTurn = PostingTurn;

            if (prev == QuestStatus.Accepted)
            {
                OnGiveup();
            }
        }

        /// <summary>
        /// set <see cref="Status"/> to <see cref="QuestStatus.Accepted"/>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// cannot mark <see cref="QuestStatus.Disabled"/> quest as <see cref="QuestStatus.Accepted"/>
        /// or
        /// cannot mark <see cref="QuestStatus.Completed"/> quest as <see cref="QuestStatus.Accepted"/>
        /// </exception>
        /// <seealso cref="Status"/>
        public void Accept()
        {
            var prev = _status;

            if (prev == QuestStatus.Accepted)
                return;
            if (prev == QuestStatus.Disabled)
                throw new InvalidOperationException("cannot mark disabled quest as accepted");
            if (prev == QuestStatus.Completed)
                throw new InvalidOperationException("cannot mark completed quest as accepted");

            _status = QuestStatus.Accepted;
            LeftTurn = LimitTurn;

            OnAccept();
        }

        /// <summary>
        /// set <see cref="Status"/> to <see cref="QuestStatus.Completed"/>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// cannot mark <see cref="QuestStatus.Disabled"/> quest as <see cref="QuestStatus.Completed"/>
        /// or
        /// cannot mark <see cref="QuestStatus.Deployed"/> quest as <see cref="QuestStatus.Completed"/>
        /// </exception>
        /// <seealso cref="Status"/>
        public void Complete()
        {
            var prev = _status;

            if (prev == QuestStatus.Completed)
                return;
            if (prev == QuestStatus.Disabled)
                throw new InvalidOperationException("cannot mark disabled quest as completed");
            if (prev == QuestStatus.Deployed)
                throw new InvalidOperationException("cannot mark deployed quest as copmleted");

            _status = QuestStatus.Completed;
            LeftTurn = -1;

            OnComplete();
        }

        /// <summary>
        /// Called before a turn.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void PreTurn()
        {
        }

        /// <summary>
        /// Called after a turn.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void PostTurn()
        {
            if (LeftTurn == 0)
            {
                if (Status == QuestStatus.Accepted)
                    Deploy();
                else if (Status == QuestStatus.Deployed)
                    Disable();
            }
            else if (LeftTurn > 0)
            {
                --LeftTurn;
            }
        }

        /// <summary>
        /// Called before a sub turn.
        /// </summary>
        /// <param name="playerInTurn">The player which the sub turn is dedicated to.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void PrePlayerSubTurn(Player playerInTurn)
        {
        }

        /// <summary>
        /// Called after a sub turn.
        /// </summary>
        /// <param name="playerInTurn">The player which the sub turn is dedicated to.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void PostPlayerSubTurn(Player playerInTurn)
        {
        }

        /// <summary>
        /// Called when [accept].
        /// </summary>
        protected abstract void OnAccept();

        /// <summary>
        /// Called when [give up].
        /// </summary>
        protected abstract void OnGiveup();

        /// <summary>
        /// Called when [complete].
        /// </summary>
        protected abstract void OnComplete();
    }
}