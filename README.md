# Projektarbetet
### Skapa ett 2D online spel för två spelare

Vårat projekt går ut på att skapa ett spel som går att spela två spelare över internet. 

Servern skrivs i språket Go och spel-klienten i C# med Unity som hjälpmedel för det grafiska.

-----------------------------------
<br><br>
# Instruktioner
För att starta servern i server-läge:

```go run server.go --mode server```

För att starta servern i client-läge:

```go run client.go --mode client```

Unity väntar in tills två klienter är anslutna innan spelet startar. Så för att Unity-klienten ska gå igång ordentligt så krävs det att servern är igång och att en annan klient ansluter inom rimlig tid.


-----------------------------------
<br><br>
#### Mapstruktur: 

# Main
Vi utgår från det här repot och branchar för varje ny funktionalitet vi skapar. 

## /2DMPGame/
Innehåller spel-klienten skapad med Unity. 

## /Server/
Innehåller koden för servern

