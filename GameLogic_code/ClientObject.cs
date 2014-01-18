using System;
using UnityEngine;
public abstract class ClientObject : MonoBehaviour
{
	public abstract void SendUpdate(GameServerUpdate update);
}
