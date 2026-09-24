using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// Ship of each seat. A seat without a ship of its own takes the default ship; without one either, a placeholder
    /// is generated in the seat colour, so a missing model never blocks the game.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Habillage/Vaisseaux", fileName = "ShipCatalog")]
    public sealed class ShipCatalog : ScriptableObject
    {
        [Tooltip("Vaisseau des sièges qui n'ont pas de vaisseau attitré.")]
        [SerializeField] private GameObject? defaultShip;

        [Tooltip("Vaisseau attitré de chaque siège, dans l'ordre de la table. Une case vide prend le vaisseau par défaut.")]
        [SerializeField] private List<GameObject?> seats = new List<GameObject?>();

        /// <summary>The ship prefab of a seat (0-based), or null when it gets a placeholder.</summary>
        public GameObject? PrefabFor(int seat)
        {
            GameObject? own = seat >= 0 && seat < seats.Count ? seats[seat] : null;
            return own != null ? own : defaultShip;
        }

        /// <summary>Creates the ship of a seat under <paramref name="parent"/>: its prefab, or a placeholder.</summary>
        public GameObject Spawn(int seat, Transform parent, Color placeholderColor)
        {
            GameObject? prefab = PrefabFor(seat);
            return prefab != null ? Instantiate(prefab, parent, false) : PlaceholderShip.Build(parent, placeholderColor);
        }

        /// <summary>Replaces the ships (editor setup and tests).</summary>
        public void Configure(GameObject? fallback, params GameObject?[] seatShips)
        {
            defaultShip = fallback;
            seats = new List<GameObject?>(seatShips);
        }
    }
}
