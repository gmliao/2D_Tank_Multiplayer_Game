using Fusion;
using System.Collections;
using UnityEngine;
using TMPro;

namespace Game.Core
{
    public class MobController : NetworkBehaviour
    {
        [SerializeField] private HealthPoint healthPoint = null;

        [SerializeField] private NetworkRigidbody2D playerNetworkRigidbody = null;

        [SerializeField] private MobViewHandler viewHandler = null;

        [Networked] private NetworkButtons buttonsPrevious { get; set; }

        [Networked] private TickTimer respawnTimer { get; set; }

        public override void Spawned()
        {
            healthPoint.Subscribe(OnHPChanged);

            if (Object.HasInputAuthority)
            {
                playerNetworkRigidbody.InterpolationDataSource = InterpolationDataSources.Predicted;
            }
            else
            {
                playerNetworkRigidbody.InterpolationDataSource = InterpolationDataSources.Snapshots;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            healthPoint.Unsubscribe(OnHPChanged);
        }

        private void OnHPChanged(float value)
        {
            viewHandler.SetHPBarValue((float)value / healthPoint.MaxHP);
        }

        public override void FixedUpdateNetwork()
        {
            if (healthPoint.HP <= 0 && !respawnTimer.IsRunning)
            {
                Die();
            }

            if (respawnTimer.Expired(Runner))
            {
                Respawn();
                respawnTimer = TickTimer.None;
            }
        }

        private void Die()
        {
            if (Object.HasStateAuthority)
            {
                respawnTimer = TickTimer.CreateFromSeconds(Runner, 2f);

                playerNetworkRigidbody.TeleportToPosition(new Vector2(-100, -100));

            }
        }

        private void Respawn()
        {
            if (Object.HasStateAuthority)
            {
                healthPoint.HP = healthPoint.MaxHP;

                var spawnManager = FindObjectOfType<SpawnManager>();

                var spawnPoint = spawnManager.GetRandomSpawnPoint();

                playerNetworkRigidbody.TeleportToPosition(spawnPoint.position);
            }
        }
    }
}