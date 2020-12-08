﻿using System;
using RogueElements;
using System.Linq;
using System.Collections.Generic;

namespace RogueEssence.Dungeon
{
    [Serializable]
    public class MapLayer
    {
        public string Name { get; set; }
        public bool Front { get; set; }
        public bool Visible { get; set; }

        public AutoTile[][] Tiles;

        public int Width => Tiles.Length;
        public int Height => Tiles[0].Length;

        public MapLayer(string name)
        {
            Name = name;
            Visible = true;
        }

        protected MapLayer(MapLayer other)
        {
            Name = other.Name;
            Front = other.Front;
            Visible = other.Visible;


            Tiles = new AutoTile[other.Tiles.Length][];
            for (int ii = 0; ii < other.Tiles.Length; ii++)
            {
                Tiles[ii] = new AutoTile[other.Tiles[0].Length];
                for (int jj = 0; jj < other.Tiles[0].Length; jj++)
                    Tiles[ii][jj] = other.Tiles[ii][jj].Copy();
            }
        }

        public MapLayer Clone() { return new MapLayer(this); }

        public void CreateNew(int width, int height)
        {
            Tiles = new AutoTile[width][];
            for (int ii = 0; ii < width; ii++)
            {
                Tiles[ii] = new AutoTile[height];
                for (int jj = 0; jj < height; jj++)
                    Tiles[ii][jj] = new AutoTile();
            }
        }

        public void ResizeJustified(int width, int height, Dir8 anchorDir)
        {
            Grid.LocAction changeOp = (Loc effectLoc) => { };
            Grid.LocAction newOp = (Loc effectLoc) => { Tiles[effectLoc.X][effectLoc.Y] = new AutoTile(); };

            Loc diff = Grid.ResizeJustified(ref Tiles,
                width, height, anchorDir.Reverse(), changeOp, newOp);
        }


        public void CalculateAutotiles(ReRandom rand, Loc rectStart, Loc rectSize)
        {
            HashSet<int> floortilesets = new HashSet<int>();
            for (int ii = rectStart.X; ii < rectStart.X + rectSize.X; ii++)
            {
                for (int jj = rectStart.Y; jj < rectStart.Y + rectSize.Y; jj++)
                {
                    if (Collision.InBounds(Width, Height, new Loc(ii, jj)))
                    {
                        if (Tiles[ii][jj].AutoTileset > -1)
                            floortilesets.Add(Tiles[ii][jj].AutoTileset);
                    }
                }
            }
            foreach (int tileset in floortilesets)
            {
                Data.AutoTileData entry = Data.DataManager.Instance.GetAutoTile(tileset);
                entry.Tiles.AutoTileArea(rand, rectStart, rectSize,
                    (int x, int y, List<TileLayer> tile) =>
                    {
                        if (Collision.InBounds(Width, Height, new Loc(x, y)))
                            Tiles[x][y].Layers = tile;
                    },
                    (int x, int y) =>
                    {
                        if (!Collision.InBounds(Width, Height, new Loc(x, y)))
                            return true;
                        if (Tiles[x][y].AutoTileset != -1 && Tiles[x][y].AutoTileset == Tiles[x][y].BorderTileset)
                            return false;
                        return Tiles[x][y].AutoTileset == tileset;
                    });
            }
        }

        public override string ToString()
        {
            return (Front ? "[Front] " : "") + Name;
        }
    }
}

