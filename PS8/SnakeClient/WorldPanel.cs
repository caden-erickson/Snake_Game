using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using SnakeGameModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Maui.Platform;
using SnakeGameController;
using Microsoft.Maui.Graphics;
using System.Collections;

// Authors: Daniel Kopta, Travis Martin (instructors)
//          Connor Blood, Caden Erickson
//
// Version history: 11/26/22 - Finalized for PS8
// 

namespace SnakeGame;

/// <summary>
/// This class deals with the actual drawing of the world.
/// </summary>
public class WorldPanel : IDrawable
{
    //Fields
    public World gameWorld;
    private IImage wall;
    private IImage background;
    private const int VIEW_SIZE = 900;

    private bool setupCompleted = false;
    private bool initializedForDrawing = false;

    private Color[] colorArray = { Colors.Red, Colors.Blue, Colors.Green, Colors.Salmon, Colors.Chartreuse, Colors.Indigo, Colors.IndianRed, Colors.MediumAquamarine };

#if MACCATALYST
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        return PlatformImage.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#else
    /// <summary>
    /// Loads images for use in game.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private IImage LoadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#endif

    /// <summary>
    /// Default constructor.
    /// </summary>
    public WorldPanel()
    {}

    /// <summary>
    /// This method is used to set up a World object in the view. This is a "workaround" for MVC architecture, as we need a pointer of sorts to the world to get
    /// the values that we need to update in the actual drawing of the world.
    /// </summary>
    /// <param name="w"></param>
    public void SetWorld(World w)
    {
        gameWorld = w;
    }

    /// <summary>
    /// A simple check before we begin fully drawing everything. Simply sets a flag to true to know that we're ready to start drawing everything.
    /// </summary>
    public void EnableDrawing()
    {
        setupCompleted = true;
    }

    /// <summary>
    /// Loads up the images we'll be using, and sets a flag to true to let the program know those images are loaded and ready to use.
    /// </summary>
    private void InitializeDrawing()
    {
        wall = LoadImage("WallSprite.png");
        background = LoadImage("Background.png");
        initializedForDrawing = true;
    }

    /// <summary>
    /// The Meat of the drawing. Only works if the setup flag is true (from EnableDrawing()). 
    /// 
    /// Takes a player's X and Y coord as values, then does drawing. First, we reset the Canvas, then set up the current view, which is following the player.
    /// 
    /// Draw the background.
    /// 
    /// Run through four helper methods to draw the Walls, Powerups, Snakes, and Explosions.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        //Only draw everything once we know the setup is done
        if (setupCompleted)
        {
            //Similar to setupCompleted, but this loads the images that we'll use.
            if (!initializedForDrawing)
                InitializeDrawing();

            //Player's X and Y coords
            float playerX, playerY;

            lock (gameWorld)
            {
                playerX = (float)(gameWorld.Snakes[gameWorld.PlayerID].BodyCoords[^1].X);
                playerY = (float)(gameWorld.Snakes[gameWorld.PlayerID].BodyCoords[^1].Y); 
            }

            //Reset the Canvas before setting the view, otherwise it will infinitely move away
            canvas.ResetState();
            canvas.Translate(-playerX + (VIEW_SIZE / 2), -playerY + (VIEW_SIZE / 2));

            //Draw the Background
            canvas.DrawImage(background, -gameWorld.WorldSize / 2, -gameWorld.WorldSize / 2, gameWorld.WorldSize, gameWorld.WorldSize);

            //Draw all the pieces of the game using helper methods.
            DrawWalls(canvas);
            DrawPowerups(canvas);
            DrawSnakes(canvas);
            DrawExplosion(canvas);
        }
    }

    /// <summary>
    /// Helper method called from Draw() to draw all of the snakes in the game.
    /// </summary>
    /// <param name="canvas"></param>
    public void DrawSnakes(ICanvas canvas)
    {
        //Set up the color for the displayed playername and score, and Stroke (used in Drawing Lines)
        canvas.StrokeSize = 10;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.FontColor = Colors.White;

        lock (gameWorld)
        {
            //Loop through each snake in the game
            foreach (Snake s in gameWorld.Snakes.Values)
            {
                //Don't draw dead snakes...
                if (s.Alive == false)
                    continue;

                //Randomly choose out of 8 colors for each snake -- colors can only be reused if there are more than 8 players.
                canvas.StrokeColor = colorArray[s.SnakeID % 8];

                //Draw the segments of the snake body
                for (int i = 0; i < s.BodyCoords.Count - 1; i++)
                {
                    //Account for not drawing lines across the game world
                    if (Math.Abs(s.BodyCoords[i].X) + Math.Abs(s.BodyCoords[i + 1].X) >= gameWorld.WorldSize ||
                        Math.Abs(s.BodyCoords[i].Y) + Math.Abs(s.BodyCoords[i + 1].Y) >= gameWorld.WorldSize)
                        continue;

                    //Draw lines connecting segments
                    canvas.DrawLine((float)s.BodyCoords[i].X, (float)s.BodyCoords[i].Y, (float)s.BodyCoords[i + 1].X, (float)s.BodyCoords[i + 1].Y);
                }
                //Draw playername and score
                canvas.DrawString(s.PlayerName + ": " + s.Score, (float)s.BodyCoords[^1].X, (float)s.BodyCoords[^1].Y + 20, HorizontalAlignment.Center);
            } 
        }
    }

    /// <summary>
    /// Helper method called from Draw() to draw the explosions of a snake that has died.
    /// </summary>
    /// <param name="canvas"></param>
    public void DrawExplosion(ICanvas canvas)
    {
        //This List is used to remove any exlosion objects that have "lived" long enough in the game, finished the animation basically.
        List<Explosion> toRemove = new List<Explosion>();

        //Explosions will be white
        canvas.FillColor = Colors.White;

        lock (gameWorld)
        {
            //Foreach currently existing explosion...
            foreach (Explosion e in gameWorld.Explosions)
            {
                //This is the animation bit, we have 8 explosion particles, and this draws each one.
                for (int i = 0; i < 8; i++)
                {
                    //Get the angle of the current particle
                    double angle = i / 8.0 * 2.0 * Math.PI;
                    //Use the angle to calculate an X and Y value
                    double xVal = e.Location.X + e.Radius * Math.Cos(angle);
                    double yVal = e.Location.Y + e.Radius * Math.Sin(angle);

                    //Draw the particle
                    canvas.FillEllipse((float)xVal, (float)yVal, 10, 10);
                }

                //This method increases the radius of the explosion, moving the particles further out next time around
                e.IncreaseRadius();
                //If the explosion is done, add it to the remove list.
                if (e.Done)
                    toRemove.Add(e);
            }

            //Removes all expired explosions from the gameWorld
            gameWorld.Explosions.RemoveAll(e => e.Done == true); 
        }
    }

    /// <summary>
    /// Helper method called from Draw() that helps in drawing the Powerups.
    /// </summary>
    /// <param name="canvas"></param>
    public void DrawPowerups(ICanvas canvas)
    {
        //Set a width, and the offset
        int width = 16;
        int offset = -(width / 2);
        //Powerup Color
        canvas.FillColor = Colors.LemonChiffon;

        lock (gameWorld)
        {
            foreach (Powerup p in gameWorld.Powerups.Values)
            {
                //Draw the Powerup using two ellipses.
                canvas.FillColor = Colors.MidnightBlue;
                canvas.FillEllipse((float)p.Location.X + offset, (float)p.Location.Y + offset, width, width);

                canvas.FillColor = Colors.LemonChiffon;
                canvas.FillEllipse((float)(p.Location.X + offset*0.6), (float)(p.Location.Y + offset*0.6), (float)(width*0.6), (float)(width*0.6));

            }
        }
    }

    /// <summary>
    /// Helper method to draw all the walls in the game.
    /// </summary>
    /// <param name="canvas"></param>
    public void DrawWalls(ICanvas canvas)
    {
        foreach (Wall w in gameWorld.Walls.Values)
        {
            //Get the number of horizontal wall segments, and vertical wall segments (each wall segment is the size of a wall, 50 pixels. A wall is separated into x amount of segments).
            int numHorizontal = (int)(Math.Abs(w.P1.X - w.P2.X) / 50 + 1);
            int numVertical = (int)(Math.Abs(w.P1.Y - w.P2.Y) / 50 + 1);

            //Find the wall segment that is most top left (closest to (0,0) in drawing coordinate scale.
            double smallestX = Math.Min(w.P1.X, w.P2.X);
            double smallestY = Math.Min(w.P1.Y, w.P2.Y);

            //Use a nested for loop to draw the horizontal or vertical segments of the wall. (there will be either 1 horizontal and x vertical, or the other way around)
            for (int i = 0; i < numHorizontal; i++)
                for (int j = 0; j < numVertical; j++)
                    DrawCenteredOnPoint(canvas, wall, smallestX + (i * 50), smallestY + (j * 50), 50, 50);
        }
    }

    /// <summary>
    /// Helper method for drawing an image centered on a specific point
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="image"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    private void DrawCenteredOnPoint(ICanvas canvas, IImage image, double x, double y, float width, float height)
    {
        canvas.DrawImage(image, (float)(x - width / 2), (float)(y - height / 2), width, height);
    }
}
