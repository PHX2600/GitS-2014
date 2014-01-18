using System;
using System.Collections.Generic;
using UnityEngine;
public class Enemy : MonoBehaviour
{
	public string enemyName = "Beast from the Unknown";
	public string killText = "% was eaten by a grue.";
	public int rarity = 0;
	public int startingHealth;
	public float fieldOfView = 160f;
	public float attackDistance = 60f;
	public float maxCorpseAge = 120f;
	public float meleeRange = 2f;
	public GameObject mapIcon;
	public Loot[] loot;
	public GameObject lootCollectionPrefab;
	public GameObject lootEffect;
	protected byte animRandom = 0;
	protected Dictionary<string, bool> animStates = new Dictionary<string, bool>();
	protected NavMeshAgent agent;
	private GameObject _target;
	public int health;
	private float corpseTimeLeft;
	protected GameObject target
	{
		get
		{
			return this._target;
		}
		set
		{
			if (value == this._target)
			{
				return;
			}
			if (this._target != null)
			{
				this._target.SendMessage("OnEnemyLost", SendMessageOptions.DontRequireReceiver);
			}
			this._target = value;
			if (this._target != null)
			{
				this._target.SendMessage("OnEnemyTargeting", SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	protected float distanceToTarget
	{
		get
		{
			if (this.target == null)
			{
				return float.PositiveInfinity;
			}
			return (this.target.transform.position - base.transform.position).magnitude;
		}
	}
	private void Awake()
	{
		this.agent = base.GetComponent<NavMeshAgent>();
		this.health = this.startingHealth;
		if (this.mapIcon != null)
		{
			this.mapIcon.SendMessage("SetToolTipText", this.enemyName);
		}
	}
	private void Update()
	{
		if (!GameState.isServer)
		{
			this.UpdateModel();
			if (this.health <= 0 && this.mapIcon != null)
			{
				UnityEngine.Object.Destroy(this.mapIcon);
			}
			return;
		}
		if (this.target != null && !this.target.activeInHierarchy)
		{
			this.target = null;
		}
		if (this.health <= 0)
		{
			LootableObject component = base.GetComponent<LootableObject>();
			if (component == null || !component.enabled)
			{
				this.corpseTimeLeft -= Time.deltaTime;
			}
			else
			{
				this.corpseTimeLeft -= Time.deltaTime * 0.25f;
			}
			if (this.corpseTimeLeft <= 0f)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
		else
		{
			this.UpdateAI();
		}
	}
	private void OnDisable()
	{
		GameState.instance.RemoveReplicatedObject(base.gameObject);
		if (!GameState.isServer)
		{
			LocalPlayerEvents.localPlayer.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
		}
	}
	protected bool DidHitObject(RaycastHit hit, GameObject obj)
	{
		return !(hit.collider == null) && (hit.collider.gameObject == obj || (!(hit.collider.transform.parent == null) && hit.collider.transform.parent.gameObject == obj));
	}
	protected GameObject LookForPlayerTarget()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("Player");
		List<GameObject> list = new List<GameObject>();
		GameObject[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			GameObject gameObject = array2[i];
			if (gameObject.GetComponent<Player>().health > 0)
			{
				Vector3 from = gameObject.transform.position - base.transform.position;
				float num = Vector3.Angle(from, base.transform.forward);
				if (num < this.fieldOfView * 0.5f)
				{
					RaycastHit hit;
					if (Physics.Raycast(base.transform.position + base.transform.up, from.normalized, out hit, this.attackDistance) && this.DidHitObject(hit, gameObject))
					{
						list.Add(gameObject);
					}
				}
				else
				{
					RaycastHit hit;
					if (Physics.Raycast(base.transform.position + base.transform.up, from.normalized, out hit, 5f) && this.DidHitObject(hit, gameObject))
					{
						list.Add(gameObject);
					}
				}
			}
		}
		if (list.Count > 0)
		{
			return list[UnityEngine.Random.Range(0, list.Count)];
		}
		return null;
	}
	protected bool IsTargetStillVisible()
	{
		if (this.target == null)
		{
			return false;
		}
		if (this.target.GetComponent<Player>() != null && this.target.GetComponent<Player>().health <= 0)
		{
			return false;
		}
		if (this.target.GetComponent<Enemy>() != null && this.target.GetComponent<Enemy>().health <= 0)
		{
			return false;
		}
		Vector3 from = this.target.transform.position - base.transform.position;
		float num = Vector3.Angle(from, base.transform.forward);
		RaycastHit hit;
		return (num < this.fieldOfView * 0.5f && Physics.Raycast(base.transform.position + base.transform.up, from.normalized, out hit) && this.DidHitObject(hit, this.target)) || (Physics.Raycast(base.transform.position + base.transform.up, from.normalized, out hit, 5f) && this.DidHitObject(hit, this.target));
	}
	protected void RotateToTarget()
	{
		if (this.target == null)
		{
			return;
		}
		Vector3 forward = base.transform.forward;
		Vector3 vector = this.target.transform.position - base.transform.position;
		if (vector.magnitude < 1.401298E-45f)
		{
			return;
		}
		float num = Vector3.Angle(forward, vector);
		if (num < 5f)
		{
			return;
		}
		Vector3 lhs = Vector3.Cross(forward, vector);
		num *= Mathf.Sign(Vector3.Dot(lhs, base.transform.up));
		num *= 0.05f;
		base.transform.rotation = base.transform.rotation * Quaternion.AngleAxis(num, base.transform.up);
	}
	protected void TryToMeleeTarget(float damage)
	{
		if (this.target == null)
		{
			return;
		}
		if (this.distanceToTarget > this.meleeRange * 1.5f)
		{
			return;
		}
		this.target.SendMessageUpwards("Damage", new Damage(base.gameObject, null, damage), SendMessageOptions.DontRequireReceiver);
	}
	public void Damage(Damage dmg)
	{
		if (this.health <= 0)
		{
			return;
		}
		this.health -= (int)dmg.amount;
		if (base.GetComponent<ReplicatedObject>() != null)
		{
			Player.SendUpdateToAllPlayers(GameServerUpdate.CreateObjectHealthUpdate(base.GetComponent<ReplicatedObject>(), this.health));
		}
		if (this.health <= 0)
		{
			this.Die(dmg.source);
		}
	}
	public void SetHealth(int val)
	{
		this.health = val;
	}
	public virtual void Die(GameObject killer)
	{
		if (GameState.isServer)
		{
			Player.SendUpdateToAllPlayers(GameServerUpdate.CreateObjectDeathUpdate(base.GetComponent<ReplicatedObject>(), (!(killer != null)) ? null : killer.GetComponent<ReplicatedObject>()));
			Player player = (!(killer != null)) ? null : killer.GetComponent<Player>();
			if (player != null)
			{
				LootableObject component = base.GetComponent<LootableObject>();
				if (component != null && this.lootCollectionPrefab != null)
				{
					Dictionary<string, int> dictionary = new Dictionary<string, int>();
					Loot[] array = this.loot;
					for (int i = 0; i < array.Length; i++)
					{
						Loot loot = array[i];
						if (UnityEngine.Random.value <= loot.chance)
						{
							int num = UnityEngine.Random.Range(loot.minimumCount, loot.maximumCount + 1);
							if (num > 0)
							{
								if (dictionary.ContainsKey(loot.itemName))
								{
									Dictionary<string, int> dictionary2;
									string itemName;
									(dictionary2 = dictionary)[itemName = loot.itemName] = dictionary2[itemName] + num;
								}
								else
								{
									dictionary.Add(loot.itemName, num);
								}
							}
						}
					}
					if (dictionary.Count > 0)
					{
						LootCollection component2 = ((GameObject)UnityEngine.Object.Instantiate(this.lootCollectionPrefab)).GetComponent<LootCollection>();
						component2.owner = player;
						component2.items = dictionary;
						component.collection = component2;
						component.enabled = true;
						if (this.lootEffect != null)
						{
							this.lootEffect.SetActive(true);
						}
					}
				}
			}
			this.target = null;
			this.agent.Stop();
			base.GetComponent<NavMeshAgent>().enabled = false;
			base.GetComponent<Rigidbody>().isKinematic = false;
			if (base.GetComponent<SpawnableObject>() != null)
			{
				base.GetComponent<SpawnableObject>().OnDeath();
			}
			if (base.GetComponent<Boss>() != null)
			{
				base.GetComponent<Boss>().OnDeath();
			}
		}
		else
		{
			if (killer == LocalPlayerEvents.localPlayer.gameObject)
			{
				GameState.gameServer.GetObjectLoot(base.GetComponent<ReplicatedObject>(), delegate(Dictionary<string, int> items)
				{
					if (items.Count > 0)
					{
						LootCollection component3 = ((GameObject)UnityEngine.Object.Instantiate(this.lootCollectionPrefab)).GetComponent<LootCollection>();
						component3.items = items;
						LootableObject component4 = base.GetComponent<LootableObject>();
						component4.collection = component3;
						component4.enabled = true;
						if (this.lootEffect != null)
						{
							this.lootEffect.SetActive(true);
						}
					}
				});
				bool flag = false;
				foreach (string current in GameState.localQuestState.availableQuests)
				{
					Quest questByName = GameState.GetQuestManager().GetQuestByName(current);
					if (!(questByName == null))
					{
						Objective objectiveByName = questByName.GetObjectiveByName(GameState.localQuestState.GetQuestState(current));
						if (!(objectiveByName == null))
						{
							if (objectiveByName is KillObjective)
							{
								KillObjective killObjective = (KillObjective)objectiveByName;
								if (!(killObjective == null))
								{
									if (killObjective.enemyName == this.enemyName)
									{
										flag = true;
										break;
									}
								}
							}
						}
					}
				}
				if (flag)
				{
					GameState.gameServer.QuestKill(this.enemyName);
					LocalPlayerEvents.localPlayer.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
				}
			}
		}
		this.corpseTimeLeft = this.maxCorpseAge;
	}
	public void OnLooted(Player player)
	{
		LootableObject component = base.GetComponent<LootableObject>();
		if (component != null)
		{
			if (GameState.isServer)
			{
				if (component.collection.owner != null && component.collection.owner != player)
				{
					return;
				}
				bool flag = false;
				foreach (string current in component.collection.items.Keys)
				{
					player.inventory.AddItem(current, component.collection.items[current]);
					if (current.Contains("Key"))
					{
						flag = true;
					}
				}
				if (flag)
				{
					GameState.UpdateCharacterItems(player.id, player.inventory);
				}
			}
			else
			{
				GameState.gameServer.TakeObjectLoot(base.GetComponent<ReplicatedObject>());
			}
			UnityEngine.Object.Destroy(component.collection);
			component.collection = null;
			component.enabled = false;
			if (this.lootEffect != null)
			{
				this.lootEffect.SetActive(false);
			}
		}
	}
	public virtual void UpdateAI()
	{
	}
	public virtual void UpdateModel()
	{
	}
	public virtual void Alert(GameObject player, Vector3 position)
	{
	}
	public virtual void StateUpdate(string name)
	{
	}
	public void UpdateRandomState()
	{
		this.UpdateRandomState((byte)UnityEngine.Random.Range(0, 256));
	}
	public void UpdateRandomState(byte val)
	{
		this.animRandom = val;
		Animator component = base.GetComponent<Animator>();
		if (component != null)
		{
			component.SetFloat("random", (float)this.animRandom / 255f);
		}
	}
	public void SetState(string name, bool val)
	{
		if (this.animStates.ContainsKey(name))
		{
			if (this.animStates[name] == val)
			{
				return;
			}
			this.animStates[name] = val;
		}
		else
		{
			this.animStates.Add(name, val);
		}
		Animator component = base.GetComponent<Animator>();
		if (component != null)
		{
			component.SetBool(name, val);
		}
		if (GameState.isServer)
		{
			Player.SendUpdateToAllPlayers(GameServerUpdate.CreateObjectAnimationUpdate(base.GetComponent<ReplicatedObject>(), name, val, this.animRandom));
		}
	}
	public void SendStatesToPlayer(ClientObject player)
	{
		foreach (string current in this.animStates.Keys)
		{
			player.SendUpdate(GameServerUpdate.CreateObjectAnimationUpdate(base.GetComponent<ReplicatedObject>(), current, this.animStates[current], this.animRandom));
		}
	}
}
