using LibraryManager.Application.Saves;
using LibraryManager.Domain.World;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Application
{
    public sealed class SaveGameMapperTests
    {
        [Test]
        public void RoundTrip_KeepsTheWholeWorld()
        {
            WorldState world = WorldState.CreateNew("Libros del Faro", 1234);
            world.AdvanceDay();
            world.Bookstore.Pay(LibraryManager.Domain.Economy.Money.FromEuros(99.95m));
            world.Random.NextULong();

            WorldState loaded = SaveGameMapper.ToWorld(SaveGameMapper.ToData(world, new FixedClock().UtcNow));

            Assert.AreEqual(world.Bookstore.Name, loaded.Bookstore.Name);
            Assert.AreEqual(world.Bookstore.Cash, loaded.Bookstore.Cash);
            Assert.AreEqual(world.Date, loaded.Date);
            Assert.AreEqual(world.Random.NextULong(), loaded.Random.NextULong());
        }

        [Test]
        public void ToData_WritesCurrentVersion()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);

            Assert.AreEqual(SaveGameSchema.CurrentVersion, data.schemaVersion);
            Assert.AreEqual("2026-11-19T18:00:00.0000000Z", data.savedAtUtc);
        }

        [Test]
        public void FutureVersion_IsRejected()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);
            data.schemaVersion = SaveGameSchema.CurrentVersion + 1;

            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));
        }

        [Test]
        public void InvalidData_IsRejectedWithSaveGameException()
        {
            SaveGameData data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);
            data.world.bookstore.name = "";
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));

            data = SaveGameMapper.ToData(WorldState.CreateNew("Libros", 1), new FixedClock().UtcNow);
            data.world.randomState = "no es un número";
            Assert.Throws<SaveGameException>(() => SaveGameMapper.ToWorld(data));
        }
    }
}
