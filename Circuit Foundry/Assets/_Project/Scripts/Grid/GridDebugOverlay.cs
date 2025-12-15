using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CircuitFoundry.Grid
{
    [ExecuteAlways]
    public class GridDebugOverlay : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private float gizmoHeight = 0.02f;
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private bool drawLabels = true;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color beltColor = new Color(0f, 0.75f, 1f, 0.35f);
        [SerializeField] private Color machineColor = new Color(1f, 0.55f, 0f, 0.35f);
        [SerializeField] private Color decorColor = new Color(0.2f, 1f, 0.4f, 0.35f);

        private void Reset()
        {
            if (grid == null)
            {
                grid = GetComponent<GridManager>();
            }
        }

        private void OnDrawGizmos()
        {
            if (grid == null)
            {
                grid = GetComponent<GridManager>();
            }

            if (grid == null)
            {
                return;
            }

            if (drawGrid)
            {
                DrawGrid();
            }

            DrawOccupancy();
        }

        private void DrawGrid()
        {
            Gizmos.color = gridColor;
            var corner = grid.GridToWorldCenter(grid.GridOrigin, gizmoHeight) -
                         new Vector3(grid.CellSize * 0.5f, 0f, grid.CellSize * 0.5f);

            for (int x = 0; x <= grid.GridSize.x; x++)
            {
                var from = corner + new Vector3(x * grid.CellSize, 0f, 0f);
                var to = from + new Vector3(0f, 0f, grid.GridSize.y * grid.CellSize);
                Gizmos.DrawLine(from, to);
            }

            for (int y = 0; y <= grid.GridSize.y; y++)
            {
                var from = corner + new Vector3(0f, 0f, y * grid.CellSize);
                var to = from + new Vector3(grid.GridSize.x * grid.CellSize, 0f, 0f);
                Gizmos.DrawLine(from, to);
            }
        }

        private void DrawOccupancy()
        {
            foreach (var layer in grid.AllLayers())
            {
                Gizmos.color = ColorForLayer(layer);

                foreach (var kvp in grid.GetOccupiedCells(layer))
                {
                    var center = grid.GridToWorldCenter(kvp.Key, gizmoHeight);
                    var size = new Vector3(grid.CellSize * 0.9f, 0.02f, grid.CellSize * 0.9f);
                    Gizmos.DrawCube(center, size);
#if UNITY_EDITOR
                    if (drawLabels)
                    {
                        Handles.Label(center, $"{layer}: {kvp.Value.TileType}");
                    }
#endif
                }
            }
        }

        private Color ColorForLayer(GridLayer layer)
        {
            return layer switch
            {
                GridLayer.Belt => beltColor,
                GridLayer.Machine => machineColor,
                GridLayer.Decor => decorColor,
                _ => gridColor
            };
        }
    }
}
