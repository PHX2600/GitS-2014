using System;
using UnityEngine;
public abstract class LocalPlayerEvents : MonoBehaviour
{
	public static LocalPlayerEvents instance;
	private bool remoteUpdateInProgress = false;
	public static Player localPlayer
	{
		get
		{
			if (LocalPlayerEvents.instance == null)
			{
				return null;
			}
			return LocalPlayerEvents.instance.GetComponent<Player>();
		}
	}
	public abstract void OnPlayMusic(string name);
	public abstract void OnPlaySound(string name, Transform position);
	public abstract void OnQuestText(NPC npc, NPCQuestText text);
	public abstract bool OnSetWeaponBySlot(int slot);
	public abstract void OnUseAccessory(string icon, string details, int intensity, float timeLeft);
	public abstract void OnUpdateHealth(int health);
	public abstract void OnRespawn();
	public abstract void OnUpdateCountdown(int seconds, string desc);
	public abstract void OnUpdatePlayerMapIcon(Player player, string name);
	public abstract void OnShowMuzzleFlash(GameObject flash);
	public abstract void OnChatMessage(string text);
	public static void PlayMusic(string name)
	{
		LocalPlayerEvents.instance.OnPlayMusic(name);
	}
	public static void PlaySound(string name, Transform position)
	{
		LocalPlayerEvents.instance.OnPlaySound(name, position);
	}
	public static void UpdateQuestText(NPC npc, NPCQuestText text)
	{
		LocalPlayerEvents.instance.OnQuestText(npc, text);
	}
	public static void UpdatePlayerMapIcon(Player player, string name)
	{
		LocalPlayerEvents.instance.OnUpdatePlayerMapIcon(player, name);
	}
	public static void ShowMuzzleFlash(GameObject flash)
	{
		LocalPlayerEvents.instance.OnShowMuzzleFlash(flash);
	}
	public static void ChatMessage(string text)
	{
		LocalPlayerEvents.instance.OnChatMessage(text);
	}
	public static void SetWeaponBySlot(int slot)
	{
		if (LocalPlayerEvents.instance.OnSetWeaponBySlot(slot) && !GameState.isServer)
		{
			GameState.gameServer.SwitchWeapon(slot);
		}
	}
	public static void UseAccessory()
	{
		if (LocalPlayerEvents.localPlayer.inventory.accessoryName == "Wine")
		{
			int num = UnityEngine.Random.Range(15, 21);
			LocalPlayerEvents.localPlayer.DrinkWine(num);
			LocalPlayerEvents.instance.OnUseAccessory("Wine", "-" + num.ToString() + "% damage", num, 60f);
		}
	}
	public static void UpdateHealth(int health)
	{
		LocalPlayerEvents.instance.OnUpdateHealth(health);
		LocalPlayerEvents.localPlayer.SetHealth(health);
	}
	public static void UpdateCountdown(int seconds, string desc)
	{
		LocalPlayerEvents.instance.OnUpdateCountdown(seconds, desc);
	}
	public static void PrepareAttack()
	{
		if (GameState.isServer)
		{
			return;
		}
		GameState.gameServer.PrepareAttack();
	}
	public static void Attack(Ray[] rays, GameObject[] hits)
	{
		if (GameState.isServer)
		{
			return;
		}
		if (rays.Length != hits.Length)
		{
			return;
		}
		int[] array = new int[hits.Length];
		for (int i = 0; i < hits.Length; i++)
		{
			GameObject gameObject = hits[i];
			ReplicatedObject replicatedObject = null;
			while (gameObject != null)
			{
				replicatedObject = gameObject.GetComponent<ReplicatedObject>();
				if (replicatedObject != null)
				{
					break;
				}
				if (gameObject.transform.parent == null)
				{
					break;
				}
				gameObject = gameObject.transform.parent.gameObject;
			}
			if (replicatedObject == null)
			{
				array[i] = -1;
			}
			else
			{
				array[i] = replicatedObject.id;
			}
		}
		GameState.gameServer.Attack(rays, array);
	}
	public static void RequestRespawn()
	{
		if (GameState.isServer)
		{
			return;
		}
		GameState.gameServer.Respawn(delegate(bool ok, Vector3 pos, Quaternion rot, int health)
		{
			if (ok)
			{
				LocalPlayerEvents.localPlayer.RespawnAt(pos, rot);
				LocalPlayerEvents.localPlayer.SetHealth(health);
				LocalPlayerEvents.instance.OnRespawn();
			}
		});
	}
	protected void RemoteUpdate()
	{
		if (this.remoteUpdateInProgress)
		{
			return;
		}
		this.remoteUpdateInProgress = true;
		if (GameState.isSpectator)
		{
			GameState.gameServer.SpectatorUpdate(delegate
			{
				this.remoteUpdateInProgress = false;
			});
		}
		else
		{
			GameState.gameServer.Update(LocalPlayerEvents.localPlayer, delegate
			{
				this.remoteUpdateInProgress = false;
			});
		}
	}
}
