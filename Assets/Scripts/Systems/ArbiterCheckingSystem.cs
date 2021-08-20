using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    //[DisableAutoCreation]
    public class ArbiterCheckingSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private GameManager gameManager;
        private static EntityArchetype _eventEntityArchetype;

        public delegate void GameWinnerDelegate(Team winningTeam);
        public event GameWinnerDelegate OnGameWin;

        protected override void OnCreate()
        {
            // Find the ECB system once and store it for later usage
            base.OnCreate();
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            gameManager = GameManager.GetInstance();
        }

        protected override void OnUpdate()
        {

            #region Initializing Data

            var ecb = ecbSystem.CreateCommandBuffer();
            _eventEntityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));

            var teamComponentArray = GetComponentDataFromEntity<TeamComponent>();
            var rankComponentArray = GetComponentDataFromEntity<RankComponent>();

            #endregion Initializing Data

            Entities
                .WithAll<ArbiterComponent>()
                .ForEach((Entity arbiterEntity, in ArbiterComponent arbiter) =>
                {
                    var attackingRank = rankComponentArray[arbiter.attackingPieceEntity].Rank;
                    var attackingTeam = teamComponentArray[arbiter.attackingPieceEntity].myTeam;
                    if (IsThereAFight(arbiter))
                    {
                        var defendingRank = rankComponentArray[arbiter.defendingPieceEntity].Rank;
                        var fightResult = FightCalculator.DetermineFightResult(attackingRank, defendingRank);

                        switch (fightResult)
                        {
                            case FightResult.AttackerWins:
                                ecb.AddComponent<CapturedComponent>(arbiter.defendingPieceEntity);
                                break;

                            case FightResult.DefenderWins:
                                ecb.AddComponent<CapturedComponent>(arbiter.attackingPieceEntity);
                                break;

                            case FightResult.BothLose:
                                ecb.AddComponent<CapturedComponent>(arbiter.defendingPieceEntity);
                                ecb.AddComponent<CapturedComponent>(arbiter.attackingPieceEntity);
                                break;

                            case FightResult.FlagDestroyed:
                                var teamWinner = attackingTeam == Team.Invader ? Team.Defender : Team.Invader;
                                DeclareWinner(ecb, teamWinner);
                                break;

                            case FightResult.NoFight:
                                Debug.Log("No fight has occurred.");
                                break;
                            default:
                                Debug.Log("Switch turned to default!");
                                break;
                        }
                    }

                    ecb.DestroyEntity(arbiterEntity);
                }).Schedule();
            CompleteDependency();
        }

        private static bool IsThereAFight(ArbiterComponent arbiter)
        {
            return arbiter.defendingPieceEntity != Entity.Null;
        }

        public static void DeclareWinner(EntityCommandBuffer entityCommandBuffer, Team winningTeam)
        {
            var eventEntity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent(eventEntity, new GameFinishedEventComponent { winningTeam = winningTeam });
        }
    }
}