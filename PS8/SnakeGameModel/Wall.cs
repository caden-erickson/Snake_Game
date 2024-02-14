using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SnakeGame;

// Authors: Connor Blood, Caden Erickson
//
// Version history: 11/26/22 - Finalized for PS8
//

namespace SnakeGameModel;

/// <summary>
/// Class that deals with the walls of the world
/// </summary>
[DataContract(Namespace = "")]
[JsonObject(MemberSerialization.OptIn)]
public class Wall
{
    //The wall's unique ID
    [DataMember(Name = "ID")]
    [JsonProperty(PropertyName = "wall")]
    public int WallID { get; private set; }
    //The first point of the wall (endpoint one)
    [DataMember(Name = "p1")]
    [JsonProperty(PropertyName = "p1")]
    public Vector2D? P1 { get; private set; }
    //The second point of the wall (endpoint two)
    [DataMember(Name = "p2")]
    [JsonProperty(PropertyName = "p2")]
    public Vector2D? P2 { get; private set; }


    /// <summary>
    /// Default Constructor
    /// </summary>
    public Wall() { }

    /// <summary>
    /// Parameterized Constructor
    /// </summary>
    /// <param name="wall"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    public Wall(int wall, Vector2D p1, Vector2D p2)
    {
        WallID = wall;
        P1 = p1;
        P2 = p2;
    }
}
