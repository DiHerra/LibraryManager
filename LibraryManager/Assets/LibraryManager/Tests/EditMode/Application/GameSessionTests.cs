using System;
using LibraryManager.Application.Game;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Application
{
    public sealed class GameSessionTests
    {
        [Test]
        public void NewGame_SaveAndLoad_RestoresTheSameWorld()
        {
            var repository = new InMemorySaveGameRepository();
            var session = new GameSession(repository, new FixedClock());

            session.StartNew("Libros del Faro", 77);
            session.AdvanceDay();
            session.AdvanceDay();
            session.Save();

            var reopened = new GameSession(repository, new FixedClock());
            Assert.IsTrue(reopened.HasSavedGame);
            reopened.Load();

            Assert.AreEqual("Libros del Faro", reopened.World.Bookstore.Name);
            Assert.AreEqual(WorldState.DefaultStartDate.AddDays(2), reopened.World.Date);
        }

        [Test]
        public void ActionsWithoutWorld_Throw()
        {
            var session = new GameSession(new InMemorySaveGameRepository(), new FixedClock());

            Assert.Throws<InvalidOperationException>(() => session.AdvanceDay());
            Assert.Throws<InvalidOperationException>(session.Save);
            Assert.IsFalse(session.IsBusy, "La compuerta se libera aunque la operación falle.");
        }

        [Test]
        public void WorldChanged_IsRaisedWhenTheDayAdvances()
        {
            var session = new GameSession(new InMemorySaveGameRepository(), new FixedClock());
            session.StartNew("Libros", 1);
            int notifications = 0;
            session.WorldChanged += () => notifications++;

            session.AdvanceDay();

            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void AdvanceWeek_ReturnsTheDaysAndKeepsThemAsLastAdvance()
        {
            var session = new GameSession(new InMemorySaveGameRepository(), new FixedClock());
            session.StartNew("Libros", 1);

            var result = session.AdvanceWeek();

            Assert.AreEqual(6, result.Days.Count);
            Assert.AreSame(result, session.LastAdvance);
        }
    }
}
