using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircuitFoundry.Grid
{
    public class GridTestBootstrap : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private bool clearBeforePlacing = true;
        [SerializeField] private bool logFailures = true;
        [SerializeField] private List<PlacementRequest> placements = new()
        {
            new PlacementRequest
            {
                origin = new Vector2Int(2, -2),
                layer = GridLayer.Belt,
                tileType = TileType.Single,
                rotation = TileRotation.North
            },
            new PlacementRequest
            {
                origin = new Vector2Int(6, 0),
                layer = GridLayer.Machine,
                tileType = TileType.Line,
                rotation = TileRotation.East
            },
            new PlacementRequest
            {
                origin = new Vector2Int(4, 7),
                layer = GridLayer.Decor,
                tileType = TileType.Block2X2,
                rotation = TileRotation.North
            },
            new PlacementRequest
            {
                origin = new Vector2Int(11, 6),
                layer = GridLayer.Decor,
                tileType = TileType.CornerL,
                rotation = TileRotation.West
            }
        };

        private void Awake()
        {
            if (grid == null)
            {
                grid = GetComponent<GridManager>();
            }
        }

        private void Start()
        {
            if (grid == null)
            {
                Debug.LogWarning("GridTestBootstrap needs a GridManager reference to place tiles.", this);
                return;
            }

            if (clearBeforePlacing)
            {
                grid.ClearAll();
            }

            foreach (var placement in placements)
            {
                if (!grid.Place(placement.origin, placement.tileType, placement.layer, placement.rotation) && logFailures)
                {
                    Debug.LogWarning(
                        $"Placement failed: {placement.tileType} at {placement.origin} on {placement.layer}",
                        this);
                }
            }
        }

        [Serializable]
        private struct PlacementRequest
        {
            public Vector2Int origin;
            public GridLayer layer;
            public TileType tileType;
            public TileRotation rotation;
        }
    }
}
