using System;
using System.Linq;
using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class FightSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var fighterQuery = GetEntityQuery(ComponentType.ReadOnly<FighterTag>(), ComponentType.ReadOnly<RankComponent>(), ComponentType.ReadOnly<TeamComponent>());
            if (fighterQuery.CalculateEntityCount() != 2) return;
            var ecb = ecbSystem.CreateCommandBuffer();
            var ecbParallelWriter = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var fighterRankArray = fighterQuery.ToComponentDataArray<RankComponent>(Allocator.TempJob);
            var fighterTeamArray = fighterQuery.ToComponentDataArray<TeamComponent>(Allocator.TempJob);
            var fighterEntityArray = fighterQuery.ToEntityArray(Allocator.TempJob);

            var attackingFighterRank = fighterRankArray[0];
            var defendingFighterRank = fighterRankArray[1];
            var attackingFighterEntity = fighterEntityArray[0];
            var defendingFighterEntity = fighterEntityArray[1];
            var attackingFighterTeam = fighterTeamArray[0];
            var defendingFighterTeam = fighterTeamArray[1];

            var fightResult = FightCalculator.DetermineFightResult(attackingFighterRank.Rank, defendingFighterRank.Rank);
            var loserEntityArray = new NativeArray<Entity>(2, Allocator.TempJob);
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
                    loserEntityArray[0] = attackingFighterEntity;
                    break;
                case FightResult.FlagDestroyed:
                    var teamWinner = (defendingFighterRank.Rank == Piece.Flag ? attackingFighterTeam.myTeam : defendingFighterTeam.myTeam);
                    ArbiterCheckingSystem.DeclareWinner(ecb, teamWinner);
                    break;
                case FightResult.NoFight:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CaptureLosers(loserEntityArray);

            fighterRankArray.Dispose();
            fighterTeamArray.Dispose();
            fighterEntityArray.Dispose();
            loserEntityArray.Dispose();
            RemoveFighterTags(ecbParallelWriter);
        }

        private void CaptureLosers(NativeArray<Entity> loserEntityArray)
        {
            foreach (var entity in loserEntityArray.Where(entity => entity != Entity.Null))
            {
                EntityManager.AddComponent(entity, typeof(CapturedComponent));
            }
        }

        private void RemoveFighterTags(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            Entities.
                WithAll<FighterTag>().
                ForEach((Entity e, int entityInQueryIndex) =>
            {
                ecbParallelWriter.RemoveComponent<FighterTag>(entityInQueryIndex, e);
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}