using LibraryManager.Domain.Common;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Domain
{
    public sealed class DeterministicRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var first = new DeterministicRandom(42);
            var second = new DeterministicRandom(42);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(first.NextULong(), second.NextULong());
            }
        }

        [Test]
        public void RestoredState_ContinuesTheSequence()
        {
            var original = new DeterministicRandom(7);
            original.NextULong();
            original.NextULong();

            DeterministicRandom restored = DeterministicRandom.FromState(original.State);

            Assert.AreEqual(original.NextInt(0, 1000), restored.NextInt(0, 1000));
        }

        [Test]
        public void NextInt_StaysInRange()
        {
            var random = new DeterministicRandom(1);
            for (int i = 0; i < 10000; i++)
            {
                int value = random.NextInt(-3, 4);
                Assert.That(value, Is.InRange(-3, 3));
            }
        }

        [Test]
        public void NextDouble_StaysInUnitInterval()
        {
            var random = new DeterministicRandom(99);
            for (int i = 0; i < 10000; i++)
            {
                double value = random.NextDouble();
                Assert.That(value, Is.GreaterThanOrEqualTo(0d).And.LessThan(1d));
            }
        }
    }
}
