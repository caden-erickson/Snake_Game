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
//

namespace SnakeGameModel;

/// <summary>
/// This class deals with the Powerups in the world. It can be serialized to JSON.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class Powerup
{
    //The unique ID of the powerup
    [JsonProperty(PropertyName = "power")]
    public int PowerupID { get; private set; }
    //The Location of the Powerup
    [JsonProperty(PropertyName = "loc")]
    public Vector2D? Location { get; private set; }
    //If the powerup has been collected or not
    [JsonProperty(PropertyName = "died")]
    public bool Died { get; set; }


    /// <summary>
    /// Default Constructor
    /// </summary>
    public Powerup() { }

    /// <summary>
    /// Parameterized constructor.
    /// </summary>
    /// <param name="power"></param>
    /// <param name="loc"></param>
    /// <param name="died"></param>
    public Powerup(int power, Vector2D loc, bool died)
    {
        PowerupID = power;
        Location = loc;
        Died = died;
    }
}
