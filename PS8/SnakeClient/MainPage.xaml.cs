using SnakeGameController;

// Authors: Daniel Kopta, Travis Martin (instructors)
//          Connor Blood, Caden Erickson
//
// Version history: 11/26/22 - Finalized for PS8
//

namespace SnakeGame;

/// <summary>
/// Code-behind file for MainPage.xaml
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly GameController controller;

    /// <summary>
    /// A constructor of sorts, initializes the graphics, and registers a number of events.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();
        graphicsView.Invalidate();

        //Sets up a controller object
        controller = new();

        //Register some event listeners.
        controller.SetupComplete += SetPanelWorld;
        controller.SetupComplete += worldPanel.EnableDrawing;
        controller.UpdateReceived += OnFrame;
        controller.ErrorOccurred += NetworkErrorHandler;
    }

    /// <summary>
    /// Sets the gameworld of the View using the controller's gameworld
    /// </summary>
    public void SetPanelWorld()
    {
        worldPanel.gameWorld = controller.GameWorld;
    }

    /// <summary>
    /// Use this method as an event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// Simple method that constantly refocuses to an entry box that controls movement
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    /// <summary>
    /// Handles whenever the text of the "movement" box changes -- controlls the actual player snake
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w")
        {
            controller.Move("up");
        }
        else if (text == "a")
        {
            controller.Move("left");
        }
        else if (text == "s")
        {
            controller.Move("down");
        }
        else if (text == "d")
        {
            controller.Move("right");
        }
        entry.Text = "";
    }

    /// <summary>
    /// Handles if a connection attempt fails -- allows the user to change their name and server, and retry a connection
    /// 
    /// Displays a warning of a failed connection.
    /// <paramref name="errorMsg">The error message to display in the DisplayAlert</paramref>
    /// </summary>
    private void NetworkErrorHandler(string errorMsg)
    {
        //Allow Reconnection
        Dispatcher.Dispatch(() =>
        {
            nameText.IsEnabled = true;
            serverText.IsEnabled = true;
            connectButton.IsEnabled = true;
            keyboardHack.IsEnabled = false;

            DisplayAlert("Error", errorMsg, "OK");
        });
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt logic here in the view, instead of the controller,
    /// because it is closely tied with disabling/enabling buttons, and showing dialogs.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }

        // set cursor to keyboard hack entry, gray out server and username entries
        keyboardHack.IsEnabled = true;
        keyboardHack.Focus();

        nameText.IsEnabled = false;
        serverText.IsEnabled = false;
        connectButton.IsEnabled = false;

        //Try connecting to server
        controller.ConnectToServer(serverText.Text, nameText.Text);
    }

    /// <summary>
    /// This is for the button that tells the user the controls of the game. Simply displays an alert with the controls.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    /// <summary>
    /// For when a user clicks the About button in the client. Simply displays some "about" information.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by Caden Erickson anď Connor Blood\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    /// <summary>
    /// Helper method that refocuses on the "movement" entry box if the "connect" button is disabled.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }
}