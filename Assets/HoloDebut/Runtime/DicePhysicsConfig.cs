using UnityEngine;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Dice Physics")]
    public sealed class DicePhysicsConfig : ScriptableObject
    {
        public float size = 0.035f, mass = 0.02f;
        public float linearDamping = 0.05f, angularDamping = 0.10f, maxAngularVelocity = 35;
        public int solverIterations = 12, solverVelocityIterations = 4;
        public float contactOffset = 0.001f;
        public float linearStop = 0.01f, angularStop = 0.10f, stopSeconds = 0.50f;
        public float timeoutSeconds = 12, minUpDot = 0.94f, minDotGap = 0.35f;
        public float flightRadius = 0.50f, flightMinY = -0.20f, interiorRadius = 0.19f;
        public float invalidMessageSeconds = 0.50f;
        public int maxInvalidAttempts = 5;
        public float diceStaticFriction = 0.50f, diceDynamicFriction = 0.40f, diceBounce = 0.25f;
        public float bowlStaticFriction = 0.60f, bowlDynamicFriction = 0.45f, bowlBounce = 0.15f;
        public Vector2 horizontalSpeed = new Vector2(0.15f, 0.35f), downwardSpeed = new Vector2(0.20f, 0.50f), spinSpeed = new Vector2(10, 25);
        public float lateralJitter = 0.08f, spawnJitter = 0.005f;
        public Vector3[] spawnPoints = { new Vector3(-0.055f,0.26f,-0.032f), new Vector3(0.055f,0.26f,-0.032f), new Vector3(0,0.26f,0.064f) };
    }
}
