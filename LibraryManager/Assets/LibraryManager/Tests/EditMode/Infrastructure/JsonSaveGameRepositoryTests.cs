using System.IO;
using LibraryManager.Application.Saves;
using LibraryManager.Domain.World;
using LibraryManager.Infrastructure.Persistence;
using LibraryManager.Tests.EditMode.Application;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Infrastructure
{
    public sealed class JsonSaveGameRepositoryTests
    {
        private string _directory;
        private JsonSaveGameRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "LibraryManagerTests", Path.GetRandomFileName());
            _repository = new JsonSaveGameRepository(Path.Combine(_directory, JsonSaveGameRepository.DefaultFileName));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void SaveAndLoad_RoundTripsThroughJson()
        {
            WorldState world = WorldState.CreateNew("Libros del Faro", ulong.MaxValue - 5);
            world.AdvanceDay();

            _repository.Save(SaveGameMapper.ToData(world, new FixedClock().UtcNow));
            WorldState loaded = SaveGameMapper.ToWorld(_repository.Load());

            Assert.AreEqual(world.Bookstore.Name, loaded.Bookstore.Name);
            Assert.AreEqual(world.Date, loaded.Date);
            Assert.AreEqual(world.Random.State, loaded.Random.State);
        }

        [Test]
        public void SecondSave_KeepsABackupOfThePreviousOne()
        {
            WorldState world = WorldState.CreateNew("Libros", 1);
            _repository.Save(SaveGameMapper.ToData(world, new FixedClock().UtcNow));
            world.AdvanceDay();
            _repository.Save(SaveGameMapper.ToData(world, new FixedClock().UtcNow));

            Assert.IsTrue(File.Exists(_repository.BackupPath));
            Assert.IsFalse(File.Exists(_repository.FilePath + ".tmp"));
        }

        [Test]
        public void MissingOrCorruptFile_ThrowsSaveGameException()
        {
            Assert.IsFalse(_repository.Exists());
            Assert.Throws<SaveGameException>(() => _repository.Load());

            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath, "{ esto no es una partida");
            Assert.Throws<SaveGameException>(() => _repository.Load());

            File.WriteAllText(_repository.FilePath, "{}");
            Assert.Throws<SaveGameException>(() => _repository.Load());
        }
    }
}
