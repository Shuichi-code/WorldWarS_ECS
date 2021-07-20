using Assets.Scripts.Class;
using Assets.Scripts.Components;
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
        private GameManager gameManager;

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

        void Awake()
        {

        }

        protected override void OnUpdate()
        {

            #region Initializing Data

            var ecb = ecbSystem.CreateCommandBuffer();
            var eventEntityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));

            var teamComponentArray = GetComponentDataFromEntity<TeamComponent>();
            var rankComponentArray = GetComponentDataFromEntity<RankComponent>();
            var flagPassedQuery = GetEntityQuery(ComponentType.ReadOnly<FlagPassingTag>());

            #endregion Initializing Data

            Entities
                .WithAll<ArbiterComponent>()
                .ForEach((Entity arbiterEntity, in ArbiterComponent arbiter) =>
                {
                    FightResult fightResult = FightResult.NoFight;
                    int attackingRank = rankComponentArray[arbiter.attackingPieceEntity].Rank;
                    Team attackingTeam = teamComponentArray[arbiter.attackingPieceEntity].myTeam;
                    var winningPieceEntity = Entity.Null;
                    if (IsThereAFight(arbiter))
                    {
                        int defendingRank = rankComponentArray[arbiter.defendingPieceEntity].Rank;
                        fightResult = FightCalculator.DetermineFightResult(attackingRank, defendingRank);

                        switch (fightResult)
                        {
                            case FightResult.AttackerWins:
                                ecb.AddComponent<CapturedComponent>(arbiter.defendingPieceEntity);
                                winningPieceEntity = arbiter.attackingPieceEntity;
                                break;

                            case FightResult.DefenderWins:
                                ecb.AddComponent<CapturedComponent>(arbiter.attackingPieceEntity);
                                winningPieceEntity = arbiter.defendingPieceEntity;
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

                            case FightResult.NoFight:
                                Debug.Log("No fight has occured.");
                                break;
                            default:
                                Debug.Log("Switch turned to default!");
                                break;
                        }
                    }

                    if (fightResult != FightResult.BothLose)
                    {
                        if (!HasComponent<PieceOnCellComponent>(arbiter.battlegroundCellEntity))
                            ecb.AddComponent<PieceOnCellComponent>(arbiter.battlegroundCellEntity);

                        ecb.SetComponent(arbiter.battlegroundCellEntity, new PieceOnCellComponent
                        {
                            PieceEntity = winningPieceEntity == Entity.Null ? arbiter.attackingPieceEntity : winningPieceEntity
                        });
                        if (attackingRank == Piece.Flag)
                            CheckIfFlagIsOnLastCell(arbiter, attackingTeam, ecb);
                    }

                    if (HasFlagAlreadyPassedLastCell(flagPassedQuery))
                        DeclareWinner(ecb, eventEntityArchetype, GameManager.SwapTeam(attackingTeam));

                    ChangeTurn(attackingTeam);

                    ecb.DestroyEntity(arbiterEntity);
                }).WithoutBurst().Run();

            CheckIfGameFinishedEventRaised(ecb);
        }

        private void CheckIfGameFinishedEventRaised(EntityCommandBuffer ecb)
        {
            Entities
                .ForEach((Entity e, in GameFinishedEventComponent eventComponent) =>
                {
                    OnGameWin?.Invoke(eventComponent.winningTeam);
                    //ecb.DestroyEntity(e);
                })
                .WithoutBurst().Run();
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
            if (!HasComponent<LastCellsTag>(arbiter.battlegroundCellEntity)) return;
            var cellTeamArray = GetComponentDataFromEntity<HomeCellComponent>();
            var cellTeam = cellTeamArray[arbiter.battlegroundCellEntity].homeTeam;
            if (attackingTeam == GameManager.SwapTeam(cellTeam))
                ecb.AddComponent<FlagPassingTag>(arbiter.attackingPieceEntity);

        }

        private static bool IsThereAFight(ArbiterComponent arbiter)
        {
            return arbiter.defendingPieceEntity != Entity.Null;
        }

        public static void DeclareWinner(EntityCommandBuffer entityCommandBuffer, EntityArchetype eventEntityArchetype, Team winningTeam)
        {
            Entity eventEntity = entityCommandBuffer.CreateEntity(eventEntityArchetype);
            entityCommandBuffer.SetComponent<GameFinishedEventComponent>(eventEntity, new GameFinishedEventComponent { winningTeam = winningTeam });
        }
    }
}