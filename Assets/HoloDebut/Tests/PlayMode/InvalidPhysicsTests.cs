using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HoloDebut.Domain;
using HoloDebut.Runtime;

namespace HoloDebut.Tests
{
    public class InvalidPhysicsTests
    {
        [UnityTest] public IEnumerator FiveOutOfBoundsAttemptsPauseAndRetrySameRollWithoutSpendingAgain()
        {
            yield return SceneManager.LoadSceneAsync("Prototype");yield return null;
            var game=Object.FindFirstObjectByType<GameSessionController>();Assert.That(game.FatalError,Is.Null);
            game.RequestUseItem();int items=game.Model.Inventory.Count(game.Model.CurrentPlayer);
            game.RequestRoll();var ticket=game.Model.ActiveRoll;float deadline=Time.realtimeSinceStartup+20;
            // Explicit test-only physical fault injection: put a flying die outside the tray.
            // This never injects a face and does not exist in production roll code.
            while(!game.Dice.ErrorPaused&&Time.realtimeSinceStartup<deadline)
            {
                var d=game.Dice.Dice[0];if(d.gameObject.activeSelf&&d.Body&&!d.Body.isKinematic)d.Body.position=new Vector3(32,0.3f,0);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(game.Dice.ErrorPaused,Is.True);Assert.That(game.Dice.History.Count,Is.EqualTo(5));
            foreach(var h in game.Dice.History){Assert.That(h.Valid,Is.False);StringAssert.Contains("bounds",h.Reason);}
            Assert.That(game.Model.ActiveRoll,Is.EqualTo(ticket));Assert.That(game.Model.CurrentPlayer.CurrentNodeId,Is.Zero);
            Assert.That(game.Model.CurrentPlayer.Subscribers,Is.Zero);Assert.That(game.Model.PendingMoveBonus,Is.EqualTo(5));Assert.That(game.Model.Inventory.Count(game.Model.CurrentPlayer),Is.EqualTo(items));
            game.RequestRetry();Assert.That(game.Dice.ErrorPaused,Is.False);Assert.That(game.Model.ActiveRoll,Is.EqualTo(ticket));
            Assert.That(game.Model.PendingMoveBonus,Is.EqualTo(5));game.RestartSession();yield return null;
            Assert.That(game.Model.Phase,Is.EqualTo(SessionPhase.AwaitRoll));
        }
    }
}
