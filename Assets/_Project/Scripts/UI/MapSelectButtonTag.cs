using ProjectZx.Core;
using UnityEngine;

namespace ProjectZx.UI
{
    /// <summary>
    /// Stable map identity for survival / campfire travel buttons.
    /// Labels change when locked or recommended, so refresh must not parse button text.
    /// </summary>
    public sealed class MapSelectButtonTag : MonoBehaviour
    {
        public SurvivalMapKind Kind;
    }
}
