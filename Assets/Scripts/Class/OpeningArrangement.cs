namespace Assets.Scripts.Class
{
    public class OpeningArrangement
    {
        public readonly int[] defaultPieceArrangementArray = new int[27]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13,13,13,13,13,13,
            0, 0, 14,15,15,15,15,15,15
        };

        public readonly int[,] defaultArrangementArray = new int[3, 9]
            {{ 1,2,3,4,5,6,7,8,9},{ 10,11,12,13,13,13,13,13,13},{ 0,0,14,15,15,15,15,15,15} };

        public int[] blitzkriegLeftPieceArrangementArray = new int[21]
        {
            1, 2, 0, 13, 13, 10, 7, 8, 9,
            3, 4, 5, 13,13,13,13,11,12,
            14, 0, 6
        };

        public int[] blitzkriegRightPieceArrangementArray = new int[21]
        {
            9, 8, 7, 13, 13, 10, 0, 2, 1,
            12, 11, 13, 13,13,13,5,4,3,
            6, 0, 14
        };

        public int[] motherShipPieceArrangementArray = new int[21]
        {
            9,  8,   13, 0, 3, 0, 13, 5, 6,
            12, 11, 13, 1, 14, 2, 13, 4, 7,
            10, 13, 13
        };

        public int[] boxPieceArrangementArray = new int[21]
        {
            1, 2, 0, 0, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13,13,13,13,
            13, 13, 14
        };

        public int[] randomArrangementArray = new int[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13,13,13,13,13,13,
            0, 0, 14
        };
    }
}