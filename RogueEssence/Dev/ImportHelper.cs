﻿using System;
using System.Collections.Generic;
using System.IO;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using System.Diagnostics;
using System.Xml;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RogueEssence.Dev
{
    public class ImportHelper
    {
        public static void BuildCharIndex(string cachePattern)
        {
            CharaIndexNode fullGuide = new CharaIndexNode();
            string search = Path.GetDirectoryName(String.Format(cachePattern, '*'));
            string pattern = Path.GetFileName(String.Format(cachePattern, '*'));
            try
            {
                foreach (string dir in Directory.GetFiles(PathMod.HardMod(search), pattern))
                {
                    string file = Path.GetFileNameWithoutExtension(dir);
                    int num = Convert.ToInt32(file);
                    using (FileStream stream = File.OpenRead(dir))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            CharaIndexNode speciesGuide = CharaIndexNode.Load(reader);
                            fullGuide.Nodes[num] = speciesGuide;
                        }
                    }

                }

                if (fullGuide.Nodes.Count > 0)
                {
                    using (FileStream stream = new FileStream(PathMod.HardMod(search + "/index.idx"), FileMode.Create, FileAccess.Write))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                            fullGuide.Save(writer);
                    }
                }
                else
                    File.Delete(PathMod.HardMod(search + "/index.idx"));
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(new Exception("Error importing index at " + search + "\n", ex));
            }
        }

        public static void BuildTileIndex(string cachePattern)
        {
            TileGuide fullGuide = new TileGuide();
            string search = Path.GetDirectoryName(String.Format(cachePattern, '*'));
            string pattern = Path.GetFileName(String.Format(cachePattern, '*'));
            try
            {
                foreach (string dir in Directory.GetFiles(PathMod.HardMod(search), pattern))
                {
                    string file = Path.GetFileNameWithoutExtension(dir);
                    using (FileStream stream = File.OpenRead(dir))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            TileIndexNode guide = TileIndexNode.Load(reader);
                            fullGuide.Nodes[file] = guide;
                        }
                    }
                }

                if (fullGuide.Nodes.Count > 0)
                {
                    using (FileStream stream = new FileStream(PathMod.HardMod(search + "/index.idx"), FileMode.Create, FileAccess.Write))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                            fullGuide.Save(writer);
                    }
                }
                else
                    File.Delete(PathMod.HardMod(search + "/index.idx"));
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(new Exception("Error importing index at " + search + "\n", ex));
            }

        }



        public delegate void BakeSpecies(string spriteDir, Dictionary<MonsterID, byte[]> spriteData, MonsterID formData);

        public static void ImportAllChars(string spriteRootDirectory, string cachePattern)
        {
            foreach (int index in GetAllNumberedDirs(spriteRootDirectory, ""))
            {
                DiagManager.Instance.LoadMsg = "Importing Charsheet #" + index;
                DiagManager.Instance.LogInfo(DiagManager.Instance.LoadMsg);
                ImportSpecies(spriteRootDirectory + index.ToString("D4") + "/", String.Format(cachePattern, index), BakeCharSheet, index);
            }
        }


        public static void BakeCharSheet(string spriteDir, Dictionary<MonsterID, byte[]> spriteData, MonsterID formData)
        {
            //check to see if files exist
            string[] pngs = Directory.GetFiles(spriteDir, "*.png", SearchOption.TopDirectoryOnly);
            if (pngs.Length < 1)
            {
                DiagManager.Instance.LogInfo("Skipped loading from " + Path.GetFullPath(spriteDir));
                return;
            }

            using (CharSheet sprite = CharSheet.Import(spriteDir))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                        sprite.Save(writer);

                    byte[] writingBytes = stream.ToArray();
                    spriteData[formData] = writingBytes;
                }
            }
            DiagManager.Instance.LogInfo("Loaded " + pngs.Length + " from " + Path.GetFullPath(spriteDir));
        }

        public static void ImportAllPortraits(string spriteRootDirectory, string cachePattern)
        {
            foreach (int index in GetAllNumberedDirs(spriteRootDirectory, ""))
            {
                DiagManager.Instance.LoadMsg = "Importing Portrait #" + index;
                DiagManager.Instance.LogInfo(DiagManager.Instance.LoadMsg);
                ImportSpecies(spriteRootDirectory + index.ToString("D4") + "/", String.Format(cachePattern, index), BakePortrait, index);
            }
        }

        public static void BakePortrait(string spriteDir, Dictionary<MonsterID, byte[]> spriteData, MonsterID formData)
        {
            //check to see if files exist
            string[] pngs = Directory.GetFiles(spriteDir, "*.png", SearchOption.TopDirectoryOnly);
            if (pngs.Length < 1)
            {
                DiagManager.Instance.LogInfo("Skipped loading from " + Path.GetFullPath(spriteDir));
                return;
            }

            using (PortraitSheet sprite = PortraitSheet.Import(spriteDir))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                        sprite.Save(writer);
                    byte[] writingBytes = stream.ToArray();
                    spriteData[formData] = writingBytes;
                }
            }
            DiagManager.Instance.LogInfo("Loaded " + pngs.Length + " files from " + Path.GetFullPath(spriteDir));
        }

        public static void ImportSpecies(string spriteDir, string destFile, BakeSpecies bakeMethod, int index)
        {
            try
            {
                if (Directory.Exists(spriteDir))
                {
                    //check main folder
                    Dictionary<MonsterID, byte[]> spriteData = new Dictionary<MonsterID, byte[]>();

                    bakeMethod(spriteDir, spriteData, new MonsterID(index, -1, -1, (Gender)(-1)));

                    //check all subfolders
                    foreach (int formDirs in GetAllNumberedDirs(spriteDir, ""))
                    {
                        //get subframes (discount if negative)
                        bakeMethod(spriteDir + formDirs.ToString("D4") + "/", spriteData, new MonsterID(index, formDirs, -1, (Gender)(-1)));

                        foreach (int skinDirs in GetAllNumberedDirs(spriteDir + formDirs.ToString("D4") + "/", ""))
                        {
                            //get subframes
                            bakeMethod(spriteDir + formDirs.ToString("D4") + "/" + skinDirs.ToString("D4") + "/",
                                spriteData, new MonsterID(index, formDirs, skinDirs, (Gender)(-1)));

                            foreach (int genderDirs in GetAllNumberedDirs(spriteDir + formDirs.ToString("D4") + "/" + skinDirs.ToString("D4") + "/", ""))
                            {
                                //get subframes
                                bakeMethod(spriteDir + formDirs.ToString("D4") + "/" + skinDirs.ToString("D4") + "/" + genderDirs.ToString("D4") + "/",
                                    spriteData, new MonsterID(index, formDirs, skinDirs, (Gender)genderDirs));
                            }
                        }
                    }

                    SaveSpecies(destFile, spriteData);
                }
                else
                    DiagManager.Instance.LogInfo("Couldn't find " + Path.GetFullPath(spriteDir) + " to load from!");
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(new Exception("Error importing #" + index + "\n", ex));
            }
        }


        public static void SaveSpecies(string destinationPath, Dictionary<MonsterID, byte[]> spriteData)
        {
            if (spriteData.Count == 0)
            {
                DiagManager.Instance.LogInfo("Skipped data for " + Path.GetFullPath(destinationPath));
                return;
            }

            //generate formtree
            CharaIndexNode guide = new CharaIndexNode();
            Dictionary<MonsterID, long> spritePositions = new Dictionary<MonsterID, long>();
            long currentPosition = 0;
            foreach (MonsterID key in spriteData.Keys)
            {
                spritePositions[key] = currentPosition;
                currentPosition += spriteData[key].LongLength;
            }
            foreach (MonsterID key in spritePositions.Keys)
                guide.AddSubValue(0, key.Form, key.Skin, (int)key.Gender);

            using (FileStream stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    //save the guide
                    guide.Save(writer);

                    //update how much space it takes
                    foreach (MonsterID key in spritePositions.Keys)
                        guide.AddSubValue(spritePositions[key] + writer.BaseStream.Position, key.Form, key.Skin, (int)key.Gender);

                    //save it again
                    writer.Seek(0, SeekOrigin.Begin);
                    guide.Save(writer);

                    //save data
                    foreach (byte[] formData in spriteData.Values)
                        writer.Write(formData);
                }
            }
            DiagManager.Instance.LogInfo("Wrote data to " + Path.GetFullPath(destinationPath));
        }


        public static string[] TILE_TITLES = { "wall", "ground", "water" };
        private static readonly string[] TILE_NODE_TITLES = { "Wall", "Floor", "Secondary" };

        public static int GetDirSize(string dirString)
        {
            string fileName = Path.GetFileName(dirString);

            string[] sides = fileName.Split("x");
            if (sides.Length != 2)
                return 0;

            if (sides[0] != sides[1])
                return 0;

            int tileSize = 0;
            if (!Int32.TryParse(sides[0], out tileSize))
                return 0;

            if (tileSize % 8 != 0)
                return 0;

            return tileSize;
        }

        public static void ImportAllTiles(string sourceDir, string cachePattern, bool includeTile, bool includeAutotile)
        {
            string[] sizeDirs = Directory.GetDirectories(sourceDir);
            foreach (string sizeDir in sizeDirs)
            {
                int tileSize = GetDirSize(sizeDir);
                ImportAllTiles(sizeDir, cachePattern, includeTile, includeAutotile, tileSize);
            }
        }

        public static void ImportAllTiles(string sourceDir, string cachePattern, bool includeTile, bool includeAutotile, int tileSize)
        {
            if (tileSize == 0)
                return;

            if (includeTile)
            {
                string[] dirs = Directory.GetFiles(sourceDir, "*.png");
                //go through each sprite folder, and each form folder
                for (int ii = 0; ii < dirs.Length; ii++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(dirs[ii]);
                    string outputFile = String.Format(cachePattern, fileName);

                    try
                    {
                        DiagManager.Instance.LoadMsg = "Importing Tile " + fileName;
                        using (BaseSheet tileset = BaseSheet.Import(dirs[ii]))
                        {
                            List<BaseSheet[]> tileList = new List<BaseSheet[]>();
                            tileList.Add(new BaseSheet[] { tileset });
                            SaveTileSheet(tileList, outputFile, tileSize);
                        }
                    }

                    catch (Exception ex)
                    {
                        DiagManager.Instance.LogError(new Exception("Error importing " + fileName + "\n", ex));
                    }
                }
            }

            if (includeAutotile)
            {
                string[] dirs = Directory.GetDirectories(sourceDir);
                for (int ii = 0; ii < dirs.Length; ii++)
                {
                    string fileName = Path.GetFileName(dirs[ii]);
                    string[] info = fileName.Split('.');
                    string outputFile = String.Format(cachePattern, info[0]);
                    DiagManager.Instance.LoadMsg = "Importing " + info[0];

                    try
                    {
                        List<BaseSheet[]> tileList = new List<BaseSheet[]>();
                        foreach (string tileTitle in TILE_TITLES)
                        {
                            int layerIndex = 0;
                            while (true)
                            {
                                string[] layers = Directory.GetFiles(dirs[ii], tileTitle + "." + String.Format("{0:D2}", layerIndex) + ".*");
                                if (layers.Length == 1)
                                {
                                    BaseSheet tileset = BaseSheet.Import(layers[0]);
                                    tileList.Add(new BaseSheet[]{ tileset });
                                }
                                else if (layers.Length > 1)
                                {
                                    throw new Exception("More files than expected");
                                }
                                else
                                {
                                    break;
                                }
                                layerIndex++;
                            }
                        }
                        SaveTileSheet(tileList, outputFile, tileSize);
                        foreach (BaseSheet[] arr in tileList)
                            foreach(BaseSheet tex in arr)
                                tex.Dispose();
                    }
                    catch (Exception ex)
                    {
                        DiagManager.Instance.LogError(new Exception("Error importing " + fileName + "\n", ex));
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tileList">A list of arrays of Basesheets.  Array members will be arranged vertically.  List members horizontally.</param>
        /// <param name="destFile"></param>
        /// <param name="tileSize"></param>
        public static void SaveTileSheet(List<BaseSheet[]> tileList, string destFile, int tileSize)
        {
            Dictionary<Loc, int> tileData = new Dictionary<Loc, int>();
            List<byte[]> tileBytes = new List<byte[]>();
            List<long> positions = new List<long>();
            long currentPosition = 0;

            int curTileWidth = 0;
            foreach (BaseSheet[] arr in tileList)
            {
                int maxTileWidth = 0;
                int totalTileHeight = 0;
                foreach (BaseSheet sheet in arr)
                {
                    int count = (sheet.Width / tileSize) * (sheet.Height / tileSize);

                    // Write header information about each tile, skip blanks
                    for (int ii = 0; ii < count; ii++)
                    {
                        int x = ii % (sheet.Width / tileSize);
                        int y = ii / (sheet.Width / tileSize);
                        //check if blank
                        if (sheet.IsBlank(x * tileSize, y * tileSize, tileSize, tileSize))
                        {
                            //don't add
                        }
                        else
                        {
                            //cut off the corresponding piece
                            using (BaseSheet tileTex = new BaseSheet(tileSize, tileSize))
                            {
                                tileTex.Blit(sheet, x * tileSize, y * tileSize, tileSize, tileSize, 0, 0);

                                //save as a PNG to a stream
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    using (BinaryWriter mw = new BinaryWriter(ms))
                                        tileTex.Save(mw);
                                    byte[] bytes = ms.ToArray();

                                    int existingData = -1;
                                    for (int nn = 0; nn < tileBytes.Count; nn++)
                                    {
                                        if (bytes.Length == tileBytes[nn].Length && bytes.SequenceEqual(tileBytes[nn]))
                                        {
                                            existingData = nn;
                                            break;
                                        }
                                    }

                                    if (existingData == -1)
                                    {
                                        tileData.Add(new Loc(x + curTileWidth, y + totalTileHeight), tileBytes.Count);
                                        positions.Add(currentPosition);
                                        tileBytes.Add(bytes);
                                        currentPosition += bytes.LongLength;
                                    }
                                    else
                                        tileData.Add(new Loc(x + curTileWidth, y + totalTileHeight), existingData);
                                }
                            }
                        }
                    }
                    maxTileWidth = Math.Max(maxTileWidth, (sheet.Width / tileSize));
                    totalTileHeight += (sheet.Height / tileSize);
                }
                curTileWidth += maxTileWidth;
            }

            //generate tileguide
            TileIndexNode tileGuide = new TileIndexNode();
            tileGuide.TileSize = tileSize;
            Dictionary<Loc, long> spritePositions = new Dictionary<Loc, long>();
            foreach (Loc key in tileData.Keys)
                spritePositions[key] = positions[tileData[key]];

            foreach (Loc key in spritePositions.Keys)
                tileGuide.Positions[key] = 0;


            using (FileStream spriteStream = new FileStream(destFile, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(spriteStream))
                {
                    //save the guide
                    tileGuide.Save(writer);

                    //update how much space it takes
                    foreach (Loc key in spritePositions.Keys)
                        tileGuide.Positions[key] = writer.BaseStream.Position + spritePositions[key];

                    //save it again
                    writer.Seek(0, SeekOrigin.Begin);
                    tileGuide.Save(writer);

                    //save data
                    foreach (byte[] data in tileBytes)
                        writer.Write(data);
                }
            }
        }

        public static void ExportTileSheet(string inputPath, string outputPath)
        {
            if (File.Exists(inputPath))
            {
                using (FileStream stream = File.OpenRead(inputPath))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        TileIndexNode guide = TileIndexNode.Load(reader);
                        Loc maxPositions = Loc.Zero;
                        Dictionary<Loc, Color[]> tiles = new Dictionary<Loc, Color[]>();
                        foreach (Loc loc in guide.Positions.Keys)
                        {
                            maxPositions = new Loc(Math.Max(loc.X, maxPositions.X), Math.Max(loc.Y, maxPositions.Y));
                            long tilePos = guide.Positions[loc];
                            reader.BaseStream.Seek(tilePos, SeekOrigin.Begin);
                            using (BaseSheet sheet = BaseSheet.Load(reader))
                                tiles.Add(loc, BaseSheet.GetData(sheet));
                        }
                        maxPositions = maxPositions + Loc.One;
                        Point imgSize = new Point(maxPositions.X * guide.TileSize, maxPositions.Y * guide.TileSize);
                        Color[] tilesetColors = new Color[imgSize.X * imgSize.Y];
                        foreach (Loc loc in tiles.Keys)
                        {
                            BaseSheet.Blit(tiles[loc], tilesetColors, new Point(guide.TileSize, guide.TileSize), imgSize,
                            new Point(loc.X * guide.TileSize, loc.Y * guide.TileSize), SpriteEffects.None);
                        }
                        BaseSheet.ExportColors(outputPath, tilesetColors, imgSize);
                    }
                }
            }
        }

        public static void ImportAllBeams(string sourceDir, string cachePattern)
        {
            string[] dirs = Directory.GetDirectories(sourceDir);
            foreach (string dir in dirs)
            {
                string fileName = Path.GetFileNameWithoutExtension(dir);
                string asset_name = fileName;
                using (BeamSheet sheet = BeamSheet.Import(dir + "/"))
                {
                    using (FileStream stream = File.OpenWrite(String.Format(cachePattern, asset_name)))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                            sheet.Save(writer);
                    }
                }
            }
        }

        /// <summary>
        /// Bakes all multi-directional spritesheets specified in the directory.
        /// </summary>
        /// <param name="sourceDir">Parent directory of the input files.</param>
        /// <param name="cachePattern">Pattern expression to save the output files as.</param>
        public static void ImportAllNameDirs(string sourceDir, string cachePattern)
        {
            string[] dirs = Directory.GetFiles(sourceDir, "*.png");
            foreach (string dir in dirs)
            {
                string fileName = Path.GetFileNameWithoutExtension(dir);
                string[] components = fileName.Split('.');
                string asset_name = components[0];

                using (DirSheet sheet = DirSheet.Import(dir))
                {
                    using (FileStream stream = File.OpenWrite(String.Format(cachePattern, asset_name)))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                            sheet.Save(writer);
                    }
                }
            }
            dirs = Directory.GetDirectories(sourceDir);
            foreach (string dir in dirs)
            {
                string fileName = Path.GetFileNameWithoutExtension(dir);
                string asset_name = fileName;
                using (DirSheet sheet = DirSheet.Import(dir + "/"))
                {
                    using (FileStream stream = File.OpenWrite(String.Format(cachePattern, asset_name)))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                            sheet.Save(writer);
                    }
                }
            }
        }

        public static void ImportAllFonts(string sourceDir, string cachePattern)
        {
            string[] fonts = new string[] { "system", "green", "blue", "yellow", "text", "banner" };
            //go through each font folder
            for (int ii = 0; ii < fonts.Length; ii++)
            {
                using (FontSheet font = FontSheet.Import(Path.Combine(sourceDir, fonts[ii]) + "/"))
                {
                    //using (FileStream stream = new FileStream(String.Format(cachePattern, fonts[ii] + ".png"), FileMode.Create, FileAccess.Write))
                    //    font.Export(stream);

                    using (FileStream stream = new FileStream(String.Format(cachePattern, fonts[ii]), FileMode.Create, FileAccess.Write))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                            font.Save(writer);
                    }
                }
            }
        }

        public static IEnumerable<int> GetAllNumberedDirs(string spriteRootDirectory, string dirName)
        {
            string[] dirs = Directory.GetDirectories(spriteRootDirectory, dirName + "*", SearchOption.TopDirectoryOnly);

            for (int ii = 0; ii < dirs.Length; ii++)
            {
                string num = dirs[ii].Substring((spriteRootDirectory + dirName).Length);
                int value;
                if (Int32.TryParse(num, out value))
                    yield return value;
            }
        }

        /// <summary>
        /// Reads all tileset folders from the input directory, and creates autotiles from them.
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="cacheDir"></param>
        public static void ImportAllAutoTiles(string sourceDir, string cacheDir)
        {
            string[] sizeDirs = Directory.GetDirectories(sourceDir);
            foreach (string sizeDir in sizeDirs)
            {
                int tileSize = GetDirSize(sizeDir);
                if (tileSize == 0)
                    continue;


                string[] dirs = Directory.GetDirectories(sizeDir);
                for (int ii = 0; ii < dirs.Length; ii++)
                {
                    string fileName = Path.GetFileName(dirs[ii]);
                    //string[] info = fileName.Split('.');
                    string outputName = fileName;


                    DiagManager.Instance.LoadMsg = "Importing " + outputName;

                    // Read XML for layer mapping
                    XmlDocument document = new XmlDocument();
                    document.Load(Path.Join(sizeDir, fileName, "tileset.xml"));

                    int TOTAL_TILES = 47;

                    try
                    {
                        int curTileWidth = 0;
                        for (int nn = 0; nn < TILE_TITLES.Length; nn++)
                        {
                            string tileTitle = TILE_TITLES[nn];
                            string tileNodeTitle = TILE_NODE_TITLES[nn];
                            XmlNode node = document.SelectSingleNode("//LegacyDungeonTileset/RogueEssence/" + tileNodeTitle);
                            int index = -1;
                            if (node != null)
                                index = Int32.Parse(node.InnerText);

                            AutoTileData autoTile = new AutoTileData();
                            AutoTileAdjacent entry = new AutoTileAdjacent();

                            List<TileLayer>[][] totalArray = new List<TileLayer>[48][];

                            for (int jj = 0; jj < TOTAL_TILES; jj++)
                            {
                                totalArray[jj] = new List<TileLayer>[3];
                                for (int kk = 0; kk < 3; kk++)
                                    totalArray[jj][kk] = new List<TileLayer>();
                            }

                            int layerIndex = 0;
                            while (true)
                            {
                                string[] layers = Directory.GetFiles(dirs[ii] + "/", tileTitle + "." + String.Format("{0:D2}", layerIndex) + ".*");
                                if (layers.Length == 1)
                                {
                                    string layerName = Path.GetFileNameWithoutExtension(layers[0]);
                                    string[] layerInfo = layerName.Split('.');

                                    using (BaseSheet tileset = BaseSheet.Import(layers[0]))
                                    {
                                        int frameLength = Convert.ToInt32(layerInfo[2]);
                                        if (frameLength == 0)
                                            frameLength = 60;
                                        int maxVariants = Convert.ToInt32(layerInfo[3]);


                                        int maxFrames = tileset.Width / tileSize / maxVariants;

                                        for (int jj = 0; jj < TOTAL_TILES; jj++)
                                        {
                                            for (int kk = 0; kk < maxVariants; kk++)
                                            {
                                                //go through each layer
                                                TileLayer anim = new TileLayer();
                                                anim.FrameLength = frameLength;

                                                for (int mm = 0; mm < maxFrames; mm++)
                                                {
                                                    //keep adding more tiles to the anim until end of blank spot is found
                                                    if (!tileset.IsBlank((kk * maxFrames + mm) * tileSize, jj * tileSize, tileSize, tileSize))
                                                        anim.Frames.Add(new TileFrame(new Loc(kk * maxFrames + mm + curTileWidth, jj), outputName));
                                                }

                                                if (anim.Frames.Count > 0)
                                                    totalArray[jj][kk].Add(anim);
                                            }
                                        }
                                        curTileWidth += (tileset.Width / tileSize);
                                    }
                                }
                                else if (layers.Length > 1)
                                {
                                    throw new Exception("More files than expected");
                                }
                                else
                                {
                                    break;
                                }
                                layerIndex++;
                            }

                            if (layerIndex > 0)
                            {
                                if (index == -1)
                                {
                                    throw new KeyNotFoundException(String.Format(
                                        "Layer index mapping for layer {0} for {1} missing.", tileTitle, fileName));
                                }

                                ImportTileVariant(entry.Tilex00, totalArray[0]);
                                ImportTileVariant(entry.Tilex01, totalArray[1]);
                                ImportTileVariant(entry.Tilex02, totalArray[2]);
                                ImportTileVariant(entry.Tilex03, totalArray[3]);
                                ImportTileVariant(entry.Tilex13, totalArray[4]);
                                ImportTileVariant(entry.Tilex04, totalArray[5]);
                                ImportTileVariant(entry.Tilex05, totalArray[6]);
                                ImportTileVariant(entry.Tilex06, totalArray[7]);
                                ImportTileVariant(entry.Tilex26, totalArray[8]);
                                ImportTileVariant(entry.Tilex07, totalArray[9]);
                                ImportTileVariant(entry.Tilex17, totalArray[10]);
                                ImportTileVariant(entry.Tilex27, totalArray[11]);
                                ImportTileVariant(entry.Tilex37, totalArray[12]);
                                ImportTileVariant(entry.Tilex08, totalArray[13]);
                                ImportTileVariant(entry.Tilex09, totalArray[14]);
                                ImportTileVariant(entry.Tilex89, totalArray[15]);
                                ImportTileVariant(entry.Tilex0A, totalArray[16]);
                                ImportTileVariant(entry.Tilex0B, totalArray[17]);
                                ImportTileVariant(entry.Tilex1B, totalArray[18]);
                                ImportTileVariant(entry.Tilex8B, totalArray[19]);
                                ImportTileVariant(entry.Tilex9B, totalArray[20]);
                                ImportTileVariant(entry.Tilex0C, totalArray[21]);
                                ImportTileVariant(entry.Tilex4C, totalArray[22]);
                                ImportTileVariant(entry.Tilex0D, totalArray[23]);
                                ImportTileVariant(entry.Tilex4D, totalArray[24]);
                                ImportTileVariant(entry.Tilex8D, totalArray[25]);
                                ImportTileVariant(entry.TilexCD, totalArray[26]);
                                ImportTileVariant(entry.Tilex0E, totalArray[27]);
                                ImportTileVariant(entry.Tilex2E, totalArray[28]);
                                ImportTileVariant(entry.Tilex4E, totalArray[29]);
                                ImportTileVariant(entry.Tilex6E, totalArray[30]);
                                ImportTileVariant(entry.Tilex0F, totalArray[31]);
                                ImportTileVariant(entry.Tilex1F, totalArray[32]);
                                ImportTileVariant(entry.Tilex2F, totalArray[33]);
                                ImportTileVariant(entry.Tilex3F, totalArray[34]);
                                ImportTileVariant(entry.Tilex4F, totalArray[35]);
                                ImportTileVariant(entry.Tilex5F, totalArray[36]);
                                ImportTileVariant(entry.Tilex6F, totalArray[37]);
                                ImportTileVariant(entry.Tilex7F, totalArray[38]);
                                ImportTileVariant(entry.Tilex8F, totalArray[39]);
                                ImportTileVariant(entry.Tilex9F, totalArray[40]);
                                ImportTileVariant(entry.TilexAF, totalArray[41]);
                                ImportTileVariant(entry.TilexBF, totalArray[42]);
                                ImportTileVariant(entry.TilexCF, totalArray[43]);
                                ImportTileVariant(entry.TilexDF, totalArray[44]);
                                ImportTileVariant(entry.TilexEF, totalArray[45]);
                                ImportTileVariant(entry.TilexFF, totalArray[46]);

                                autoTile.Tiles = entry;

                                autoTile.Name = new LocalText(outputName + tileTitle);

                                DataManager.SaveData(index, DataManager.DataType.AutoTile.ToString(), autoTile);
                                Debug.WriteLine(String.Format("{0:D3}: {1}", index, autoTile.Name));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DiagManager.Instance.LogError(new Exception("Error importing " + outputName + "\n", ex));
                    }
                }
            }
        }

        private static void ImportTileVariant(List<List<TileLayer>> list, List<TileLayer>[] data)
        {
            //add the variant to the appropriate entry list
            for (int kk = 0; kk < 3; kk++)
            {
                if (data[kk].Count > 0)
                    list.Add(data[kk]);
            }
        }
    }
}
