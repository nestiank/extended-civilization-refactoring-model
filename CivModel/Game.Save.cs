using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CivModel
{
    public partial class Game
    {
        /// <summary>
        /// See remark section of <see cref="Guid.ParseExact(string, string)"/>.
        /// </summary>
        private const string _guidSaveFormat = "D";

        /// <summary>
        /// Re-initializes the <see cref="Game"/> object, by loading a existing save file.
        /// </summary>
        /// <param name="stream"><see cref="StreamReader"/> object which contains a save file.</param>
        /// <param name="schemeFactories">
        /// the candidates of factories for <see cref="IGameScheme"/> of the game.
        /// If <c>null</c>, use <see cref="Scheme"/> property.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="schemeFactories"/> is <c>null</c> and <see cref="Scheme"/> is not initialized yet
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// save file or stream is invalid
        /// or
        /// there is no <see cref="IGameSchemeFactory"/> for this save file.
        /// </exception>
        public void Load(StreamReader stream, IEnumerable<IGameSchemeFactory> schemeFactories)
        {
            if (schemeFactories == null && Scheme == null)
                throw new ArgumentNullException(nameof(schemeFactories), "schemeFactories is null and Scheme is not initialized yet");

            string errmsg = "save file or stream is invalid";

            Guid guid;
            int[] ints;
            int numOfPlayer;

            PreInitialize();

            string readLine()
            {
                string s;
                do
                {
                    if (stream.EndOfStream)
                        throw new InvalidDataException(errmsg);
                    s = stream.ReadLine();
                }
                while (s == "");
                return s;
            }

            try
            {
                guid = Guid.ParseExact(readLine(), _guidSaveFormat);

                IGameSchemeFactory factory;
                if (schemeFactories != null)
                {
                    factory = schemeFactories.Where(f => f != null && f.Guid == guid).FirstOrDefault();
                    if (factory == null)
                        throw new InvalidDataException("there is no IGameSchemeFactory for this save file");
                }
                else
                {
                    if (guid != Scheme.Factory.Guid)
                        throw new InvalidDataException("Scheme is not appropriate for this save file");

                    factory = Scheme.Factory;
                }

                Scheme = factory.Create();
                RegisterGuid();

                SubTurnNumber = Convert.ToInt32(readLine());
                if (SubTurnNumber < 0)
                    throw new InvalidDataException(errmsg);

                ints = readLine().Split(' ').Select(str => Convert.ToInt32(str)).ToArray();
                if (ints.Length != 3 || ints.Count(x => x <= 0) != 0)
                    throw new InvalidDataException(errmsg);

                numOfPlayer = ints[0];
                Terrain = new Terrain(ints[1], ints[2]);

                for (int y = 0; y < Terrain.Height; ++y)
                {
                    string line = readLine();
                    for (int x = 0; x < Terrain.Width; ++x)
                    {
                        if (x >= line.Length)
                            throw new InvalidDataException(errmsg);

                        int idx = "POMFSTIH".IndexOf(line[x]);
                        if (idx == -1)
                            throw new InvalidDataException(errmsg);

                        var point = Terrain.GetPoint(x, y);
                        point.Type = (TerrainType)idx;
                    }
                }

                for (int i = 0; i < numOfPlayer; ++i)
                {
                    _players.Add(new Player(this));

                    var territory = readLine().Split(':')
                        .Where(str => str != "")
                        .Select(s1 => s1.Split(',').Select(s2 => Convert.ToInt32(s2)).ToArray());
                    foreach (var terr in territory)
                    {
                        if (terr.Length != 2)
                            throw new InvalidDataException(errmsg);

                        var ptTerr = Terrain.GetPoint(terr[0], terr[1]);
                        _players[i].AddTerritory(ptTerr);
                    }
                }

                while (!stream.EndOfStream)
                {
                    ints = readLine().Split(',').Select(str => Convert.ToInt32(str)).ToArray();

                    if (ints.Length != 3)
                        throw new InvalidDataException(errmsg);
                    if (ints[0] < 0 || ints[0] >= numOfPlayer)
                        throw new InvalidDataException(errmsg);

                    var pos = Position.FromPhysical(ints[1], ints[2]);
                    if (!Terrain.IsValidPosition(pos))
                        throw new InvalidDataException(errmsg);
                    var pt = Terrain.GetPoint(pos);

                    guid = Guid.ParseExact(readLine(), _guidSaveFormat);
                    var obj = GuidManager.Create(guid, Players[ints[0]], pt);
                    switch (obj)
                    {
                        case CityBase city:
                        {
                            if (!city.TrySetCityName(readLine()))
                                throw new InvalidDataException(errmsg);

                            city.Population = Convert.ToDouble(readLine());

                            int len = Convert.ToInt32(readLine());
                            if (len < 0)
                                throw new InvalidDataException(errmsg);
                            for (int i = 0; i < len; ++i)
                            {
                                guid = Guid.ParseExact(readLine(), _guidSaveFormat);
                                GuidManager.Create(guid, Players[ints[0]], city.PlacedPoint.Value);
                            }

                            break;
                        }
                        case Unit unit:
                        {
                            unit.RemainAP = Convert.ToInt32(readLine());
                            unit.RemainHP = Convert.ToInt32(readLine());
                            break;
                        }
                        default:
                            throw new InvalidDataException(errmsg);
                    }
                }

                _shouldStartTurnResumeGame = true;

                Initialize(false);
            }
            catch (InvalidCastException)
            {
                throw new InvalidDataException(errmsg);
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidDataException(errmsg);
            }
            catch (FormatException)
            {
                throw new InvalidDataException(errmsg);
            }
            catch (OverflowException)
            {
                throw new InvalidDataException(errmsg);
            }
        }

        /// <summary>
        /// Saves current status of the game to the specified save file.
        /// </summary>
        /// <param name="saveFile">The path of the save file.</param>
        public void Save(string saveFile)
        {
            using (var file = File.CreateText(saveFile))
            {
                file.WriteLine(Scheme.Factory.Guid.ToString(_guidSaveFormat));

                file.WriteLine(SubTurnNumber);
                file.WriteLine(Players.Count + " " + Terrain.Width + " " + Terrain.Height);
                for (int y = 0; y < Terrain.Height; ++y)
                {
                    for (int x = 0; x < Terrain.Width; ++x)
                    {
                        file.Write("POMFSTIH"[(int)Terrain.GetPoint(x, y).Type]);
                    }
                    file.WriteLine();
                }

                foreach (var player in Players)
                {
                    foreach (var tile in player.Territory)
                    {
                        file.Write(":" + tile.Position.X + "," + tile.Position.Y);
                    }
                    file.WriteLine();
                }

                for (int i = 0; i < Players.Count; ++i)
                {
                    foreach (var city in Players[i].Cities)
                    {
                        if (city.PlacedPoint?.Position is Position pos)
                        {
                            file.WriteLine(i + "," + pos.X + "," + pos.Y);
                            file.WriteLine(city.Guid.ToString(_guidSaveFormat));
                            file.WriteLine(city.Name);
                            file.WriteLine(city.Population);

                            file.WriteLine(city.InteriorBuildings.Count);
                            foreach (var building in city.InteriorBuildings)
                            {
                                file.WriteLine(building.Guid);
                            }
                        }
                    }

                    foreach (var unit in Players[i].Units)
                    {
                        if (unit.PlacedPoint?.Position is Position pos)
                        {
                            file.WriteLine(i + "," + pos.X + "," + pos.Y);
                            file.WriteLine(unit.Guid.ToString(_guidSaveFormat));
                            file.WriteLine(unit.RemainAP);
                            file.WriteLine(unit.RemainHP);
                        }
                    }
                }
            }
        }
    }
}