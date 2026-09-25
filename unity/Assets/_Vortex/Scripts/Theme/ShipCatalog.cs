using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// Ship of each seat. A seat without a ship of its own takes the default ship; without one either, a placeholder
    /// is generated in the seat colour, so a missing model never blocks the game. A model paints its parts that use the
    /// seat material (<see cref="SeatMaterialName"/>) in the seat colour, so one model can serve every seat.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Habillage/Vaisseaux", fileName = "ShipCatalog")]
    public sealed class ShipCatalog : ScriptableObject
    {
        /// <summary>
        /// Name of the material that takes the seat colour (docs/ASSETS.md section 2). Blender's copies of it
        /// (<c>Siege.001</c>) count too.
        /// </summary>
        public const string SeatMaterialName = "Siege";

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

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
        public GameObject Spawn(int seat, Transform parent, Color seatColor)
        {
            GameObject? prefab = PrefabFor(seat);
            if (prefab == null)
            {
                return PlaceholderShip.Build(parent, seatColor);
            }

            GameObject ship = Instantiate(prefab, parent, false);
            PaintSeat(ship, seatColor);
            return ship;
        }

        /// <summary>Paints the parts of <paramref name="ship"/> that use the seat material in the seat colour.</summary>
        public static void PaintSeat(GameObject ship, Color seatColor)
        {
            if (ship == null)
            {
                throw new System.ArgumentNullException(nameof(ship));
            }

            // The colour multiplies the material's texture. A property block recolours this ship only, without
            // creating a material that would have to be destroyed with it.
            var block = new MaterialPropertyBlock();
            foreach (Renderer renderer in ship.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    if (materials[index] != null && IsSeatMaterial(materials[index].name))
                    {
                        renderer.GetPropertyBlock(block, index);
                        block.SetColor(BaseColor, seatColor);
                        renderer.SetPropertyBlock(block, index);
                    }
                }
            }
        }

        private static bool IsSeatMaterial(string name) =>
            name == SeatMaterialName || name.StartsWith(SeatMaterialName + ".", System.StringComparison.Ordinal);

        /// <summary>Replaces the ships (editor setup and tests).</summary>
        public void Configure(GameObject? fallback, params GameObject?[] seatShips)
        {
            defaultShip = fallback;
            seats = new List<GameObject?>(seatShips);
        }
    }
}
