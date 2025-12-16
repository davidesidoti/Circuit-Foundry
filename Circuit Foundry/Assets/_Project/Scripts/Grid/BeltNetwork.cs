using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircuitFoundry.Grid
{
    public enum GridDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public static class GridDirectionUtils
    {
        private static readonly Dictionary<GridDirection, Vector2Int> DirectionVectors = new()
        {
            { GridDirection.North, Vector2Int.up },
            { GridDirection.East, Vector2Int.right },
            { GridDirection.South, Vector2Int.down },
            { GridDirection.West, Vector2Int.left }
        };

        public static Vector2Int ToVector(GridDirection direction)
        {
            return DirectionVectors[direction];
        }

        public static GridDirection FromRotation(TileRotation rotation)
        {
            return rotation switch
            {
                TileRotation.North => GridDirection.North,
                TileRotation.East => GridDirection.East,
                TileRotation.South => GridDirection.South,
                TileRotation.West => GridDirection.West,
                _ => GridDirection.North
            };
        }

        public static GridDirection Opposite(GridDirection direction)
        {
            return direction switch
            {
                GridDirection.North => GridDirection.South,
                GridDirection.East => GridDirection.West,
                GridDirection.South => GridDirection.North,
                GridDirection.West => GridDirection.East,
                _ => GridDirection.North
            };
        }
    }

    public class BeltTile
    {
        public Vector2Int Cell { get; }
        public TileRotation Rotation { get; }
        public GridDirection Direction => GridDirectionUtils.FromRotation(Rotation);
        public BeltTile Input => GetNeighbor(GridDirectionUtils.Opposite(Direction));
        public BeltTile Output => GetNeighbor(Direction);
        public BeltTile North => GetNeighbor(GridDirection.North);
        public BeltTile East => GetNeighbor(GridDirection.East);
        public BeltTile South => GetNeighbor(GridDirection.South);
        public BeltTile West => GetNeighbor(GridDirection.West);

        private readonly Dictionary<GridDirection, BeltTile> neighbors = new();

        public BeltTile(GridOccupant occupant)
        {
            Cell = occupant.Origin;
            Rotation = occupant.Rotation;
        }

        public void ClearNeighbors()
        {
            neighbors.Clear();
        }

        public void SetNeighbor(GridDirection direction, BeltTile tile)
        {
            neighbors[direction] = tile;
        }

        public BeltTile GetNeighbor(GridDirection direction)
        {
            return neighbors.TryGetValue(direction, out var neighbor) ? neighbor : null;
        }
    }

    public class BeltNetwork : MonoBehaviour
    {
        [SerializeField] private GridManager grid;

        private readonly Dictionary<Vector2Int, BeltTile> beltTiles = new();

        public IReadOnlyDictionary<Vector2Int, BeltTile> BeltTiles => beltTiles;

        private static readonly GridDirection[] Directions =
        {
            GridDirection.North,
            GridDirection.East,
            GridDirection.South,
            GridDirection.West
        };

        private void Awake()
        {
            if (grid == null)
            {
                grid = FindObjectOfType<GridManager>();
            }

            BuildFromExisting();
        }

        private void Start()
        {
            // Rebuild after other startup scripts (e.g. bootstrap placements) have run.
            BuildFromExisting();
        }

        public bool TryGet(Vector2Int cell, out BeltTile tile)
        {
            return beltTiles.TryGetValue(cell, out tile);
        }

        public BeltTile AddOrUpdate(GridOccupant occupant)
        {
            if (occupant.Layer != GridLayer.Belt)
            {
                return null;
            }

            var beltTile = new BeltTile(occupant);
            beltTiles[occupant.Origin] = beltTile;
            RefreshConnectionsAround(occupant.Origin);
            return beltTile;
        }

        public void RemoveAt(Vector2Int cell)
        {
            if (!beltTiles.Remove(cell))
            {
                return;
            }

            RefreshConnectionsAround(cell);
        }

        private void BuildFromExisting()
        {
            beltTiles.Clear();
            if (grid == null)
            {
                return;
            }

            foreach (var occupant in grid.GetOccupants(GridLayer.Belt))
            {
                beltTiles[occupant.Origin] = new BeltTile(occupant);
            }

            RefreshAllConnections();
        }

        private void RefreshAllConnections()
        {
            foreach (var kvp in beltTiles)
            {
                RefreshTile(kvp.Key);
            }
        }

        private void RefreshConnectionsAround(Vector2Int cell)
        {
            RefreshTile(cell);
            foreach (var dir in Directions)
            {
                RefreshTile(cell + GridDirectionUtils.ToVector(dir));
            }
        }

        private void RefreshTile(Vector2Int cell)
        {
            if (!beltTiles.TryGetValue(cell, out var tile))
            {
                return;
            }

            tile.ClearNeighbors();
            foreach (var dir in Directions)
            {
                var neighborPos = cell + GridDirectionUtils.ToVector(dir);
                if (beltTiles.TryGetValue(neighborPos, out var neighbor))
                {
                    tile.SetNeighbor(dir, neighbor);
                    neighbor.SetNeighbor(GridDirectionUtils.Opposite(dir), tile);
                }
            }
        }
    }
}
