using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CivModel;

namespace CivPresenter
{
    /// <summary>
    /// Represents a presenter.
    /// </summary>
    /// <remarks>
    /// The presenter works like a Finite State Machine.
    /// <see cref="Presenter.State"/> is changed by Command~~~ operations.
    /// </remarks>
    public partial class Presenter
    {
        /// <summary>
        /// The <see cref="IView"/> object
        /// </summary>
        public IView View => _view;
        private readonly IView _view;

        /// <summary>
        /// The <see cref="Game"/> object
        /// </summary>
        public Game Game => _game;
        private readonly Game _game;

        /// <summary>
        /// The selected <see cref="Actor"/>.
        /// </summary>
        public Actor SelectedActor => _selectedActor?.PlacedPoint != null ? _selectedActor : null;
        private Actor _selectedActor;

        /// <summary>
        /// The focused <see cref="Terrain.Point"/>.
        /// This point can be changed by [arrow key] command, or View's calling setter.
        /// </summary>
        public Terrain.Point FocusedPoint { get; set; }

        private Actor[] _standbyActors = null;
        private int _standbyActorIndex = -1;

        /// <summary>
        /// Whether there is something to do in this turn.
        /// If this value is <c>false</c>, user can go to the next turn
        /// </summary>
        public bool IsThereTodos { get; private set; }

        /// <summary>
        /// The <see cref="IReadOnlyActorAction"/> object used now.
        /// <c>null</c> if no action is being done.
        /// </summary>
        public IReadOnlyActorAction RunningAction { get; private set; }

        /// <summary>
        /// Index of the selected investment.
        /// If <see cref="SelectedProduction"/> is not <c>-1</c>, this value is <c>-1</c>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductUI"/></c>.
        /// See remarks section for information about the value.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///  <item><c>0</c> if <see cref="Player.TaxRate"/> is selected.</item>
        ///  <item><c>1</c> if <see cref="Player.EconomicInvestmentRatio"/> is selected.</item>
        ///  <item><c>2</c> if <see cref="Player.ResearchInvestmentRatio"/> is selected.</item>
        ///  <item><c>3</c> if <see cref="Player.RepairInvestmentRatio"/> is selected.</item>
        ///  <item><c>-1</c> if there is no selected deploy.</item>
        /// </list>
        /// </remarks>
        public int SelectedInvestment { get; private set; } = -1;
        private const int _selectedInvestmentCount = 4;

        /// <summary>
        /// Index of the selected deploy to <see cref="Player.Deployment"/> list.
        /// <c>-1</c> if there is no selected deploy.
        /// If <see cref="SelectedProduction"/> or <see cref="SelectedInvestment"/> is not <c>-1</c>, this value is <c>-1</c>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductUI"/></c>.
        /// </summary>
        public int SelectedDeploy { get; private set; } = -1;

        /// <summary>
        /// Index of the selected production to <see cref="Player.Production"/> list.
        /// <c>-1</c> if there is no selected production.
        /// If <see cref="SelectedDeploy"/> or <see cref="SelectedInvestment"/> is not <c>-1</c>, this value is <c>-1</c>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductUI"/> || <see cref="State"/> == <see cref="States.ProductAdd"/></c>.
        /// </summary>
        public int SelectedProduction { get; private set; } = -1;

        /// <summary>
        /// Whether user is manipulating a production.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductUI"/></c>
        /// </summary>
        public bool IsProductManipulating { get; private set; } = false;

        /// <summary>
        /// The list of the available production, retrieved by <see cref="Player.AvailableProduction"/>
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductAdd"/></c>
        /// </summary>
        public IReadOnlyList<IProductionFactory> AvailableProduction { get; private set; }

        /// <summary>
        /// The production to deploy.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.Deploy"/></c>
        /// </summary>
        public Production DeployProduction { get; private set; }

        /// <summary>
        /// The state of <see cref="Presenter"/>.
        /// </summary>
        public States State { get; private set; }

        /// <summary>
        /// The parameter of this <see cref="State"/>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.SpecialAct"/></c>,
        /// and the value is the number of a special action.
        /// </summary>
        public int StateParam { get; private set; } = -1;

        /// <summary>
        /// The list of <see cref="Quest"/> of <see cref="Game.PlayerInTurn"/> whose status is <see cref="QuestStatus.Accepted"/>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.Quest"/></c>.
        /// </summary>
        public IReadOnlyList<Quest> AcceptedQuests => _acceptedQuests;
        private List<Quest> _acceptedQuests;
        /// <summary>
        /// The list of <see cref="Quest"/> of <see cref="Game.PlayerInTurn"/> whose status is <see cref="QuestStatus.Deployed"/>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.Quest"/></c>.
        /// </summary>
        public IReadOnlyList<Quest> DeployedQuests => _deployedQuests;
        private List<Quest> _deployedQuests;
        /// <summary>
        /// The list of <see cref="Quest"/> of <see cref="Game.PlayerInTurn"/> whose status is <see cref="QuestStatus.Completed"/>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.Quest"/></c>.
        /// </summary>
        public IReadOnlyList<Quest> CompletedQuests => _completedQuests;
        private List<Quest> _completedQuests;
        /// <summary>
        /// The list of <see cref="Quest"/> of <see cref="Game.PlayerInTurn"/> whose status is <see cref="QuestStatus.Disabled"/>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.Quest"/></c>.
        /// </summary>
        public IReadOnlyList<Quest> DisabledQuests => _disabledQuests;
        private List<Quest> _disabledQuests;
        /// <summary>
        /// Index of the selected quest to <see cref="Player.Quests"/> list.
        /// <c>-1</c> if there is no selected quest.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.Quest"/></c>.
        /// </summary>
        public int SelectedQuest { get; private set; } = -1;
        private int _questsCount = -1;

        /// <summary>
        /// The selected city.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.CityView"/></c>.
        /// </summary>
        public CityBase SelectedCity { get; private set; }

        /// <summary>
        /// The path to move. If there is no vaild path, <c>null</c>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.PathFinding"/></c>
        /// </summary>
        public IMovePath MovePath { get; private set; }

        /// <summary>
        /// The reachable points from <see cref="SelectedActor"/> in its remained AP.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.Move"/> || <see cref="State"/> == <see cref="States.MovingAttack"/></c>
        /// </summary>
        public Terrain.Point[] ReachablePoints { get; private set; }

        private bool[] _endingNotified;

        /// <summary>
        /// The path of the save file.
        /// </summary>
        public string SaveFile { get; set; }

        private Action OnApply;
        private Action OnCancel;
        private Action<Direction> OnArrowKey;
        private Action<int> OnNumeric;
        private Action OnRemove;
        private Action OnSkip;
        private Action OnSleep;

        /// <summary>
        /// Initializes a new instance of the <see cref="Presenter"/> class, by creating a new game with testing-purpose parameters.
        /// </summary>
        /// <param name="view">The <see cref="IView"/> object.</param>
        /// <param name="prototypes">The path of prototype files.</param>
        /// <exception cref="ArgumentNullException"><paramref name="view"/> is <c>null</c></exception>
        /// <remarks>
        /// This constructor calls <see cref="Presenter(IView, string[], int, int, int)"/> constructor with preset testing-purpsoe parameters.
        /// </remarks>
        /// <seealso cref="Presenter(IView, string[], int, int, int)"/>
        public Presenter(IView view, string[] prototypes) : this(view, prototypes, -1, -1, -1) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Presenter"/> class, by creating a new game.
        /// </summary>
        /// <param name="view">The <see cref="IView"/> object.</param>
        /// <param name="prototypes">The path of prototype files.</param>
        /// <param name="terrainWidth"><see cref="Terrain.Width"/> of the new game. If this value is <c>-1</c>, uses default value.</param>
        /// <param name="terrainHeight"><see cref="Terrain.Height"/> of the new game. If this value is <c>-1</c>, uses default value.</param>
        /// <param name="numOfPlayer">The number of players of the new game. If this value is <c>-1</c>, uses default value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="view"/> is <c>null</c></exception>
        public Presenter(IView view, string[] prototypes, int terrainWidth, int terrainHeight, int numOfPlayer)
        {
            _view = view ?? throw new ArgumentNullException("view");
            SaveFile = null;

#if DEBUG_CORE
            var factory = new CivModel.FakeModule.GameSchemeFactory();
#else
            var factory = new CivModel.Quests.GameSchemeFactory();
#endif
            var knownFactory = new IGameSchemeFactory[] {
#if !DEBUG_CORE
                new CivModel.AI.GameSchemeFactory(),
                new CivModel.Finno.GameSchemeFactory(),
                new CivModel.Hwan.GameSchemeFactory(),
                new CivModel.Zap.GameSchemeFactory(),
#endif
            };

            var readers = new List<TextReader>();
            try
            {
                foreach (var proto in prototypes)
                {
                    readers.Add(File.OpenText(proto));
                }

                _game = new Game(terrainWidth, terrainHeight, numOfPlayer,
                    readers.ToArray(), factory, knownFactory);
            }
            finally
            {
                foreach (var r in readers)
                {
                    r.Dispose();
                }
            }

            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Presenter"/> class, by loading a existing save file.
        /// </summary>
        /// <param name="view">The <see cref="IView"/> object.</param>
        /// <param name="prototypes">The path of prototype files.</param>
        /// <param name="saveFile">The path of the save file to load. If <c>null</c>, create a new game.</param>
        /// <exception cref="ArgumentNullException"><paramref name="view"/> is <c>null</c></exception>
        /// <remarks>
        /// This constructor calls <see cref="Game.Game(string, TextReader[], IEnumerable{IGameSchemeFactory})"/> constructor.
        /// See the <strong>exceptions</strong> and <strong>remarks</strong> parts of
        /// the documentation of <see cref="Game.Game(string, TextReader[], IEnumerable{IGameSchemeFactory})"/> constructor.
        /// </remarks>
        /// <seealso cref="Game.Game(string, TextReader[], IEnumerable{IGameSchemeFactory})"/>
        public Presenter(IView view, string[] prototypes, string saveFile)
        {
            _view = view ?? throw new ArgumentNullException("view");
            SaveFile = saveFile ?? throw new ArgumentNullException("saveFile");

            var knownFactory = new IGameSchemeFactory[] {
#if DEBUG_CORE
                new CivModel.FakeModule.GameSchemeFactory(),
#else
                new CivModel.AI.GameSchemeFactory(),
                new CivModel.Finno.GameSchemeFactory(),
                new CivModel.Hwan.GameSchemeFactory(),
                new CivModel.Zap.GameSchemeFactory(),
                new CivModel.Quests.GameSchemeFactory(),
#endif
            };

            var readers = new List<TextReader>();
            try
            {
                foreach (var proto in prototypes)
                {
                    readers.Add(File.OpenText(proto));
                }

                _game = new Game(saveFile, readers.ToArray(), knownFactory);
            }
            finally
            {
                foreach (var r in readers)
                {
                    r.Dispose();
                }
            }


            Initialize();
        }

        private void Initialize()
        {
            _endingNotified = new bool[Game.Players.Count];

            // (0, 0) is fallback point
            // ProceedTurn() would set FocusedPoint if any actor exists.
            FocusedPoint = Game.Terrain.GetPoint(0, 0);
            ProceedTurn();

            StateNormal();
        }

        /// <summary>
        /// Gives the command [apply].
        /// </summary>
        public void CommandApply()
        {
            OnApply();
        }

        /// <summary>
        /// Gives the command [cancel].
        /// </summary>
        public void CommandCancel()
        {
            OnCancel();
        }

        /// <summary>
        /// Gives the command [arrow key].
        /// </summary>
        /// <param name="direction">The direction.</param>
        public void CommandArrowKey(Direction direction)
        {
            OnArrowKey(direction);
        }

        /// <summary>
        /// Gives the command [numeric].
        /// This method may introduce <see cref="States.SpecialAct"/> state
        ///  if called when <see cref="States.Normal"/> state.
        /// </summary>
        /// <param name="index">The index.</param>
        public void CommandNumeric(int index)
        {
            OnNumeric(index);
        }

        /// <summary>
        /// Gives the command [remove].
        /// </summary>
        public void CommandRemove()
        {
            OnRemove();
        }

        /// <summary>
        /// Gives the command [skip].
        /// </summary>
        public void CommandSkip()
        {
            OnSkip();
        }

        /// <summary>
        /// Gives the command [sleep].
        /// </summary>
        public void CommandSleep()
        {
            OnSleep();
        }

        /// <summary>
        /// Gives the command [refocus].
        /// </summary>
        public void CommandRefocus()
        {
            Refocus();
        }

        /// <summary>
        /// Gives the command [select].
        /// </summary>
        public void CommandSelect()
        {
            if (FocusedPoint.Unit is Unit unit && unit.Owner == Game.PlayerInTurn)
            {
                if (unit.IsControllable && unit != SelectedActor)
                {
                    SelectActor(unit);
                    return;
                }
            }
            if (FocusedPoint.TileBuilding is TileBuilding tb && tb.Owner == Game.PlayerInTurn)
            {
                if (tb.IsControllable && tb != SelectedActor)
                {
                    SelectActor(tb);
                    return;
                }
            }
        }

        /// <summary>
        /// Gives the command [save].
        /// </summary>
        public void CommandSave()
        {
            Game.Save(SaveFile);
        }

        /// <summary>
        /// Gives the command [move].
        /// This method may introduce <see cref="States.Move"/> state.
        /// </summary>
        public void CommandMove()
        {
            if (State == States.Normal)
                StateMove();
            else if (State == States.Move)
                OnCancel();
        }

        /// <summary>
        /// Gives the command [moving attack].
        /// This method may introduce <see cref="States.MovingAttack"/> state.
        /// </summary>
        public void CommandMovingAttack()
        {
            if (State == States.Normal)
                StateMovingAttack();
            else if (State == States.MovingAttack)
                OnCancel();
        }

        /// <summary>
        /// Gives the command [holding attack].
        /// This method may introduce <see cref="States.HoldingAttack"/> state.
        /// </summary>
        public void CommandHoldingAttack()
        {
            if (State == States.Normal)
                StateHoldingAttack();
            else if (State == States.HoldingAttack)
                OnCancel();
        }

        /// <summary>
        /// Gives the command [pillage].
        /// </summary>
        public void CommandPillage()
        {
            if (State == States.Normal)
            {
                if (SelectedActor?.PillageAct is IActorAction act && !act.IsParametered)
                {
                    DoUnparameteredAction(act);
                }
            }
        }

        /// <summary>
        /// Gives the command [product UI].
        /// This method may introduce <see cref="States.ProductUI"/> state.
        /// </summary>
        public void CommandProductUI()
        {
            if (State == States.Normal)
                StateProductUI();
            else if (State == States.ProductUI)
                OnCancel();
        }

        /// <summary>
        /// Gives the command [quest].
        /// This method may introduce <see cref="States.Quest"/> state.
        /// </summary>
        public void CommandQuest()
        {
            if (State == States.Normal)
                StateQuest();
            else if (State == States.Quest)
                OnCancel();
        }

        /// <summary>
        /// Gives the command [city view].
        /// This method may introduce <see cref="States.CityView"/> state.
        /// </summary>
        public void CommandCityView()
        {
            if (State == States.Normal && FocusedPoint.TileBuilding is CityBase city)
                StateCityView(city);
            else if (State == States.CityView)
                OnCancel();
        }

        /// <summary>
        /// Gives the command [path finding].
        /// This method may introduce <see cref="States.PathFinding"/> state.
        /// </summary>
        public void CommandPathFinding()
        {
            if (State == States.Normal && SelectedActor?.MoveAct != null)
                StatePathFinding();
            else if (State == States.PathFinding)
                OnCancel();
        }

        /// <summary>
        /// Gives the command [actor cancel].
        /// </summary>
        public void CommandActorCancel()
        {
            if (State == States.Normal)
            {
                if (SelectedActor?.MovePath != null)
                    SelectedActor.MovePath = null;
            }
        }

        private void ProceedTurn()
        {
            if (Game.IsInsideTurn)
                Game.EndTurn();
            Game.StartTurn();

            SelectNextActor();
            if (_selectedActor == null)
            {
                if (Game.PlayerInTurn.Cities.FirstOrDefault() is CityBase city)
                {
                    if (city.PlacedPoint is Terrain.Point pt)
                        FocusedPoint = pt;
                }
            }

            StateNormal();
        }

        private void SelectNextActor()
        {
            int tryNumber = (_standbyActorIndex == -1) ? 1 : 2;

            for (int j = 0; j < tryNumber; ++j)
            {
                if (_standbyActorIndex == -1)
                {
                    _standbyActors = Game.PlayerInTurn.Actors.ToArray();
                }

                int idx = _standbyActorIndex + 1;
                for (; idx < _standbyActors.Length; ++idx)
                {
                    var actor = _standbyActors[idx];
                    if (actor is Unit && actor.RemainAP > 0 && !actor.SkipFlag
                        && actor.IsControllable && actor.PlacedPoint.HasValue
                        && actor.MovePath == null)
                    {
                        _standbyActorIndex = idx;
                        _selectedActor = _standbyActors[idx];
                        IsThereTodos = true;
                        Refocus();
                        return;
                    }
                }

                _selectedActor = null;
                _standbyActorIndex = -1;
                IsThereTodos = false;
            }
        }

        private void SelectActor(Actor actor)
        {
            var actors = Game.PlayerInTurn.Actors.ToArray();
            int idx = Array.IndexOf(actors, actor);

            if (idx == -1)
                return;
            if (!actor.IsControllable)
                return;

            _selectedActor = actor;
            actor.SkipFlag = false;

            _standbyActors = actors;
            _standbyActorIndex = idx;
            IsThereTodos = true;
            Refocus();
        }

        private bool CheckEnding()
        {
            if (Game.PlayerInTurn.HasEnding)
            {
                StateNormal();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Refocus()
        {
            if (SelectedActor != null)
                FocusedPoint = SelectedActor.PlacedPoint.Value;
            View.Refocus();
        }

        private void MoveSight(Direction direction)
        {
            var pos = FocusedPoint.Position;
            switch (direction)
            {
                case Direction.Up:
                    pos.Y = Math.Max(pos.Y - 1, 0);
                    break;
                case Direction.Down:
                    pos.Y = Math.Min(pos.Y + 1, Game.Terrain.Height - 1);
                    break;
                case Direction.Left:
                    pos.X = pos.X - 1;
                    break;
                case Direction.Right:
                    pos.X = pos.X + 1;
                    break;
            }
            FocusedPoint = Game.Terrain.GetPoint(pos);
        }

        private void StateNormal()
        {
            if (Game.PlayerInTurn.HasEnding)
            {
                if (!_endingNotified[Game.PlayerNumberInTurn])
                {
                    StateEnding();

                    _endingNotified[Game.PlayerNumberInTurn] = true;
                    return;
                }
            }

            State = States.Normal;

            OnApply = () => {
                if (Game.PlayerInTurn.IsAIControlled)
                {
                    StateAIControl();
                }
                else if (!IsThereTodos)
                {
                    ProceedTurn();
                }
                else
                {
                    SelectNextActor();
                }
            };
            OnCancel = () => {
                if (SelectedActor != null)
                {
                    _selectedActor = null;
                }
                else
                {
                    //View.Shutdown();
                }
            };
            OnArrowKey = direction => {
                MoveSight(direction);
            };
            OnNumeric = index => {
                if (State != States.SpecialAct || StateParam != index)
                    StateSpeicalAct(index);
                else
                    OnCancel();
            };
            OnRemove = () => { };
            OnSkip = () => {
                if (SelectedActor != null)
                {
                    SelectedActor.SkipFlag = !SelectedActor.SkipFlag;
                    SelectNextActor();
                }
            };
            OnSleep = () => {
                if (SelectedActor != null)
                {
                    SelectedActor.SleepFlag = !SelectedActor.SleepFlag;
                    SelectNextActor();
                }
            };
        }

        private void StateMove()
        {
            if (SelectedActor?.MoveAct is IActorAction action)
            {
                State = States.Move;
                ReachablePoints = CivModel.Path.ActorMovePath.GetReachablePoint(SelectedActor, false);

                Action onFinished = () => {
                    ReachablePoints = null;
                    StateNormal();
                };
                Action onApplyFinished = () => {
                    Refocus();
                    onFinished();
                };
                Action onCancelFinished = () => {
                    onFinished();
                };

                StateParameteredAction(action, onApplyFinished, onCancelFinished);
            }
        }

        private void StateMovingAttack()
        {
            if (SelectedActor?.MovingAttackAct is IActorAction action)
            {
                State = States.MovingAttack;
                ReachablePoints = CivModel.Path.ActorMovePath.GetReachablePoint(SelectedActor, true);

                Action onFinished = () => {
                    ReachablePoints = null;
                    StateNormal();
                };
                Action onApplyFinished = () => {
                    Refocus();
                    onFinished();
                };
                Action onCancelFinished = () => {
                    onFinished();
                };

                StateParameteredAction(action, onApplyFinished, onCancelFinished);
            }
        }

        private void StateHoldingAttack()
        {
            if (SelectedActor?.HoldingAttackAct is IActorAction action)
            {
                State = States.HoldingAttack;

                Action onFinished = () => {
                    StateNormal();
                };
                Action onApplyFinished = () => {
                    Refocus();
                    onFinished();
                };
                Action onCancelFinished = () => {
                    onFinished();
                };

                StateParameteredAction(action, onApplyFinished, onCancelFinished);
            }
        }

        private void StateSpeicalAct(int index)
        {
            if (SelectedActor?.SpecialActs == null)
                return;
            if (index < 0 || index >= SelectedActor.SpecialActs.Count)
                return;

            var action = SelectedActor.SpecialActs[index];
            if (action.IsParametered)
            {
                State = States.SpecialAct;
                StateParam = index;

                Action onFinished = () => {
                    StateParam = -1;
                    StateNormal();
                };

                StateParameteredAction(action, onFinished, onFinished);
            }
            else
            {
                DoUnparameteredAction(action);
            }
        }

        private void StateParameteredAction(IActorAction action, Action onApplyFinished, Action onCancelFinished)
        {
            if (!action.IsParametered)
                throw new ArgumentException("action is not parametered", "action");

            RunningAction = action;

            void clear()
            {
                RunningAction = null;
            }
            OnApply = () => {
                if (action.IsActable(FocusedPoint))
                {
                    action.Act(FocusedPoint);
                    clear();

                    if (!CheckEnding())
                    {
                        onApplyFinished();
                    }
                }
            };
            OnCancel = () => {
                clear();
                onCancelFinished();
            };
            OnArrowKey = direction => {
                MoveSight(direction);
            };
            OnNumeric = index => { };
            OnRemove = () => { };
            OnSkip = () => { };
            OnSleep = () => { };
        }

        private void DoUnparameteredAction(IActorAction action)
        {
            if (action.IsParametered)
                throw new ArgumentException("action is parametered", "action");

            if (action.IsActable(null))
                action.Act(null);

            CheckEnding();
        }

        private void StateProductUI()
        {
            State = States.ProductUI;

            SelectedDeploy = -1;
            SelectedProduction = -1;
            SelectedInvestment = -1;
            IsProductManipulating = false;

            Game.PlayerInTurn.EstimateResourceInputs();

            void clear()
            {
                SelectedDeploy = -1;
                SelectedProduction = -1;
                SelectedInvestment = -1;
                IsProductManipulating = false;
            }
            OnApply = () => {
                if (IsProductManipulating)
                {
                    IsProductManipulating = false;
                }
                else if (SelectedDeploy != -1)
                {
                    var node = Game.PlayerInTurn.Deployment.First;
                    for (int i = 0; i < SelectedDeploy; ++i)
                        node = node.Next;
                    clear();
                    StateDeploy(node);
                }
                else if (SelectedProduction != -1)
                {
                    IsProductManipulating = true;
                }
                else if (SelectedInvestment != -1)
                {
                    clear();
                    StateNormal();
                }
                else
                {
                    clear();
                    StateProductAdd();
                }
            };
            OnCancel = () => {
                if (IsProductManipulating)
                {
                    IsProductManipulating = false;
                }
                else
                {
                    clear();
                    StateNormal();
                }
            };
            OnArrowKey = direction => {
                if (IsProductManipulating && SelectedProduction != -1)
                {
                    switch (direction)
                    {
                        case Direction.Up:
                            if (SelectedProduction > 0)
                            {
                                var node = Game.PlayerInTurn.Production.First;
                                for (int i = 0; i < SelectedProduction; ++i)
                                    node = node.Next;
                                var prev = node.Previous;
                                Game.PlayerInTurn.Production.Remove(node);
                                Game.PlayerInTurn.Production.AddBefore(prev, node.Value);
                                Game.PlayerInTurn.EstimateResourceInputs();
                                --SelectedProduction;
                            }
                            break;
                        case Direction.Down:
                            if (SelectedProduction + 1 < Game.PlayerInTurn.Production.Count)
                            {
                                var node = Game.PlayerInTurn.Production.First;
                                for (int i = 0; i < SelectedProduction; ++i)
                                    node = node.Next;
                                var next = node.Next;
                                Game.PlayerInTurn.Production.Remove(node);
                                Game.PlayerInTurn.Production.AddAfter(next, node.Value);
                                Game.PlayerInTurn.EstimateResourceInputs();
                                ++SelectedProduction;
                            }
                            break;
                    }
                }
                else if (!IsProductManipulating)
                {
                    switch (direction)
                    {
                        case Direction.Up:
                            if (SelectedProduction >= 0)
                            {
                                if (--SelectedProduction == -1)
                                {
                                    if (Game.PlayerInTurn.Deployment.Count != 0)
                                        SelectedDeploy = 0;
                                }
                            }
                            else if (SelectedDeploy >= 0)
                            {
                                --SelectedDeploy;
                            }
                            else if (SelectedInvestment == -1)
                            {
                                SelectedInvestment = _selectedInvestmentCount - 1;
                            }
                            else if (SelectedInvestment > 0)
                            {
                                --SelectedInvestment;
                            }
                            break;
                        case Direction.Down:
                            if (SelectedInvestment >= 0)
                            {
                                if (++SelectedInvestment == _selectedInvestmentCount)
                                {
                                    SelectedInvestment = -1;
                                }
                            }
                            else if (SelectedProduction == -1)
                            {
                                if (++SelectedDeploy >= Game.PlayerInTurn.Deployment.Count)
                                {
                                    SelectedDeploy = -1;
                                    if (Game.PlayerInTurn.Production.Count != 0)
                                        SelectedProduction = 0;
                                }
                            }
                            else if (SelectedProduction + 1 < Game.PlayerInTurn.Production.Count)
                            {
                                ++SelectedProduction;
                            }
                            break;
                    }
                }
            };
            OnNumeric = index => {
                if (IsProductManipulating)
                    return;

                if (SelectedInvestment != -1)
                {
                    double[] maxval = { 1, 2, 2, 1 };
                    double value = maxval[SelectedInvestment] * (index / 8.0);

                    var player = Game.PlayerInTurn;
                    if (SelectedInvestment == 0)
                        player.TaxRate = value;
                    else if (SelectedInvestment == 1)
                        player.EconomicInvestmentRatio = value;
                    else if (SelectedInvestment == 2)
                        player.ResearchInvestmentRatio = value;
                    else if (SelectedInvestment == 3)
                        player.RepairInvestmentRatio = value;
                }
                else if (index < Game.PlayerInTurn.Deployment.Count)
                {
                    SelectedDeploy = index;
                    SelectedProduction = 0;
                }
            };
            OnRemove = () => {
                if (IsProductManipulating)
                {
                    var node = Game.PlayerInTurn.Production.First;
                    for (int i = 0; i < SelectedProduction; ++i)
                        node = node.Next;
                    Game.PlayerInTurn.Production.Remove(node);

                    IsProductManipulating = false;
                    SelectedProduction = -1;
                }
            };
            OnSkip = () => { };
            OnSleep = () => { };
        }

        private void StateProductAdd()
        {
            State = States.ProductAdd;

            AvailableProduction = Game.PlayerInTurn.AvailableProduction.ToList();
            if (AvailableProduction.Count == 0)
                SelectedProduction = -1;
            else
                SelectedProduction = 0;

            void clear()
            {
                AvailableProduction = null;
            }
            OnApply = () => {
                if (SelectedProduction != -1)
                {
                    var production = AvailableProduction[SelectedProduction].Create(Game.PlayerInTurn);
                    Game.PlayerInTurn.Production.AddLast(production);
                }
                OnCancel();
            };
            OnCancel = () => {
                clear();
                StateProductUI();
            };
            OnArrowKey = direction => {
                switch (direction)
                {
                    case Direction.Up:
                        if (SelectedProduction >= 0)
                            --SelectedProduction;
                        break;
                    case Direction.Down:
                        if (SelectedProduction + 1 < AvailableProduction.Count)
                            ++SelectedProduction;
                        break;
                }
            };
            OnNumeric = index => {
                if (index < AvailableProduction.Count)
                    SelectedProduction = index;
            };
            OnRemove = () => { };
            OnSkip = () => { };
            OnSleep = () => { };
        }

        private void StateDeploy(LinkedListNode<Production> node)
        {
            State = States.Deploy;

            DeployProduction = node.Value;

            void clear()
            {
                DeployProduction = null;
            }
            OnApply = () => {
                if (DeployProduction.IsPlacable(FocusedPoint))
                {
                    Game.PlayerInTurn.Deployment.Remove(node);
                    DeployProduction.Place(FocusedPoint);
                    SelectNextActor();
                    OnCancel();
                }
            };
            OnCancel = () => {
                clear();
                StateNormal();
            };
            OnArrowKey = direction => {
                MoveSight(direction);
            };
            OnNumeric = index => { };
            OnRemove = () => { };
            OnSkip = () => { };
            OnSleep = () => { };
        }

        private void StateQuest()
        {
            State = States.Quest;

            _acceptedQuests = Game.PlayerInTurn.Quests.Where(q => q.Status == QuestStatus.Accepted).ToList();
            _deployedQuests = Game.PlayerInTurn.Quests.Where(q => q.Status == QuestStatus.Deployed).ToList();
            _completedQuests = Game.PlayerInTurn.Quests.Where(q => q.Status == QuestStatus.Completed).ToList();
            _disabledQuests = Game.PlayerInTurn.Quests.Where(q => q.Status == QuestStatus.Disabled).ToList();
            SelectedQuest = 0;
            _questsCount = Game.PlayerInTurn.Quests.Count;

            void clear()
            {
                _acceptedQuests = null;
                _deployedQuests = null;
                _completedQuests = null;
                _disabledQuests = null;
                SelectedQuest = -1;
                _questsCount = -1;
            }
            OnApply = () => {
                Quest quest = null;
                if (SelectedQuest < AcceptedQuests.Count)
                    quest = AcceptedQuests[SelectedQuest];
                else if (SelectedQuest < AcceptedQuests.Count + DeployedQuests.Count)
                    quest = DeployedQuests[SelectedQuest - AcceptedQuests.Count];
                else if (SelectedQuest < AcceptedQuests.Count + DeployedQuests.Count + CompletedQuests.Count)
                    quest = CompletedQuests[SelectedQuest - AcceptedQuests.Count - DeployedQuests.Count];
                else if (SelectedQuest < _questsCount)
                    quest = DisabledQuests[SelectedQuest - AcceptedQuests.Count - DeployedQuests.Count - CompletedQuests.Count];

                if (quest != null)
                {
                    if (quest.Status == QuestStatus.Deployed)
                    {
                        quest.Accept();
                        _deployedQuests.Remove(quest);
                        _acceptedQuests.Insert(0, quest);
                        SelectedQuest = 0;
                    }
                    else if (quest.Status == QuestStatus.Accepted)
                    {
                        quest.Disable();
                        _acceptedQuests.Remove(quest);
                        _disabledQuests.Insert(0, quest);
                        SelectedQuest = _acceptedQuests.Count;
                    }
                }
            };
            OnCancel = () => {
                clear();
                StateNormal();
            };
            OnArrowKey = direction => {
                switch (direction)
                {
                    case Direction.Up:
                        if (SelectedQuest > 0)
                            --SelectedQuest;
                        break;
                    case Direction.Down:
                        if (SelectedQuest + 1 < _questsCount)
                            ++SelectedQuest;
                        break;
                }
            };
            OnNumeric = index => {
                if (index < _questsCount)
                    SelectedQuest = index;
            };
            OnRemove = () => { };
            OnSkip = () => { };
            OnSleep = () => { };
        }

        private void StateEnding()
        {
            State = States.Ending;

            OnApply = () => {
                OnCancel();
            };
            OnCancel = () => {
                StateNormal();
            };
            OnArrowKey = direction => { };
            OnNumeric = index => { };
            OnRemove = () => { };
            OnSkip = () => { };
            OnSleep = () => { };
        }

        private void StateAIControl()
        {
            State = States.AIControl;

            OnApply = () => { };
            OnCancel = () => { };
            OnArrowKey = direction => { };
            OnNumeric = index => { };
            OnRemove = () => { };
            OnSkip = () => { };
            OnSleep = () => { };

            Game.PlayerInTurn.DoAITurnAction().ContinueWith(
                task => View.Invoke(() => {
                    ProceedTurn();
                }));
        }

        private void StateCityView(CityBase city)
        {
            State = States.CityView;

            SelectedCity = city;

            OnApply = () => {
                OnCancel();
            };
            OnCancel = () => {
                SelectedCity = null;
                StateNormal();
            };
            OnArrowKey = direction => { };
            OnNumeric = index => { };
            OnRemove = () => { };
            OnSkip = () => { };
            OnSleep = () => { };
        }

        private void StatePathFinding()
        {
            State = States.PathFinding;

            void updatePath()
            {
                if (SelectedActor.PlacedPoint != FocusedPoint && SelectedActor.MoveAct != null)
                {
                    IMovePath path = new CivModel.Path.ActorMovePath(
                        SelectedActor, FocusedPoint, SelectedActor.MoveAct);
                    if (!path.IsInvalid)
                    {
                        MovePath = path;
                        return;
                    }
                }
                MovePath = null;
            }

            updatePath();

            OnApply = () => {
                if (MovePath != null && !MovePath.IsInvalid)
                {
                    SelectedActor.MovePath = MovePath;
                    MovePath.ActFullWalkForRemainAP();
                }
                OnCancel();
            };
            OnCancel = () => {
                MovePath = null;
                StateNormal();
            };
            OnArrowKey = direction => {
                MoveSight(direction);
                updatePath();
            };
            OnNumeric = index => { };
            OnRemove = () => { };
            OnSkip = () => { };
            OnSleep = () => { };
        }
    }
}
