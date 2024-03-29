using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
using System.IO;

namespace CivModel
{
    /// <summary>
    /// Represents one civ game.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>
        /// The <see cref="SchemeLoader"/> of this game.
        /// </summary>
        public SchemeLoader SchemeLoader;

        /// <summary>
        /// The constants of this game.
        /// </summary>
        /// <remarks>
        /// For performance purpose, constant values are copied from <see cref="IGameConstantsScheme"/> into this property when game starts.
        /// </remarks>
        /// <seealso cref="GameConstants"/>
        /// <seealso cref="IGameConstantsScheme"/>
        public GameConstants Constants;

        // prototype loader
        private PrototypeLoader _prototypeLoader; // init by PreInitialize

        /// <summary>
        /// The random generator of this game.
        /// </summary>
        /// <remarks>
        /// All random values used in this game must be generated from this object by default.
        /// </remarks>
        public Random Random { get; private set; } // init by PreInitialize

        /// <summary>
        /// <see cref="Terrain"/> of this game.
        /// </summary>
        public Terrain Terrain { get; private set; }

        /// <summary>
        /// The players of this game.
        /// </summary>
        public IReadOnlyList<Player> Players => _players;
        private List<Player> _players; // init by PreInitialize

        /// <summary>
        /// The subturn number.
        /// </summary>
        /// <remarks>
        /// Subturn represents a part of turn, dedicated to each player.
        /// </remarks>
        public int SubTurnNumber { get; private set; } // init by PreInitialize

        /// <summary>
        /// The turn number.
        /// </summary>
        public int TurnNumber => SubTurnNumber / Players.Count;

        /// <summary>
        /// Gets a value indicating whether this game is inside a turn.
        /// </summary>
        public bool IsInsideTurn { get; private set; } // init by PreInitialize

        /// <summary>
        /// Gets the index of <see cref="PlayerInTurn"/>.
        /// </summary>
        public int PlayerNumberInTurn => SubTurnNumber % Players.Count;

        /// <summary>
        /// The player who plays in this turn.
        /// </summary>
        public Player PlayerInTurn => Players[PlayerNumberInTurn];

        /// <summary>
        /// The count of teams of this game.
        /// </summary>
        public int TeamCount { get; private set; }

        // if this value is true, StartTurn resume the loaded game rather than start a new turn.
        // see StartTurn() comment
        private bool _shouldStartTurnResumeGame; // init by PreInitialize

        // a set of used city names in this game.
        // this is used to validate city name in CityCenter class.
        internal ISet<string> UsedCityNames { get; } = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> class, by creating a new game.
        /// </summary>
        /// <param name="width">
        /// The width of the <see cref="Terrain"/> of this game. It must be positive.
        /// if the value is <c>-1</c>, uses <see cref="IGameStartupScheme.DefaultTerrainWidth"/> of the scheme.
        /// </param>
        /// <param name="height">
        /// The height of the <see cref="Terrain"/> of this game. It must be positive.
        /// if the value is <c>-1</c>, uses <see cref="IGameStartupScheme.DefaultTerrainHeight"/> of the scheme.
        /// </param>
        /// <param name="numOfPlayer">
        /// The number of players. It must be positive.
        /// if the value is <c>-1</c>, uses <see cref="IGameStartupScheme.DefaultNumberOfPlayers"/> of the scheme.
        /// </param>
        /// <param name="prototypes">
        /// The array of <see cref="TextReader"/> for xml prototype data.
        /// </param>
        /// <param name="rootFactory">The factory for <see cref="IGameScheme"/> of the game.</param>
        /// <param name="knownSchemes">
        /// the known factories of <see cref="IGameScheme"/> for the game.
        /// If <c>null</c>, use only <paramref name="rootFactory"/> and those <paramref name="rootFactory"/> provides.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="rootFactory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="width"/> is not positive
        /// or
        /// <paramref name="height"/> is not positive
        /// or
        /// <paramref name="numOfPlayer"/> is not positive
        /// or
        /// parameter is not equal to default value of scheme, while scheme forces to be.
        /// </exception>
        public Game(int width, int height, int numOfPlayer, TextReader[] prototypes,
            IGameSchemeFactory rootFactory, IEnumerable<IGameSchemeFactory> knownSchemes = null)
        {
            if (rootFactory == null)
                throw new ArgumentNullException(nameof(rootFactory));

            PreInitialize();

            SchemeLoader = new SchemeLoader(rootFactory, knownSchemes);
            //// TODO: REMOVE HARDCODING
            foreach (var ff in knownSchemes)
            {
                if (ff != rootFactory)
                {
                    SchemeLoader.Load(ff, knownSchemes);
                }
            }
            /////////////////////

            LoadPrototype(prototypes);

            var constantsScheme = SchemeLoader.GetExclusiveScheme<IGameConstantsScheme>();
            if (constantsScheme.Constants != null)
                Constants = new GameConstants(constantsScheme.Constants);
            else
                Constants = _prototypeLoader.GetGameConstants(constantsScheme.Factory.Guid);

            var startup = SchemeLoader.GetExclusiveScheme<IGameStartupScheme>();

            if (width == -1)
                width = startup.DefaultTerrainWidth;
            if (height == -1)
                height = startup.DefaultTerrainHeight;
            if (numOfPlayer == -1)
                numOfPlayer = startup.DefaultNumberOfPlayers;

            if (width <= 0)
                throw new ArgumentException("width is not positive", "width");
            if (height <= 0)
                throw new ArgumentException("height is not positive", "height");
            if (numOfPlayer <= 0)
                throw new ArgumentException("numOfPlayer is not positive", "numOfPlayer");

            if (startup.OnlyDefaultTerrain)
            {
                if (width != startup.DefaultTerrainWidth)
                    throw new ArgumentException("parameter is not equal to default value of scheme, while scheme forces to be", "width");
                if (height != startup.DefaultTerrainHeight)
                    throw new ArgumentException("parameter is not equal to default value of scheme, while scheme forces to be", "height");
            }
            if (startup.OnlyDefaultPlayers)
            {
                if (numOfPlayer != startup.DefaultNumberOfPlayers)
                    throw new ArgumentException("parameter is not equal to default value of scheme, while scheme forces to be", "numOfPlayer");
            }

            Terrain = new Terrain(width, height);

            TeamCount = numOfPlayer;
            for (int i = 0; i < numOfPlayer; ++i)
            {
                _players.Add(new Player(this, i));
            }

            startup.InitializeGame(this, true);
            foreach (var scheme in SchemeLoader.SchemaTree)
            {
                scheme.OnAfterInitialized(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> class, by loading a existing save file.
        /// </summary>
        /// <param name="saveFile">The path of the save file.</param>
        /// <param name="prototypes">
        /// The array of <see cref="TextReader"/> for xml prototype data.
        /// </param>
        /// <param name="schemeFactories">the candidates of factories for <see cref="IGameScheme"/> of the game.</param>
        /// <exception cref="ArgumentNullException"><paramref name="schemeFactories"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidDataException">
        /// save file is invalid
        /// or
        /// there is no <see cref="IGameSchemeFactory"/> for this save file.
        /// </exception>
        /// <remarks>
        /// <para>
        ///  This constructor uses <see cref="File.OpenText(string)"/>.
        ///  See the list of the exceptions <see cref="File.OpenText(string)"/> may throw.
        /// </para>
        /// <para>
        ///  This constructor is wrapper of <see cref="Load(StreamReader, TextReader[], IEnumerable{IGameSchemeFactory})"/>.
        ///  See <see cref="Load(StreamReader, TextReader[], IEnumerable{IGameSchemeFactory})"/> for more information.
        /// </para>
        /// </remarks>
        /// <seealso cref="Load(StreamReader, TextReader[], IEnumerable{IGameSchemeFactory})"/>
        public Game(string saveFile, TextReader[] prototypes, IEnumerable<IGameSchemeFactory> schemeFactories)
        {
            if (schemeFactories == null)
                throw new ArgumentNullException(nameof(schemeFactories));

            using (var stream = File.OpenText(saveFile))
            {
                Load(stream, prototypes, schemeFactories);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> class, by loading a existing save file from stream.
        /// </summary>
        /// <param name="stream"><see cref="StreamReader"/> object which contains a save file.</param>
        /// <param name="prototypes">
        /// The array of <see cref="TextReader"/> for xml prototype data.
        /// </param>
        /// <param name="schemeFactories">the candidates of factories for <see cref="IGameScheme"/> of the game.</param>
        /// <exception cref="ArgumentNullException"><paramref name="schemeFactories"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidDataException">
        /// save file is invalid
        /// or
        /// there is no <see cref="IGameSchemeFactory"/> for this save file.
        /// </exception>
        /// <remarks>
        /// This constructor is wrapper of <see cref="Load(StreamReader, TextReader[], IEnumerable{IGameSchemeFactory})"/>.
        /// See <see cref="Load(StreamReader, TextReader[], IEnumerable{IGameSchemeFactory})"/> for more information.
        /// </remarks>
        /// <seealso cref="Load(StreamReader, TextReader[], IEnumerable{IGameSchemeFactory})"/>
        public Game(StreamReader stream, TextReader[] prototypes, IEnumerable<IGameSchemeFactory> schemeFactories)
        {
            if (schemeFactories == null)
                throw new ArgumentNullException(nameof(schemeFactories));

            Load(stream, prototypes, schemeFactories);
        }

        private void PreInitialize()
        {
            _prototypeLoader = new PrototypeLoader();
            Random = new Random();

            _players = new List<Player>();
            SubTurnNumber = 0;
            IsInsideTurn = false;
            _shouldStartTurnResumeGame = false;

            InitializeObservable();
        }

        private void LoadPrototype(TextReader[] prototypes)
        {
            foreach (var r in prototypes)
                _prototypeLoader.AddData(r);

            foreach (var s in SchemeLoader.GetOverlappableScheme<IGameScheme>())
            {
                _prototypeLoader.EnablePackage(s.Factory.Guid, s.GetType());
            }
        }

        /// <summary>
        /// Gets the prototype for specified type.
        /// </summary>
        /// <typeparam name="Proto">The type of the prototype.</typeparam>
        /// <param name="type">The type targeted by the prototype.</param>
        /// <returns>The prototype object.</returns>
        /// <exception cref="KeyNotFoundException">
        /// the prototype of specified type is not found
        /// or
        /// the prototype of specified type cannot be cast into specified prototype
        /// </exception>
        public Proto GetPrototype<Proto>(Type type)
            where Proto : GuidObjectPrototype
        {
            return _prototypeLoader.GetPrototype<Proto>(type);
        }

        /// <summary>
        /// Starts the turn. If the game is loaded from a save file and not resumed, Resume the game.
        /// </summary>
        /// <remarks>
        /// This method also resumes the game loaded from a save file. In this case, Turn/Subturn does not change.
        /// </remarks>
        /// <exception cref="InvalidOperationException">this game is inside turn yet</exception>
        public void StartTurn()
        {
            if (IsInsideTurn)
                throw new InvalidOperationException("this game is inside turn yet");

            if (_shouldStartTurnResumeGame)
            {
                _shouldStartTurnResumeGame = false;
            }
            else
            {
                if (SubTurnNumber % Players.Count == 0)
                {
                    TurnEvent.RaiseFixedForward(r => r.FixedPreTurn());
                    TurnEvent.RaiseFixedForward(r => r.FixedAfterPreTurn());
                    TurnEvent.RaiseObservable(o => o.PreTurn());
                    TurnEvent.RaiseObservable(o => o.AfterPreTurn());
                }

                TurnEvent.RaiseFixedForward(r => r.FixedPreSubTurn(PlayerInTurn));
                TurnEvent.RaiseFixedForward(r => r.FixedAfterPreSubTurn(PlayerInTurn));
                TurnEvent.RaiseObservable(o => o.PreSubTurn(PlayerInTurn));
                TurnEvent.RaiseObservable(o => o.AfterPreSubTurn(PlayerInTurn));
            }

            IsInsideTurn = true;
        }

        /// <summary>
        /// Ends the turn.
        /// </summary>
        /// <exception cref="InvalidOperationException">the turn is not started yet</exception>
        public void EndTurn()
        {
            if (!IsInsideTurn)
                throw new InvalidOperationException("the turn is not started yet");

            TurnEvent.RaiseFixedBackward(r => r.FixedPostSubTurn(PlayerInTurn));
            TurnEvent.RaiseObservable(o => o.PostSubTurn(PlayerInTurn));
            TurnEvent.RaiseFixedBackward(r => r.FixedAfterPostSubTurn(PlayerInTurn));
            TurnEvent.RaiseObservable(o => o.AfterPostSubTurn(PlayerInTurn));

            if ((SubTurnNumber + 1) % Players.Count == 0)
            {
                TurnEvent.RaiseFixedBackward(r => r.FixedPostTurn());
                TurnEvent.RaiseObservable(o => o.PostTurn());
                TurnEvent.RaiseFixedBackward(r => r.FixedAfterPostTurn());
                TurnEvent.RaiseObservable(o => o.AfterPostTurn());
            }

            ++SubTurnNumber;
            IsInsideTurn = false;
        }
    }
}
