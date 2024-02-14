// Authors: Connor Blood, Caden Erickson
//
// Version history: 11/26/22 - Finalized for PS8
//                  12/6/22 - In process of updates for PS9
//                  12/8/22 - Finished PS9

using SnakeGame;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Text;

namespace SnakeGameModel;

/// <summary>
/// This class is a container of sorts for the World of the game. Stores collections containing each snake, powerup, wall, and explosion. 
/// It also stores the player's ID and the size of the world.
/// </summary>
public class World
{
    // Used by client and server
    public Dictionary<int, Wall> Walls { get; set; }
    public Dictionary<int, Snake> Snakes { get; set; }
    public Dictionary<int, Powerup> Powerups { get; set; }
    public List<Explosion> Explosions { get; set; }
    public int PlayerID { get; private set; }
    public int WorldSize { get; private set; }

    private int FramesSinceLastPowerup = 0;
    private int TotalPowerupsEver = 0;

    // Used by server only
    private readonly Vector2D[] directions = { new Vector2D(0.0, -1.0), new Vector2D(1.0, 0.0), new Vector2D(0.0, 1.0), new Vector2D(-1.0, 0.0) };

    /// <summary>
    /// Constructor -- initializes all values, either to empty Dictionaries and Lists, or sets the PlayerID and world's size.
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="worldSize"></param>
    public World(int playerID, int worldSize)
    {
        PlayerID = playerID;
        WorldSize = worldSize;
        Walls = new Dictionary<int, Wall>();
        Snakes = new Dictionary<int, Snake>();
        Powerups = new Dictionary<int, Powerup>();
        Explosions = new List<Explosion>();
    }


    // Adding/updating snakes
    /// <summary>
    /// Adds a new snake to the world -- only runs once per client, this is for brand new connections only
    /// </summary>
    /// <param name="snakeID"></param>
    /// <param name="playerName"></param>
    /// <param name="snakeLength"></param>
    public void AddNewSnake(int snakeID, string playerName, int snakeLength)
    {
        // Choose a random direction
        Random rng = new();
        Vector2D randomDir = directions[rng.NextInt64(3)];

        //Generate Random Snake Starting Location
        List<Vector2D> bodyCoords = RandomSnakeLocation(randomDir, snakeLength);

        //Add the new player to our collection
        Snakes[snakeID] = new Snake(snakeID, playerName, bodyCoords, randomDir, 0, false, true, false, true);
    }

    /// <summary>
    /// This method updates the snake direction when a movement command is received.
    /// </summary>
    /// <param name="snake"></param>
    /// <param name="movementDirection"></param>
    public void ChangeSnakeDirection(Snake snake, string movementDirection)
    {
        bool changedDir = false;
        Vector2D segmentDir;
        bool can180 = false;

        if (snake.BodyCoords!.Count > 2)
        {
            segmentDir = snake.BodyCoords![^2] - snake.BodyCoords[^3];
            segmentDir.Normalize();
            can180 = true;
        }

        lock (Snakes)
        {
            //This checks if we can move right our left, meaning we must have been moving up or down
            if (snake.Direction == directions[0] || snake.Direction == directions[2])
            {
                //Check if it's been long enough to turn again (10 units)
                if (can180)
                {
                    if (Math.Abs(snake.BodyCoords[^1].Y - snake.BodyCoords[^2].Y) < 10)
                        return; 
                }

                switch (movementDirection)
                {
                    case "right":
                        snake.Direction = directions[1];
                        changedDir = true;
                        break;
                    case "left":
                        snake.Direction = directions[3];
                        changedDir = true;
                        break;
                    default:
                        break;
                }
            }
            //This checks for moving up or down, in which case we had to have been going right or left
            else if (snake.Direction == directions[1] || snake.Direction == directions[3])
            {
                //Check if it's been long enough to turn again (10 units)
                if (can180)
                {
                    if (Math.Abs(snake.BodyCoords[^1].X - snake.BodyCoords[^2].X) < 10)
                        return;
                }

                switch (movementDirection)
                {
                    case "up":
                        snake.Direction = directions[0];
                        changedDir = true;
                        break;
                    case "down":
                        snake.Direction = directions[2];
                        changedDir = true;
                        break;
                    default:
                        break;
                }
            }

            //If we successfully changed direction...
            if (changedDir)
            {
                //Add a new body coordinate where the old head was upon changing direction
                snake.BodyCoords!.Add(new Vector2D(snake.BodyCoords![^1]));
            }
        }
    }

    /// <summary>
    /// This method updates the current positions of snakes.
    /// </summary>
    /// <param name="snakeSpeed"></param>
    /// <param name="snakeGrowth"></param>
    /// <param name="respawnRate"></param>
    /// <param name="snakeLength"></param>
    public void UpdateSnakePositions(int snakeSpeed, int snakeGrowth, int respawnRate, int snakeLength)
    {
        lock (Snakes)
        {
            foreach (Snake snake in Snakes.Values)
            {
                //Check if the snake is alive and should be drawn
                if (!snake.Alive)
                {
                    if (snake.FramesSinceDied < respawnRate)
                    {
                        snake.FramesSinceDied++;
                        continue;
                    }
                    else
                    {
                        //Redraw snake, time to respawn
                        snake.BodyCoords = RandomSnakeLocation(snake.Direction!, snakeLength);
                        snake.Alive = true;
                    }
                }


                // Get the direction of the tail as a unit vector
                Vector2D tailDirection = snake.BodyCoords![1] - snake.BodyCoords![0];
                tailDirection.Normalize();

                //Check if the snake has collected a powerup -- if we have, the tail should not move for a certain amount of time, to grow the snake
                if (!snake.Growing)
                {
                    // Update tail using tail direction vector (and velocity, eventually)
                    snake.BodyCoords![0] += tailDirection * snakeSpeed;
                    if (snake.BodyCoords![0].Equals(snake.BodyCoords![1]))
                        snake.BodyCoords!.Remove(snake.BodyCoords![0]);

                    //Check for wraparound, remove extra segment if we did wrap.
                    if (Math.Abs(snake.BodyCoords[0].X) >= WorldSize/2 || Math.Abs(snake.BodyCoords[0].Y) >= WorldSize/2)
                        snake.BodyCoords.Remove(snake.BodyCoords[0]);
                }
                //The snake is currently growing...
                else
                {
                    //Check if we're done growing or not
                    if (snake.FramesSinceEaten < snakeGrowth)
                        snake.FramesSinceEaten++;
                    else
                        snake.Growing = false;
                }

                // Update head using snake direction vector
                snake.BodyCoords![^1] += snake.Direction! * snakeSpeed;
            }
        }
    }


    // Adding/updating powerups

    /// <summary>
    /// Method that adds initial powerups to the world equal to the maxPowerups defined in game settings.
    /// </summary>
    /// <param name="maxPowerups"></param>
    public void AddFirstPowerups(int maxPowerups)
    {
        for (int i = 0; i < maxPowerups; i++)
        {
            Powerups.Add(TotalPowerupsEver, new Powerup(TotalPowerupsEver, RandomPowerupLocation(8), false));
            TotalPowerupsEver++;
        }
    }

    /// <summary>
    /// This method is used to add new powerups to the world based on a time delay, and if we currently already have max powerups.
    /// </summary>
    /// <param name="maxPowerups"></param>
    public void UpdatePowerups(int maxPowerups, int maxPowerupDelay)
    {
        //If it is time to add a new powerup...
        if (FramesSinceLastPowerup == 0)
        {
            //Check if we are at the max powerup limit
            if (Powerups.Count < maxPowerups)
            {
                //Add a new powerup, increment our ID counter
                Powerups.Add(TotalPowerupsEver, new Powerup(TotalPowerupsEver, RandomPowerupLocation(8), false));
                TotalPowerupsEver++;
            }

            //Set a random timer for when we should spawn a new powerup
            Random rng = new();
            FramesSinceLastPowerup = (int)rng.NextInt64(maxPowerupDelay);
        }
        //Count down time to next powerup
        else
        {
            FramesSinceLastPowerup--;
        }
    }



    // ======================================================================================
    // Checking collisions
    // ======================================================================================

    /// <summary>
    /// Checks snakes colliding with powerups. Changes fields/properties as appropriate if/when collisions happen.
    /// </summary>
    public void CheckSnakeOnPowerupCollision()
    {
        lock (Snakes)
        {
            foreach (Snake snake in Snakes.Values)
            {
                //Make sure the snake is alive before checking collisions
                if (!snake.Alive)
                    continue;

                lock (Powerups)
                {
                    foreach (Powerup powerup in Powerups.Values)
                    {
                        //If there is a collision...
                        if (Math.Abs(snake.BodyCoords![^1].X - powerup.Location!.X) < 13 && Math.Abs(snake.BodyCoords[^1].Y - powerup.Location.Y) < 13)
                        {
                            //Mark the powerup for despawn, increment score, and mark the snake as growing
                            powerup.Died = true;
                            snake.Score += 1;

                            snake.Growing = true;
                            snake.FramesSinceEaten = 0;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks snakes colliding with walls. Changes fields/properties as appropriate if/when collisions happen.
    /// </summary>
    public void CheckSnakeOnWallCollision()
    {
        lock (Snakes)
        {
            foreach (Snake snake in Snakes.Values)
            {
                //Don't check collisions if snake is dead.
                if (!snake.Alive)
                    continue;

                foreach (Wall wall in Walls.Values)
                {
                    //Get a set of coordinates for the wall
                    double wallLeftX = Math.Min(wall.P1!.X, wall.P2!.X) - 25;
                    double wallRightX = Math.Max(wall.P1!.X, wall.P2!.X) + 25;
                    double wallTopY = Math.Min(wall.P1!.Y, wall.P2!.Y) - 25;
                    double wallBottomY = Math.Max(wall.P1!.Y, wall.P2!.Y) + 25;

                    //Get a set of coordinates for our snake's head
                    double snakeLeftX = snake.BodyCoords![^1].X - 5;
                    double snakeRightX = snake.BodyCoords![^1].X + 5;
                    double snakeTopY = snake.BodyCoords![^1].Y - 5;
                    double snakeBottomY = snake.BodyCoords![^1].Y + 5;

                    //If there is an intersection...
                    if (snakeLeftX < wallRightX && snakeRightX > wallLeftX &&
                        snakeTopY < wallBottomY && snakeBottomY > wallTopY)
                    {
                        //Mark our snake as dead, reset score, and mark when they died with FramesSinceDied.
                        snake.Died = true;
                        snake.Alive = false;
                        snake.Score = 0;
                        snake.FramesSinceDied = 0;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks snakes colliding with other snakes. Changes fields/properties as appropriate if/when collisions happen.
    /// </summary>
    public void CheckSnakeOnSnakeCollision()
    {
        lock (Snakes)
        {
            foreach (Snake playerSnake in Snakes.Values)
            {
                //Don't check collisions if the snake being checked is dead.
                if (!playerSnake.Alive)
                    continue;

                // Get a set of coordinates for the head of this snake
                double playerHeadLeftX = playerSnake.BodyCoords![^1].X - 5;
                double playerHeadRightX = playerSnake.BodyCoords[^1].X + 5;
                double playerHeadTopY = playerSnake.BodyCoords[^1].Y - 5;
                double playerHeadBottomY = playerSnake.BodyCoords[^1].Y + 5;

                foreach (Snake opponent in Snakes.Values)
                {
                    // Don't check collisions if the snake being checked against is dead
                    if (!opponent.Alive)
                        continue;

                    // Don't check collisions if the snake being checked is the same as the snake being checked against
                    if (playerSnake.SnakeID == opponent.SnakeID)
                        continue;

                    // Check against all snake segments
                    for (int i = 0; i < opponent.BodyCoords!.Count - 1; i++)
                    {
                        //Get a set of coordinates for our snake's head
                        double oppLeftX = Math.Min(opponent.BodyCoords[i].X, opponent.BodyCoords[i + 1].X) - 5;
                        double oppRightX = Math.Max(opponent.BodyCoords[i].X, opponent.BodyCoords[i + 1].X) + 5;
                        double oppTopY = Math.Min(opponent.BodyCoords[i].Y, opponent.BodyCoords[i + 1].Y) - 5;
                        double oppBottomY = Math.Max(opponent.BodyCoords[i].Y, opponent.BodyCoords[i + 1].Y) + 5;

                        //If there is an intersection...
                        if (playerHeadLeftX < oppRightX && playerHeadRightX > oppLeftX &&
                            playerHeadTopY < oppBottomY && playerHeadBottomY > oppTopY)
                        {
                            //Mark our snake as dead, reset score, and mark when they died with FramesSinceDied.
                            playerSnake.Died = true;
                            playerSnake.Alive = false;
                            playerSnake.Score = 0;
                            playerSnake.FramesSinceDied = 0;
                            break;
                        }
                    }

                    // if the snake being checked collided with something, we need to break out of one more level of loop
                    if (playerSnake.Died)
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Checks snakes colliding with themselves. Changes fields/properties as appropriate if/when collisions happen.
    /// </summary>
    public void CheckSnakeOnSelfCollision(int worldSize)
    {
        lock (Snakes)
        {
            foreach (Snake snake in Snakes.Values)
            {
                if (!snake.Alive)
                    continue;

                int checkIndex = 0;

                // Get head direction (already in snake), and find its opposite
                // Negative 0s are an issue here, hence the somewhat unsightly ternary expressions
                Vector2D oppositeDir = new(snake.Direction!);
                oppositeDir.X = (oppositeDir.X == 0 ? 0 : oppositeDir.X * -1.0);
                oppositeDir.Y = (oppositeDir.Y == 0 ? 0 : oppositeDir.Y * -1.0);

                // Loop backwards through body segments, calculating and normalizing, until the first segment with direction opposite the head is found
                for (int i = snake.BodyCoords!.Count - 1; i > 0; i--)
                {
                    Vector2D segmentDir = snake.BodyCoords[i] - snake.BodyCoords[i - 1];

                    if (segmentDir.Length() >= worldSize)
                        continue;

                    segmentDir.Normalize();

                    if (oppositeDir.Equals(segmentDir))
                    {
                        checkIndex = i - 1;
                        break;
                    }
                }

                // Snake head coordinates
                double headLeftX = snake.BodyCoords![^1].X - 5;
                double headRightX = snake.BodyCoords[^1].X + 5;
                double headTopY = snake.BodyCoords[^1].Y - 5;
                double headBottomY = snake.BodyCoords[^1].Y + 5;

                // Continue looping backwards from that point, checking collision with head
                for (int i = checkIndex; i > 0; i--)
                {
                    //Get the coordinates for the current segmnet being checked
                    double segmentLeftX = Math.Min(snake.BodyCoords[i].X, snake.BodyCoords[i - 1].X) - 5;
                    double segmentRightX = Math.Max(snake.BodyCoords[i].X, snake.BodyCoords[i - 1].X) + 5;
                    double segmentTopY = Math.Min(snake.BodyCoords[i].Y, snake.BodyCoords[i - 1].Y) - 5;
                    double segmentBottomY = Math.Max(snake.BodyCoords[i].Y, snake.BodyCoords[i - 1].Y) + 5;

                    //If there is an intersection...
                    if (headLeftX < segmentRightX && headRightX > segmentLeftX &&
                        headTopY < segmentBottomY && headBottomY > segmentTopY)
                    {
                        //Mark our snake as dead, reset score, and mark when they died with FramesSinceDied.
                        snake.Died = true;
                        snake.Alive = false;
                        snake.Score = 0;
                        snake.FramesSinceDied = 0;
                        break;
                    }
                }
            } 
        }
    }


    // Helpers for generating random spawn locations

    /// <summary>
    /// Helper method to randomly choose spawn locations for Powerups.
    /// </summary>
    /// <param name="entityRadius"></param>
    /// <returns></returns>
    private Vector2D RandomPowerupLocation(int entityRadius)
    {
        Random rng = new();
        //Vector to track potential spawn location
        Vector2D loc;

        bool inWall;
        //Loop until we find a spot that isn't colliding with something.
        do
        {
            inWall = false;
            //Choose a random location
            loc = new((int)rng.NextInt64(-WorldSize / 2, WorldSize / 2), (int)rng.NextInt64(-WorldSize / 2, WorldSize / 2));

            //See if our location is colliding with any walls
            foreach (Wall wall in Walls.Values)
            {
                //Wall coordinates
                double leftX = Math.Min(wall.P1!.X, wall.P2!.X) - 35;
                double rightX = Math.Max(wall.P1!.X, wall.P2!.X) + 35;
                double topY = Math.Min(wall.P1!.Y, wall.P2!.Y) - 35;
                double bottomY = Math.Max(wall.P1!.Y, wall.P2!.Y) + 35;

                //If colliding...
                if (loc.X + entityRadius > leftX && loc.X - entityRadius < rightX &&
                    loc.Y + entityRadius > topY && loc.Y - entityRadius < bottomY)
                    inWall = true;
            }
        } while (inWall == true);

        //Return the position that is safe to spawn a powerup in
        return loc;
    }

    /// <summary>
    /// Helper method for choosing a spawn location for a snake.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="snakeLength"></param>
    /// <returns></returns>
    private List<Vector2D> RandomSnakeLocation(Vector2D direction, int snakeLength)
    {
        Random rng = new();
        //snake tail point
        Vector2D tailPoint;
        //snake head point
        Vector2D headPoint;

        bool colliding;

        //Loop until we find a spot that isn't colliding with anything.
        do
        {
            colliding = false;
            // Choose a random tail point
            tailPoint = new((int)rng.NextInt64(-WorldSize / 2, WorldSize / 2), (int)rng.NextInt64(-WorldSize / 2, WorldSize / 2));
            // Create a headpoint based off the tail point
            headPoint = tailPoint + direction * snakeLength;

            // Snake coordinates
            double snakeLeftX = Math.Min(tailPoint.X, headPoint.X) - 5;
            double snakeRightX = Math.Max(tailPoint.X, headPoint.X) + 5;
            double snakeTopY = Math.Min(tailPoint.Y, headPoint.Y) - 5;
            double snakeBottomY = Math.Max(tailPoint.Y, headPoint.Y) + 5;

            // Check collisions with walls
            foreach (Wall wall in Walls.Values)
            {
                // Wall coordinates - note the buffer on the wall was increased for the sake of not getting spawn-killed by a super close wall
                double wallLeftX = Math.Min(wall.P1!.X, wall.P2!.X) - 45;
                double wallRightX = Math.Max(wall.P1!.X, wall.P2!.X) + 45;
                double wallTopY = Math.Min(wall.P1!.Y, wall.P2!.Y) - 45;
                double wallBottomY = Math.Max(wall.P1!.Y, wall.P2!.Y) + 45;

                // Checks for rectangular intersection
                if (snakeLeftX < wallRightX && snakeRightX > wallLeftX &&
                    snakeTopY < wallBottomY && snakeBottomY > wallTopY)
                {
                    colliding = true;
                }
            }

            // Check collisions with snakes
            foreach (Snake opponent in Snakes.Values)
            {
                // Don't check collisions if the snake being checked against is dead
                if (!opponent.Alive)
                    continue;

                // Check against all snake segments
                for (int i = 0; i < opponent.BodyCoords!.Count - 1; i++)
                {
                    // Other snake body segment coordinates
                    // Note the buffer on the bounds was increased for the sake of not getting spawn-killed by a super close snake
                    double oppLeftX = Math.Min(opponent.BodyCoords[i].X, opponent.BodyCoords[i + 1].X) - 25;
                    double oppRightX = Math.Max(opponent.BodyCoords[i].X, opponent.BodyCoords[i + 1].X) + 25;
                    double oppTopY = Math.Min(opponent.BodyCoords[i].Y, opponent.BodyCoords[i + 1].Y) - 25;
                    double oppBottomY = Math.Max(opponent.BodyCoords[i].Y, opponent.BodyCoords[i + 1].Y) + 25;

                    //If there is an intersection...
                    if (snakeLeftX < oppRightX && snakeRightX > oppLeftX &&
                        snakeTopY < oppBottomY && snakeBottomY > oppTopY)
                    {
                        colliding = true;
                    }
                }
            }

        } while (colliding == true);

        //Return our body coordinates for a new snake
        return new List<Vector2D> { tailPoint, headPoint };
    }

    /// <summary>
    /// Helper method that checks if a snake has reached the edge of the world and should 
    /// be wrapped to the opposite side.
    /// </summary>
    public void CheckSnakeWraparound(int worldSize)
    {
        lock (Snakes)
        {
            foreach (Snake snake in Snakes.Values)
            {
                //Wrapping Horizontal
                if (Math.Abs(snake.BodyCoords![^1].X) >= worldSize/2)
                {
                    snake.BodyCoords!.Add(new Vector2D(snake.BodyCoords![^1].X * -1, snake.BodyCoords[^1].Y));
                    snake.BodyCoords!.Add(new Vector2D(snake.BodyCoords![^1].X, snake.BodyCoords[^1].Y));

                }

                //Wrapping Vertical
                if (Math.Abs(snake.BodyCoords![^1].Y) >= worldSize/2)
                {
                    snake.BodyCoords!.Add(new Vector2D(snake.BodyCoords![^1].X, snake.BodyCoords[^1].Y * -1));
                    snake.BodyCoords!.Add(new Vector2D(snake.BodyCoords![^1].X, snake.BodyCoords[^1].Y));
                }

            } 
        }
    }
}