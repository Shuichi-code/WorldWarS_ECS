using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Class
{
    public class Location
    {
        public static float2 RemoveZ(float3 refTranslation)
        {
            return new float2(refTranslation.x, refTranslation.y);
        }

        public static bool HasMatch(NativeArray<Translation> trnsArray, Translation refTranslation)
        {
            int i = 0;
            while (i < trnsArray.Length)
            {
                if (IsMatchLocation(math.round(refTranslation.Value), trnsArray[i].Value))
                {
                    return true;
                }
                i++;
            }
            return false;
        }

        public static Entity GetMatchedEntity(NativeArray<Entity> cellEntityArray, NativeArray<Translation> cellTranslationArray, float3 pieceTranslation)
        {
            int i = 0;
            while (i < cellTranslationArray.Length)
            {
                if (IsMatchLocation(cellTranslationArray[i].Value, pieceTranslation))
                {
                    return cellEntityArray[i];
                }
                i++;
            }
            return Entity.Null;
        }

        public static bool IsMatchLocation(float3 cellLocation, float3 selectedPieceLocation)
        {
            return math.distance(RemoveZ(selectedPieceLocation), RemoveZ(cellLocation)) == 0;
        }
    }
}
