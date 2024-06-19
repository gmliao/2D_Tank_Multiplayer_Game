using System.Collections;
using UnityEngine;
using Fusion;

namespace Game.Core
{
	public class MobNetworkData : NetworkBehaviour
	{
		private GameManager gameManager = null;

		[Networked(OnChanged = nameof(OnPlayerNameChanged))] public string PlayerName { get; set; }
		[Networked(OnChanged = nameof(OnIsReadyChanged))] public NetworkBool IsReady { get; set; }

		public override void Spawned()
		{
			gameManager = GameManager.Instance;

			transform.SetParent(GameManager.Instance.transform);

		}

		#region - RPCs -

		[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
		public void SetPlayerName_RPC(string name)
		{
			PlayerName = name;
		}

		[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
		public void SetReady_RPC(bool isReady)
		{
			IsReady = isReady;
		}

		#endregion

		#region - OnChanged Events -
		private static void OnPlayerNameChanged(Changed<MobNetworkData> changed)
		{
			GameManager.Instance.UpdatePlayerList();
		}

		private static void OnIsReadyChanged(Changed<MobNetworkData> changed)
		{
			GameManager.Instance.UpdatePlayerList();
		}
		#endregion
	}
}