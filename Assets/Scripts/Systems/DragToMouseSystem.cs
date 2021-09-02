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
            var worldPosNormalized = new float3(worldPos.x, worldPos.y, GameConstants.PieceZ);
            const int speed = 1;

            var mouseButtonHeld = Input.GetKey(KeyCode.Mouse0);
            var cellQuery = GetEntityQuery(ComponentType.ReadOnly<CellTag>(), ComponentType.ReadOnly<Translation>());
            var highlightedCellQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(), ComponentType.ReadOnly<Translation>());
            var enemyCellQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyCellTag>(), ComponentType.ReadOnly<Translation>());
            var ecb = EcbSystem.CreateCommandBuffer();
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

                        pieceTranslation.Value = math.round(pieceTranslation.Value);
                        originalLocation.originalLocation = pieceTranslation.Value;

                        CheckForCollisions(ecb, entityInQueryIndex);

                        ChangeTurn( ecb, teamComponent, entityInQueryIndex);
                        UpdatePiecesOnCells(ecb, entityInQueryIndex);
                    }
                    else
                        pieceTranslation.Value = originalLocation.originalLocation;

                    ecb.RemoveComponent<SelectedTag>( e);
                }).Schedule();
                CompleteDependency();

            cellTranslation.Dispose();
            highlightedCellTranslationArray.Dispose();
            enemyCellTranslationArray.Dispose();
            enemyCellEntities.Dispose();
            cellEntities.Dispose();
        }

        private static void CheckForCollisions(EntityCommandBuffer ecb, int entityInQueryIndex)
        {
            var checkColliderEntity = ecb.CreateEntity();
            ecb.AddComponent(checkColliderEntity, new PieceCollisionCheckerTag());
        }

        /// <summary>
        /// Creates PieceOnCellUpdaterTag To Activate Syste
        /// </summary>
        /// <param name="ecbParallel"></param>
        /// <param name="entityInQueryIndex"></param>
        private static void UpdatePiecesOnCells(EntityCommandBuffer ecbParallel, int entityInQueryIndex)
        {
            var pieceOnCellUpdaterEntity = ecbParallel.CreateEntity();
            ecbParallel.AddComponent(pieceOnCellUpdaterEntity, new PieceOnCellUpdaterTag());
        }

        private static void ChangeTurn(EntityCommandBuffer ecb, TeamComponent teamComponent, int entityInQueryIndex)
        {
            var changeTurnEntity = ecb.CreateEntity();
            ecb.AddComponent( changeTurnEntity, new ChangeTurnComponent()
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
