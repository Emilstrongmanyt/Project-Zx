using ProjectZx.Core;
using ProjectZx.UI;
using UnityEngine;

namespace ProjectZx.Player
{
    [RequireComponent(typeof(NpcInteractable))]
    public class HeroCampNpc : MonoBehaviour
    {
        public PlayableHero Hero { get; private set; }

        public void Initialize(PlayableHero hero)
        {
            Hero = hero;
            GetComponent<NpcInteractable>().Initialize(() =>
            {
                CampHeroManager.Instance?.SelectHeroFromNpc(Hero, transform.position);
            });
        }
    }
}