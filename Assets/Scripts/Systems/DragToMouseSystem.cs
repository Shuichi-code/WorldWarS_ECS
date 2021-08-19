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
    public class DragToMouseSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            #region Initializing Data
            var mousePos = Input.mousePosition;
            var worldPos = (float3)Camera.main.ScreenToWorldPoint(mousePos);
            float3 worldPosNormalized = new float3(worldPos.x, worldPos.y, GameConstants.PieceZ);
            float speed = 1;

            bool mouseButtonHeld = Input.GetKey(KeyCode.Mouse0);
            var cellQuery = GetEntityQuery(ComponentType.ReadOnly<CellTag>(), ComponentType.ReadOnly<Translation>());
            var highlightedCellQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(), ComponentType.ReadOnly<Translation>());
            var enemyCellQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyCellTag>(), ComponentType.ReadOnly<Translation>());
            var ecbParallel = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            NativeArray<Translation> cellTranslation = cellQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Translation> highlightedCellTranslationArray = highlightedCellQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Translation> enemyCellTranslationArray = enemyCellQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Entity> cellEntities = cellQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<Entity> enemyCellEntities = enemyCellQuery.ToEntityArray(Allocator.TempJob);
            #endregion
            //WARNING! This job needs to be run only on Schedule() to prevent race condition
            Entities.
                WithAll<SelectedTag>().
                ForEach((Entity e, int entityInQueryIndex, ref Translation pieceTranslation, ref PieceTag piece, ref OriginalLocationComponent originalLocation, in TeamComponent teamComponent) =>
                {

                    pieceTranslation.Value = math.lerp(pieceTranslation.Value, worldPosNormalized, speed);

                    //this is the code for when the user has dropped the piece
                    if (mouseButtonHeld) return;

                    if (IsValidMove(highlightedCellTranslationArray, enemyCellTranslationArray, pieceTranslation))
                    {
                        if(Location.HasMatch(enemyCellTranslationArray, pieceTranslation))
                        {
                            ecbParallel.AddComponent(entityInQueryIndex,e , new FighterTag());
                            ecbParallel.AddComponent(entityInQueryIndex,e, new FighterTag());
                        }

                        pieceTranslation.Value = math.round(pieceTranslation.Value);
                        originalLocation.originalLocation = pieceTranslation.Value;

                        ChangeTurn(entityInQueryIndex, ecbParallel, teamComponent);
                        UpdatePiecesOnCells(ecbParallel, entityInQueryIndex);
                    }
                    else
                        pieceTranslation.Value = originalLocation.originalLocation;

                    ecbParallel.RemoveComponent<SelectedTag>(entityInQueryIndex,e);
                }).Schedule();
                CompleteDependency();

            cellTranslation.Dispose();
            highlightedCellTranslationArray.Dispose();
            enemyCellTranslationArray.Dispose();
            enemyCellEntities.Dispose();
            cellEntities.Dispose();
        }

        private static void UpdatePiecesOnCells(EntityCommandBuffer.ParallelWriter ecbParallel, int entityInQueryIndex)
        {
            var pieceOnCellUpdaterEntity = ecbParallel.CreateEntity(entityInQueryIndex);
            ecbParallel.AddComponent(entityInQueryIndex, pieceOnCellUpdaterEntity, new PieceOnCellUpdaterTag());
        }

        private static void ChangeTurn(int entityInQueryIndex, EntityCommandBuffer.ParallelWriter ecb, TeamComponent teamComponent)
        {
            var changeTurnEntity = ecb.CreateEntity(entityInQueryIndex);
            ecb.AddComponent(entityInQueryIndex, changeTurnEntity, new ChangeTurnComponent()
            {
                currentTurnTeam = teamComponent.myTeam
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
    }
}
