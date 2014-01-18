using System;
using System.Collections.Generic;
using UnityEngine;
public class BearChest : MonoBehaviour
{
	public float timeToOpen = 300f;
	public float level2Time = 240f;
	public float level3Time = 120f;
	public float armBearsTime = 90f;
	public GameObject[] level1Spawners;
	public GameObject[] level2Spawners;
	public GameObject[] level3Spawners;
	public float maximumDistance = 20f;
	private bool opening = false;
	private float previousMinimumCountdown;
	private Dictionary<Player, float> countdowns = new Dictionary<Player, float>();
	private Dictionary<Player, string> countdownText = new Dictionary<Player, string>();
	private void Start()
	{
		this.previousMinimumCountdown = this.timeToOpen;
	}
	private void Update()
	{
		if (!GameState.isServer)
		{
			return;
		}
		if (GameState.isSpectator)
		{
			return;
		}
		if (!this.opening)
		{
			return;
		}
		if (this.countdowns.Count == 0)
		{
			this.Stop();
			return;
		}
		float num = this.timeToOpen;
		GameObject[] array = GameObject.FindGameObjectsWithTag("Enemy");
		List<GameObject> list = new List<GameObject>();
		GameObject[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			GameObject gameObject = array2[i];
			if (!(gameObject.GetComponent<Enemy>() == null))
			{
				if (gameObject.GetComponent<Enemy>().enabled)
				{
					if (gameObject.GetComponent<Enemy>().health > 0)
					{
						list.Add(gameObject);
					}
				}
			}
		}
		int num2 = (list.Count + this.countdowns.Count - 1) / this.countdowns.Count;
		int num3 = 0;
		List<Player> list2 = new List<Player>(this.countdowns.Keys);
		foreach (Player current in list2)
		{
			string text = "";
			string desc = "Protect " + current.GetComponent<ReplicatedObject>().replicationName;
			if (this.previousMinimumCountdown < this.armBearsTime + 5f && this.previousMinimumCountdown > this.armBearsTime - 5f)
			{
				text = "We support the right to arm bears";
				desc = text;
			}
			if (current.id == -1)
			{
				this.countdowns.Remove(current);
				this.countdownText.Remove(current);
			}
			else
			{
				if ((current.transform.position - base.transform.position).magnitude > this.maximumDistance)
				{
					current.priorityCountdown = false;
					current.remoteCountdown = -1;
					current.SendUpdate(GameServerUpdate.CreateCountdownUpdate(-1, ""));
					current.SendUpdateToTeammates(GameServerUpdate.CreateCountdownUpdate(-1, ""));
					this.countdowns.Remove(current);
					this.countdownText.Remove(current);
				}
				else
				{
					bool flag = (current.transform.position - base.transform.position).magnitude > this.maximumDistance * 0.66f;
					if (flag)
					{
						text = "Poor signal, stay close to the chest";
					}
					if (flag)
					{
						Dictionary<Player, float> dictionary;
						Player key;
						(dictionary = this.countdowns)[key = current] = dictionary[key] - Time.deltaTime * 0.5f;
					}
					else
					{
						Dictionary<Player, float> dictionary;
						Player key2;
						(dictionary = this.countdowns)[key2 = current] = dictionary[key2] - Time.deltaTime;
					}
					if (current.health <= 0)
					{
						current.priorityCountdown = false;
						current.remoteCountdown = -1;
						current.SendUpdate(GameServerUpdate.CreateCountdownUpdate(-1, ""));
						current.SendUpdateToTeammates(GameServerUpdate.CreateCountdownUpdate(-1, ""));
						this.countdowns.Remove(current);
						this.countdownText.Remove(current);
					}
					else
					{
						int num4 = (int)this.countdowns[current];
						if (current.remoteCountdown != num4)
						{
							current.SendUpdate(GameServerUpdate.CreateCountdownUpdate(num4, text));
							current.SendUpdateToTeammates(GameServerUpdate.CreateCountdownUpdate(num4, desc));
							current.priorityCountdown = true;
							current.remoteCountdown = num4;
							this.countdownText[current] = text;
						}
						if (text != this.countdownText[current])
						{
							int seconds = (int)this.countdowns[current];
							current.SendUpdate(GameServerUpdate.CreateCountdownUpdate(seconds, text));
							current.SendUpdateToTeammates(GameServerUpdate.CreateCountdownUpdate(seconds, desc));
							this.countdownText[current] = text;
						}
						if (this.countdowns[current] < num)
						{
							num = this.countdowns[current];
						}
						if (this.countdowns[current] < 0f)
						{
							current.priorityCountdown = false;
							current.remoteCountdown = -1;
							current.SendUpdate(GameServerUpdate.CreateCountdownUpdate(-1, ""));
							current.SendUpdateToTeammates(GameServerUpdate.CreateCountdownUpdate(-1, ""));
							this.countdowns.Remove(current);
							this.countdownText.Remove(current);
							GameState.GetQuestManager().OnKillBoss(current, "BearChest");
						}
						else
						{
							for (int j = 0; j < num2; j++)
							{
								GameObject gameObject2 = list[num3 + j];
								Enemy component = gameObject2.GetComponent<Enemy>();
								if (component != null)
								{
									component.Alert(current.gameObject, base.transform.position);
								}
							}
							num3 += num2;
						}
					}
				}
			}
		}
		if (num < this.level2Time)
		{
			GameObject[] array3 = this.level2Spawners;
			for (int k = 0; k < array3.Length; k++)
			{
				GameObject gameObject3 = array3[k];
				gameObject3.SetActive(true);
			}
		}
		if (num < this.level3Time)
		{
			GameObject[] array4 = this.level3Spawners;
			for (int l = 0; l < array4.Length; l++)
			{
				GameObject gameObject4 = array4[l];
				gameObject4.SetActive(true);
			}
		}
		foreach (GameObject current2 in list)
		{
			if (!(current2.GetComponent<Bear>() == null))
			{
				current2.GetComponent<Bear>().SetArmed(num < this.armBearsTime);
			}
		}
		GameObject[] array5 = this.level1Spawners;
		for (int m = 0; m < array5.Length; m++)
		{
			GameObject gameObject5 = array5[m];
			gameObject5.GetComponent<Spawner>().maximumInstances = 3 * list2.Count;
		}
		GameObject[] array6 = this.level2Spawners;
		for (int n = 0; n < array6.Length; n++)
		{
			GameObject gameObject6 = array6[n];
			gameObject6.GetComponent<Spawner>().maximumInstances = 3 * list2.Count;
		}
		GameObject[] array7 = this.level3Spawners;
		for (int num5 = 0; num5 < array7.Length; num5++)
		{
			GameObject gameObject7 = array7[num5];
			gameObject7.GetComponent<Spawner>().maximumInstances = list2.Count;
		}
		this.previousMinimumCountdown = num;
	}
	public void Stop()
	{
		GameObject[] array = this.level1Spawners;
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i];
			gameObject.GetComponent<Spawner>().maximumInstances = 3;
		}
		GameObject[] array2 = this.level2Spawners;
		for (int j = 0; j < array2.Length; j++)
		{
			GameObject gameObject2 = array2[j];
			gameObject2.GetComponent<Spawner>().maximumInstances = 3;
			gameObject2.SetActive(false);
		}
		GameObject[] array3 = this.level3Spawners;
		for (int k = 0; k < array3.Length; k++)
		{
			GameObject gameObject3 = array3[k];
			gameObject3.GetComponent<Spawner>().maximumInstances = 1;
			gameObject3.SetActive(false);
		}
		GameObject[] array4 = GameObject.FindGameObjectsWithTag("Enemy");
		for (int l = 0; l < array4.Length; l++)
		{
			GameObject gameObject4 = array4[l];
			if (!(gameObject4.GetComponent<Bear>() == null))
			{
				gameObject4.GetComponent<Bear>().SetArmed(false);
			}
		}
		this.opening = false;
		this.previousMinimumCountdown = this.timeToOpen;
	}
	public void Interact(Player player)
	{
		if (!player.questState.HasQuest("BearQuest"))
		{
			return;
		}
		if (player.questState.GetQuestState("BearQuest") != "Open")
		{
			return;
		}
		if (!GameState.isServer)
		{
			GameState.gameServer.Interact("BearChest");
			return;
		}
		if (this.countdowns.ContainsKey(player))
		{
			return;
		}
		foreach (Player current in this.countdowns.Keys)
		{
			if (current.team == player.team)
			{
				return;
			}
		}
		this.opening = true;
		this.countdowns.Add(player, this.timeToOpen);
		this.countdownText.Add(player, "");
	}
}
