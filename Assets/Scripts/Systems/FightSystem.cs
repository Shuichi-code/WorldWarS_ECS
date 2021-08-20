using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Assets.Scripts.Systems
{
    [DisableAutoCreation]
    public class FightSystem : ParallelSystem
    {

        protected override void OnUpdate()
        {
            var fighterQuery = GetEntityQuery(ComponentType.ReadOnly<FighterTag>(), ComponentType.ReadOnly<RankComponent>(), ComponentType.ReadOnly<TeamComponent>(), ComponentType.ReadOnly<ArmyComponent>());
            if (fighterQuery.CalculateEntityCount() != 2) return;
            var ecb = EcbSystem.CreateCommandBuffer();
            var fighterRankArray = GetComponentDataFromEntity<RankComponent>();
            var fighterTeamArray = GetComponentDataFromEntity<TeamComponent>();
            var fighterEntityArray = fighterQuery.ToEntityArray(Allocator.Temp);


            var attackingFighterEntity = fighterEntityArray[0];
            var defendingFighterEntity = fighterEntityArray[1];
            var attackingFighterTeam = fighterTeamArray[attackingFighterEntity];
            var defendingFighterTeam = fighterTeamArray[defendingFighterEntity];
            var attackingFighterRank = fighterRankArray[attackingFighterEntity];
            var defendingFighterRank = fighterRankArray[defendingFighterEntity];

            var fightResult = FightCalculator.DetermineFightResult(attackingFighterRank.Rank, defendingFighterRank.Rank);
            var loserEntityArray = new NativeArray<Entity>(2, Allocator.Temp);
            switch (fightResult)
            {
                case FightResult.AttackerWins:
                    loserEntityArray[0] = defendingFighterEntity;
                    break;
                case FightResult.DefenderWins:
                    loserEntityArray[0] = attackingFighterEntity;
                    break;
                case FightResult.BothLose:
                    loserEntityArray[0] = defendingFighterEntity;
                    loserEntityArray[1] = attackingFighterEntity;
                    break;
                case FightResult.FlagDestroyed:
                    var teamWinner = (defendingFighterRank.Rank == Piece.Flag ? attackingFighterTeam.myTeam : defendingFighterTeam.myTeam);
                    DeclareWinner(teamWinner);
                    break;
                case FightResult.NoFight:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CaptureLosers(loserEntityArray);

            EntityManager.RemoveComponent<FighterTag>(fighterQuery);
                //RemoveFighterTags(ecb);
        }

        private void DeclareWinner(Team teamWinner)
        {
            var declareWinnerEntity = EntityManager.CreateEntity();
            EntityManager.AddComponent<GameFinishedEventComponent>(declareWinnerEntity);
            EntityManager.SetComponentData(declareWinnerEntity, new GameFinishedEventComponent()
            {
                winningTeam = teamWinner
            });
        }

        private void CaptureLosers(NativeArray<Entity> loserEntityArray)
        {
            foreach (var entity in loserEntityArray.Where(entity => entity != Entity.Null))
            {
                EntityManager.AddComponent(entity, typeof(CapturedComponent));
            }
        }

        private void RemoveFighterTags(EntityCommandBuffer ecb)
        {
            Entities.
                WithAll<FighterTag>().
                ForEach((Entity e, int entityInQueryIndex) =>
            {
                ecb.RemoveComponent<FighterTag>(e);
            }).Schedule();
            CompleteDependency();
            //EcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}