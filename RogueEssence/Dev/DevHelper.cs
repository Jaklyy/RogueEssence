﻿using System;
using System.IO;
using RogueEssence.Data;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Xml.Serialization;
using RogueEssence.Content;

namespace RogueEssence.Dev
{
    public static class DevHelper
    {
        //TODO: v0.6: remove this
        static int legacy = 0;

        public static void ReserializeBase()
        {
            if (legacy == 2)
            {
                string dir = PathMod.ModPath(DataManager.DATA_PATH + "Universal.bin");
                object data = LegacyLoad(dir);
                LegacySave(dir, data);
            }
            else if (legacy == 1)
            {
                string dir = PathMod.ModPath(DataManager.DATA_PATH + "Universal.bin");
                object data = LegacyLoad(dir);
                DataManager.SaveData(dir, data);
                DataManager.LoadData<ActiveEffect>(dir);
                LegacySave(dir, data);
            }
            else
            {
                string dir = PathMod.ModPath(DataManager.DATA_PATH + "Universal.bin");
                object data = LoadWithLegacySupport<ActiveEffect>(dir);
                DataManager.SaveData(dir, data);
            }

            foreach (string dir in PathMod.GetModFiles(DataManager.FX_PATH, "*.fx"))
            {
                object data;
                if (legacy == 2)
                {
                    if (Path.GetFileName(dir) == "NoCharge.fx")
                        data = LegacyLoad(dir);
                    else
                        data = LegacyLoad(dir);
                    LegacySave(dir, data);
                }
                else if (legacy == 1)
                {
                    if (Path.GetFileName(dir) == "NoCharge.fx")
                        data = LegacyLoad(dir);
                    else
                        data = LegacyLoad(dir);
                    DataManager.SaveData(dir, data);
                    if (Path.GetFileName(dir) == "NoCharge.fx")
                        data = DataManager.LoadData<EmoteFX>(dir);
                    else
                        data = DataManager.LoadData<BattleFX>(dir);
                    LegacySave(dir, data);
                }
                else
                {
                    if (Path.GetFileName(dir) == "NoCharge.fx")
                        data = LoadWithLegacySupport<EmoteFX>(dir);
                    else
                        data = LoadWithLegacySupport<BattleFX>(dir);
                    DataManager.SaveData(dir, data);
                }
            }


            foreach (string dir in Directory.GetFiles(Path.Combine(PathMod.RESOURCE_PATH, "Extensions"), "*.op"))
            {
                object data;
                if (legacy == 2)
                {
                    data = LegacyLoad(dir);
                    LegacySave(dir, data);
                }
                else if (legacy == 1)
                {
                    data = LegacyLoad(dir);
                    DataManager.SaveData(dir, data);
                    data = DataManager.LoadData<CharSheetOp>(dir);
                    LegacySave(dir, data);
                }
                else
                {
                    data = LoadWithLegacySupport<CharSheetOp>(dir);
                    DataManager.SaveData(dir, data);
                }
            }
        }

        public static void Reserialize(DataManager.DataType conversionFlags)
        {
            foreach (DataManager.DataType type in Enum.GetValues(typeof(DataManager.DataType)))
            {
                if (type != DataManager.DataType.All && (conversionFlags & type) != DataManager.DataType.None)
                {
                    ReserializeData(DataManager.DATA_PATH + type.ToString() + "/", DataManager.DATA_EXT, type.GetClassType());
                }
            }
        }

        public static void ReserializeData<T>(string dataPath, string ext)
        {
            ReserializeData(dataPath, ext, typeof(T));
        }

        public static void ReserializeData(string dataPath, string ext, Type t)
        {
            foreach (string dir in PathMod.GetModFiles(dataPath, "*" + ext))
            {
                if (legacy == 2)
                {
                    object data = LegacyLoad(dir);
                    LegacySave(dir, data);
                }
                else if (legacy == 1)
                {
                    object data = LegacyLoad(dir);
                    DataManager.SaveData(dir, data);
                    object json = DataManager.LoadData(dir, t);
                    LegacySave(dir, json);
                }
                else
                {
                    object data = LoadWithLegacySupport(dir, t);
                    DataManager.SaveData(dir, data);
                }
            }
        }


        /// <summary>
        /// Bakes all assets from the "Work files" directory specified in the flags.
        /// </summary>
        /// <param name="conversionFlags">Chooses which asset type to bake</param>
        public static void RunIndexing(DataManager.DataType conversionFlags)
        {
            foreach (DataManager.DataType type in Enum.GetValues(typeof(DataManager.DataType)))
            {
                if (type != DataManager.DataType.All && (conversionFlags & type) != DataManager.DataType.None)
                    IndexNamedData(DataManager.DATA_PATH + type.ToString() + "/", type.GetClassType());
            }
        }
        public static void RunExtraIndexing(DataManager.DataType conversionFlags)
        {
            //index extra based on triggers
            foreach (BaseData baseData in DataManager.Instance.UniversalData)
            {
                if ((baseData.TriggerType & conversionFlags) != DataManager.DataType.None)
                {
                    baseData.ReIndex();
                    DataManager.SaveData(PathMod.HardMod(DataManager.MISC_PATH + baseData.FileName + DataManager.DATA_EXT), baseData);
                }
            }
            DataManager.SaveData(PathMod.HardMod(DataManager.MISC_PATH + "Index.bin"), DataManager.Instance.UniversalData);
        }


        public static void IndexNamedData(string dataPath, Type t)
        {
            try
            {
                EntryDataIndex fullGuide = new EntryDataIndex();
                List<EntrySummary> entries = new List<EntrySummary>();
                foreach (string dir in Directory.GetFiles(PathMod.HardMod(dataPath), "*" + DataManager.DATA_EXT))
                {
                    string file = Path.GetFileNameWithoutExtension(dir);
                    int num = Convert.ToInt32(file);
                    IEntryData data = (IEntryData)LoadWithLegacySupport(dir, t);
                    while (entries.Count <= num)
                        entries.Add(null);
                    entries[num] = data.GenerateEntrySummary();
                }
                fullGuide.Entries = entries.ToArray();

                if (entries.Count > 0)
                {
                    using (Stream stream = new FileStream(PathMod.HardMod(dataPath + "index.idx"), FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        Serializer.SerializeData(stream, fullGuide);
                    }
                }
                else
                    File.Delete(PathMod.HardMod(dataPath + "index.idx"));
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(new Exception("Error importing index at " + dataPath + "\n", ex));
            }
        }

        public static void DemoAllData(DataManager.DataType conversionFlags)
        {

            foreach (DataManager.DataType type in Enum.GetValues(typeof(DataManager.DataType)))
            {
                if (type != DataManager.DataType.All && (conversionFlags & type) != DataManager.DataType.None)
                    DemoData(DataManager.DATA_PATH + type.ToString() + "/", DataManager.DATA_EXT, type.GetClassType());
            }
        }

        public static void DemoData(string dataPath, string ext, Type t)
        {
            foreach (string dir in PathMod.GetModFiles(dataPath, "*" + ext))
            {
                IEntryData data = (IEntryData)LoadWithLegacySupport(dir, t);
                if (!data.Released)
                    data = (IEntryData)ReflectionExt.CreateMinimalInstance(data.GetType());
                DataManager.SaveData(dir, data);
            }
        }

        //TODO: v0.6 Delete this
        private static T LoadWithLegacySupport<T>(string path)
        {
            return (T)LoadWithLegacySupport(path, typeof(T));
        }

        //TODO: v0.6 Delete this
        public static object LoadWithLegacySupport(string path, Type t)
        {
            try
            {
                return DataManager.LoadData(path, t);
            }
            catch (Exception)
            {
                return LegacyLoad(path);
            }
        }

        //TODO: v0.6 Delete this
        public static object LegacyLoad(string path)
        {
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                    IFormatter formatter = new BinaryFormatter();
                    return formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                }
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex);
            }
            return null;
        }

        //Needed to use this for testing.
        //TODO: v0.6 Delet this
        public static void LegacySave(string path, object entry)
        {
            using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                IFormatter formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                formatter.Serialize(stream, entry);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
        }

        public static void MergeQuest(string quest)
        {
            DiagManager.Instance.LogInfo(String.Format("Creating standalone from quest: {0}", String.Join(", ", quest)));

            string outputPath = Path.Combine(PathMod.ExePath, "Build", quest);

            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);

            //Exe - direct copy from game
            File.Copy(Path.Combine(PathMod.ExePath, PathMod.ExeName), Path.Combine(outputPath, PathMod.ExeName));
            //PNG - direct copy from game
            string pngName = Path.GetFileNameWithoutExtension(PathMod.ExeName) + ".png";
            if (File.Exists(Path.Combine(PathMod.ExePath, pngName)))
                File.Copy(Path.Combine(PathMod.ExePath, pngName), Path.Combine(outputPath, pngName));

            //Base - direct copy from game
            copyRecursive(GraphicsManager.BASE_PATH, Path.Combine(outputPath, "Base"));

            //Strings - deep merge files
            Directory.CreateDirectory(outputPath);
            copyRecursive(PathMod.NoMod("Strings"), Path.Combine(outputPath, "Strings"));
            mergeStringXmlRecursive(PathMod.HardMod("Strings"), Path.Combine(outputPath, "Strings"));

            //Editor - direct copy from game
            copyRecursive(PathMod.RESOURCE_PATH, Path.Combine(outputPath, "Editor"));

            //Licenses - merged copy
            copyRecursive(PathMod.NoMod("Licenses"), Path.Combine(outputPath, "Licenses"));
            copyRecursive(PathMod.HardMod("Licenses"), Path.Combine(outputPath, "Licenses"));

            //Font - direct copy from game
            copyRecursive(PathMod.NoMod("Font"), Path.Combine(outputPath, "Font"));

            //Controls - direct copy from game
            copyRecursive(PathMod.NoMod("Controls"), Path.Combine(outputPath, "Controls"));

            //Content - merged copy for files, deep merge for indices
            //TODO: only copy what is indexed for characters and portraits
            copyRecursive(PathMod.NoMod(GraphicsManager.CONTENT_PATH), Path.Combine(outputPath, GraphicsManager.CONTENT_PATH));
            copyRecursive(PathMod.HardMod(GraphicsManager.CONTENT_PATH), Path.Combine(outputPath, GraphicsManager.CONTENT_PATH));
            //save merged content indices
            {
                CharaIndexNode charaIndex = GraphicsManager.LoadCharaIndices(GraphicsManager.CONTENT_PATH + "Chara/");
                using (FileStream stream = new FileStream(Path.Combine(outputPath, GraphicsManager.CONTENT_PATH + "Chara/" + "index.idx"), FileMode.Create, FileAccess.Write))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                        charaIndex.Save(writer);
                }
                CharaIndexNode portraitIndex = GraphicsManager.LoadCharaIndices(GraphicsManager.CONTENT_PATH + "Portrait/");
                using (FileStream stream = new FileStream(Path.Combine(outputPath, GraphicsManager.CONTENT_PATH + "Portrait/" + "index.idx"), FileMode.Create, FileAccess.Write))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                        portraitIndex.Save(writer);
                }
                TileGuide tileIndex = GraphicsManager.LoadTileIndices(GraphicsManager.CONTENT_PATH + "Tile/");
                using (FileStream stream = new FileStream(Path.Combine(outputPath, GraphicsManager.CONTENT_PATH + "Tile/" + "index.idx"), FileMode.Create, FileAccess.Write))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                        tileIndex.Save(writer);
                }
            }

            //Data - merge copy everything including script
            //script will do fine with duplicate files being merged over, EXCEPT for strings files
            //TODO: only copy what is indexed for characters and portraits
            Directory.CreateDirectory(Path.Combine(outputPath, DataManager.DATA_PATH));

            //universal data, start params, etc.
            foreach (string subPath in Directory.GetFiles(PathMod.NoMod(DataManager.DATA_PATH)))
            {
                string path = Path.GetFileName(subPath);
                File.Copy(subPath, Path.Combine(outputPath, DataManager.DATA_PATH, path));
            }

            //actual data files
            copyRecursive(PathMod.NoMod(DataManager.DATA_PATH), Path.Combine(outputPath, DataManager.DATA_PATH));
            copyRecursive(PathMod.HardMod(DataManager.DATA_PATH), Path.Combine(outputPath, DataManager.DATA_PATH));
            //TODO: merge strings files for ground map strings instead of overwrite

            //save merged data indices
            foreach (DataManager.DataType type in Enum.GetValues(typeof(DataManager.DataType)))
            {
                if (type == DataManager.DataType.All || type == DataManager.DataType.None)
                    continue;

                EntryDataIndex idx = DataManager.GetIndex(type);
                using (Stream stream = new FileStream(Path.Combine(outputPath, DataManager.DATA_PATH + type.ToString() + "/" + "index.idx"), FileMode.Create, FileAccess.Write, FileShare.None))
                    Serializer.SerializeData(stream, idx);
            }


            DiagManager.Instance.LogInfo(String.Format("Standalone game output to {0}", outputPath));
        }

        private static void copyRecursive(string srcDir, string destDir)
        {
            if (!Directory.Exists(srcDir))
                return;

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (string directory in Directory.GetDirectories(srcDir))
                copyRecursive(directory, Path.Combine(destDir, Path.GetFileName(directory)));

            foreach (string file in Directory.GetFiles(srcDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
        }

        private static void mergeStringXmlRecursive(string srcDir, string destDir)
        {
            if (!Directory.Exists(srcDir))
                return;

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (string directory in Directory.GetDirectories(srcDir))
                mergeStringXmlRecursive(directory, Path.Combine(destDir, Path.GetFileName(directory)));

            foreach (string file in Directory.GetFiles(srcDir))
            {
                if (Path.GetExtension(file) == ".resx")
                    mergeStringXml(file, Path.Combine(destDir, Path.GetFileName(file)));
                else
                    File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
            }
        }

        private static void mergeStringXml(string srcPath, string destPath)
        {
            Dictionary<string, (string val, string comment)> srcDict = Text.LoadDevStringResx(srcPath);
            Dictionary<string, (string val, string comment)> destDict = Text.LoadDevStringResx(destPath);
            foreach (string key in srcDict.Keys)
                destDict[key] = srcDict[key];
            Text.SaveStringResx(destPath, destDict);
        }
    }
}
