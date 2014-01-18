using System;
using System.Collections;
using UnityEngine;
public class ChestOfTheDead : MonoBehaviour
{
	private bool open = false;
	private int enemyCount;
	private void Start()
	{
		this.enemyCount = 0;
		GameObject[] array = GameObject.FindGameObjectsWithTag("Enemy");
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i];
			Enemy component = gameObject.GetComponent<Enemy>();
			if (component != null && component.health > 0)
			{
				this.enemyCount++;
			}
		}
		if (GameState.isServer)
		{
			GameObject[] array2 = GameObject.FindGameObjectsWithTag("Enemy");
			for (int j = 0; j < array2.Length; j++)
			{
				GameObject gameObject2 = array2[j];
				if (gameObject2.GetComponent<ReplicatedObject>() == null)
				{
					GameState.instance.AddServerReplicatedObject(gameObject2.GetComponent<Enemy>().enemyName, gameObject2);
				}
			}
			GameState.masterServer.GetTeamState("Undead", delegate(bool set, int val)
			{
				if (set)
				{
					GameObject[] array4 = GameObject.FindGameObjectsWithTag("Enemy");
					for (int l = 0; l < array4.Length; l++)
					{
						GameObject gameObject4 = array4[l];
						if (this.enemyCount <= val)
						{
							break;
						}
						Enemy component2 = gameObject4.GetComponent<Enemy>();
						if (component2 != null && component2.health > 0)
						{
							UnityEngine.Object.Destroy(component2);
							this.enemyCount--;
						}
					}
				}
			});
		}
		else
		{
			IEnumerator enumerator = base.GetComponent<Animation>().GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					AnimationState animationState = (AnimationState)enumerator.Current;
					animationState.speed = 0.15f;
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = (enumerator as IDisposable)) != null)
				{
					disposable.Dispose();
				}
			}
			GameObject[] array3 = GameObject.FindGameObjectsWithTag("Enemy");
			for (int k = 0; k < array3.Length; k++)
			{
				GameObject gameObject3 = array3[k];
				if (gameObject3.GetComponent<ReplicatedObject>() == null)
				{
					UnityEngine.Object.Destroy(gameObject3);
				}
			}
		}
	}
	private void Update()
	{
		if (this.open)
		{
			return;
		}
		if (GameState.isSpectator)
		{
			return;
		}
		if (!GameState.isServer && !GameState.gameServer.loadComplete)
		{
			return;
		}
		int num = 0;
		GameObject[] array = GameObject.FindGameObjectsWithTag("Enemy");
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i];
			Enemy component = gameObject.GetComponent<Enemy>();
			if (component != null && component.health > 0)
			{
				num++;
			}
		}
		if (num != this.enemyCount)
		{
			if (GameState.isServer)
			{
				GameState.masterServer.SetTeamState("Undead", num);
			}
			this.enemyCount = num;
		}
		if (num == 0)
		{
			if (!GameState.isServer)
			{
				base.GetComponent<Animation>().Play("open");
			}
			this.open = true;
			if (GameState.isServer)
			{
				base.GetComponent<LootableObject>().enabled = true;
			}
			else
			{
				GameState.gameServer.IsNamedObjectLootAvailable(base.GetComponent<ChestLoot>().lootName, delegate(bool ok)
				{
					if (ok)
					{
						base.GetComponent<LootableObject>().enabled = true;
					}
				});
			}
		}
	}
}
