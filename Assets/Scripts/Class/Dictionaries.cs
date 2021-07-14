using System.Collections.Generic;

namespace Assets.Scripts.Class
{
    public class Dictionaries
    {
        public Dictionary<int, string> mPieceRank = new System.Collections.Generic.Dictionary<int, string>()
        {
            { Piece.Spy,"Spy"},
            { Piece.FiveStarGeneral,"G5S" },
            { Piece.FourStarGeneral,"G4S" },
            { Piece.LieutenantGeneral,"LtG"},
            { Piece.MajorGeneral,"MjG"},
            { Piece.BrigadierGeneral,"BrG"},
            { Piece.Colonel,"Col"},
            { Piece.LieutenantColonel,"LtCol"},
            { Piece.Major,"Maj" },
            { Piece.Captain,"Cpt"},
            { Piece.FirstLieutenant,"1Lt"},
            { Piece.SecondLieutenant,"2Lt"},
            { Piece.Sergeant,"Sgt"},
            { Piece.Private,"Pvt"},
            { Piece.Flag,"Flg"},
        };
        public Dictionary<string, int[,]> mOpenings = new System.Collections.Generic.Dictionary<string, int[,]>()
        {
            { DefaultArrangementString, new OpeningArrangement().defaultArrangementArray},
            { BlitzkriegLeftString, new OpeningArrangement().blitzkriegLeftPieceArrangementArray},
            { BlitzkriegRightString, new OpeningArrangement().blitzkriegRightPieceArrangementArray},
            { MothershipString, new OpeningArrangement().motherShipPieceArrangementArray},
            { BoxString, new OpeningArrangement().boxPieceArrangementArray},
        };

        public List<string> openingList = new List<string>() { DefaultArrangementString, BlitzkriegLeftString, BlitzkriegRightString, MothershipString, BoxString };

        private const string DefaultArrangementString = "Default";
        private const string BlitzkriegLeftString = "Blitzkrieg-Left";
        private const string BlitzkriegRightString = "Blitzkrieg-Right";
        private const string MothershipString = "Mothership";
        private const string BoxString = "Box";
    }
}