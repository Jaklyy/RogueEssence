﻿#region Using Statements
using System;
using System.Threading;
using System.Globalization;
using RogueEssence.Content;
using RogueEssence.Data;
using RogueEssence.Dev;
using RogueEssence.Dungeon;
using RogueEssence.Script;
using RogueEssence;
using System.Reflection;
using Microsoft.Xna.Framework;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using SDL2;
#endregion

namespace RogueEssence.Examples
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            InitDllMap();
            //TODO: figure out how to set this switch in appconfig
            AppContext.SetSwitch("Switch.System.Runtime.Serialization.SerializationGuard.AllowFileWrites", true);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            DiagManager.InitInstance();
            GraphicsManager.InitParams();
            Text.Init();
            Text.SetCultureCode("");


            try
            {
                DiagManager.Instance.LogInfo("=========================================");
                DiagManager.Instance.LogInfo(String.Format("SESSION STARTED: {0}", String.Format("{0:yyyy/MM/dd HH:mm:ss}", DateTime.Now)));
                DiagManager.Instance.LogInfo("Version: " + Versioning.GetVersion().ToString());
                DiagManager.Instance.LogInfo(Versioning.GetDotNetInfo());
                DiagManager.Instance.LogInfo("=========================================");

                string[] args = System.Environment.GetCommandLineArgs();
                bool logInput = true;
                GraphicsManager.AssetType convertAssets = GraphicsManager.AssetType.None;
                DataManager.DataType convertIndices = DataManager.DataType.None;
                DataManager.DataType reserializeIndices = DataManager.DataType.None;
                DataManager.DataType dump = DataManager.DataType.None;
                string langArgs = "";
                for (int ii = 1; ii < args.Length; ii++)
                {
                    if (args[ii] == "-dev")
                        DiagManager.Instance.DevMode = true;
                    else if (args[ii] == "-play" && args.Length > ii + 1)
                    {
                        DiagManager.Instance.LoadInputs(args[ii + 1]);
                        ii++;
                    }
                    else if (args[ii] == "-lang" && args.Length > ii + 1)
                    {
                        langArgs = args[ii + 1];
                        ii++;
                    }
                    else if (args[ii] == "-nolog")
                        logInput = false;
                    else if (args[ii] == "-convert")
                    {
                        int jj = 1;
                        while (args.Length > ii + jj)
                        {
                            GraphicsManager.AssetType conv = GraphicsManager.AssetType.None;
                            foreach (GraphicsManager.AssetType type in Enum.GetValues(typeof(GraphicsManager.AssetType)))
                            {
                                if (args[ii + jj].ToLower() == type.ToString().ToLower())
                                {
                                    conv = type;
                                    break;
                                }
                            }
                            if (conv != GraphicsManager.AssetType.None)
                                convertAssets |= conv;
                            else
                                break;
                            jj++;
                        }
                        ii += jj - 1;
                    }
                    else if (args[ii] == "-index")
                    {
                        int jj = 1;
                        while (args.Length > ii + jj)
                        {
                            DataManager.DataType conv = DataManager.DataType.None;
                            foreach (DataManager.DataType type in Enum.GetValues(typeof(DataManager.DataType)))
                            {
                                if (args[ii + jj].ToLower() == type.ToString().ToLower())
                                {
                                    conv = type;
                                    break;
                                }
                            }
                            if (conv != DataManager.DataType.None)
                                convertIndices |= conv;
                            else
                                break;
                            jj++;
                        }
                        ii += jj - 1;
                    }
                    else if (args[ii] == "-dump")
                    {
                        int jj = 1;
                        while (args.Length > ii + jj)
                        {
                            DataManager.DataType conv = DataManager.DataType.None;
                            foreach (DataManager.DataType type in Enum.GetValues(typeof(DataManager.DataType)))
                            {
                                if (args[ii + jj].ToLower() == type.ToString().ToLower())
                                {
                                    conv = type;
                                    break;
                                }
                            }
                            if (conv != DataManager.DataType.None)
                                dump |= conv;
                            else
                                break;
                            jj++;
                        }
                        ii += jj - 1;
                    }
                    else if (args[ii] == "-reserialize")
                    {
                        int jj = 1;
                        while (args.Length > ii + jj)
                        {
                            DataManager.DataType conv = DataManager.DataType.None;
                            foreach (DataManager.DataType type in Enum.GetValues(typeof(DataManager.DataType)))
                            {
                                if (args[ii + jj].ToLower() == type.ToString().ToLower())
                                {
                                    conv = type;
                                    break;
                                }
                            }
                            if (conv != DataManager.DataType.None)
                                reserializeIndices |= conv;
                            else
                                break;
                            jj++;
                        }
                        ii += jj - 1;
                    }
                }

                if (convertAssets != GraphicsManager.AssetType.None)
                {
                    //run conversions
                    using (GameBase game = new GameBase())
                    {
                        GraphicsManager.InitSystem(game.GraphicsDevice);
                        GraphicsManager.RunConversions(convertAssets);
                    }

                    return;
                }

                if (reserializeIndices != DataManager.DataType.None)
                {
                    LuaEngine.InitInstance();
                    DataManager.InitInstance();
                    RogueEssence.Dev.DevHelper.Reserialize(reserializeIndices, null);
                    RogueEssence.Dev.DevHelper.ReserializeData(DataManager.DATA_PATH + "Map/", DataManager.MAP_EXT, null);
                    RogueEssence.Dev.DevHelper.ReserializeData(DataManager.DATA_PATH + "Ground/", DataManager.GROUND_EXT, null);
                    RogueEssence.Dev.DevHelper.RunIndexing(reserializeIndices);
                    return;
                }

                if (convertIndices != DataManager.DataType.None)
                {
                    LuaEngine.InitInstance();
                    DataManager.InitInstance();
                    DevHelper.RunIndexing(convertIndices);
                    return;
                }

                //For exporting to data
                if (dump > DataManager.DataType.None)
                {
                    LuaEngine.InitInstance();

                    {
                        DataManager.InitInstance();
                        DataInfo.AddEditorOps();
                        DataInfo.AddSystemFX();
                        DataInfo.AddUniversalData();

                        if ((dump & DataManager.DataType.Element) != DataManager.DataType.None)
                            DataInfo.AddElementData();
                        if ((dump & DataManager.DataType.GrowthGroup) != DataManager.DataType.None)
                            DataInfo.AddGrowthGroupData();
                        if ((dump & DataManager.DataType.SkillGroup) != DataManager.DataType.None)
                            DataInfo.AddSkillGroupData();
                        if ((dump & DataManager.DataType.Emote) != DataManager.DataType.None)
                            DataInfo.AddEmoteData();
                        if ((dump & DataManager.DataType.AI) != DataManager.DataType.None)
                            DataInfo.AddAIData();
                        if ((dump & DataManager.DataType.Tile) != DataManager.DataType.None)
                            DataInfo.AddTileData();
                        if ((dump & DataManager.DataType.Terrain) != DataManager.DataType.None)
                            DataInfo.AddTerrainData();
                        if ((dump & DataManager.DataType.Rank) != DataManager.DataType.None)
                            DataInfo.AddRankData();
                        if ((dump & DataManager.DataType.Skin) != DataManager.DataType.None)
                            DataInfo.AddSkinData();

                        if ((dump & DataManager.DataType.Monster) != DataManager.DataType.None)
                            DataInfo.AddMonsterData();

                        if ((dump & DataManager.DataType.Skill) != DataManager.DataType.None)
                            DataInfo.AddSkillData();

                        if ((dump & DataManager.DataType.Intrinsic) != DataManager.DataType.None)
                            DataInfo.AddIntrinsicData();
                        if ((dump & DataManager.DataType.Status) != DataManager.DataType.None)
                            DataInfo.AddStatusData();
                        if ((dump & DataManager.DataType.MapStatus) != DataManager.DataType.None)
                            DataInfo.AddMapStatusData();

                        if ((dump & DataManager.DataType.Item) != DataManager.DataType.None)
                            DataInfo.AddItemData();

                        if ((dump & DataManager.DataType.Zone) != DataManager.DataType.None)
                        {
                            DataInfo.AddMapData();
                            DataInfo.AddGroundData();
                            DataInfo.AddZoneData();
                        }

                        DevHelper.RunIndexing(dump);
                    }
                    return;
                }

                if (langArgs != "" && DiagManager.Instance.CurSettings.Language == "")
                {
                    if (langArgs.Length > 0)
                    {
                        DiagManager.Instance.CurSettings.Language = langArgs.ToLower();
                        Text.SetCultureCode(langArgs.ToLower());
                    }
                    else
                        DiagManager.Instance.CurSettings.Language = "en";
                }
                Text.SetCultureCode(DiagManager.Instance.CurSettings.Language == "" ? "" : DiagManager.Instance.CurSettings.Language.ToString());


                logInput = false; //this feature is disabled for now...
                if (DiagManager.Instance.ActiveDebugReplay == null && logInput)
                    DiagManager.Instance.BeginInput();

                if (DiagManager.Instance.DevMode)
                {
                    InitDataEditor();
                    AppBuilder builder = Dev.Program.BuildAvaloniaApp();
                    builder.StartWithClassicDesktopLifetime(args);
                }
                else
                {
                    DiagManager.Instance.DevEditor = new EmptyEditor();
                    using (GameBase game = new GameBase())
                        game.Run();
                }


            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex);
                throw ex;
            }
        }

        // TheSpyDog's branch on resolving dllmap for DotNetCore
        // https://github.com/FNA-XNA/FNA/pull/315
        public static void InitDllMap()
        {
            CoreDllMap.Init();
            Assembly fnaAssembly = Assembly.GetAssembly(typeof(Game));
            CoreDllMap.Register(fnaAssembly);
            //load SDL first before FNA3D to sidestep multiple dylibs problem
            SDL.SDL_GetPlatform();
        }

        public static void InitDataEditor()
        {
            DataEditor.Init();
            DataEditor.AddConverter(new AnimDataEditor());
            DataEditor.AddConverter(new SoundEditor());
            DataEditor.AddConverter(new MusicEditor());
            DataEditor.AddConverter(new EntryDataEditor());
            DataEditor.AddConverter(new FrameTypeEditor());

            DataEditor.AddConverter(new BaseEmitterEditor());
            DataEditor.AddConverter(new BattleDataEditor());
            DataEditor.AddConverter(new BattleFXEditor());
            DataEditor.AddConverter(new CircleSquareEmitterEditor());
            DataEditor.AddConverter(new CombatActionEditor());
            DataEditor.AddConverter(new ExplosionDataEditor());
            DataEditor.AddConverter(new ShootingEmitterEditor());
            DataEditor.AddConverter(new SkillDataEditor());
            DataEditor.AddConverter(new ColumnAnimEditor());
            DataEditor.AddConverter(new StaticAnimEditor());
            DataEditor.AddConverter(new TypeDictEditor());
            DataEditor.AddConverter(new SpawnListEditor());
            DataEditor.AddConverter(new SpawnRangeListEditor());
            DataEditor.AddConverter(new PriorityListEditor());
            DataEditor.AddConverter(new PriorityEditor());
            DataEditor.AddConverter(new SegLocEditor());
            DataEditor.AddConverter(new LocEditor());
            DataEditor.AddConverter(new IntRangeEditor());
            DataEditor.AddConverter(new FlagTypeEditor());
            DataEditor.AddConverter(new ColorEditor());
            DataEditor.AddConverter(new TypeEditor());
            DataEditor.AddConverter(new ArrayEditor());
            DataEditor.AddConverter(new DictionaryEditor());
            DataEditor.AddConverter(new ListEditor());
            DataEditor.AddConverter(new EnumEditor());
            DataEditor.AddConverter(new StringEditor());
            DataEditor.AddConverter(new DoubleEditor());
            DataEditor.AddConverter(new SingleEditor());
            DataEditor.AddConverter(new BooleanEditor());
            DataEditor.AddConverter(new IntEditor());
            DataEditor.AddConverter(new ByteEditor());
            DataEditor.AddConverter(new ObjectEditor());
        }
    }

}
