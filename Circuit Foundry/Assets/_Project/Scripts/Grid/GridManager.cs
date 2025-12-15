using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircuitFoundry.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private Vector3 worldOrigin = Vector3.zero;
        [SerializeField] private Vector2Int gridOrigin = Vector2Int.zero;
        [SerializeField] private Vector2Int gridSize = new Vector2Int(16, 16);
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [SerializeField] private bool enforceBounds = true;
        [SerializeField] private List<TileDefinition> tileDefinitions = new List<TileDefinition>();

        private readonly Dictionary<GridLayer, Dictionary<Vector2Int, GridOccupant>> occupantsByOrigin = new();
        private readonly Dictionary<GridLayer, Dictionary<Vector2Int, GridOccupant>> occupantsByCell = new();
        private Dictionary<TileType, TileDefinition> definitionLookup = new();

        public GridBounds Bounds => new GridBounds(gridOrigin, gridSize);
        public float CellSize => cellSize;
        public Vector3 WorldOrigin => worldOrigin;
        public Vector2Int GridOrigin => gridOrigin;
        public Vector2Int GridSize => gridSize;

        private void Awake()
        {
            EnsureLayers();
            BuildDefinitions();
        }

        private void OnValidate()
        {
            cellSize = Mathf.Max(0.01f, cellSize);
            gridSize.x = Mathf.Max(1, gridSize.x);
            gridSize.y = Mathf.Max(1, gridSize.y);
            EnsureLayers();
            BuildDefinitions();
        }

        public Vector3 GridToWorldCenter(Vector2Int cell, float heightOffset = 0f)
        {
            var offset = new Vector3(
                (cell.x - gridOrigin.x) * cellSize,
                0f,
                (cell.y - gridOrigin.y) * cellSize);
            return worldOrigin + offset + new Vector3(0f, heightOffset, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            var relative = worldPosition - worldOrigin;
            var x = Mathf.RoundToInt(relative.x / cellSize) + gridOrigin.x;
            var y = Mathf.RoundToInt(relative.z / cellSize) + gridOrigin.y;
            return new Vector2Int(x, y);
        }

        public Bounds GetWorldBounds(float height = 0f)
        {
            var size = new Vector3(gridSize.x * cellSize, 0f, gridSize.y * cellSize);
            var corner = worldOrigin - new Vector3(0.5f * cellSize, 0f, 0.5f * cellSize);
            var center = corner + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            center.y = height;
            return new Bounds(center, size);
        }

        public bool IsCellFree(Vector2Int cell, GridLayer layer)
        {
            EnsureLayers();
            if (enforceBounds && !Bounds.Contains(cell))
            {
                return false;
            }

            return !occupantsByCell[layer].ContainsKey(cell);
        }

        public bool TryGetOccupantAtCell(Vector2Int cell, GridLayer layer, out GridOccupant occupant)
        {
            EnsureLayers();
            return occupantsByCell[layer].TryGetValue(cell, out occupant);
        }

        public bool Place(Vector2Int origin, TileType tileType, GridLayer layer, TileRotation rotation)
        {
            return Place(origin, tileType, layer, rotation, out _);
        }

        public bool Place(Vector2Int origin, TileType tileType, GridLayer layer, TileRotation rotation, out GridOccupant occupant)
        {
            EnsureLayers();
            occupant = null;

            if (!definitionLookup.TryGetValue(tileType, out var definition))
            {
                Debug.LogWarning($"No tile definition found for {tileType}", this);
                return false;
            }

            var footprint = BuildFootprint(origin, definition, rotation);
            if (!IsAreaFree(footprint, layer))
            {
                return false;
            }

            occupant = new GridOccupant(tileType, layer, origin, rotation, footprint);
            occupantsByOrigin[layer][origin] = occupant;
            foreach (var cell in footprint)
            {
                occupantsByCell[layer][cell] = occupant;
            }

            return true;
        }

        public bool Remove(Vector2Int origin, GridLayer layer)
        {
            EnsureLayers();
            if (!occupantsByOrigin[layer].TryGetValue(origin, out var occupant))
            {
                return false;
            }

            foreach (var cell in occupant.OccupiedCells)
            {
                occupantsByCell[layer].Remove(cell);
            }

            occupantsByOrigin[layer].Remove(origin);
            return true;
        }

        public bool RemoveAtCell(Vector2Int cell, GridLayer layer)
        {
            EnsureLayers();
            if (!occupantsByCell[layer].TryGetValue(cell, out var occupant))
            {
                return false;
            }

            return Remove(occupant.Origin, layer);
        }

        public void ClearLayer(GridLayer layer)
        {
            EnsureLayers();
            occupantsByOrigin[layer].Clear();
            occupantsByCell[layer].Clear();
        }

        public void ClearAll()
        {
            EnsureLayers();
            foreach (GridLayer layer in Enum.GetValues(typeof(GridLayer)))
            {
                ClearLayer(layer);
            }
        }

        public IEnumerable<KeyValuePair<Vector2Int, GridOccupant>> GetOccupiedCells(GridLayer layer)
        {
            EnsureLayers();
            foreach (var kvp in occupantsByCell[layer])
            {
                yield return kvp;
            }
        }

        public IEnumerable<GridOccupant> GetOccupants(GridLayer layer)
        {
            EnsureLayers();
            return occupantsByOrigin[layer].Values;
        }

        public IEnumerable<GridLayer> AllLayers()
        {
            return (GridLayer[])Enum.GetValues(typeof(GridLayer));
        }

        private void BuildDefinitions()
        {
            if (tileDefinitions == null)
            {
                tileDefinitions = new List<TileDefinition>();
            }

            if (tileDefinitions.Count == 0)
            {
                tileDefinitions = TileDefinition.CreateDefaultSet();
            }

            definitionLookup = new Dictionary<TileType, TileDefinition>();
            foreach (var definition in tileDefinitions)
            {
                definitionLookup[definition.Type] = definition;
            }
        }

        private void EnsureLayers()
        {
            foreach (GridLayer layer in Enum.GetValues(typeof(GridLayer)))
            {
                if (!occupantsByOrigin.ContainsKey(layer))
                {
                    occupantsByOrigin[layer] = new Dictionary<Vector2Int, GridOccupant>();
                }

                if (!occupantsByCell.ContainsKey(layer))
                {
                    occupantsByCell[layer] = new Dictionary<Vector2Int, GridOccupant>();
                }
            }
        }

        private List<Vector2Int> BuildFootprint(Vector2Int origin, TileDefinition definition, TileRotation rotation)
        {
            var footprint = new List<Vector2Int>();
            foreach (var offset in definition.GetCells(rotation))
            {
                footprint.Add(origin + offset);
            }

            return footprint;
        }

        private bool IsAreaFree(IEnumerable<Vector2Int> cells, GridLayer layer)
        {
            foreach (var cell in cells)
            {
                if (enforceBounds && !Bounds.Contains(cell))
                {
                    return false;
                }

                if (occupantsByCell[layer].ContainsKey(cell))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class GridOccupant
    {
        public TileType TileType { get; }
        public GridLayer Layer { get; }
        public Vector2Int Origin { get; }
        public TileRotation Rotation { get; }
        public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;

        private readonly List<Vector2Int> occupiedCells;

        public GridOccupant(TileType tileType, GridLayer layer, Vector2Int origin, TileRotation rotation, List<Vector2Int> occupiedCells)
        {
            TileType = tileType;
            Layer = layer;
            Origin = origin;
            Rotation = rotation;
            this.occupiedCells = occupiedCells;
        }
    }
}
