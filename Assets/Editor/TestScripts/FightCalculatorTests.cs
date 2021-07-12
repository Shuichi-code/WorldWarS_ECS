using Assets.Scripts.Class;
using NUnit.Framework;

namespace Assets.Editor.TestScripts
{
    [TestFixture]
    public class FightCalculatorTests
    {
        [Test]
        [TestCase(Piece.Spy, Piece.BrigadierGeneral)]
        [TestCase(Piece.Private, Piece.Spy)]
        public void IsAttackerWinner__ReturnsTrue(int attackingRank, int defendingRank)
        {
            var result = FightCalculator.IsAttackerWinner(attackingRank, defendingRank);

            Assert.That(result, Is.True);
        }

        [Test]
        [TestCase(Piece.BrigadierGeneral, Piece.Spy)]
        [TestCase(Piece.Spy, Piece.Private)]
        [TestCase(Piece.Flag, Piece.Flag)]
        [TestCase(Piece.Spy, Piece.Spy)]
        public void IsAttackerWinner__ReturnsFalse(int attackingRank, int defendingRank)
        {
            var result = FightCalculator.IsAttackerWinner(attackingRank, defendingRank);

            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase(Piece.BrigadierGeneral, Piece.Spy)]
        [TestCase(Piece.Spy, Piece.Private)]
        public void IsDefenderWinner__ReturnsTrue(int attackingRank, int defendingRank)
        {
            var result = FightCalculator.IsDefenderWinner(attackingRank, defendingRank);

            Assert.That(result, Is.True);
        }

        [Test]
        [TestCase(Piece.Spy, Piece.BrigadierGeneral)]
        [TestCase(Piece.Flag, Piece.Flag)]
        [TestCase(Piece.Private, Piece.Spy)]
        public void IsDefenderWinner__ReturnsFalse(int attackingRank, int defendingRank)
        {
            var result = FightCalculator.IsDefenderWinner(attackingRank, defendingRank);

            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase(Piece.Spy, Piece.BrigadierGeneral)]
        [TestCase(Piece.Private, Piece.Spy)]
        public void DetermineFightResult_WhenAttackerWins_ReturnFightResultAttackerWins(int attackingRank, int defendingRank)
        {
            var result = FightCalculator.DetermineFightResult(attackingRank, defendingRank);
            Assert.That(result, Is.EqualTo(FightResult.AttackerWins));
        }

        [Test]
        [TestCase(Piece.BrigadierGeneral, Piece.Spy)]
        [TestCase(Piece.Spy, Piece.Private)]
        public void DetermineFightResult_WhenDefenderWins_ReturnFightResultDefenderWins(int attackingRank, int defendingRank)
        {
            var result = FightCalculator.DetermineFightResult(attackingRank, defendingRank);
            Assert.That(result, Is.EqualTo(FightResult.DefenderWins));
        }

        [Test]
        [TestCase(Piece.Spy, Piece.Spy)]
        public void DetermineFightResult_WhenBothLose_ReturnFightResultBothLose(int attackingRank, int defendingRank)
        {
            var result = FightCalculator.DetermineFightResult(attackingRank, defendingRank);
            Assert.That(result, Is.EqualTo(FightResult.BothLose));
        }

        [Test]
        [TestCase(Piece.Flag, Piece.Flag)]
        [TestCase(Piece.Spy, Piece.Flag)]
        [TestCase(Piece.Private, Piece.Flag)]
        [TestCase(Piece.Flag, Piece.Private)]
        public void DetermineFightResult_WhenFlagIsAttacked_ReturnFightResultFlagDestroyed(int attackingRank, int defendingRank)
        {
            var result = FightCalculator.DetermineFightResult(attackingRank, defendingRank);
            Assert.That(result, Is.EqualTo(FightResult.FlagDestroyed));
        }
    }
}
