using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CircuitFoundry.Items
{
    [CreateAssetMenu(menuName = "CircuitFoundry/Item Registry", fileName = "ItemRegistry")]
    public class ItemRegistry : ScriptableObject
    {
        [SerializeField] private List<ItemDefinitionSO> items = new();

        private readonly Dictionary<string, ItemDefinitionSO> itemsById = new();

        public IReadOnlyList<ItemDefinitionSO> Items => items;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            BuildLookup();
        }

        public bool TryGet(string id, out ItemDefinitionSO item)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                item = null;
                return false;
            }

            return itemsById.TryGetValue(id.Trim(), out item);
        }

        public ItemDefinitionSO GetOrNull(string id)
        {
            TryGet(id, out var item);
            return item;
        }

        public bool ContainsId(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && itemsById.ContainsKey(id.Trim());
        }

        private void BuildLookup()
        {
            itemsById.Clear();
            if (items == null)
            {
                items = new List<ItemDefinitionSO>();
            }

            foreach (var item in items.Where(i => i != null))
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                itemsById[item.Id.Trim()] = item;
            }
        }
    }
}
