using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CivModel;
using CivModel.Common;

namespace CivPresenter
{
    /// <summary>
    /// Represents a presenter.
    /// </summary>
    /// <remarks>
    /// The presenter works like a Finite State Machine.
    /// <see cref="Presenter.State"/> is changed by Command~~~ operations.
    /// </remarks>
    public class Presenter
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

        private Unit[] _standbyUnits = null;
        private int _standbyUnitIndex = -1;

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
        /// Index of the selected deploy to <see cref="Player.Deployment"/> list.
        /// <c>-1</c> if there is no selected deploy.
        /// If <see cref="SelectedProduction"/> is not <c>-1</c>, this value is <c>-1</c>.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductUI"/></c>
        /// </summary>
        public int SelectedDeploy { get; private set; } = -1;

        /// <summary>
        /// Index of the selected production to <see cref="Player.Production"/> list.
        /// <c>-1</c> if there is no selected production.
        /// If <see cref="SelectedDeploy"/> is not <c>-1</c>, this value is <c>-1</c>
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductUI"/> || <see cref="State"/> == <see cref="States.ProductAdd"/></c>
        /// </summary>
        public int SelectedProduction { get; private set; } = -1;

        /// <summary>
        /// Whether user is manipulating a production.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductUI"/></c>
        /// </summary>
        public bool IsProductManipulating { get; private set; } = false;

        /// <summary>
        /// The list of the available production, retrieved by <see cref="Player.GetAvailableProduction"/>
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.ProductAdd"/></c>
        /// </summary>
        public IReadOnlyList<IProductionFactory> AvailableProduction { get; private set; }

        /// <summary>
        /// The production to deploy.
        /// This value is valid iff <c><see cref="State"/> == <see cref="States.Deploy"/></c>
        /// </summary>
        public Production DeployProduction { get; private set; }

        /// <summary>
        /// Indicates the state of <see cref="Presenter"/>.
        /// </summary>
        public enum States
        {
            Normal, Move, MovingAttack, HoldingAttack, SpecialAct,
            ProductUI, ProductAdd, Deploy,
            Victory, Defeated
        }

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

        private bool[] _victoryNotified = null;

        private Action OnApply;
        private Action OnCancel;
        private Action<Direction> OnArrowKey;
        private Action<int> OnNumeric;
        private Action OnRemove;
        private Action OnSkip;

        /// <summary>
        /// Initializes a new instance of the <see cref="Presenter"/> class.
        /// </summary>
        /// <param name="view">The <see cref="IView"/> object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="view"/> is <c>null</c></exception>
        public Presenter(IView view)
        {
            _view = view ?? throw new ArgumentNullException("view");

            _game = new Game(width: 10, height: 10, numOfPlayer: 2);

            // fallback point
            // ProceedTurn() would set FocusedPoint if any unit/city exists.
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
            if (FocusedPoint.Unit != null && FocusedPoint.Unit.Owner == Game.PlayerInTurn)
            {
                SelectUnit(FocusedPoint.Unit);
            }
        }

        /// <summary>
        /// Gives the command [move].
        /// </summary>
        public void CommandMove()
        {
            if (State == States.Normal)
                StateMove();
            else
                OnCancel();
        }

        /// <summary>
        /// Gives the command [moving attack].
        /// </summary>
        public void CommandMovingAttack()
        {
            if (State == States.Normal)
                StateMovingAttack();
            else
                OnCancel();
        }

        /// <summary>
        /// Gives the command [holding attack].
        /// </summary>
        public void CommandHoldingAttack()
        {
            if (State == States.Normal)
                StateHoldingAttack();
            else
                OnCancel();
        }

        /// <summary>
        /// Gives the command [product UI].
        /// </summary>
        public void CommandProductUI()
        {
            if (State == States.Normal)
                StateProductUI();
            else
                OnCancel();
        }

        private void ProceedTurn()
        {
            if (Game.IsInsideTurn)
                Game.EndTurn();
            Game.StartTurn();

            SelectNextUnit();
            if (_selectedActor == null)
            {
                if (Game.PlayerInTurn.Cities.FirstOrDefault() is CityCenter city)
                {
                    if (city.PlacedPoint is Terrain.Point pt)
                        FocusedPoint = pt;
                }
            }

            StateNormal();
        }

        private void SelectNextUnit()
        {
            int tryNumber = (_standbyUnitIndex == -1) ? 1 : 2;

            for (int j = 0; j < tryNumber; ++j)
            {
                if (_standbyUnitIndex == -1)
                {
                    _standbyUnits = Game.PlayerInTurn.Units.ToArray();
                }

                int idx = _standbyUnitIndex + 1;
                for (; idx < _standbyUnits.Length; ++idx)
                {
                    var unit = _standbyUnits[idx];
                    if (unit.RemainAP > 0 && !unit.SkipFlag && unit.PlacedPoint.HasValue)
                    {
                        _standbyUnitIndex = idx;
                        _selectedActor = _standbyUnits[idx];
                        IsThereTodos = true;
                        Refocus();
                        return;
                    }
                }

                _selectedActor = null;
                _standbyUnitIndex = -1;
                IsThereTodos = false;
            }
        }

        private void SelectUnit(Unit unit)
        {
            var units = Game.PlayerInTurn.Units.ToArray();
            int idx = Array.IndexOf(units, unit);

            if (idx == -1)
                return;

            _selectedActor = unit;
            unit.SkipFlag = false;

            _standbyUnits = units;
            _standbyUnitIndex = idx;
            IsThereTodos = true;
            Refocus();
        }

        private bool CheckVictory()
        {
            var survivors = Game.Players.Where(player => !player.IsDefeated);
            if (survivors.Count() <= 1)
            {
                _victoryNotified = new bool[Game.Players.Count];
                StateNormal();
                return true;
            }
            return false;
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
                    pos.X = Math.Max(pos.X - 1, 0);
                    break;
                case Direction.Right:
                    pos.X = Math.Min(pos.X + 1, Game.Terrain.Width - 1);
                    break;
            }
            FocusedPoint = Game.Terrain.GetPoint(pos);
        }

        private void StateNormal()
        {
            if (_victoryNotified != null)
            {
                int idx = 0;
                for (; idx < Game.Players.Count; ++idx)
                {
                    if (Game.Players[idx] == Game.PlayerInTurn)
                        break;
                }

                if (!_victoryNotified[idx])
                {
                    if (Game.PlayerInTurn.IsDefeated)
                        StateDefeated();
                    else
                        StateVictory();

                    _victoryNotified[idx] = true;
                    return;
                }
            }

            State = States.Normal;

            OnApply = () => {
                if (!IsThereTodos)
                {
                    ProceedTurn();
                }
                else
                {
                    SelectNextUnit();
                }
            };
            OnCancel = () => {
                if (SelectedActor != null)
                {
                    _selectedActor = null;
                }
                else
                {
                    View.Shutdown();
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
                    SelectNextUnit();
                }
            };
        }

        private void StateMove()
        {
            if (SelectedActor?.MoveAct is IActorAction action)
            {
                State = States.Move;

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

        private void StateMovingAttack()
        {
            if (SelectedActor?.MovingAttackAct is IActorAction action)
            {
                State = States.MovingAttack;

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

            Action clear = () => {
                RunningAction = null;
            };
            OnApply = () => {
                if (action.IsActable(FocusedPoint))
                {
                    action.Act(FocusedPoint);
                    clear();

                    if (!CheckVictory())
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
        }

        private void DoUnparameteredAction(IActorAction action)
        {
            if (action.IsParametered)
                throw new ArgumentException("action is parametered", "action");

            if (action.IsActable(null))
                action.Act(null);

            CheckVictory();
        }

        private void StateProductUI()
        {
            State = States.ProductUI;

            SelectedDeploy = -1;
            SelectedProduction = -1;
            IsProductManipulating = false;

            Game.PlayerInTurn.EstimateLaborInputing();

            Action clear = () => {
                SelectedDeploy = -1;
                SelectedProduction = -1;
                IsProductManipulating = false;
            };
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
                if (IsProductManipulating)
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
                                Game.PlayerInTurn.EstimateLaborInputing();
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
                                Game.PlayerInTurn.EstimateLaborInputing();
                                ++SelectedProduction;
                            }
                            break;
                    }
                }
                else
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
                            break;
                        case Direction.Down:
                            if (SelectedProduction == -1)
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

                if (index < Game.PlayerInTurn.Deployment.Count)
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
        }

        private void StateProductAdd()
        {
            State = States.ProductAdd;

            AvailableProduction = Game.PlayerInTurn.GetAvailableProduction();
            if (AvailableProduction.Count == 0)
                SelectedProduction = -1;
            else
                SelectedProduction = 0;

            Action clear = () => {
                AvailableProduction = null;
            };
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
        }

        private void StateDeploy(LinkedListNode<Production> node)
        {
            State = States.Deploy;

            DeployProduction = node.Value;

            Action clear = () => {
                DeployProduction = null;
            };
            OnApply = () => {
                if (DeployProduction.IsPlacable(FocusedPoint))
                {
                    Game.PlayerInTurn.Deployment.Remove(node);
                    DeployProduction.Place(FocusedPoint);
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
        }

        private void StateVictory()
        {
            State = States.Victory;

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
        }

        private void StateDefeated()
        {
            State = States.Defeated;

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
        }
    }
}
