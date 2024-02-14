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
/// Class that deals with Explosions in the game. Stores some values to help draw explosions, as well as a method that assists in the animation.
/// </summary>
public class Explosion
{
    //Track if the explosion is finished
    public bool Done { get; private set; }
    //The current radius of the explosion -- how far out from the epicenter to draw the particles
    public double Radius { get; private set; }
    //Location of the epicenter of the explosion
    public Vector2D Location { get; private set; }

    /// <summary>
    /// Constructor that initializes explosion
    /// </summary>
    /// <param name="location"></param>
    public Explosion(Vector2D location)
    {
        Done = false;
        Radius = 0;
        this.Location = location;
    }

    /// <summary>
    /// Static way of steadily increasing the radius
    /// </summary>
    public void IncreaseRadius()
    {
        //Increase the radius by 5 pixels per call
        Radius += 5;

        //Check to see if explosion has finished
        if (Radius > 100)
            Done = true;
    }
}
