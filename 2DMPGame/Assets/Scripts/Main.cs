using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System;

[DataContract]
internal class Message
{
    [DataMember]
    internal string PlayerName;

    [DataMember]
    internal float PlayerPosX;

    [DataMember]
    internal float PlayerPosY;

    [DataMember]
    internal float Hp;

    [DataMember]
    internal bool Attack;

    [DataMember]
    internal float Angle;

    [DataMember]
    internal float SecondPlayerHp;
}

public class Main : MonoBehaviour
{

    private Socket ClientSocket;
    private const int Port = 12345;
    private static bool _closing;
    private const int BufferSize = 2048;
    private static readonly byte[] Buffer = new byte[BufferSize];

    private ThreadStart sendThreadRef;
    private Thread sendChildThread;
    private ThreadStart listenThreadRef;
    private Thread listenChildThread;

    //public GameObject Player;
    private GameObject secondPlayer;
    private GameObject thisPlayer;
    public GameObject[] players = new GameObject[2];
    private Player playerScript;
    private SecondPlayer secondPlayerScript;

    private static float playerX = 0;
    private static float playerY = 0;
    private static float playerRotation = 0;
    private static float playerHp = 100;
    private static float playerHpLast = playerHp;
    public static bool playerAttack = false;

    private static float secondPlayerX = 0;
    private static float secondPlayerY = 0;
    private static float secondPlayerRotation = 0;
    private static bool  secondPlayerAttack = false;
    private static float secondPlayerHp = 100;
    private static float secondPlayerHpLast = secondPlayerHp;

    private bool gameStart = false;

    private Vector3 cameraOffset;

    // Use this for initialization
    void Start()
    {
        ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        ConnectToServer();
        cameraOffset = transform.position - thisPlayer.transform.position;

    }

    void Update()
    {
        if (gameStart) {
            playerX = thisPlayer.transform.position.x;
            playerY = thisPlayer.transform.position.y;
            playerRotation = thisPlayer.transform.eulerAngles.z;

            if(playerHp != playerHpLast) {
                playerScript.gotHit(playerHp);
                playerHpLast = playerHp;
            }

            secondPlayer.transform.position = new Vector3(secondPlayerX, secondPlayerY, 0);
            secondPlayer.transform.eulerAngles = new Vector3(0, 0, secondPlayerRotation);

            if(secondPlayerHp != secondPlayerHpLast) {
                Debug.Log(secondPlayerHp);
                secondPlayerScript.gotHit(secondPlayerHp);
                secondPlayerHpLast = secondPlayerHp;
            }
        }


    }
    void LateUpdate()
    {
        transform.position = thisPlayer.transform.position + cameraOffset;
    }
    private void SendThread()
    {
        while (true)
        {
            Thread.Sleep(10);
            //Create User object.
            Message message = new Message();
            message.PlayerName = "Player1";
            message.PlayerPosX = playerX;
            message.PlayerPosY = playerY;
            message.Hp = playerHp;
            message.Attack = playerAttack;
            message.Angle = playerRotation;
            message.SecondPlayerHp = secondPlayerHp;

            if (playerAttack) playerAttack = false;

            //Create a stream to serialize the object to.
            MemoryStream ms = new MemoryStream();

            // Serializer the User object to the stream.
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Message));
            ser.WriteObject(ms, message);
            byte[] json = ms.ToArray();
            ms.Close();
            ClientSocket.Send(json, SocketFlags.None);
        }
    }

    private void ListenThread()
    {

        while (true)
        {
            //Thread.Sleep(10);
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received > 0) {
                string json = Encoding.UTF8.GetString(buffer, 0, received);

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json))) {
                    // Deserialization from JSON
                    DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(Message));
                    try {
                        Message message = (Message)deserializer.ReadObject(ms);
                        secondPlayerX = message.PlayerPosX;
                        secondPlayerY = message.PlayerPosY;
                        secondPlayerRotation = message.Angle;
                        secondPlayerHp = message.Hp;
                        playerHp = message.SecondPlayerHp;

                    } catch (SerializationException e) {
                        Debug.Log("message error");
                    }

                }
            }
        }
    }

    /**
     * Wait for both player to connect to start the game.
     * Assigns the right graphic to the players.
     *
     * **/
    private void setupListen(){
        int secondPlayerIndex = 0;
        while (true) {
            //First message recieved sets up the player
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received > 0) {
                string json = Encoding.UTF8.GetString(buffer, 0, received);

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json))) {
                    // Deserialization from JSON
                    DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(Message));
                    try {
                        Message message = (Message)deserializer.ReadObject(ms);
                        //Assigns the right gameobject to the player
                        if(thisPlayer == null) {
                            if (message.PlayerName == "Player1") {
                                thisPlayer = Instantiate(players[0], new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                                secondPlayerIndex = 1;
                            } else {
                                thisPlayer = Instantiate(players[1], new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                                secondPlayerIndex = 0;
                            }
                            playerScript = thisPlayer.GetComponent<Player>();
                            playerScript.enabled = true;
                            //thisPlayer.GetComponent<Player>().enabled = true;
                        } else {
                            secondPlayer = Instantiate(players[secondPlayerIndex], new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                            //secondPlayer.GetComponent<SecondPlayer>().enabled = true;
                            secondPlayerScript = secondPlayer.GetComponent<SecondPlayer>();
                            secondPlayerScript.enabled = true;
                            gameStart = true;
                            break;
                        }

                    } catch (SerializationException e) {
                        Debug.Log("message error");
                    }

                }
            }
        }
    }
    private void ConnectToServer()
    {
        int attempts = 0;

        while (!ClientSocket.Connected)
        {
            try
            {
                attempts++;
                Debug.Log("Connection attempt " +attempts);
                //ClientSocket.Connect(IPAddress.Loopback, Port);
                ClientSocket.Connect("130.229.141.33", Port); 
                Debug.Log("Connected");

                setupListen();

                sendThreadRef = new ThreadStart(SendThread);
                sendChildThread = new Thread(sendThreadRef);
                sendChildThread.Start();

                listenThreadRef = new ThreadStart(ListenThread);
                listenChildThread = new Thread(listenThreadRef);
                listenChildThread.Start();



            }
            catch (SocketException)
            {
                Debug.Log("Error");
            }
        }
    }
    void OnApplicationQuit()
    {
        _closing = true;
        sendChildThread.Abort();
        ClientSocket.Shutdown(SocketShutdown.Both);
        ClientSocket.Close();
    }
}
