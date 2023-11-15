using System.Collections;
using UnityEngine;

namespace WarperTravel
{
    public class WarperGateBehaviour : MonoBehaviour, IProtoEventListenerAsync
    {
        private static GameObject warperBall; 
        private static GameObject portalEffect; 
        private static Material warpedMaterial;
        private static bool things_loaded;

        private void Start() 
        {
            InstantiateFX();
            // Plugin.logSource.LogInfo($"warpgate started at: {transform.position}");
        }

        private void InstantiateFX()
        {
            var portal = Instantiate(portalEffect, transform);
            portal.transform.localPosition = Vector3.zero;
            Component.Destroy(portal.GetComponent<VFXDestroyAfterSeconds>());
            var particleSystems = portal.GetComponentsInChildren<ParticleSystem>();

            foreach (var system in particleSystems)
            {
                var particleMain = system.main;
                particleMain.loop = true;
                var sizeControl = system.sizeOverLifetime;

                particleMain.duration = 600;
                particleMain.startDelay = 0;
                particleMain.useUnscaledTime = false;
                // sizeControl.sizeMultiplier = 1.5f;

                particleMain.startLifetime = particleMain.duration;
            }

            var emissionModule = portal.GetComponent<ParticleSystem>().emission;
            emissionModule.burstCount = 0;
            emissionModule.rateOverTime = 10;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody.gameObject == Player.main.gameObject)
            {
                // only warp players. maybe also extend to creatures for ~fun~
                var ball = Instantiate(warperBall, other.transform.position, Quaternion.identity);
                var tag = ball.AddComponent<WarpBallSubspaceTag>();
                tag.destination = WarperSubspace.GetOUTLocation();
            }
        }

        public static IEnumerator LoadWarperGraphic()
        {
            if (things_loaded) yield break;

            var warperPrefabRequest = CraftData.GetPrefabForTechTypeAsync(TechType.Warper, false);
            yield return warperPrefabRequest;

            var warperObject = warperPrefabRequest.GetResult();
            warperBall = warperObject.GetComponent<RangedAttackLastTarget>().attackTypes[0].ammoPrefab;
            // var warperVFXPrefab = warperObject.GetComponent<Warper>().warpInEffectPrefab;
            portalEffect = warperObject.GetComponent<Warper>().warpInEffectPrefab;
            warpedMaterial = warperObject.GetComponent<Warper>().warpedMaterial;
            things_loaded = true;
        }

        public IEnumerator OnProtoDeserializeAsync(ProtobufSerializer serializer)
        {
            yield return LoadWarperGraphic();
        }
    }
}