# HybridCast Server Relay

A server relay for doing multiplayer games.

# Build & Run

Open the folder in a terminal and then run this to build the image:
```
docker build -t hybrid-cast-server-relay .
```

Then launch the application via run in Visual Studio which will run with the right parameters(The run button should say "Container (Dockerfile)" on it)

# Rough Usage:

This is a rough list of the important endpoints:
```
HTTPS GET:
/game/CheckRoomCode?{roomCode} - Returns true if the room exists
/game/newgame - Returns a room code that you will use to connect


WebSocket:
/game/connect?{roomCode}&{playerName} - Connects to a game room on the server as a player
```

Once connected you can send messages which get relayed to all other players.

Game specific messages are ignored by the server and just relayed without touching them.