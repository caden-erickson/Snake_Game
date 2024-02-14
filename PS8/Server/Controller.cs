//Authors: Connor Blood, Caden Erickson
//
//Version History: 12/6/22 - In process of updates for PS9
//                 12/8/22 - Finished PS9

using NetworkUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnakeGameModel;
using System.Data;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using SnakeGame;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace Server;

public class Controller
{
    public World? GameWorld { get; private set; }
    private readonly Dictionary<long, SocketState> clients;
    private readonly GameSettings settings;
    private readonly Stopwatch watch;

    /// <summary>
    /// Server application entry point
    /// </summary>
    /// <param name="args">unused</param>
    static void Main(string[] args)
    {
        Controller controller;
        try
        {
            controller = new();
        }
        catch (Exception ex)
        {

            Console.WriteLine(ex.Message);
            return;
        }
        Networking.StartServer(controller.NewClientConnected, 11000);
        Console.WriteLine("Server running");
        controller.Update();
    }

    /// <summary>
    /// Constructor for the controller
    /// </summary>
    public Controller()
    {
        //Initialize client list
        clients = new();

        //Read the settings file
        DataContractSerializer ser = new(typeof(GameSettings));

        //Check for settings file in multiple locations, throw exception if we can't find it.
        XmlReader reader;
        try
        {
            reader = XmlReader.Create("settings.xml");
        }
        catch
        {
            try
            {
                reader = XmlReader.Create("../../../settings.xml");
            }
            catch
            {

                throw new Exception("Settings file not found.");
            }
        }
        
        settings = (GameSettings)ser.ReadObject(reader)!;

        GameWorld = new(0, settings.UniverseSize);

        // Populate walls with walls in GameSettings, which come from settings file
        foreach (Wall wall in settings.Walls!)
            GameWorld.Walls.Add(wall.WallID, wall);

        // Pre-populate world with 20 powerups
        GameWorld!.AddFirstPowerups((int)settings.MaxPowerups!);

        //Initialize stopwatch
        watch = new Stopwatch();
    }

    /// <summary>
    /// Client connected networking action
    /// </summary>
    /// <param name="state"></param>
    private void NewClientConnected(SocketState state)
    {
        //Print annoucement to console
        Console.WriteLine("New client connected: client " + state.ID);
        //Change network action
        state.OnNetworkAction = NameReceived;
        //Start receiving data from client
        Networking.GetData(state);
    }

    /// <summary>
    /// Networking setup for when the client sends the player name
    /// </summary>
    /// <param name="state"></param>
    private void NameReceived(SocketState state)
    {
        // Get name, remove from buffer, and truncate the newline character
        string name = state.GetData();
        state.RemoveData(0, name.Length);
        name = name[..^1];
        Console.WriteLine("Client " + state.ID + " name received: " + name);
        
        // Send player ID and world size
        string nextMsg = state.ID + "\n" + settings.UniverseSize + "\n";
        Networking.Send(state.TheSocket, nextMsg);

        // Send walls
        foreach (Wall wall in GameWorld!.Walls.Values)
        {
            Networking.Send(state.TheSocket, JsonConvert.SerializeObject(wall) + "\n");
        }

        // Adjust network action, get data for control commands from client
        state.OnNetworkAction = ControlCommandReceived;
        Networking.GetData(state);

        lock (clients)
        {
            // This socket now pertains to an established client, and it can start receiving world data
            clients.Add(state.ID, state); 
        }

        lock (GameWorld!.Snakes)
        {
            //Generate snake for player
            GameWorld!.AddNewSnake((int)state.ID, name, (int)settings.SnakeStartLength!);
        }
    }

    /// <summary>
    /// Networking setup for when player sends a movement command
    /// </summary>
    /// <param name="state"></param>
    private void ControlCommandReceived(SocketState state)
    {
        //Issue with client -- probably disconnected
        if(state.ErrorOccurred)
        {
            lock (GameWorld!.Snakes)
            {
                //Client Disconnect, update snake accordingly
                GameWorld!.Snakes[(int)state.ID].Disconnected = true;
                GameWorld!.Snakes[(int)state.ID].Died = true; 
            }

            //Close the socket
            state.TheSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            state.TheSocket.Close();
            
            //Annouce the disconnect
            Console.WriteLine("Client " + state.ID + " has disconnected.");

            //remove client from clients list
            clients.Remove(state.ID);

            return;
        }


        //Get the movement command from the client
        string command = state.GetData();

        //Account for possibility of multiple commands sent within one frame
        string[] moveInputs = Regex.Split(command, @"(?<=\n)");

        //Console.WriteLine("ctrl cmd received: " + command);

        //Watch for malformed client requests, ignore them and reset the loop
        JObject movement;
        try
        {
            //Process Movement
            movement = JObject.Parse(moveInputs[0]);
        }
        catch
        {
            state.RemoveData(0, command.Length);
            Networking.GetData(state);
            return;
        }

        // Move the appropriate snake based on the given movement command
        GameWorld!.ChangeSnakeDirection(GameWorld!.Snakes[(int)state.ID], (string)movement["moving"]!);

        //Clear messages from buffer after processing
        state.RemoveData(0, command.Length);

        //Event Loop
        Networking.GetData(state);
    }

    /// <summary>
    /// Constantly running update method to send server updates to clients
    /// </summary>
    private void Update()
    {
        //Use a watch to only send updates per frame
        watch.Start();
        while (true)
        {
            while (watch.ElapsedMilliseconds < settings.MSPerFrame) { }
            watch.Restart();

            //Apply, then send updates.
            ApplyUpdates();
            SendWorld();
        }
    }

    /// <summary>
    /// Method that applies updates such as snake movement, collisions, etc.
    /// </summary>
    private void ApplyUpdates()
    {
        // Advance all the snake positions. 
        GameWorld!.UpdateSnakePositions((int)settings.SnakeSpeed!, (int)settings.SnakeGrowth!, (int)settings.RespawnRate!, (int)settings.SnakeStartLength!);

        // Add more powerups if the required time has elapsed, and the powerup cap hasn't been hit
        GameWorld!.UpdatePowerups((int)settings.MaxPowerups!, (int)settings.MaxPowerupDelay!);

        //Check collisions
        GameWorld.CheckSnakeOnPowerupCollision();
        GameWorld.CheckSnakeOnWallCollision();
        GameWorld.CheckSnakeOnSnakeCollision();
        GameWorld.CheckSnakeOnSelfCollision(settings.UniverseSize);

        //Check Wraparound
        GameWorld.CheckSnakeWraparound(settings.UniverseSize);
    }

    /// <summary>
    /// Send the world state over the network to each client
    /// </summary>
    private void SendWorld()
    {
        StringBuilder worldString = new();

        //Trackers for destroyed powerups and snakes
        List<int> pUpsToRemove = new();
        List<int> snakesToRemove = new();

        // Add snakes to world JSON
        lock (GameWorld!.Snakes)
        {
            foreach (Snake snake in GameWorld!.Snakes.Values)
            {
                //If a client disconnected
                if (snake.Disconnected)
                {
                    snake.Died = true;
                    snake.Alive = false;
                    snakesToRemove.Add(snake.SnakeID);
                }

                worldString.Append(JsonConvert.SerializeObject(snake) + "\n");

                //Update the "joined" flag
                if (snake.Join)
                    snake.Join = false;

                //Update the "died" flag
                if (snake.Died)
                    snake.Died = false;
            }

            //Remove snakes that disconnected
            foreach (int ID in snakesToRemove)
                GameWorld.Snakes.Remove(ID);
        }

        // Add powerups to world JSON
        lock (GameWorld!.Powerups)
        {
            foreach (Powerup powerup in GameWorld!.Powerups.Values)
            {
                //If a powerup was collected, mark it to be removed
                if (powerup.Died)
                    pUpsToRemove.Add(powerup.PowerupID);

                //Send the powerups
                worldString.Append(JsonConvert.SerializeObject(powerup) + "\n");
            }

            //Remove collected powerups
            foreach(int ID in pUpsToRemove)
                GameWorld.Powerups.Remove(ID);
        }

        // Actually send the data
        lock (clients)
        {
            foreach (SocketState state in clients.Values)
                Networking.Send(state.TheSocket, worldString.ToString());
        }
    }
}