using Newtonsoft.Json;
using SnakeGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Authors: Connor Blood, Caden Erickson
//
// Version history: 11/26/22 - Finalized for PS8
//                  12/6/22 - Modified for PS9
//                  12/8/22 - Finished PS9

namespace SnakeGameModel;

/// <summary>
/// Class that deals with the snakes of the world.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class Snake
{
    //The unique ID of the snake
    [JsonProperty(PropertyName = "snake")]
    public int SnakeID { get; private set; }
    //The player's name
    [JsonProperty(PropertyName = "name")]
    public string? PlayerName { get; private set; }
    //The list of body coordinates for the snake
    [JsonProperty(PropertyName = "body")]
    public List<Vector2D>? BodyCoords { get; set; }
    //The direction the snake is currently pointed
    [JsonProperty(PropertyName = "dir")]
    public Vector2D? Direction { get; set; }
    //The player's score
    [JsonProperty(PropertyName = "score")]
    public int Score { get; set; }
    //If the snake has died
    [JsonProperty(PropertyName = "died")]
    public bool Died { get; set; }
    //If the snake is alive
    [JsonProperty(PropertyName = "alive")]
    public bool Alive { get; set; }
    //If the player has disconnected
    [JsonProperty(PropertyName = "dc")]
    public bool Disconnected { get; set; }
    //If the player has just joined
    [JsonProperty(PropertyName = "join")]
    public bool Join { get; set; }

    //For if the snake is growing
    public bool Growing { get; set; }
    //For "timing" the growth
    public int FramesSinceEaten { get; set; }
    //Snake Respawning
    public int FramesSinceDied { get; set; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    public Snake()
    { }

    /// <summary>
    /// Parameterized Constructor
    /// </summary>
    /// <param name="snake"></param>
    /// <param name="name"></param>
    /// <param name="body"></param>
    /// <param name="dir"></param>
    /// <param name="score"></param>
    /// <param name="died"></param>
    /// <param name="alive"></param>
    /// <param name="dc"></param>
    /// <param name="join"></param>
    public Snake(int snake, string name, List<Vector2D> body, Vector2D dir, int score, bool died, bool alive, bool dc, bool join)
    {
        SnakeID = snake;
        PlayerName = name;
        BodyCoords = body;
        Direction = dir;
        Score = score;
        Died = died;
        Alive = alive;
        Disconnected = dc;
        Join = join;
    }
}
