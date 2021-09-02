using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Systems.Special_Ability_Systems;
using Assets.Scripts.Tags;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    [UpdateAfter(typeof(CollideSystem))]
    [UpdateBefore(typeof(UpdatePieceOnCellSystem))]
    //[DisableAutoCreation]
    public class ArbiterCheckingSystem : ParallelSystem
    {

        protected override void OnUpdate()
        {

            #region Initializing Data
            var ecb = EcbSystem.CreateCommandBuffer();
            var teamComponentArray = GetComponentDataFromEntity<TeamComponent>();
            var rankComponentArray = GetComponentDataFromEntity<RankComponent>();
            #endregion Initializing Data

            Entities
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

                    UpdatePieceOnCellComponents(ecb);
                    ecb.DestroyEntity(arbiterEntity);
                }).Schedule();
            Dependency.Complete();
        }

        private static void UpdatePieceOnCellComponents(EntityCommandBuffer ecb)
        {
            var pieceOnCellUpdaterEntity = ecb.CreateEntity();
            ecb.AddComponent<PieceOnCellUpdaterTag>(pieceOnCellUpdaterEntity);
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