package main

import (
    "flag"
    "fmt"
    "net"
	"strings"
	"time"
	"encoding/json"
	"strconv"
	"math"
)

/*
	Client manager
	Håller clienter som mappar sin pekar till en bool om den är connectad eller inte.
	broadcast är kanalen för att sända information från klienterna som ska broadcastas ut till båda
	register är kanal som tar hand om clienter som försöker connecta
	unregister är kanal som tar hand om clienter som tar bort sin anslutning
*/
type ClientManager struct {
    clients    map[*Client]bool
    broadcast  chan []byte
    register   chan *Client
	unregister chan *Client
	clientList [2]*Client
}

//Klienten, håller en connection via socket och en kanal för att sända data
type Client struct {
    socket 	net.Conn
	data   	chan []byte
	name	string
	posX	float64
	posY	float64
	hp 		float64
	attack	bool
	angle 	float64
}

//Meddelanden som går att skicka via socketen
type Message struct {
	PlayerName 		string
	PlayerPosX 		float64
	PlayerPosY	 	float64
	Hp 				float64
	Attack			bool
	Angle 			float64
	SecondPlayerHp 	float64
}


//Vector3
type Vector3 struct{
	x float64
	y float64
	z float64
}

/**
	Metod som tillhör ClientManager.
	Startas i samband med programmet.
	Lyssnar för klienter som ansluter till servern.
	Tar bort klienter som avbryter sin anslutning.
**/
func (manager *ClientManager) start() {
	//Oändlig loop
    for {
		// Lyssnar efter register/unregister/broadcast
        select {
			//Om någon försöker connecta
        case connection := <-manager.register:
            manager.clients[connection] = true
			fmt.Println("Added new connection!")

			//Om någon anslutning brts
        case connection := <-manager.unregister:
            if _, ok := manager.clients[connection]; ok {
                close(connection.data)
                delete(manager.clients, connection)
                fmt.Println("A connection has terminated!")
			}
        }
    }
}

/**
	Manager tar emot data från sin client
**/

func (client *Client) receive(manager *ClientManager){
	for{
		//Tar emot meddelande
		message := make([]byte,2048)
		length, err := client.socket.Read(message)
		if err != nil {
			fmt.Println(err)
			manager.unregister <- client
			client.socket.Close()
			break
		}
		//Om vi fått meddelande
		if length > 0 {
			//Kontrollerar längden på meddelande
			i := 0
			for ; i < len(message) ; i++ {
				if(message[i] == 0){
					break
				}
			}
			message = message[:i]

			var m Message
			json.Unmarshal(message, &m)
			if m.PlayerName != "" {
				client.update(m)
				if m.Attack {
					for i := 0; i < len(manager.clients); i++ {
						if client != manager.clientList[i] {
							client2 := manager.clientList[i]
							client.attackFunc(client2);
							break
						}
					}
				}
			}
		}
	}
}


func (client *Client) attackFunc(client2 *Client){
	fmt.Println("ATTACK");
	if collisionDetection(client.posX,client.posY,client.angle, client2.posX, client2.posY) {
		fmt.Println("HIT")
		client2.hp -= 10
	}
}

func collisionDetection( xCord float64, yCord float64, angle float64, hitPointX float64, hitPointY float64) bool {
	//Start point
	v1 := Vector3{xCord,yCord,0}
	//Left point
	v2 := Vector3{xCord + (math.Cos((math.Pi/180) * (angle+90 + 80)) * 1.5),yCord + (math.Sin((math.Pi/180) * (angle+90 + 70)) * 3), 0 }

	//Right point
	v3 := Vector3{xCord + (math.Cos((math.Pi/180) * (angle+90 - 15)) * 5),yCord + (math.Sin((math.Pi/180) * (angle+90 - 45)) * 3), 0 }

	hitPoint := Vector3{hitPointX, hitPointY, 0}

	if pointInTriangle(v1,v2,v3,hitPoint) {
		return true
	} else {
		return false
	}

}

func pointInTriangle(v1 Vector3, v2 Vector3, v3 Vector3, point Vector3) bool {
	if (sameSide(point,v1,v2,v3) &&
	sameSide(point, v2,v1,v3) &&
	sameSide(point, v3, v1, v2)) {
		return true
	} else {
		return false
	}
}

func sameSide(p1 Vector3,p2 Vector3 ,a Vector3, b Vector3) bool{
	cp1 := crossProduct(vectorMinus(b,a), vectorMinus(p1,a))
	cp2 := crossProduct(vectorMinus(b,a),vectorMinus(p2,a))

	if dotProduct(cp1,cp2) >= 0 {
		return true
	} else {
		return false
	}
}

func vectorMinus(v1 Vector3, v2 Vector3) Vector3 {
	v := Vector3{v1.x - v2.x, v1.y - v2.y, v1.z - v2.z}
	return v
}

func dotProduct(v1 Vector3, v2 Vector3) float64 {
	return (v1.x*v2.x) + (v1.y*v2.y) + (v1.z * v2.z)
}
func crossProduct(v1 Vector3, v2 Vector3) Vector3{
	x := (v1.y * v2.z) - (v1.z * v2.y)
	y := (v1.z * v2.x) - (v1.x*v2.z)
	z := (v1.x * v2.y) - (v1.y * v2.x)
	return Vector3{x,y,z}
}
/*
	Funktion som sänder data till klienten.
	Den kollar namnet på klienten och sänder ut information om den andra spelaren.

*/
func (client *Client) send(manager *ClientManager){
	for{
		time.Sleep(10 * time.Millisecond)
		for i := 0; i < len(manager.clients); i++ {
			if client != manager.clientList[i] {
				j := 0
				if i == 0 { j = 1 }
				m := Message{manager.clientList[i].name,
					manager.clientList[i].posX,
					manager.clientList[i].posY,
					manager.clientList[i].hp,
					manager.clientList[i].attack,
					manager.clientList[i].angle,
					manager.clientList[j].hp}

				b, _ := json.Marshal(m)
				client.socket.Write(b)
				break
			}
		}
	}
}

//Uppdaterar klientens värden
func (client *Client) update(m Message){
	fmt.Println("RECEIVED: " , m)
	client.posX = m.PlayerPosX
	client.posY = m.PlayerPosY
	//client.hp 	= m.Hp
	client.angle = m.Angle;
}

func startServerMode() {

	fmt.Println("Starting server...")
	//Lyssnar efter uppringare
	listener, error := net.Listen("tcp", "130.229.141.33:12345")
	if error != nil {
        fmt.Println(error)
	}
	manager := ClientManager{
        clients:    make(map[*Client]bool),
        broadcast:  make(chan []byte),
        register:   make(chan *Client),
        unregister: make(chan *Client),
    }
    go manager.start()
	//Oändlig loop
	for {
		//Om lyssnaren hittar någon, skapar connection
		connection , error := listener.Accept()
		if error != nil {
			fmt.Println(error)
		} else {
			fmt.Println("A connection is trying to be made")
		}
		if(len(manager.clients) < 2){
			//Skapar en klient och kopplar en socket och en datakanal till den. Lägger clienten i en array hos managern och registrerar den.
			playerName := "Player" + strconv.Itoa(len(manager.clients) + 1)
			client := &Client{socket: connection, data: make(chan []byte), name: playerName, posX: 0, posY: 0, hp: 100, angle: 0}
			manager.clientList[len(manager.clients)] = client
			manager.register <- client


			//Skickar startdata till klienten
			m := Message{client.name,
				client.posX,
				client.posY,
				client.hp,
				client.attack,
				client.angle,
				100}

			b, _ := json.Marshal(m)
			client.socket.Write(b)

			// Skapar en send och recieve för den specifika clienten.
			go client.receive(&manager)
			go client.send(&manager)
		}

	}
}

func startClientMode() {
	fmt.Println("Starting client...")
	connection, error := net.Dial("tcp", "localhost:12345")
	//_,error := net.Dial("tcp","localhost:12345")
    if error != nil {
        fmt.Println(error)
    } else {
		fmt.Println("A server has been found")
		//connection.Write([]byte("hej"))
	}

	go func() {
		m := Message{"Player1",2,3,99,false,0,100}
		b, _ := json.Marshal(m)
		fmt.Println(len(b))
		connection.Write(b)
		for{
			time.Sleep(1000 * time.Millisecond)
			connection.Write(b)
		}
	}()

	select {}
}

func main() {
    flagMode := flag.String("mode", "server", "start in client or server mode")
    flag.Parse()
    if strings.ToLower(*flagMode) == "server" {
        startServerMode()
    } else {
        startClientMode()
    }
}
