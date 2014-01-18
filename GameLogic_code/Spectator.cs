using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
public class Spectator : ClientObject
{
	public ClientHandler clientHandler = null;
	private List<GameServerUpdate> updates = new List<GameServerUpdate>();
	public static Spectator SpawnOnServer()
	{
		GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("RemoteSpectator", typeof(GameObject)));
		Spectator component = gameObject.GetComponent<Spectator>();
		component.tag = "Spectator";
		GameState.instance.SendSpawnUpdatesForAllReplicatedObjects(component);
		component.SendUpdate(GameServerUpdate.CreateLoadCompleteUpdate());
		if (GameState.isArena)
		{
			component.SendUpdate(GameServerUpdate.CreateArenaTeamScoreUpdate(0, ArenaState.instance.GetTeamScore(0)));
			component.SendUpdate(GameServerUpdate.CreateArenaTeamScoreUpdate(1, ArenaState.instance.GetTeamScore(1)));
		}
		return component;
	}
	public override void SendUpdate(GameServerUpdate update)
	{
		if (GameState.isServer)
		{
			object obj = this.updates;
			Monitor.Enter(obj);
			try
			{
				this.updates.Add(update);
			}
			finally
			{
				Monitor.Exit(obj);
			}
		}
	}
	public static void SendUpdateToAllSpectators(GameServerUpdate update)
	{
		if (GameState.isServer)
		{
			GameObject[] array = GameObject.FindGameObjectsWithTag("Spectator");
			for (int i = 0; i < array.Length; i++)
			{
				GameObject gameObject = array[i];
				gameObject.GetComponent<Spectator>().SendUpdate(update);
			}
		}
	}
	public void AddUpdatesToMessage(GameServerMessage msg)
	{
		object obj = this.updates;
		Monitor.Enter(obj);
		try
		{
			foreach (GameServerUpdate current in this.updates)
			{
				msg.WriteUpdate(current);
			}
			this.updates.Clear();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public static void CheckConnectionStatus()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("Spectator");
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i];
			Spectator component = gameObject.GetComponent<Spectator>();
			if (component.clientHandler != null)
			{
				component.clientHandler.CheckConnectionStatus();
			}
		}
	}
	public void RemoveFromServer()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}
	public void ForceDisconnectFromServer()
	{
		if (this.clientHandler != null)
		{
			this.clientHandler.ForceDisconnect();
		}
	}
	public static void DisconnectAllSpectators()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("Spectator");
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i];
			Spectator component = gameObject.GetComponent<Spectator>();
			component.ForceDisconnectFromServer();
		}
	}
}
