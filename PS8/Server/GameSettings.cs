//Authors: Connor Blood, Caden Erickson
//
//Version History: 12/6/22 - In process of updates for PS9
//                 12/8/22 - Finished PS9

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using SnakeGameModel;

namespace Server;

[DataContract(Namespace = "")]
public class GameSettings
{
    // REQUIRED SETTINGS
    [DataMember]
    public int FramesPerShot { get; private set; }
    [DataMember]
    public int MSPerFrame { get; private set; }
    [DataMember]
    public int RespawnRate { get; private set; }
    [DataMember]
    public int UniverseSize { get; private set; }
    [DataMember]
    public List<Wall>? Walls {get; private set; }

    // OPTIONAL SETTINGS
    [DataMember]
    public int? SnakeSpeed { get; private set; }
    [DataMember]
    public int? SnakeStartLength { get; private set; }
    [DataMember]
    public int? SnakeGrowth { get; private set; }
    [DataMember]
    public int? MaxPowerups { get; private set; }
    [DataMember]
    public int? MaxPowerupDelay { get; private set; }


    /// <summary>
    /// Default constructor
    /// </summary>
    public GameSettings() { }

    /// <summary>
    /// Method to take care of extra settings that may not exist in a given settings folder -- if they don't, default them here.
    /// </summary>
    /// <param name="context"></param>
    [OnDeserialized()]
    private void OnSettingsDeserialized(StreamingContext context)
    {
        // Hooray null-coalescing operator!!
        // If L-hand side is null, assign value of R-hand side
        SnakeSpeed ??= 3;
        SnakeStartLength ??= 120;
        SnakeGrowth ??= 12;
        MaxPowerups ??= 20;
        MaxPowerupDelay ??= 200;
    }
}
