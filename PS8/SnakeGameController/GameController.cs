using NetworkUtil;
using System.Text.RegularExpressions;
using SnakeGameModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

// Authors: Connor Blood, Caden Erickson
//
// Version history: 11/26/22 - Finalized for PS8
// 

namespace SnakeGameController;

/// <summary>
/// Class that is the Controller in the MVC architecture for the Snake Game.
/// </summary>
public class GameController
{
    //Fields
    private SocketState? state;
    public World? GameWorld { get; private set; }
    public bool canAcceptCommands = false;

    public delegate void InitialDataHandler();
    public event InitialDataHandler? SetupComplete;

    public delegate void UpdateReceivedHandler();
    public event UpdateReceivedHandler? UpdateReceived;

    public delegate void ErrorOccurredHandler(string s);
    public event ErrorOccurredHandler? ErrorOccurred;

    /// <summary>
    /// Default Constructor
    /// </summary>
    public GameController()
    {
    }

    /// <summary>
    /// This method is what moves the snake after a command is received.
    /// 
    /// Sends JSON text to the server with the requested direction of movement.
    /// </summary>
    /// <param name="direction"></param>
    public void Move(string direction)
    {
        //Send the movement direction received from the entry in the graphics.
        if (canAcceptCommands)
            Networking.Send(state!.TheSocket, "{\"moving\":\"" + direction + "\"}\n");

        //Prevent more than one move commmand per frame
        Thread.Sleep(17);
    }

    /// <summary>
    /// Method that assists in connecting to a server using the Networking.dll
    /// </summary>
    /// <param name="server"></param>
    /// <param name="username"></param>
    public void ConnectToServer(string server, string username)
    {
        //Runs the ConnectToServer method from Networking.dll -- hardcoded to use port 11000 per network requirements for this game.
        Networking.ConnectToServer(ConnectCallback, server, 11000);

        // The callback method for ConnectToServer
        void ConnectCallback(SocketState state)
        {
            this.state = state;

            // If an error occurred while connecting, invoke the appropriate event,
            // get rid of the SocketState and leave the method early
            if (state.ErrorOccurred)
            {
                ErrorOccurred?.Invoke("Failed to connect to server.");
                this.state = null;
                return;
            }
            
            //Sends the player's username to the server
            Networking.Send(state.TheSocket, username + "\n");

            //Set up the new NetworkAction to our next method that handles the PlayerID and world size (the first two items the Server will send to us)
            state.OnNetworkAction = FirstTwoItemsReceived;
            //Recieve some data from the server.
            Networking.GetData(state);
        }

        //Leave if our state is null -- i.e. connection didn't work at some point
        if (state == null)
            return;
    }

    /// <summary>
    /// Method that handles the first two items the server will send to us as part of the "handshake".
    /// </summary>
    /// <param name="state"></param>
    private void FirstTwoItemsReceived(SocketState state)
    {
        //Split up the state buffer
        string[] splitData = Regex.Split(state.GetData(), @"(?<=\n)");

        //Create our gameworld off the received playerID and world size
        GameWorld = new(int.Parse(splitData[0]), int.Parse(splitData[1]));
        //Clear the first two items from the buffer
        state.RemoveData(0, splitData[0].Length + splitData[1].Length);

        //Call GetData to start reading the rest of the world state
        state.OnNetworkAction = WallsReceived;
        Networking.GetData(state);
    }

    /// <summary>
    /// Method that takes care of storing the walls sent by the server
    /// </summary>
    /// <param name="state"></param>
    private void WallsReceived(SocketState state)
    {
        //Split up the state's buffer -- leaving in the newline character that marks the end of a JSON object.
        string[] splitData = Regex.Split(state.GetData(), @"(?<=\n)");

        //Loop through all the objects we put into our array
        foreach (string obj in splitData)
        {
            //This if statement finds one entire JSON object that is sent
            if (obj.StartsWith("{") && obj.EndsWith("\n"))
            {
                //Parse the object.
                JObject jObj = JObject.Parse(obj);

                // if the object is a snake or powerup, we're through all the walls.
                // Change the OnNetworkAction, and break out of this loop entirely.
                if (jObj["snake"] != null || jObj["power"] != null)
                {
                    state.OnNetworkAction = RecurringDataReceived;
                    SetupComplete?.Invoke();
                    canAcceptCommands = true;
                    break;
                }

                //Get a JToken from the object
                JToken? token = jObj["wall"];

                //If we indeed have a wall...
                if (token != null)
                {
                    //Convert the JSON object into a Wall object
                    Wall wall = JsonConvert.DeserializeObject<Wall>(obj)!;

                    lock (GameWorld!)
                    {
                        //Add it to the wall collection
                        GameWorld.Walls.Add((int)token, wall);
                    }

                    //Remove this object from the buffer
                    state.RemoveData(0, obj.Length);
                }
            }
        }

        //We will reach this point when we read through all the complete JSON objects in the buffer, or if we started reading snakes or powerups, meaning there is no more walls.
        Networking.GetData(state);
    }

    /// <summary>
    /// This method reads the information for the rest of the time of the game (after player ID, world size, and walls are taken care of).
    /// </summary>
    /// <param name="state"></param>
    private void RecurringDataReceived(SocketState state)
    {
        if (state.ErrorOccurred)
        {
            ErrorOccurred?.Invoke("A server connection issue occurred.\nPlease restart the client and try reconnecting.");
            return;
        }

        //Split up the state buffer, leaving in the newline character denoting the end of a JSON object
        string[] splitData = Regex.Split(state.GetData(), @"(?<=\n)");

        JObject gameObject;

        //For each JSON object...
        foreach (string obj in splitData)
        {
            //If it is NOT a complete JSON object, don't interact with it
            if (!obj.StartsWith("{") || !obj.EndsWith("\n"))
                continue;

            //Parse the object
            gameObject = JObject.Parse(obj);

            lock (GameWorld!)
            {
                //Check for snake
                if (gameObject["snake"] != null)
                {
                    //store the ID
                    int snakeID = (int)gameObject["snake"]!;

                    //If the snake has not disconnected...
                    if ((bool)gameObject["dc"]! == false)
                    {
                        //Add it to the snake collection
                        Snake snake = JsonConvert.DeserializeObject<Snake>(obj)!;
                        GameWorld.Snakes[snakeID] = snake;

                        //If the snake has died, create an explosion
                        if (snake.Died)
                        {
                            GameWorld.Explosions.Add(new Explosion(snake.BodyCoords![^1]));
                        }
                    }
                    //The snake has disconnected, remove it from the World's snake collection so that it isn't drawn anymore.
                    else
                    {
                        GameWorld.Snakes.Remove(snakeID);
                    }

                    //Remove the object from the buffer
                    state.RemoveData(0, obj.Length);
                }
                //It's a powerup
                else if (gameObject["power"] != null)
                {
                    //store the ID
                    int powerID = (int)gameObject["power"]!;

                    //If the powerup is "alive" -- i.e. not collected yet...
                    if ((bool)gameObject["died"]! == false)
                    {
                        //Add it to the powerup collection
                        GameWorld.Powerups[powerID] = JsonConvert.DeserializeObject<Powerup>(obj)!;
                    }
                    //If the Powerup "died", remove it so it doesn't get redrawn
                    else
                    {
                        GameWorld.Powerups.Remove(powerID);
                    }

                    //remove the object from the buffer
                    state.RemoveData(0, obj.Length);
                }
            }
        }

        //Invoke our event saying that we've received and processed a server update (this queues redrawing the world)
        UpdateReceived?.Invoke();
        //Receive more data
        Networking.GetData(state);
    }
}