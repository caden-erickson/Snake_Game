# CS 3500 Snake Game
Developed by Connor Blood and Caden Erickson  
Software Practice I  
University of Utah  
Fall 2022  

## Introduction  
We built this program as part of an assignment for our CS 3500- Software Practice I class, during the fall semester of 2022. It is a take on the classic 2D Snake game. As an exercise for the networking principles we were learning in the class, it supports multiplayer play.

For the GUI, we used Microsoft's (currently) brand new .NET MAUI framework. Since MAUI is so new, there is a lot still left to be desired, though hopefully as it's developed more, more features will become available, and current features will become more robust.
  
## PS9  
The PS9 assignment involved the design of only the server code.
  
### Design Choices  
**Use of MVC**  
One less-than-ideal aspect of this assignment is the merging of code between client and server; that is, both use the same class(es) for the Model. As such, there is client code that the server does not use, and server code that the client does not use, though all the Model code is contained in the same classes. This is a feature of the assignment, and is expected. This assignment merely served to teach the basics of MVC.  
We still maintained the server View and Controller separately. The server is only a console application, so the View is just the console. All of our controller code, handling interaction with and modification of the Model, was contained in a separate Controller class reserved specifically for the server.

**Loading Settings**  
We check for the settings.xml file (name is required that way) in two places-- first, in the same directory as the server executable, and second, in the project folder where the server .csproj file is located. If the file is not find in either location, the server discontinues starting and an error message is displayed to the console.

**Malformed Client Requests**  
If a client sends a movement request that is "malformed", or syntactically correct in any way, the server attempts to parse it. If the parse attempt fails, the server ignores the request, and reenters the event loop by clearing the SocketState buffer, re-calling GetData, and exiting the callback method.

**Handling Movement Restriction**  
In order to prevent the snake from doubling back on itself in such a way that would create overlap, we added some specific code to our method that handles movement requests from the server.

**Snake Self-Collision**  
We check a snake's collision with itself using the method given in class. We first check through all the body segments, starting at the head, looking for the first segment with a direction opposite to that of the head segment. In this stage, if the snake is in the process of wrapping to the other side of the world after crossing over the world border, the invisible "segment" spanning the entire world is ignored, as it does have the opposite direction as the head segment. Once the first valid point is found, all body segments between there and the tail are valid for collision detection, and we check them each against the head, using the same method we used to check snake collision with walls and other snakes. We treat each body segment as a rectangle, and the region at the head of the snake as a square, and check the alignment of their respective X and Y boundaries to see if the shapes overlap. If so, the server registers a self-collision, and the snake dies.
  
  
### Known Issues  
The main issue currently is self-collision during a wrap-around. Though we accounted for it in our self-collision detection method, it still does occasionally occur that a snake can collide with its invisible body segment if it's wrapping around the world. This seems to only happen in the half second or so after wraparound occurs. After that time has passed, even if the snake is still wrapping around the world, it can't collid with its invisible segment. The instructors' provided server doesn't address collision with the invisible body segment, so we chose to leave this be.
  
  
## PS8
The PS8 assignment involved the design of only the client code.
  
### Design Choices
**Use of MVC**  
As per the assignment requirements, we followed the program structure of MVC (Model, View, Client). All GUI interaction and manipulation was kept in the SnakeClient project, which contained all MAUI and XAML code. This was our View. We had a simple World class, which acted as a container for data structures of other simple element classes: Snake, Powerup, Wall, and Explosion, all of which stored relevant info as fields. This World class, and its contained elements, was our Model. Finally, we had a GameController class, which handled the interaction between the server, the Model, and the View. The Controller connected to the server, received info from the server, parsed it into objects, populated the Model with those objects, and invoked events to be handled by the View for when View elements should be redrawn, or if other things should be drawn (error messages, etc.).

**Dealing with retrying connections**  
If connection to a server failed, then the SocketState in the ConnectToServer callback method would have its ErrorOccurred flag set to true. If so, we invoked an ErroOccurred event, reset the SocketState, and immediately returned. The event was handled by a method in the GUI code-behind file, which reset the fields and buttons in the GUI to their original states, and displayed an error message that the connection had failed.  
An error message is also displayed if the server is closed while the game is running.

**Drawing Walls**  
We approached drawing the walls as we would drawing a grid. Each wall was a grid with either only one row, or only one column. The given reference points for the wall positions were points centered in the end 50x50 pixel sections of a given wall, so 25px from each side and 25px from the ends. Thus, the distance between the two reference points would be the desired length of the wall minus 50. We took the distance between the points, divided by 50, and added 1, to get the number of wall segments needed to be drawn in order to draw the wall. We did this for the distance between the X components and the Y components of the reference points, which would give us 1 in one direction and the correct number of wall segments in the other. We also used a DrawCenteredOnPoint method to draw the wall sprites with a 25px offset, in order to draw them centered on the desired points.

**Drawing Snakes**  
To draw the snakes, we simply drew lines with 10px strokes, connecting each segment in the snake objects' body coordinates arrays. Untouched, this would draw strokes with rectangular ends, so we had to specify the StrokeLineCap to be LineCap.Round, so that the joints, heads, and tails of the snakes would be round.

**Snake Wraparound**  
To avoid displaying a snake segment spanning the entire length of the world, we used an if-block checking if the sum of the absolute values of the pertinent X or Y coordinates (the ones that would be different on a wraparound), when added together, equaled or exceeded the size of the game world. If so, we skipped drawing that segment.

**Drawing Player Names/Scores**  
Drawing the names and scores of each player was very simple. We used MAUI's ICanvas DrawString method, concatenating the player name and score for each snake, and drawing the string at the coordinates of the head, plus 20px in the y-coordinate.

**Drawing Powerups**  
We used the same DrawCenteredOnPoint method we used for drawing the walls, but much more simply, to draw 2 concentric filled circles for the powerups, centered on the points given by the server.

**Drawing Explosions**  
The tricky aspect of explosions is that explosions were elements not given to us by the server. All snakes, walls, and powerups were communicated from the server on every frame, but explosions are entirely client-side. Since the server only communicates on one frame that a particular snake has died, the problem was left to be solved as to how to make an explosion animation that persisted after the snake's "died" flag had reset to "false". To do this, we created a List of explosion objects. When the client received a snake object with its "died" flag set to "true", it would add an Explosion to that List. An Explosion object represents a ring of 8 white circles. Each time the canvas was redrawn, the ring would get slightly bigger. Once the radius of an Explosion reached a certain maximum, it would be removed from the Explosions List.
  
### Known Issues
The main issue currently is regarding the Connect button. For now, if the connect button is clicked, it is grayed out and disabled. If subsuquent interaction causes the Connect button to need to be used again, it does become enabled and interactable again, but remains visually grayed out. This is a well-reported .NET MAUI bug (see [here](https://github.com/dotnet/maui/issues/7377)), and we chose to leave it as is and not bother trying to implement any clunky workarounds.

---
  
## Credits
Skeleton code for assignments, as well as working server, client, and AI client executables (for testing) and a working Networking library, were provided by Joe Zachary, Daniel Kopta, and Travis Martin (instructors)

## Version History
11/26/2022 - Version submitted for PS8 assignment  
12/08/2022 - Version submitted for PS9 assignment  
