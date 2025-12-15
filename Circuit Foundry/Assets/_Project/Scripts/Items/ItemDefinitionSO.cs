using System.Collections.Generic;
using UnityEngine;

namespace CircuitFoundry.Items
{
    [CreateAssetMenu(menuName = "CircuitFoundry/Item Definition", fileName = "ItemDefinition")]
    public class ItemDefinitionSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private List<string> tags = new();

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public Sprite Icon => icon;
        public IReadOnlyList<string> Tags => tags;

        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                id = id.Trim();
            }
        }
    }
}
