using System;
using LibraryManager.Application.Common;
using NUnit.Framework;

namespace LibraryManager.Tests.EditMode.Application
{
    public sealed class OperationGateTests
    {
        [Test]
        public void FirstEntry_IsAccepted_AndReentryRejected()
        {
            var gate = new OperationGate();

            Assert.IsTrue(gate.TryEnter("guardar"));
            Assert.IsFalse(gate.TryEnter("avanzar día"));
            Assert.AreEqual("guardar", gate.ActiveOperation);

            gate.Exit("guardar");
            Assert.IsFalse(gate.IsBusy);
        }

        [Test]
        public void ExitWithoutOperation_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new OperationGate().Exit("guardar"));
        }

        [Test]
        public void ExitOfAnotherOperation_Throws()
        {
            var gate = new OperationGate();
            gate.TryEnter("guardar");

            Assert.Throws<InvalidOperationException>(() => gate.Exit("cargar"));
        }
    }
}
