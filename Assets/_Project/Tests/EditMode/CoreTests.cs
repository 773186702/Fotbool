using Fotbool.AI;
using Fotbool.Core;
using Fotbool.Data;
using Fotbool.Management;
using NUnit.Framework;
using UnityEngine;

namespace Fotbool.Tests
{
    public sealed class CoreTests
    {
        private GameObject root;
        private ClubManager manager;
        [SetUp] public void SetUp()
        {
            root = new GameObject("Test club"); manager = root.AddComponent<ClubManager>();
            manager.CreateClub("Test FC", "Test Arena", Color.white, Color.black, 0);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); }
        private static PlayerProfile Player() => new PlayerProfile
        {
            id = "player-1", fullName = "Test Player", marketValue = 100000, wage = 1000,
            potential = 90, attributes = new[] {70,70,70,70,70,70,70}
        };
        [Test] public void UniformAttributesRetainOverallAcrossPositions()
        {
            foreach (PlayerPosition position in System.Enum.GetValues(typeof(PlayerPosition)))
                Assert.AreEqual(70, PlayerProfile.ComputeOverall(position, Player().attributes));
        }
        [Test] public void InvalidAttributesAreRejected()
        { Assert.Throws<System.ArgumentException>(() => PlayerProfile.ComputeOverall(PlayerPosition.CM, new int[2])); }
        [Test] public void FiveDifficultiesImproveReactionAndAccuracy()
        {
            for (int i = 1; i < 5; i++)
            {
                Assert.Less(FootballAI.ReactionSeconds((AIDifficulty)i), FootballAI.ReactionSeconds((AIDifficulty)(i-1)));
                Assert.Less(FootballAI.AimError((AIDifficulty)i), FootballAI.AimError((AIDifficulty)(i-1)));
            }
        }
        [Test] public void TransferConservesMoneyAndMovesOwnershipOnce()
        {
            var seller = new TeamProfile(); var player = Player(); seller.squad.Add(player);
            long total = seller.budget + manager.Club.budget;
            Assert.IsTrue(manager.BuyPlayer(seller, player.id, 100000, 1000, 3));
            Assert.AreEqual(total, seller.budget + manager.Club.budget);
            Assert.IsFalse(seller.squad.Contains(player)); Assert.Contains(player, manager.Club.squad);
            Assert.IsFalse(manager.BuyPlayer(seller, player.id, 100000, 1000, 3));
        }
        [Test] public void RejectedOfferDoesNotMutateEconomy()
        {
            var seller = new TeamProfile(); var player = Player(); seller.squad.Add(player);
            long budget = manager.Club.budget;
            Assert.IsFalse(manager.BuyPlayer(seller, player.id, -1, 1000, 3));
            Assert.IsFalse(manager.BuyPlayer(seller, player.id, 100000, long.MaxValue, 3));
            Assert.AreEqual(budget, manager.Club.budget); Assert.Contains(player, seller.squad);
        }
        [Test] public void TrainingIsCappedAndLimitedToOncePerWeek()
        {
            var player = Player(); player.potential = 71; manager.Club.squad.Add(player);
            Assert.IsTrue(manager.Train(player.id, AttributeType.Pace));
            Assert.AreEqual(71, player.GetAttribute(AttributeType.Pace));
            Assert.IsFalse(manager.Train(player.id, AttributeType.Passing));
            Assert.IsTrue(manager.AdvanceWeek());
            Assert.IsFalse(manager.Train(player.id, AttributeType.Pace));
            Assert.IsTrue(manager.Train(player.id, AttributeType.Passing));
        }
        [Test] public void FormationSlotsRemainInsidePitch()
        {
            foreach (string formation in new[] { "4-3-3", "4-4-2" })
                for (int i = 0; i < 11; i++)
                { var slot = TacticalAI.Slot(formation, i); Assert.Less(Mathf.Abs(slot.x), 34); Assert.Less(Mathf.Abs(slot.y), 52.5f); }
        }

        [Test] public void PositionFamilyMappingIsCorrect()
        {
            Assert.AreEqual(PositionFamily.Goalkeeper, PlayerPosition.GK.Family());
            Assert.AreEqual(PositionFamily.Defender, PlayerPosition.CB.Family());
            Assert.AreEqual(PositionFamily.Defender, PlayerPosition.LB.Family());
            Assert.AreEqual(PositionFamily.Defender, PlayerPosition.RB.Family());
            Assert.AreEqual(PositionFamily.Midfielder, PlayerPosition.CM.Family());
            Assert.AreEqual(PositionFamily.Midfielder, PlayerPosition.CDM.Family());
            Assert.AreEqual(PositionFamily.Midfielder, PlayerPosition.CAM.Family());
            Assert.AreEqual(PositionFamily.Attacker, PlayerPosition.ST.Family());
            Assert.AreEqual(PositionFamily.Attacker, PlayerPosition.LW.Family());
            Assert.AreEqual(PositionFamily.Attacker, PlayerPosition.RW.Family());
        }

        [Test] public void ShortNamesAreNonEmpty()
        {
            foreach (PlayerPosition pos in System.Enum.GetValues(typeof(PlayerPosition)))
                Assert.IsFalse(string.IsNullOrEmpty(pos.ShortName()));
            foreach (AttributeType attr in System.Enum.GetValues(typeof(AttributeType)))
                Assert.IsFalse(string.IsNullOrEmpty(attr.ShortName()));
        }

        [Test] public void AIDifficultyReactionAndAccuracyMonotonic()
        {
            for (int i = 1; i <= 4; i++)
            {
                Assert.Less(FootballAI.ReactionSeconds((AIDifficulty)i), FootballAI.ReactionSeconds((AIDifficulty)(i - 1)));
                Assert.Less(FootballAI.AimError((AIDifficulty)i), FootballAI.AimError((AIDifficulty)(i - 1)));
            }
        }

        [Test] public void MatchEventTypeHasExpectedValues()
        {
            Assert.AreEqual(MatchEventType.Goal, MatchEventType.Goal);
            Assert.AreEqual(MatchEventType.YellowCard, MatchEventType.YellowCard);
            Assert.AreEqual(MatchEventType.RedCard, MatchEventType.RedCard);
            Assert.AreEqual(MatchEventType.Corner, MatchEventType.Corner);
            Assert.AreEqual(MatchEventType.Offside, MatchEventType.Offside);
        }

        [Test] public void MatchPhaseHasAllRequiredPhases()
        {
            Assert.AreEqual(11, System.Enum.GetValues(typeof(MatchPhase)).Length);
            Assert.AreEqual(MatchPhase.FirstHalf, MatchPhase.FirstHalf);
            Assert.AreEqual(MatchPhase.SecondHalf, MatchPhase.SecondHalf);
            Assert.AreEqual(MatchPhase.Halftime, MatchPhase.Halftime);
            Assert.AreEqual(MatchPhase.ExtraTimeFirstHalf, MatchPhase.ExtraTimeFirstHalf);
            Assert.AreEqual(MatchPhase.ExtraTimeSecondHalf, MatchPhase.ExtraTimeSecondHalf);
            Assert.AreEqual(MatchPhase.PenaltyShootout, MatchPhase.PenaltyShootout);
            Assert.AreEqual(MatchPhase.Finished, MatchPhase.Finished);
        }

        [Test] public void CompetitionTypeHasExpectedValues()
        {
            Assert.AreEqual(4, System.Enum.GetValues(typeof(CompetitionType)).Length);
            Assert.AreEqual(CompetitionType.League, CompetitionType.League);
            Assert.AreEqual(CompetitionType.Cup, CompetitionType.Cup);
            Assert.AreEqual(CompetitionType.ChampionsLeague, CompetitionType.ChampionsLeague);
            Assert.AreEqual(CompetitionType.Friendly, CompetitionType.Friendly);
        }

        [Test] public void MatchOutcomeCalculationsAreCorrect()
        {
            var outcome = new MatchOutcome
            {
                HomeGoals = 2,
                AwayGoals = 1,
                WentToPenalties = true,
                HomePenaltyScore = 4,
                AwayPenaltyScore = 3
            };

            Assert.IsTrue(outcome.HomeWon);
            Assert.IsFalse(outcome.AwayWon);
            Assert.IsFalse(outcome.IsDraw);
            Assert.IsTrue(outcome.WentToPenalties);
        }

        [Test] public void MatchOutcomeDrawCalculations()
        {
            var outcome = new MatchOutcome { HomeGoals = 1, AwayGoals = 1 };
            Assert.IsFalse(outcome.HomeWon);
            Assert.IsFalse(outcome.AwayWon);
            Assert.IsTrue(outcome.IsDraw);
        }

        [Test] public void PrizeTypeHasExpectedValues()
        {
            Assert.AreEqual(5, System.Enum.GetValues(typeof(PrizeType)).Length);
            Assert.AreEqual(PrizeType.MatchWin, PrizeType.MatchWin);
            Assert.AreEqual(PrizeType.TournamentWin, PrizeType.TournamentWin);
            Assert.AreEqual(PrizeType.Individual, PrizeType.Individual);
            Assert.AreEqual(PrizeType.League, PrizeType.League);
            Assert.AreEqual(PrizeType.Cup, PrizeType.Cup);
        }

        [Test] public void TournamentFormatHasExpectedValues()
        {
            Assert.AreEqual(4, System.Enum.GetValues(typeof(TournamentFormat)).Length);
            Assert.AreEqual(TournamentFormat.League, TournamentFormat.League);
            Assert.AreEqual(TournamentFormat.SingleElimination, TournamentFormat.SingleElimination);
            Assert.AreEqual(TournamentFormat.DoubleElimination, TournamentFormat.DoubleElimination);
            Assert.AreEqual(TournamentFormat.GroupStage, TournamentFormat.GroupStage);
        }
    }
}
