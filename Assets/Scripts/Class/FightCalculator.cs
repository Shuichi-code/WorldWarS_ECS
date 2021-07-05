namespace Assets.Scripts.Class
{
    public class FightCalculator
    {
        public static FightResult DetermineFightResult(int attackingRank, int defendingRank)
        {
            if (IsFlagDestroyed(attackingRank, defendingRank))
            {
                return FightResult.FlagDestroyed;
            }
            if (IsAttackerWinner(attackingRank, defendingRank))
            {
                //return the defending entity for destruction
                return FightResult.AttackerWins;
            }

            //if attacking rank is lower than defending rank or the attacker is a spy and the defender is a private, defending side wins
            else if (IsDefenderWinner(attackingRank, defendingRank))
            {
                //return attacking entity for destruction
                return FightResult.DefenderWins;
            }

            return FightResult.BothLose;

        }

        private static bool IsFlagDestroyed(int attackingRank, int defendingRank)
        {
            return defendingRank == Piece.Flag || (attackingRank == Piece.Flag);
        }

        /// <summary>
        /// Returns true if a flag attacks a flag, or if a private attacks a spy, or if the attacker has a higher rank than the defender and a spy is not attacking a private
        /// </summary>
        /// <param name="attackingRank"></param>
        /// <param name="defendingRank"></param>
        /// <returns></returns>
        public static bool IsAttackerWinner(int attackingRank, int defendingRank)
        {
            return (attackingRank == 13 && defendingRank == 0) ||
                   ((attackingRank < defendingRank) && !(attackingRank == 0 && defendingRank == 13));
        }

        /// <summary>
        /// Returns true if the defender has a higher rank or if a spy is attacking a private
        /// </summary>
        /// <param name="attackingRank"></param>
        /// <param name="defendingRank"></param>
        /// <returns></returns>
        public static bool IsDefenderWinner(int attackingRank, int defendingRank)
        {
            return (attackingRank > defendingRank && !(attackingRank == 13 && defendingRank == 0)) ||
                   (attackingRank == 0 && defendingRank == 13);
        }
    }
}