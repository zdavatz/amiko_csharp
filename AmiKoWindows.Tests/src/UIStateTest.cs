using System;
using System.IO;
using System.Globalization;
using NUnit.Framework;

using AmiKoWindows;


namespace AmiKoWindows.Tests
{
    [TestFixture]
    public class UIStateTest
    {
        [TearDown]
        public void TearDown()
        {
            // pass
        }

        [Test]
        public void Test_States()
        {
            UIState uiState;

            uiState = new UIState();
            Assert.IsTrue(uiState.IsCompendium); // default
            Assert.IsFalse(uiState.IsFavorites);
            Assert.IsFalse(uiState.IsInteractions);
            Assert.IsFalse(uiState.IsPrescriptions);

            uiState = new UIState();
            uiState.SetState(UIState.State.Favorites);
            Assert.IsFalse(uiState.IsCompendium);
            Assert.IsTrue(uiState.IsFavorites);

            uiState = new UIState();
            uiState.SetState(UIState.State.Interactions);
            Assert.IsFalse(uiState.IsCompendium);
            Assert.IsTrue(uiState.IsInteractions);

            uiState = new UIState();
            uiState.SetState(UIState.State.Prescriptions);
            Assert.IsFalse(uiState.IsCompendium);
            Assert.IsTrue(uiState.IsPrescriptions);
        }
    }
}
