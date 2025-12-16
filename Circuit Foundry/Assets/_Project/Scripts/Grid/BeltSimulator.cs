using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CircuitFoundry.Grid
{
    public class BeltSimulator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager grid;
        [SerializeField] private BeltNetwork beltNetwork;
        [SerializeField] private GameObject itemVisualPrefab;

        [Header("Simulation")]
        [SerializeField, Min(0.01f)] private float tickDelta = 0.02f;
        [SerializeField, Min(0.01f)] private float beltSpeed = 2f;
        [SerializeField, Range(0.05f, 0.9f)] private float minSpacing = 0.25f;
        [SerializeField] private float itemHeight = 0.1f;

        [Header("Debug Spawn")]
        [SerializeField] private bool autoSpawn = true;
        [SerializeField, Min(0f)] private float spawnInterval = 0.25f;
        [SerializeField] private string debugItemId = "ore";
        [SerializeField] private int spawnBurst = 1;

        private readonly Dictionary<Vector2Int, SegmentState> segments = new();
        private readonly Dictionary<long, ItemInstance> itemInstances = new();
        private float accumulator;
        private float spawnAccumulator;
        private long nextItemId = 1;

        private static readonly GridDirection[] Directions =
        {
            GridDirection.North, GridDirection.East, GridDirection.South, GridDirection.West
        };

        private void Awake()
        {
            if (grid == null)
            {
                grid = FindObjectOfType<GridManager>();
            }

            if (beltNetwork == null)
            {
                beltNetwork = FindObjectOfType<BeltNetwork>();
            }

            RebuildSegments();
        }

        private void Start()
        {
            RebuildSegments();
        }

        private void Update()
        {
            accumulator += Time.deltaTime;
            while (accumulator >= tickDelta)
            {
                Step(tickDelta);
                accumulator -= tickDelta;
            }

            UpdateVisuals();
        }

        private void Step(float delta)
        {
            var orderedSegments = segments.Values.OrderBy(s => s.Cell.x).ThenBy(s => s.Cell.y).ToList();
            var processed = new HashSet<Vector2Int>();
            foreach (var segment in orderedSegments)
            {
                if (processed.Contains(segment.Cell))
                {
                    continue;
                }

                AdvanceSegment(segment, delta, processed);
                processed.Add(segment.Cell);
            }

            if (autoSpawn)
            {
                spawnAccumulator += delta;
                while (spawnAccumulator >= spawnInterval)
                {
                    SpawnDebugItems(spawnBurst);
                    spawnAccumulator -= spawnInterval;
                }
            }
        }

        private void AdvanceSegment(SegmentState segment, float delta, HashSet<Vector2Int> processedThisStep)
        {
            if (segment.Items.Count == 0)
            {
                return;
            }

            segment.Items.Sort((a, b) => a.Position.CompareTo(b.Position));

            for (int i = segment.Items.Count - 1; i >= 0; i--)
            {
                var item = segment.Items[i];
                float maxPos = 1f;
                if (i < segment.Items.Count - 1)
                {
                    maxPos = segment.Items[i + 1].Position - minSpacing;
                }

                maxPos = Mathf.Max(maxPos, item.Position);

                float targetPos = Mathf.Min(item.Position + beltSpeed * delta, maxPos);

                if (targetPos >= 1f)
                {
                    TryMoveToNext(segment, item, targetPos - 1f, processedThisStep);
                }
                else
                {
                    item.Position = Mathf.Clamp01(targetPos);
                }
            }
        }

        private void TryMoveToNext(SegmentState segment, ItemData item, float overflow, HashSet<Vector2Int> processedThisStep)
        {
            var dir = GridDirectionUtils.ToVector(segment.Tile.Direction);
            var nextCell = segment.Cell + dir;
            if (segments.TryGetValue(nextCell, out var nextSegment))
            {
                nextSegment.Items.Sort((a, b) => a.Position.CompareTo(b.Position));
                float entryPos = Mathf.Clamp(overflow, 0f, 1f);
                if (nextSegment.Items.Count > 0)
                {
                    float maxAllowed = nextSegment.Items[0].Position - minSpacing;
                    if (maxAllowed < 0f)
                    {
                        // Blocked, stay on current belt at current edge.
                        item.Position = Mathf.Min(1f, item.Position);
                        return;
                    }

                    entryPos = Mathf.Min(entryPos, maxAllowed);
                }

                // Remove from current segment and add to next without destroying the visual.
                segment.Items.Remove(item);
                item.Position = entryPos;
                item.SegmentCell = nextSegment.Cell;
                nextSegment.Items.Add(item);
                itemInstances[item.InstanceId].Segment = nextSegment;
                processedThisStep?.Add(nextSegment.Cell);
            }
            else
            {
                // No forward belt; park at the end of the current belt.
                item.Position = 1f;
            }
        }

        private void UpdateVisuals()
        {
            foreach (var kvp in itemInstances)
            {
                var inst = kvp.Value;
                if (inst.Segment == null)
                {
                    continue;
                }

                var dir = GridDirectionUtils.ToVector(inst.Segment.Tile.Direction);
                var dir3 = new Vector3(dir.x, 0f, dir.y);
                var center = grid.GridToWorldCenter(inst.Segment.Cell, itemHeight);
                var worldOffset = dir3 * (inst.Data.Position - 0.5f) * grid.CellSize;
                inst.Visual.transform.position = center + worldOffset;
                inst.Visual.transform.rotation = Quaternion.Euler(0f, (int)inst.Segment.Tile.Rotation, 0f);
            }
        }

        private void SpawnDebugItems(int count)
        {
            if (segments.Count == 0)
            {
                return;
            }

            var startSegment = segments.Values.OrderBy(s => s.Cell.x).ThenBy(s => s.Cell.y).First();
            for (int i = 0; i < count; i++)
            {
                SpawnItem(startSegment, debugItemId);
            }
        }

        private void SpawnItem(SegmentState segment, string itemId)
        {
            // Ensure spacing at the start.
            segment.Items.Sort((a, b) => a.Position.CompareTo(b.Position));
            if (segment.Items.Count > 0 && segment.Items[0].Position <= minSpacing)
            {
                return;
            }

            var item = new ItemData
            {
                Id = itemId,
                Position = 0f,
                SegmentCell = segment.Cell,
                InstanceId = nextItemId++
            };
            segment.Items.Add(item);

            var visual = CreateVisual();
            itemInstances[item.InstanceId] = new ItemInstance(item, visual, segment);
        }

        private GameObject CreateVisual()
        {
            if (itemVisualPrefab == null)
            {
                return null;
            }

            var visual = Instantiate(itemVisualPrefab, transform);
            visual.name = $"Item_{nextItemId}";
            return visual;
        }

        public void RebuildSegments()
        {
            var preserved = itemInstances.Values.ToList();
            itemInstances.Clear();
            segments.Clear();

            if (beltNetwork == null)
            {
                ClearPreserved(preserved);
                return;
            }

            foreach (var kvp in beltNetwork.BeltTiles)
            {
                segments[kvp.Key] = new SegmentState(kvp.Value);
            }

            // Reattach preserved items to new segments where possible.
            foreach (var inst in preserved)
            {
                if (segments.TryGetValue(inst.Data.SegmentCell, out var seg))
                {
                    seg.Items.Add(inst.Data);
                    inst.Segment = seg;
                    itemInstances[inst.Data.InstanceId] = inst;
                }
                else
                {
                    if (inst.Visual != null)
                    {
                        Destroy(inst.Visual);
                    }
                }
            }
        }

        public void OnBeltPlaced(GridOccupant occupant)
        {
            if (occupant.Layer != GridLayer.Belt)
            {
                return;
            }

            RebuildSegments();
        }

        public void OnBeltRemoved(Vector2Int cell)
        {
            if (segments.TryGetValue(cell, out var segment))
            {
                var itemsCopy = segment.Items.ToList();
                foreach (var item in itemsCopy)
                {
                    RemoveItemInternal(segment, item.InstanceId);
                }
                segments.Remove(cell);
            }

            RebuildSegments();
        }

        private void RemoveItemInternal(SegmentState segment, long instanceId)
        {
            var index = segment.Items.FindIndex(it => it.InstanceId == instanceId);
            if (index >= 0)
            {
                var data = segment.Items[index];
                segment.Items.RemoveAt(index);
                if (itemInstances.Remove(data.InstanceId, out var inst))
                {
                    if (inst.Visual != null)
                    {
                        Destroy(inst.Visual);
                    }
                }
            }
        }

        private void ClearPreserved(IEnumerable<ItemInstance> preserved)
        {
            foreach (var inst in preserved)
            {
                if (inst.Visual != null)
                {
                    Destroy(inst.Visual);
                }
            }

            itemInstances.Clear();
        }

        private class SegmentState
        {
            public Vector2Int Cell { get; }
            public BeltTile Tile { get; }
            public List<ItemData> Items { get; } = new();

            public SegmentState(BeltTile tile)
            {
                Tile = tile;
                Cell = tile.Cell;
            }
        }

        private class ItemData
        {
            public long InstanceId;
            public string Id;
            public float Position;
            public Vector2Int SegmentCell;
        }

        private class ItemInstance
        {
            public ItemData Data { get; }
            public GameObject Visual { get; }
            public SegmentState Segment { get; set; }

            public ItemInstance(ItemData data, GameObject visual, SegmentState segment)
            {
                Data = data;
                Visual = visual;
                Segment = segment;
            }
        }
    }
}
