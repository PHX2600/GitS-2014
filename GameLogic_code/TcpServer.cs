using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
public class TcpServer
{
	private Thread thread;
	private TcpListener listener;
	public int port
	{
		get
		{
			return ((IPEndPoint)this.listener.LocalEndpoint).Port;
		}
	}
	public TcpServer(int bindPort = 0)
	{
		this.listener = new TcpListener(IPAddress.Any, bindPort);
		this.thread = new Thread(new ThreadStart(this.ServerThread));
	}
	public void Start()
	{
		this.listener.Start();
		this.thread.Start();
	}
	private void ServerThread()
	{
		while (true)
		{
			TcpClient tcpClient = this.listener.AcceptTcpClient();
			tcpClient.NoDelay = true;
			tcpClient.ReceiveTimeout = 15000;
			tcpClient.SendTimeout = 15000;
			ClientHandler clientHandler = new ClientHandler(tcpClient.GetStream());
			clientHandler.Start();
		}
	}
}
