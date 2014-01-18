using System;
using System.Threading;
using UnityEngine;
public class ReplicatedObject : MonoBehaviour
{
	public string replicationName;
	public int id = -1;
	public Vector3 remotePosition;
	public Quaternion remoteOrientation;
	private void FixedUpdate()
	{
		if (base.GetComponent<Enemy>() != null && base.GetComponent<Enemy>().health <= 0)
		{
			return;
		}
		if (base.GetComponent<Player>() != null && base.GetComponent<Player>().health <= 0)
		{
			return;
		}
		if (GameState.isServer)
		{
			Monitor.Enter(this);
			try
			{
				this.remotePosition = base.transform.position;
				Player component = base.GetComponent<Player>();
				if (component == null)
				{
					this.remoteOrientation = base.transform.rotation;
				}
				else
				{
					object obj = component;
					Monitor.Enter(obj);
					try
					{
						this.remoteOrientation = component.remoteLookDirection;
					}
					finally
					{
						Monitor.Exit(obj);
					}
				}
			}
			finally
			{
				Monitor.Exit(this);
			}
		}
		else
		{
			if (this.id == GameState.localPlayerInfo.replicatedPlayerId)
			{
				return;
			}
			Monitor.Enter(this);
			try
			{
				base.transform.position = Vector3.Lerp(base.transform.position, this.remotePosition, 0.2f);
				if (base.GetComponent<Player>() == null)
				{
					base.transform.rotation = Quaternion.Lerp(base.transform.rotation, this.remoteOrientation, 0.2f);
				}
			}
			finally
			{
				Monitor.Exit(this);
			}
		}
	}
}
