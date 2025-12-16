using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CircuitFoundry.Grid
{
    public class GridPlacementTool : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager grid;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BeltNetwork beltNetwork;
        [SerializeField] private GameObject beltPrefab;
        [SerializeField] private GameObject ghostPrefab;
        [SerializeField] private LayerMask raycastMask = ~0;

        [Header("Settings")]
        [SerializeField] private float raycastDistance = 200f;
        [SerializeField] private GridLayer layer = GridLayer.Belt;
        [SerializeField] private TileType beltTileType = TileType.Line;
        [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
        [SerializeField] private KeyCode toggleRemoveModeKey = KeyCode.R;
        [SerializeField] private float ghostHeightOffset = 0.05f;

        private readonly Dictionary<Vector2Int, GameObject> spawnedByOrigin = new();
        private GameObject ghostInstance;
        private Renderer[] ghostRenderers;
        private TileRotation rotation = TileRotation.North;
        private bool removeMode;
        private bool hasHit;
        private Vector2Int hitCell;
        private bool placementValid;
        private bool removalValid;

        private void Awake()
        {
            if (grid == null)
            {
                grid = FindObjectOfType<GridManager>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (beltNetwork == null)
            {
                beltNetwork = FindObjectOfType<BeltNetwork>();
            }

            SpawnGhost();
        }

        private void OnDisable()
        {
            if (ghostInstance != null)
            {
                ghostInstance.SetActive(false);
            }
        }

        private void Update()
        {
            if (grid == null || targetCamera == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            HandleRotationInput(keyboard);
            HandleRemoveToggle(keyboard);
            RaycastMouse();
            UpdateGhost();

            if (!hasHit)
            {
                return;
            }

            HandlePlacementInput(mouse);
            HandleRemovalInput(mouse);
        }

        private void HandleRotationInput(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return;
            }

            var leftKey = MapKey(rotateLeftKey);
            var rightKey = MapKey(rotateRightKey);

            if (leftKey.HasValue && keyboard[leftKey.Value].wasPressedThisFrame)
            {
                rotation = PreviousRotation(rotation);
            }
            else if (rightKey.HasValue && keyboard[rightKey.Value].wasPressedThisFrame)
            {
                rotation = NextRotation(rotation);
            }
        }

        private void HandleRemoveToggle(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return;
            }

            var toggleKey = MapKey(toggleRemoveModeKey);
            if (toggleKey.HasValue && keyboard[toggleKey.Value].wasPressedThisFrame)
            {
                removeMode = !removeMode;
            }
        }

        private void RaycastMouse()
        {
            hasHit = false;
            placementValid = false;
            removalValid = false;

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var mousePos = mouse.position.ReadValue();
            var ray = targetCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            if (!Physics.Raycast(ray, out var hit, raycastDistance, raycastMask))
            {
                return;
            }

            hasHit = true;
            hitCell = grid.WorldToGrid(hit.point);

            var hasOccupant = grid.TryGetOccupantAtCell(hitCell, layer, out _);
            removalValid = hasOccupant;
            placementValid = !removeMode && grid.CanPlace(hitCell, beltTileType, layer, rotation);
        }

        private void UpdateGhost()
        {
            if (ghostInstance == null)
            {
                return;
            }

            ghostInstance.SetActive(hasHit);
            if (!hasHit)
            {
                return;
            }

            ghostInstance.transform.position = grid.GridToWorldCenter(hitCell, ghostHeightOffset);
            ghostInstance.transform.rotation = Quaternion.Euler(0f, (int)rotation, 0f);

            var mouse = Mouse.current;
            bool rightHeld = mouse != null && mouse.rightButton.isPressed;
            var (color, alpha) = DetermineGhostColor(rightHeld);
            ApplyGhostColor(color, alpha);
        }

        private (Color color, float alpha) DetermineGhostColor(bool rightHeld)
        {
            if (removeMode || rightHeld)
            {
                return removalValid
                    ? (new Color(1f, 0.4f, 0.2f), 0.5f)
                    : (new Color(1f, 0.1f, 0.1f), 0.35f);
            }

            return placementValid
                ? (new Color(0.2f, 1f, 0.4f), 0.5f)
                : (new Color(1f, 0.1f, 0.1f), 0.35f);
        }

        private void ApplyGhostColor(Color color, float alpha)
        {
            if (ghostRenderers == null)
            {
                return;
            }

            color.a = alpha;
            foreach (var renderer in ghostRenderers)
            {
                if (renderer == null) continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                // Support both Built-in/Standard (_Color) and URP Lit (_BaseColor).
                block.SetColor("_Color", color);
                block.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(block);
            }
        }

        private void HandlePlacementInput(Mouse mouse)
        {
            if (mouse == null)
            {
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame || removeMode)
            {
                return;
            }

            if (!placementValid)
            {
                return;
            }

            if (grid.Place(hitCell, beltTileType, layer, rotation, out var occupant))
            {
                SpawnBeltVisual(occupant);
                beltNetwork?.AddOrUpdate(occupant);
            }
        }

        private void HandleRemovalInput(Mouse mouse)
        {
            if (mouse == null)
            {
                return;
            }

            var wantsRemoval = mouse.rightButton.wasPressedThisFrame || (removeMode && mouse.leftButton.wasPressedThisFrame);
            if (!wantsRemoval || !removalValid)
            {
                return;
            }

            if (grid.TryGetOccupantAtCell(hitCell, layer, out var occupant))
            {
                if (grid.Remove(occupant.Origin, layer))
                {
                    beltNetwork?.RemoveAt(occupant.Origin);
                    if (spawnedByOrigin.TryGetValue(occupant.Origin, out var instance))
                    {
                        Destroy(instance);
                        spawnedByOrigin.Remove(occupant.Origin);
                    }
                }
            }
        }

        private void SpawnBeltVisual(GridOccupant occupant)
        {
            if (beltPrefab == null)
            {
                return;
            }

            var instance = Instantiate(
                beltPrefab,
                grid.GridToWorldCenter(occupant.Origin, 0f),
                Quaternion.Euler(0f, (int)occupant.Rotation, 0f),
                transform);

            spawnedByOrigin[occupant.Origin] = instance;
        }

        private void SpawnGhost()
        {
            if (ghostPrefab == null)
            {
                return;
            }

            ghostInstance = Instantiate(ghostPrefab);
            ghostInstance.name = "Ghost";
            ghostRenderers = ghostInstance.GetComponentsInChildren<Renderer>();

            foreach (var collider in ghostInstance.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            ghostInstance.SetActive(false);
        }

        private static Key? MapKey(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.Q => Key.Q,
                KeyCode.E => Key.E,
                KeyCode.R => Key.R,
                KeyCode.LeftShift => Key.LeftShift,
                KeyCode.RightShift => Key.RightShift,
                _ => null
            };
        }

        private static TileRotation NextRotation(TileRotation current)
        {
            return current switch
            {
                TileRotation.North => TileRotation.East,
                TileRotation.East => TileRotation.South,
                TileRotation.South => TileRotation.West,
                TileRotation.West => TileRotation.North,
                _ => TileRotation.North
            };
        }

        private static TileRotation PreviousRotation(TileRotation current)
        {
            return current switch
            {
                TileRotation.North => TileRotation.West,
                TileRotation.West => TileRotation.South,
                TileRotation.South => TileRotation.East,
                TileRotation.East => TileRotation.North,
                _ => TileRotation.North
            };
        }
    }
}
