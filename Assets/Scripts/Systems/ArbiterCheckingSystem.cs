using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours;
using Assets.Scripts.Monobehaviours.Managers;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    [UpdateBefore(typeof(RemoveTagsSystem))]
    [UpdateAfter(typeof(DragToMouseSystem))]
    public class ArbiterCheckingSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        public delegate void GameWinnerDelegate(Team winningTeam);

        public event GameWinnerDelegate OnGameWin;

        protected override void OnCreate()
        {
            // Find the ECB system once and store it for later usage
            base.OnCreate();
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            #region CheckGameState

            EntityQuery gmQuery = GetEntityQuery(typeof(GameManagerComponent));
            Entity gmEntity = gmQuery.GetSingletonEntity();

            #endregion CheckGameState

            #region Initializing Data

            var ecb = ecbSystem.CreateCommandBuffer();
            EntityArchetype eventEntityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));

            ComponentDataFromEntity<PieceComponent> pieceComponentArray = GetComponentDataFromEntity<PieceComponent>();
            var flagPassedQuery = GetEntityQuery(ComponentType.ReadOnly<FlagPassingTag>());

            #endregion Initializing Data

            Entities
                .WithAll<ArbiterComponent>()
                .ForEach((Entity arbiterEntity, in ArbiterComponent arbiter) =>
                {
                    FightResult fightResult = FightResult.NoFight;
                    int attackingRank = pieceComponentArray[arbiter.attackingPieceEntity].pieceRank;
                    Team attackingTeam = pieceComponentArray[arbiter.attackingPieceEntity].team;

                    if (IsThereAFight(arbiter))
                    {
                        int defendingRank = pieceComponentArray[arbiter.defendingPieceEntity].pieceRank;
                        fightResult = FightCalculator.DetermineFightResult(attackingRank, defendingRank);

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
                                ecb.RemoveComponent<PieceOnCellComponent>(arbiter.battlegroundCellEntity);
                                break;

                            case FightResult.FlagDestroyed:
                                var teamWinner = (defendingRank == Piece.Flag ? attackingTeam : GameManager.SwapTeam(attackingTeam));
                                DeclareWinner(ecb, eventEntityArchetype, teamWinner);
                                break;

                            default:
                                Debug.Log("Switch turned to default!");
                                break;
                        }
                    }

                    if (fightResult != FightResult.BothLose)
                    {
                        if(!HasComponent<PieceOnCellComponent>(arbiter.battlegroundCellEntity))
                            ecb.AddComponent<PieceOnCellComponent>(arbiter.battlegroundCellEntity);
                        ecb.SetComponent(arbiter.battlegroundCellEntity, new PieceOnCellComponent { PieceEntity = arbiter.attackingPieceEntity });
                        if(attackingRank == Piece.Flag)
                            CheckIfFlagIsOnLastCell(arbiter, attackingTeam, ecb);
                    }

                    if (HasFlagAlreadyPassedLastCell(flagPassedQuery))
                        DeclareWinner(ecb, eventEntityArchetype, GameManager.SwapTeam(attackingTeam));

                    ChangeTurn(attackingTeam);

                    ecb.DestroyEntity(arbiterEntity);
                }).WithoutBurst().Run();

            Entities
                .ForEach((in GameFinishedEventComponent eventComponent) =>
                {
                    OnGameWin?.Invoke(eventComponent.winningTeam);
                }).WithoutBurst().Run();
            EntityManager.DestroyEntity(GetEntityQuery(typeof(GameFinishedEventComponent)));
        }

        private static void ChangeTurn(Team attackingTeam)
        {
            GameManager.GetInstance().SetGameState(GameState.Playing, GameManager.SwapTeam(attackingTeam));
        }

        private static bool HasFlagAlreadyPassedLastCell(EntityQuery flagPassedQuery)
        {
            return flagPassedQuery.CalculateEntityCount() > 0;
        }

        private void CheckIfFlagIsOnLastCell(ArbiterComponent arbiter, Team attackingTeam, EntityCommandBuffer ecb)
        {
            if (IsPieceOnLastCell(arbiter, attackingTeam))
                ecb.AddComponent<FlagPassingTag>(arbiter.attackingPieceEntity);
        }

        private static bool IsThereAFight(ArbiterComponent arbiter)
        {
            return arbiter.defendingPieceEntity != Entity.Null;
        }

        private bool IsPieceOnLastCell(ArbiterComponent arbiter, Team attackingTeam)
        {
            return (attackingTeam == Team.Defender && HasComponent<LastCellsForDefenderTag>(arbiter.battlegroundCellEntity)) || (attackingTeam == Team.Invader && HasComponent<LastCellsForInvaderTag>(arbiter.battlegroundCellEntity));
        }

        private static void DeclareWinner(EntityCommandBuffer entityCommandBuffer, EntityArchetype eventEntityArchetype, Team winningTeam)
        {
            Entity eventEntity = entityCommandBuffer.CreateEntity(eventEntityArchetype);
            entityCommandBuffer.SetComponent<GameFinishedEventComponent>(eventEntity, new GameFinishedEventComponent { winningTeam = winningTeam });
        }
    }
}