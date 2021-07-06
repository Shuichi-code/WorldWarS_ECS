using System.Collections.Generic;

namespace Assets.Scripts.Class
{
    public class Dictionaries
    {
        public Dictionary<int, string> mPieceRank = new System.Collections.Generic.Dictionary<int, string>()
        {
            { 0,"Spy"},
            { 1,"G5S" },
            { 2,"G4S" },
            { 3,"LtG"},
            { 4,"MjG"},
            { 5,"BrG"},
            { 6,"Col"},
            { 7,"LtCol"},
            { 8,"Maj" },
            { 9,"Cpt"},
            { 10,"1Lt"},
            { 11,"2Lt"},
            { 12,"Sgt"},
            { 13,"Pvt"},
            { 14,"Flg"},
        };
        public Dictionary<string, int[,]> mOpenings = new System.Collections.Generic.Dictionary<string, int[,]>()
        {
            { "Default", new OpeningArrangement().defaultArrangementArray},
            { "Blitzkrieg-Left", new OpeningArrangement().blitzkriegLeftPieceArrangementArray},
            { "Blitzkrieg-Right", new OpeningArrangement().blitzkriegRightPieceArrangementArray},
            { "Mothership", new OpeningArrangement().motherShipPieceArrangementArray},
            { "Box", new OpeningArrangement().boxPieceArrangementArray},
        };
    }
}