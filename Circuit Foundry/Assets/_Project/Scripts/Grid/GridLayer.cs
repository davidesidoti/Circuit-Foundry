using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CircuitFoundry.Grid
{
    public enum GridLayer
    {
        Belt = 0,
        Machine = 1,
        Decor = 2
    }

    public enum TileType
    {
        Single = 0,
        Line = 1,
        Block2X2 = 2,
        CornerL = 3
    }

    public enum TileRotation
    {
        North = 0,
        East = 90,
        South = 180,
        West = 270
    }

    [Serializable]
    public struct TileDefinition
    {
        [SerializeField] private TileType type;
        [SerializeField] private Vector2Int[] footprint;

        public TileType Type => type;
        public IReadOnlyList<Vector2Int> Footprint => footprint;

        public TileDefinition(TileType type, IEnumerable<Vector2Int> footprint)
        {
            this.type = type;
            this.footprint = footprint?.ToArray() ?? Array.Empty<Vector2Int>();
        }

        public IEnumerable<Vector2Int> GetCells(TileRotation rotation)
        {
            foreach (var cell in footprint)
            {
                yield return Rotate(cell, rotation);
            }
        }

        public static Vector2Int Rotate(Vector2Int cell, TileRotation rotation)
        {
            return rotation switch
            {
                TileRotation.East => new Vector2Int(cell.y, -cell.x),
                TileRotation.South => new Vector2Int(-cell.x, -cell.y),
                TileRotation.West => new Vector2Int(-cell.y, cell.x),
                _ => cell
            };
        }

        public static List<TileDefinition> CreateDefaultSet()
        {
            return new List<TileDefinition>
            {
                new TileDefinition(TileType.Single, new[] { Vector2Int.zero }),
                new TileDefinition(TileType.Line, new[] { Vector2Int.zero, Vector2Int.right }),
                new TileDefinition(
                    TileType.Block2X2,
                    new[]
                    {
                        Vector2Int.zero,
                        Vector2Int.right,
                        Vector2Int.up,
                        Vector2Int.right + Vector2Int.up
                    }),
                new TileDefinition(TileType.CornerL, new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up })
            };
        }
    }

    [Serializable]
    public struct GridBounds
    {
        [SerializeField] private Vector2Int origin;
        [SerializeField] private Vector2Int size;

        public Vector2Int Origin => origin;
        public Vector2Int Size => size;

        public GridBounds(Vector2Int origin, Vector2Int size)
        {
            this.origin = origin;
            this.size = size;
        }

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= origin.x &&
                   cell.x < origin.x + size.x &&
                   cell.y >= origin.y &&
                   cell.y < origin.y + size.y;
        }
    }
}
