using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class DragToMouseSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        public delegate void GameWinnerDelegate(Team winningTeam);
        public event GameWinnerDelegate OnGameWin;

        protected override void OnCreate()
        {
            base.OnCreate();
            // Find the ECB system once and store it for later usage
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            #region Initializing Data
            var mousePos = Input.mousePosition;
            var worldPos = (float3)Camera.main.ScreenToWorldPoint(mousePos);
            float3 worldPosNormalized = new float3(worldPos.x, worldPos.y, PieceManager.PieceZ);
            float speed = 1;

            bool mouseButtonHeld = Input.GetKey(KeyCode.Mouse0);
            var cellQuery = GetEntityQuery(ComponentType.ReadOnly<CellTag>(), ComponentType.ReadOnly<Translation>());
            var highlightedCellQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(), ComponentType.ReadOnly<Translation>());
            var enemyCellQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyCellTag>(), ComponentType.ReadOnly<Translation>());
            var ecb = ecbSystem.CreateCommandBuffer();
            NativeArray<Translation> cellTranslation = cellQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Translation> highlightedCellTranslation = highlightedCellQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Translation> enemyCellTranslation = enemyCellQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Entity> cellEntities = cellQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<Entity> enemyCellEntities = enemyCellQuery.ToEntityArray(Allocator.TempJob);

            ComponentDataFromEntity<PieceOnCellComponent> pieceOnCellComponentArray = GetComponentDataFromEntity<PieceOnCellComponent>();
            #endregion

            Entities.
                WithAll<SelectedTag>().
                ForEach((Entity e, int entityInQueryIndex, ref Translation pieceTranslation, ref PieceTag piece, ref OriginalLocationComponent originalLocation, in TeamComponent teamComponent) =>
                {

                    pieceTranslation.Value = math.lerp(pieceTranslation.Value, worldPosNormalized, speed);

                    //this is the code for when the user has dropped the piece
                    if (mouseButtonHeld) return;

                    Entity originalCellEntity = Location.GetMatchedEntity(cellEntities, cellTranslation, originalLocation.originalLocation);
                    Entity newCellEntity = Location.GetMatchedEntity(cellEntities, cellTranslation, math.round(pieceTranslation.Value));
                    Entity enemyCell = Location.GetMatchedEntity(enemyCellEntities, enemyCellTranslation, math.round(pieceTranslation.Value));
                    Entity enemyPieceEntity = GetEnemyPiece(pieceOnCellComponentArray, enemyCell);

                    if (IsValidMove(highlightedCellTranslation, enemyCellTranslation, pieceTranslation))
                    {
                        if(Location.HasMatch(enemyCellTranslation, pieceTranslation))
                        {
                            ecb.AddComponent(e, new FighterTag());
                            ecb.AddComponent(enemyPieceEntity, new FighterTag());
                        }

                        var pieceOnCellUpdaterEntity = ecb.CreateEntity();
                        ecb.AddComponent(pieceOnCellUpdaterEntity, new PieceOnCellUpdaterTag());

                        pieceTranslation.Value = math.round(pieceTranslation.Value);
                        originalLocation.originalLocation = pieceTranslation.Value;

                        ChangeTurn(ecb, teamComponent);
                    }
                    else
                        pieceTranslation.Value = originalLocation.originalLocation;

                    ecb.RemoveComponent<SelectedTag>(e);
                }).Schedule();
            Dependency.Complete();
            //ecbSystem.AddJobHandleForProducer(this.Dependency);

            cellTranslation.Dispose();
            highlightedCellTranslation.Dispose();
            enemyCellTranslation.Dispose();
            enemyCellEntities.Dispose();
            cellEntities.Dispose();
        }

        private static void ChangeTurn(EntityCommandBuffer ecb, TeamComponent teamComponent)
        {
            var changeTurnEntity = ecb.CreateEntity();
            ecb.AddComponent(changeTurnEntity, new ChangeTurnComponent()
            {
                currentTurnTeam = teamComponent.myTeam
            });
        }

        private static void CreateArbiter(EntityCommandBuffer ecb, Entity e, Entity enemyPiece, Entity newCellEntity,
            Entity originalCellEntity)
        {
            Entity arbiter = ecb.CreateEntity();
            ecb.AddComponent<ArbiterComponent>(arbiter);
            ecb.SetComponent(arbiter, new ArbiterComponent
            {
                attackingPieceEntity = e,
                defendingPieceEntity = enemyPiece,
                battlegroundCellEntity = newCellEntity,
                originalCellEntity = originalCellEntity
            });
        }

        private static Entity GetEnemyPiece(ComponentDataFromEntity<PieceOnCellComponent> pieceOnCellComponentArray, Entity enemyCell)
        {
            return enemyCell != Entity.Null ? pieceOnCellComponentArray[enemyCell].PieceEntity : Entity.Null;
        }

        private static bool IsValidMove(NativeArray<Translation> highlightedCellTranslation, NativeArray<Translation> enemyCellTranslation, Translation pieceTranslation)
        {
            return Location.HasMatch(enemyCellTranslation, pieceTranslation) || Location.HasMatch(highlightedCellTranslation, pieceTranslation);
        }

        private void CheckIfGameFinishedEventRaised(EntityCommandBuffer ecb)
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
