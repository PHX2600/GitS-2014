using System;
using System.Diagnostics;
using UnityEngine;
public class Server : MonoBehaviour
{
	public static TcpServer tcpServer = null;
	public static bool init = false;
	public static DateTime startTime = DateTime.Now;
	private void Awake()
	{
		Application.targetFrameRate = 30;
		if (Server.init)
		{
			GameState.masterServer.CheckForServerAssignment(new MasterServerConnection.CheckForServerAssignmentCallback(this.ServerAssignmentCallback));
			return;
		}
		GameState.isServer = true;
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string text = null;
		string user = null;
		string pass = null;
		string host = null;
		int port = -1;
		int bindPort = 0;
		try
		{
			for (int i = 0; i < commandLineArgs.Length; i++)
			{
				if (commandLineArgs[i] == "--master")
				{
					text = commandLineArgs[++i];
					port = int.Parse(commandLineArgs[++i]);
				}
				else
				{
					if (commandLineArgs[i] == "--account")
					{
						user = commandLineArgs[++i];
						pass = commandLineArgs[++i];
					}
					else
					{
						if (commandLineArgs[i] == "--host")
						{
							host = commandLineArgs[++i];
						}
						else
						{
							if (commandLineArgs[i] == "--port")
							{
								bindPort = int.Parse(commandLineArgs[++i]);
							}
							else
							{
								if (commandLineArgs[i] == "--disable-spectator")
								{
									GameState.allowSpectator = false;
								}
							}
						}
					}
				}
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
			Process.GetCurrentProcess().Kill();
			return;
		}
		if (text == null || user == null || pass == null || host == null)
		{
			UnityEngine.Debug.LogError("Server requires --master, --account, and --host options on command line");
			Process.GetCurrentProcess().Kill();
			return;
		}
		int serverPort;
		try
		{
			Server.tcpServer = new TcpServer(bindPort);
			Server.tcpServer.Start();
			serverPort = Server.tcpServer.port;
		}
		catch (Exception value2)
		{
			Console.WriteLine(value2);
			Process.GetCurrentProcess().Kill();
			return;
		}
		GameState.ConnectToMasterServer(text, port, delegate(bool connectOk, string errorMsg)
		{
			if (!connectOk)
			{
				UnityEngine.Debug.LogError(errorMsg);
				Process.GetCurrentProcess().Kill();
				return;
			}
			GameState.masterServer.Login(user, pass, delegate(bool loginOk, int id, string teamName)
			{
				if (!loginOk)
				{
					UnityEngine.Debug.LogError("Access denied");
					Process.GetCurrentProcess().Kill();
					return;
				}
				GameState.masterServer.AddServerToPool(host, serverPort);
				GameState.masterServer.CheckForServerAssignment(new MasterServerConnection.CheckForServerAssignmentCallback(this.ServerAssignmentCallback));
			});
		});
		Server.init = true;
	}
	private void ServerAssignmentCallback(bool valid, string map)
	{
		if (!valid)
		{
			GameState.masterServer.CheckForServerAssignment(new MasterServerConnection.CheckForServerAssignmentCallback(this.ServerAssignmentCallback));
			return;
		}
		GameState.ServerLog("Transitioning to map " + map);
		Application.LoadLevel(map);
	}
	private void Update()
	{
		if ((DateTime.Now - Server.startTime).TotalHours > 4.0)
		{
			Process.GetCurrentProcess().Kill();
		}
	}
}
