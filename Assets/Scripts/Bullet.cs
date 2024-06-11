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

        private List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

        private Vector3 interpolateFrom;
        private Vector3 interpolateTo;

        private Vector3 velocity;

        public override void Spawned()
        {
            life = TickTimer.CreateFromSeconds(Runner, 5.0f);

            networkRigidbody.InterpolationTarget.gameObject.SetActive(true);

            //networkRigidbody.Rigidbody.velocity = Vector2.zero;
            networkRigidbody.Rigidbody.velocity = networkRigidbody.Transform.TransformDirection(Vector2.up) * speed;
        }

        public override void FixedUpdateNetwork()
        {
            if (life.Expired(Runner))
            {
                Runner.Despawn(Object);
            }

            // DetectCollisionToWall();
            DetectCollision();
        }

        // private void OnCollisionEnter2D(Collision2D collision)
        // {
        //     Debug.Log("Bullet collided with: " + collision.gameObject.name);
        //     if (collision.gameObject.CompareTag("Wall"))
        //     {
        //         Vector2 inDirection = networkRigidbody.Rigidbody.velocity.normalized;
        //         Vector2 normal = collision.contacts[0].normal;  // 使用碰撞的第一個接觸點的法線
        //         Vector2 reflectDirection = Vector2.Reflect(inDirection, normal);

        //         //Debug.Log(other.gameObject.name + "reflectDirection: " + reflectDirection + " other.transform.right: " + other.transform.right);

        //         // 把子彈的方向轉換成反射方向
        //         networkRigidbody.Rigidbody.velocity = reflectDirection * networkRigidbody.Rigidbody.velocity.magnitude;
        //         // 物體面向速度向量的方向
        //         float angle = Mathf.Atan2(reflectDirection.y, reflectDirection.x) * Mathf.Rad2Deg;
        //         networkRigidbody.Transform.rotation = Quaternion.Euler(0, angle, 0);

        //     }
        // }

        private void OnTriggerEnter2D(Collider2D collider)
        {

            // Check if the bullet hit a wall
            if (collider.gameObject.CompareTag("Wall"))
            {
                Debug.Log("Something entered the trigger zone: " + collider.name);
                // Calculate the reflection vector
                Vector2 inDirection = networkRigidbody.Rigidbody.velocity.normalized;

                Vector2 normal = collider.transform.right;
                // 跟物件的X軸做反射
                Vector2 reflectDirection = Vector2.Reflect(inDirection, normal);

                Debug.Log(collider.gameObject.name + " inDirection:" + inDirection + " reflectDirection:" + reflectDirection + " normal:" + normal);

                // Rotate the bullet to the reflection direction
                float angle = Mathf.Atan2(reflectDirection.y, reflectDirection.x) * Mathf.Rad2Deg;
                networkRigidbody.Transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                // Set the bullet's velocity to the reflection direction
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

