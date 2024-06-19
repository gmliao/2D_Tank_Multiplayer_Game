using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public class Bullet : NetworkBehaviour, IPredictedSpawnBehaviour
    {
        [SerializeField] private NetworkRigidbody2D networkRigidbody = null;

        [SerializeField] private float speed = 20f;
        [SerializeField] private int damage = 10;

        [Networked] private TickTimer life { get; set; }

        private readonly List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

        private Vector3 interpolateFrom;
        private Vector3 interpolateTo;

        private Vector3 velocity;

        public override void Spawned()
        {
            life = TickTimer.CreateFromSeconds(Runner, 5.0f);

            networkRigidbody.InterpolationTarget.gameObject.SetActive(true);

            networkRigidbody.Rigidbody.velocity = networkRigidbody.Transform.TransformDirection(Vector2.up) * speed;
        }

        public override void FixedUpdateNetwork()
        {
            if (life.Expired(Runner))
            {
                Runner.Despawn(Object);
            }

            DetectCollision();
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {

            // Check if the bullet hit a wall
            if (collider.gameObject.CompareTag("Wall"))
            {
                // 取得入射方向
                Vector2 inDirection = networkRigidbody.Rigidbody.velocity.normalized;

                // 跟物件的X軸做反射
                Vector2 normal = collider.transform.right;
                Vector2 reflectDirection = Vector2.Reflect(inDirection, normal);

                // Rotate the bullet to the reflection direction
                float angle = Mathf.Atan2(reflectDirection.y, reflectDirection.x) * Mathf.Rad2Deg;
                networkRigidbody.Transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                // 改變速度方向
                networkRigidbody.Rigidbody.velocity = reflectDirection * networkRigidbody.Rigidbody.velocity.magnitude;
            }
        }


        private void DetectCollision()
        {
            hits.Clear();

            if (Object == null) return;

            Runner.LagCompensation.OverlapBox(
                networkRigidbody.Transform.position,
                new Vector3(.2f, 0f, .4f),
                Quaternion.Euler(0, 0, networkRigidbody.Rigidbody.rotation),
                Object.InputAuthority,
                hits,
                -1,
                HitOptions.IncludePhysX);

            foreach (var hit in hits)
            {
                var netObj = hit.GameObject.GetComponent<NetworkBehaviour>().Object;

                if (netObj == null) return;

                var damageable = hit.GameObject.GetComponent<IDamageable>();

                if (damageable != null && netObj.InputAuthority != Object.InputAuthority)
                {
                    damageable.TakeDamage(damage);

                    networkRigidbody.InterpolationTarget.gameObject.SetActive(false);

                    Runner.Despawn(Object, true);
                }
            }
        }

        public void PredictedSpawnSpawned()
        {
            interpolateTo = transform.position;
            interpolateFrom = interpolateTo;
            networkRigidbody.InterpolationTarget.position = interpolateTo;
        }

        public void PredictedSpawnUpdate()
        {
            interpolateFrom = interpolateTo;
            interpolateTo = transform.position;

            Vector3 pos = networkRigidbody.Transform.position;
            pos += networkRigidbody.Transform.TransformDirection(Vector2.up) * speed * Runner.DeltaTime;
            networkRigidbody.Transform.position = pos;
        }

        public void PredictedSpawnRender()
        {
            var a = Runner.Simulation.StateAlpha;
            networkRigidbody.InterpolationTarget.position = Vector3.Lerp(interpolateFrom, interpolateTo, a);
        }

        public void PredictedSpawnFailed()
        {
            Runner.Despawn(Object, true);
        }

        public void PredictedSpawnSuccess()
        {

        }
    }
}

