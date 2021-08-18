using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    /// <summary>
    /// System that creates event that informs UI that game is finished
    /// </summary>
    public class DeclareWInnerSystem : SystemBase
    {
        private GameObject chargedAbilityUi;

        public delegate void GameWinnerDelegate(Team winningTeam);
        public event GameWinnerDelegate OnGameWin;

        protected override void OnUpdate()
        {
            Entities
                .ForEach((Entity e, in GameFinishedEventComponent eventComponent) =>
                {
                    OnGameWin?.Invoke(eventComponent.winningTeam);

                })
                .WithoutBurst().Run();
            EntityManager.DestroyEntity(GetEntityQuery(typeof(GameFinishedEventComponent)));
        }
    }
}
