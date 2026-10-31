using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HoloDebut.Runtime;
using HoloDebut.Domain;

namespace HoloDebut.Tests
{
    public class PrototypePlayTests
    {
        private GameSessionController game;
        [UnitySetUp] public IEnumerator LoadPrototype()
        {
            yield return SceneManager.LoadSceneAsync("Prototype");yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameSessionController>();
            Assert.That(game,Is.Not.Null);Assert.That(game.FatalError,Is.Null);Assert.That(game.Model.Players.Count,Is.EqualTo(4));
        }
        [UnityTearDown] public IEnumerator ResetSpeed(){if(game&&game.Dice)game.Dice.Cancel();Time.timeScale=1;yield return null;}
        [UnityTest] public IEnumerator SceneStartsAndRestartDoesNotDuplicateDiceOrBoard()
        {
            Assert.That(UnityEngine.Object.FindObjectsByType<TileVisual>(FindObjectsSortMode.None).Length,Is.EqualTo(121));
            string old=game.Model.SessionId;game.RestartSession();yield return null;
            Assert.That(game.Model.SessionId,Is.Not.EqualTo(old));Assert.That(game.FatalError,Is.Null);
            Assert.That(game.Dice.Dice.Count,Is.EqualTo(3));Assert.That(UnityEngine.Object.FindObjectsByType<TileVisual>(FindObjectsSortMode.None).Length,Is.EqualTo(121));
        }
        [UnityTest] public IEnumerator JapaneseFontAndSceneLabelsUseBundledJapaneseGlyphs()
        {
            var font=JapaneseFont.Load();
            foreach(char c in "ホロデビュー兎田ぺこら宝鐘マリン不知火フレア白銀ノエル投球配信脱出資金登録者")
                Assert.That(font.HasCharacter(c),Is.True,"Missing Japanese glyph: "+c);
            Assert.That(game.Model.Players[0].DisplayName,Does.Contain("兎田ぺこら"));
            var labels=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None);
            Assert.That(System.Array.Exists(labels,t=>t.text=="ホロデビュー"),Is.True);
            Assert.That(System.Array.Exists(labels,t=>t.text.Contains("サイコロを振る")),Is.True);
            foreach(var t in labels)Assert.That(t.font,Is.SameAs(font));
            yield return null;
        }
        [UnityTest] public IEnumerator RollSpamIsRejectedAndCancelRejectsOldPhysics()
        {
            string old=game.Model.SessionId;game.RequestRoll();var ticket=game.Model.ActiveRoll;
            game.RequestRoll();game.RequestRoll();Assert.That(game.Model.ActiveRoll,Is.EqualTo(ticket));
            game.RestartSession();yield return null;Assert.That(game.Model.SessionId,Is.Not.EqualTo(old));Assert.That(game.Model.Phase,Is.EqualTo(SessionPhase.AwaitRoll));
            Assert.That(game.Model.TryAcceptResult(ticket,new[]{1,1}),Is.False);
        }
        [UnityTest] public IEnumerator PlanningToEnduranceAndBackPreservesNormalPawnAndEndsTurnOnce()
        {
            // Test-only rule input reaches a planning tile; this does not validate physical dice.
            var model=game.Model;var player=model.CurrentPlayer;
            Assert.That(model.TryBeginRoll(out var normal),Is.True);
            Assert.That(model.TryAcceptResult(normal,new[]{5,5}),Is.True);
            while(model.RemainingSteps>0)model.TryCompleteStep(normal,model.StepIndex+1);
            model.TryResolveLanding(normal);player.SetSkill(SkillKind.Stamina,0);
            game.RequestEndurance();string challenge=game.Endurance.Session.ChallengeId;
            game.RequestEndurance();Assert.That(game.Endurance.Session.ChallengeId,Is.EqualTo(challenge));
            Assert.That(model.Phase,Is.EqualTo(SessionPhase.RunningSubgame));
            Assert.That(game.transform.Find("BoardRoot").gameObject.activeSelf,Is.False);
            Assert.That(game.transform.Find("PlayersRoot").gameObject.activeSelf,Is.False);
            Assert.That(player.CurrentNodeId,Is.EqualTo(10));Assert.That(model.Turns.CompletedTurns,Is.Zero);
            var endurance=game.Endurance.Session;endurance.TryBeginRoll(out var roll);endurance.TryAcceptResult(roll,1);
            while(endurance.RemainingSteps>0)endurance.TryCompleteStep(roll,endurance.StepIndex+1);
            endurance.TryResolveLanding(roll);game.RequestFinishEndurance();game.RequestFinishEndurance();
            Assert.That(model.Phase,Is.EqualTo(SessionPhase.AwaitRoll));Assert.That(model.Turns.CompletedTurns,Is.EqualTo(1));
            Assert.That(player.CurrentNodeId,Is.EqualTo(10));
            Assert.That(game.transform.Find("BoardRoot").gameObject.activeSelf,Is.True);
            Assert.That(game.transform.Find("PlayersRoot").gameObject.activeSelf,Is.True);
            Assert.That(game.transform.Find("EnduranceBoardRoot").gameObject.activeSelf,Is.False);
            Assert.That(endurance.TryAcceptResult(roll,6),Is.False);
            yield return null;
        }
        [Test] public void UpperFacesFollowPhysicalRotationAndRejectTilt()
        {
            for(int face=1;face<=6;face++)
            {
                var rotation=Quaternion.FromToRotation(DiceFaceDetector.Normals[face-1],Vector3.up);
                Assert.That(DiceFaceDetector.TryRead(rotation,0.94f,0.35f,out int found,out _,out _),Is.True);Assert.That(found,Is.EqualTo(face));
            }
            Assert.That(DiceFaceDetector.TryRead(Quaternion.Euler(45,0,0),0.94f,0.35f,out _,out _,out _),Is.False);
        }
        [UnityTest] public IEnumerator FixedTimestepRemainsUnchangedAtDoubleSpeed()
        {
            float dt=Time.fixedDeltaTime;game.Speed.PointerHeld=true;yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(Time.timeScale,Is.EqualTo(2));Assert.That(Time.fixedDeltaTime,Is.EqualTo(dt));
            game.Speed.PointerHeld=false;yield return null;Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator EnduranceBoardHasExactNodeCountAndSeparatePawn()
        {
            var root=new GameObject("test_board");var board=root.AddComponent<EnduranceBoardGenerator>();
            foreach(int n in new[]{2,3,4,5,6,8,10,11,12})
            {
                board.Generate(n,game.config.endurance,game.config.tilePrefab);Assert.That(board.Nodes.Length,Is.EqualTo(n));
                Assert.That(board.Nodes[0].Kind,Is.EqualTo(RewardKind.Subscribers));
                foreach(Transform child in root.transform)UnityEngine.Object.Destroy(child.gameObject);yield return null;
            }
            UnityEngine.Object.Destroy(root);
        }
        [UnityTest] public IEnumerator PhysicsTwoDiceAt1x()=>PhysicalBatch(2,1);
        [UnityTest] public IEnumerator PhysicsThreeDiceAt1x()=>PhysicalBatch(3,1);
        [UnityTest] public IEnumerator PhysicsTwoDiceAt2x()=>PhysicalBatch(2,2);
        [UnityTest] public IEnumerator PhysicsThreeDiceAt2x()=>PhysicalBatch(3,2);
        [Serializable] private sealed class Attempt
        {public int roll,attempt;public bool valid;public float seconds;public string reason;public int[] faces;}
        [Serializable] private sealed class Report
        {public int diceCount,speed,validRolls;public List<Attempt> attempts=new List<Attempt>();}
        private IEnumerator PhysicalBatch(int count,int speed)
        {
            // Disable input speed updates only; physics timestep remains 1/120. Never inject faces.
            game.Speed.enabled=false;Time.timeScale=speed;Time.fixedDeltaTime=1f/120;
            var report=new Report{diceCount=count,speed=speed};
            try
            {
                for(int i=0;i<20;i++)
                {
                    int[] result=null;bool active=true;
                    var coroutine=game.Dice.StartCoroutine(game.Dice.Roll(new RollTicket("physical-matrix",0,i+1,count),()=>active,f=>result=f));
                    float deadline=Time.realtimeSinceStartup+90f/speed;
                    while(result==null&&!game.Dice.ErrorPaused&&Time.realtimeSinceStartup<deadline)yield return null;
                    active=false;game.Dice.StopCoroutine(coroutine);
                    if(result!=null){report.validRolls++;foreach(int face in result)Assert.That(face,Is.InRange(1,6));}
                    Assert.That(result,Is.Not.Null,"Physical batch failed: "+game.Dice.Status);
                }
            }
            finally
            {
                foreach(var h in game.Dice.History)report.attempts.Add(new Attempt{roll=(int)h.Ticket.RollId,attempt=h.AttemptIndex,valid=h.Valid,seconds=h.Seconds,reason=h.Reason,faces=h.Faces});
                Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/physics_"+count+"dice_"+speed+"x.json",JsonUtility.ToJson(report,true));
                game.Dice.Cancel();Time.timeScale=1;game.Speed.enabled=true;
            }
            Assert.That(report.validRolls,Is.EqualTo(20));
        }
    }
}
