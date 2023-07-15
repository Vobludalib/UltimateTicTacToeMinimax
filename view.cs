using Cairo;
using Gdk;
using Gtk;
using Color = Cairo.Color;
using static Gdk.EventMask;
using Timeout = GLib.Timeout;

delegate void NotifyMove();

// Window class that handles the application
class MyWindow : Gtk.Window {
    // If true, prints debugging statements to console
    bool debug = false;

    // Default start up constants ( also dictates minimum window size )
    const int defaultWindowSize = 650;
    const int defaultPaddingForArea = 20;

    // Game related fields ( note the 'difficulty' is simply changing search depth for the minimax player )
    int[] minimaxDepths = { 2, 4, 6 };
    GameArea area;
    Game game;
    IPlayer player;
    event NotifyMove moveMade;
    Label turnLabel; // This label is what gets displayed to show who's turn it is
    int minimaxDepth;
    bool playerPlaysFirst;
    const bool playerPlaysFirstOnStartup = true;
    enum difficulties { Easy, Intermediate, Hard }; 
    const difficulties defaultDifficulty = difficulties.Intermediate;
    difficulties difficulty;

    public MyWindow() : base( "Ultimate Tic Tac Toe" ) {
        int size = defaultWindowSize;
        int padding = defaultPaddingForArea;
        Resize(size, size);

        // Settings settings to default values
        difficulty = defaultDifficulty;
        minimaxDepth = minimaxDepths[ (int) difficulty];
        playerPlaysFirst = playerPlaysFirstOnStartup;
        game = new Game();
        player = new MinimaxPlayer( minimaxDepth );

        // Create a grid that is the top bar of the window
        Grid grid = new Grid();
        grid.ColumnSpacing = 100;
        grid.Hexpand = true; // Should expand horizontally, but not vertically
        grid.Vexpand = false;

        // Create the new game button and attach it to the grid
        Button newGameButton = new Button("New game");
        newGameButton.Halign = Align.Start;
        newGameButton.Clicked += handleNewGame;
        grid.Attach( newGameButton, 0, 0, 1, 1 );

        // Create the settings button and attach it to the grid
        Button settingsButton = new Button("");
        settingsButton.Halign = Align.End;
        Image settingsImage = new Image("settings.png");
        settingsButton.AlwaysShowImage = true;
        settingsButton.Image = settingsImage;
        settingsButton.Clicked += handleSettings;
        grid.Attach( settingsButton, 2, 0, 1, 1);

        // Create the turn label, get the correct initial value, attach it to the grid
        turnLabel = new Label("");
        handleMove();
        turnLabel.Halign = Align.Center;
        turnLabel.Hexpand = true;
        grid.Attach( turnLabel, 1, 0, 1, 1 );

        // Register handleMove as a handler for the moveMade event
        moveMade += handleMove;
        // Create the game area with the correct parameters for startup and dynamic sizing
        area = new GameArea( size, padding, game, player, moveMade );
        area.WidthRequest = size;
        area.HeightRequest = size;
        area.Hexpand = true;
        area.Vexpand = true;

        // Create a grid that positions the top bar, seperator and game area correctly
        Grid totalGrid = new Grid();
        totalGrid.Attach( grid, 0, 0, 1, 1 );
        totalGrid.Attach( new Separator( Orientation.Horizontal ), 0, 1, 1, 1 );
        totalGrid.Attach( area, 0, 2, 1, 1 );
        totalGrid.Expand = true;
        totalGrid.RowHomogeneous = false;
        // Place this grid into the window
        Add( totalGrid );

        // The onTimeout code is registered to be called every 0.5secs ( so that, even if briefly, the computer's turn takes artificially longer )
        Timeout.Add( 500, onTimeout );
    }

    // Called every 0.5s to check if it's the computer's turn, and if so, proceed accordingly
    bool onTimeout() {
        if ( area.inClickLockout && area.computerShouldMove ) {
            Move? move = player.move( game );
            if ( move is not null ) game.move( move );
            // Again, we need to call on all moveMade handlers to swap the turn label etc.
            if ( moveMade != null ) moveMade();
            area.inClickLockout = false;
            area.computerShouldMove = false;
            QueueDraw();
        }
        return true;
    }

    // This handles the resizing of the window
    protected override bool OnConfigureEvent(EventConfigure e) {
        // We first call the base onConfigureEvent to handle all the backend Gtk/Gdk stuff to make it work
        base.OnConfigureEvent(e);
        if ( debug ) {
            System.Console.WriteLine($"Window actual size is: {this.AllocatedWidth} x {this.AllocatedHeight}");
            System.Console.WriteLine($"Area actual size is: {area.AllocatedWidth} x {area.AllocatedHeight}");
        }
        // Then we call on the area to resize to the smaller of the two dimensions of the game area's allocated size
        area.handleNewSize( area.AllocatedHeight > area.AllocatedWidth ? area.AllocatedWidth : area.AllocatedHeight );
        return true;
    }

    // Handler for when a move is made, that changes the turn labels
    void handleMove() {
        if ( game.winner() is not null ) {
            int? winner = game.winner();
            string text;
            if ( winner == 0 ) {
                text = "Tie!";
            } else if ( ( playerPlaysFirst && winner == 1 ) || ( !playerPlaysFirst && winner == 2 ) ) {
                text = "You win!";
            } else {
                text = "Computer wins!";
            }
            turnLabel.Text = text;
        } 
        else if ( ( playerPlaysFirst && game.turn == 1 ) || ( !playerPlaysFirst && game.turn == 2 ) ) turnLabel.Text = "Your turn";
        else turnLabel.Text = "Computer's turn";
    }

    // Handler for when a new game is created, which creates a new game, sets turn trackers and creates a new player based on the new difficulty
    void handleNewGame(object? sender, EventArgs args) {
        System.Console.WriteLine("New Game");
        game = new Game();
        player = new MinimaxPlayer( minimaxDepth );
        if ( !playerPlaysFirst ) { area.computerShouldMove = true; area.inClickLockout = true; }
        else { area.inClickLockout = false; area.computerShouldMove = false; }
        handleMove();
        area.changeGame(game, player);
    }

    // Handler for when the settings button is pressed
    void handleSettings(object? sender, EventArgs args) {
        // Create a new popup window
        Gtk.Window popup = new Gtk.Window("Settings");
        popup.PresentWithTime(0);
        popup.Resize(400, 400);

        bool updatedPlayerFirst = playerPlaysFirst;
        difficulties updatedDifficulty = difficulty;

        // Create the grid that holds all our widgets
        Grid grid = new Grid();
        grid.RowSpacing = 10;
        grid.ColumnHomogeneous = false;
        grid.Expand = true;

        grid.Attach( new Label(" "), 1, 0, 1, 1 );

        Label startLabel = new Label("Who starts: (only applies when a new game starts)");
        startLabel.Hexpand = true;
        startLabel.Halign = Align.Center;
        grid.Attach( startLabel, 1, 1, 1, 1 );

        // Creates the radio buttons and corresponding handlers for selecting who plays first
        RadioButton humanFirst = new RadioButton("You");
        humanFirst.Halign = Align.Start;
        if (updatedPlayerFirst) humanFirst.Active = true;
        humanFirst.Clicked += handlePlayerFirst;
        grid.Attach( humanFirst, 0, 2, 1, 1 );

        void handlePlayerFirst(object? sender, EventArgs args) {
            updatedPlayerFirst = true;
        }

        RadioButton computerFirst = new RadioButton(humanFirst, "Computer");
        computerFirst.Halign = Align.End;
        if (!updatedPlayerFirst) computerFirst.Active = true;
        computerFirst.Clicked += handleComputerFirst;
        grid.Attach( computerFirst, 2, 2, 1, 1 );

        void handleComputerFirst(object? sender, EventArgs args) {
            updatedPlayerFirst = false;
        }

        // Creates the radio buttons and corresponding handlers for selecting a new difficulty
        Label difficultyLabel = new Label("Choose a difficulty:");
        difficultyLabel.Hexpand = true;
        difficultyLabel.Halign = Align.Center;
        grid.Attach( difficultyLabel, 1, 3, 1, 1 );

        RadioButton easyButton = new RadioButton("Easy");
        easyButton.Halign = Align.Start;
        if (updatedDifficulty == difficulties.Easy) easyButton.Active = true;
        easyButton.Clicked += handleEasy;
        grid.Attach( easyButton, 0, 4, 1, 1 );

        void handleEasy(object? sender, EventArgs args) {
            updatedDifficulty = difficulties.Easy;
        }

        RadioButton intermediateButton = new RadioButton(easyButton, "Intermediate");
        intermediateButton.Halign = Align.Center;
        if (updatedDifficulty == difficulties.Intermediate) intermediateButton.Active = true;
        intermediateButton.Clicked += handleIntermediate;
        grid.Attach( intermediateButton, 1, 4, 1, 1 );

        void handleIntermediate(object? sender, EventArgs args) {
            updatedDifficulty = difficulties.Intermediate;
        }

        RadioButton hardButton = new RadioButton(easyButton, "Hard");
        hardButton.Halign = Align.End;
        if (updatedDifficulty == difficulties.Hard) hardButton.Active = true;
        hardButton.Clicked += handleHard;
        grid.Attach( hardButton, 2, 4, 1, 1 );

        void handleHard(object? sender, EventArgs args) {
            updatedDifficulty = difficulties.Hard;
        }

        // Creates the grid for the cancel and confirm buttons
        Grid buttonGrid = new Grid();
        buttonGrid.Expand = true;

        // Creates the cancel and confirm buttons and appropriate handlers
        Button cancelButton = new Button("Cancel");
        cancelButton.Halign = Align.End;
        cancelButton.Clicked += handleCancel;
        buttonGrid.Attach(cancelButton, 0, 0, 1, 1);

        void handleCancel(object? sender, EventArgs args) {
            popup.Destroy();
        }

        Button confirmButton = new Button("Confirm");
        confirmButton.Halign = Align.Start;
        confirmButton.Clicked += handleConfirm;
        buttonGrid.Attach(confirmButton, 1, 0, 1, 1);

        void handleConfirm(object? sender, EventArgs args) {
            playerPlaysFirst = updatedPlayerFirst;
            difficulty = updatedDifficulty;
            player = new MinimaxPlayer( minimaxDepths[ (int) difficulty ] );
            popup.Destroy();
        }

        // Places these elements into a box
        Box box = new Box(Orientation.Vertical, 5);
        box.Add( grid );
        box.Add( new Separator(Orientation.Horizontal) );
        box.Add( buttonGrid );

        // Adds that box to a window, makes the window modal ( can't interact with any other window of the app ), shows it, and grabs focus
        popup.Add( box );
        popup.Modal = true;
        popup.ShowAll();
        popup.GrabFocus();
    }


    // Handles when the window manager signals to delete the window ( we quit the app )
    protected override bool OnDeleteEvent(Event ev) {
        Application.Quit();
        return true;
    }

    
}

// The drawing area that draws UTTT board
class GameArea : DrawingArea {
    bool debug = false; // Set to true to enable console outputs for drawing area

    Game game;
    IPlayer player;
    event NotifyMove moveMade;

    public int areaSize; //Stores the size of the width and length of the 'actual' area
    // This is needed, as the actual allocated area for the drawing area could be non-square
    int padding; // The padding from the sides of the 'actual' area for where to draw
    const int gridEntrySizePercentage = 70; // These constants specify the size and padding for elements inside each grid space
    const int innerGridPadding = 10;
    int individualGridEntryWidth; // Worked out based on size, padding, inner grip padding and gridEntrySizePercentage
    (int x, int y)[,] topLeftGrid; // 2D array that stores the actual pixel values of the top left of each grid square
    // e.g. the actual pixel value of the top left of the square at 0-based coordinates 2, 3 would be at topLeftGrid[2, 3]
    bool gridFilledOut; // Bool that stores whether or not the topLeftGrid has to be recomputed
    int horizontalOffset; // These offsets are used to offset from the actual pixel values of the allocated size, transforming 
    // higher-level functions to work as if the drawing area was always square
    int verticalOffset;

    ImageSurface canvas;
    Color red = new Color( 1, 0, 0 ),
    blue = new Color( 0, 0, 1 ),
    black = new Color( 0, 0, 0 ),
    white = new Color( 1, 1, 1 ),
    transparentHighlight = new Color( 253, 255, 0, 0.5 );
    bool wrongClick; // Used to store information about when to display notifications about invalid clicks
    public bool inClickLockout;
    public bool computerShouldMove; // If it's the computer's turn, this is set to true

    public GameArea( int size, int padding, Game game, IPlayer player, NotifyMove moveMadeEvent ) {
        canvas = new ImageSurface( Format.Rgb24, size, size );
        this.padding = padding;
        areaSize = size;

        this.game = game;
        this.player = player;

        using (Context c = new Context(canvas)) {
            c.SetSourceColor( white );
            c.Paint();
        }

        AddEvents( (int) ButtonPressMask ); // Listens for button press events
        wrongClick = false;
        inClickLockout = false;

        topLeftGrid = new (int,int)[ 9, 9 ];
        gridFilledOut = false;
        individualGridEntryWidth = ( ( areaSize - padding * 2 ) / 3 - innerGridPadding * 2 ) / 3;
        moveMade = moveMadeEvent;

        horizontalOffset = 0;
        verticalOffset = 0;
    }

    public void handleNewSize( int newSize ) { // Handles the calculation of vertical and horizontal offsets to give appearance of a square area
    // Called when the window is resized
        areaSize = newSize;
        horizontalOffset = (int) ( AllocatedWidth - areaSize ) / 2;
        verticalOffset = (int) ( AllocatedHeight - areaSize ) / 2;
        individualGridEntryWidth = ( ( areaSize - padding * 2 ) / 3 - innerGridPadding * 2 ) / 3;
        gridFilledOut = false;
        QueueDraw();
    }

    public void changeGame(Game game, IPlayer player) { // Handles changing the game state and type of player
    // Called when a new game is started
        this.game = game;
        this.player = player;
        QueueDraw();
    }

    // NOTE! The pixel coordinates in method parameters all take the 'adjusted' pixel values.
    // I.e. putting topLeftX = 100 will mean it will be drawn 100 + horizontalOffset

    void drawTicTacToeBoard( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, int stroke = 4, bool isSmallBoard = false, int smallBoardX = -1, int smallBoardY = -1 ) {
    // Handles drawing a tic tac board, with seperate handling for the small and large boards and recalculating the topLeftGrid when needed
        if ( debug ) System.Console.WriteLine($"Drawing board from {topLeftX + horizontalOffset}, {topLeftY + verticalOffset} with width/height {totalWidth}, and padding {paddingFromTopLeft}");
        c.SetSourceColor( black );
        c.LineWidth = stroke;
        
        int boardSizeWithPadding = totalWidth - ( paddingFromTopLeft * 2 );
        int[] divisions = { boardSizeWithPadding / 3 + paddingFromTopLeft, ( boardSizeWithPadding / 3 ) * 2 + paddingFromTopLeft };

        if ( isSmallBoard && !gridFilledOut ) { // Fill in topLeftGrid if needed
            for ( int x = 0; x < 3; ++x ) {
                for ( int y = 0; y < 3; ++y ) {
                    int xDivision = x > 0 ? divisions[ x - 1 ] - paddingFromTopLeft : 0;
                    int yDivision = y > 0 ? divisions[ y - 1 ] - paddingFromTopLeft : 0;
                    topLeftGrid[ smallBoardX * 3 + x, smallBoardY * 3 + y ] = (topLeftX + xDivision + horizontalOffset, topLeftY + yDivision + verticalOffset);
                    if ( debug ) System.Console.WriteLine($"Top left of cell at: {topLeftX + xDivision + horizontalOffset}, {topLeftY + yDivision + verticalOffset}");
                }
            }
        }

        for ( int i = 0; i < 2; ++i ) { // Draw vertical lines
            c.MoveTo( topLeftX + divisions[ i ] + horizontalOffset, paddingFromTopLeft + topLeftY + verticalOffset);
            c.LineTo( topLeftX + divisions[ i ] + horizontalOffset, paddingFromTopLeft + boardSizeWithPadding + topLeftY + verticalOffset);
            c.Stroke();
        }

        for ( int i = 0; i < 2; ++i ) { // Draw horizontal lines
            c.MoveTo( paddingFromTopLeft + topLeftX + horizontalOffset, topLeftY + divisions[ i ] + verticalOffset);
            c.LineTo( paddingFromTopLeft + boardSizeWithPadding + topLeftX + horizontalOffset, topLeftY + divisions[ i ] + verticalOffset);
            c.Stroke();
        }
    }

    void drawCross( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, Color color, int stroke = 2 ) {
        if (debug) System.Console.WriteLine($"Drawing cross from {topLeftX}, {topLeftY} with width/height {totalWidth}, and padding {paddingFromTopLeft}");
        c.SetSourceColor( color );
        c.LineWidth = stroke;

        c.MoveTo( topLeftX + paddingFromTopLeft, topLeftY + paddingFromTopLeft );
        c.LineTo( topLeftX + totalWidth + paddingFromTopLeft, topLeftY + totalWidth + paddingFromTopLeft );
        c.Stroke();

        c.MoveTo( topLeftX + totalWidth + paddingFromTopLeft, topLeftY + paddingFromTopLeft );
        c.LineTo( topLeftX + paddingFromTopLeft, topLeftY + totalWidth + paddingFromTopLeft );
        c.Stroke();
    }

    void drawCircle( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, Color color, int stroke = 2 ) {
        if (debug) System.Console.WriteLine($"Drawing circle from {topLeftX}, {topLeftY} with width/height {totalWidth}, and padding {paddingFromTopLeft}");
        c.SetSourceColor( color );
        c.LineWidth = stroke;

        c.Arc( xc: topLeftX + paddingFromTopLeft + totalWidth / 2, yc: topLeftY + paddingFromTopLeft + totalWidth / 2, radius: totalWidth / 2, angle1: 0.0, angle2: 2 * Math.PI );
        c.Stroke();
    }

    void showWrongClickText( Context c ) { // Shows warning text about wrong click in the top middle of the drawing area
        c.SetSourceColor( black );
        string s = "Invalid selection";
        TextExtents te = c.TextExtents( s ); 
        c.MoveTo(areaSize / 2 - (te.Width / 2 + te.XBearing),
               padding - 40 - (te.Height / 2 + te.YBearing));
        c.ShowText(s);
    }

    protected override bool OnDrawn( Context c ) { // Whenever QueueDraw() or Show() is called
        drawTicTacToeBoard( c, 0, 0, areaSize, padding ); // Draw big board
        int[] bigBoardDivisions = { padding, ( areaSize - padding * 2 ) / 3 + padding, ( areaSize - padding * 2 ) / 3 * 2 + padding };
        for ( int i = 0; i < 3; ++i ) { // Draw all the smaller boards
            for ( int j = 0; j < 3; ++j ) {
                drawTicTacToeBoard( c, bigBoardDivisions[ i ], bigBoardDivisions[ j ], ( areaSize - padding * 2 ) / 3, innerGridPadding, stroke:2, isSmallBoard:true, smallBoardX:i, smallBoardY:j );
            }
        }
        gridFilledOut = true;

        // Display all individual grid entries
        for ( int x = 0; x < 9; ++x ) {
            for ( int y = 0; y < 9; ++y ) {
                int player = game.bigGrid[ x / 3, y / 3 ].grid[ x % 3, y % 3 ];
                int entrySize = ( int ) ( individualGridEntryWidth * ( (float) gridEntrySizePercentage / 100 ));
                int entryPadding = ( individualGridEntryWidth - entrySize ) / 2;
                if ( player == 1 ) {
                    drawCircle( c, topLeftGrid[ x, y ].x + innerGridPadding, topLeftGrid[ x, y ].y + innerGridPadding, entrySize, entryPadding, blue );
                } else if ( player == 2 ) {
                    drawCross( c, topLeftGrid[ x, y ].x + innerGridPadding, topLeftGrid[ x, y ].y + innerGridPadding, entrySize, entryPadding, red );
                }
            }
        }

        // Cover over won big squares
        for ( int x = 0; x < 3; ++x ) {
            for ( int y = 0; y < 3; ++y ) {
                int? winner = game.bigGrid[ x, y ].winner();
                if (debug) System.Console.WriteLine($"Game at {x}, {y} has winner {winner}");
                Color color = black;
                if ( winner is null ) continue;
                else if ( winner == 1 ) {
                    color = blue;
                } else if ( winner == 2 ) {
                    color = red;
                }
                c.SetSourceColor( color );
                c.Rectangle( x: topLeftGrid[ x * 3, y * 3 ].x, y: topLeftGrid[ x * 3, y * 3 ].y, width: ( areaSize - padding * 2 ) / 3, height: ( areaSize - padding * 2 ) / 3 );
                c.Fill();
            }
        }

        // If the game has a winner, display it
        if ( game.winner() >= 0 ) {
            System.Console.WriteLine($"Game has winner {game.winner()}");
            inClickLockout = true;
            computerShouldMove = false;
            string s;
            if ( game.winner() > 0 ) {
                s = $"Winner is: {game.winner()}";
            } else {
                s = "Tie!";
            }
            moveMade();
            return true;
        }

        // Highlight all the legal moves of the current player on the board
        foreach ( Move m in game.possibleMoves() ) {
            c.SetSourceColor( transparentHighlight );
            int rectSize = ( int ) ( individualGridEntryWidth * ( (float) gridEntrySizePercentage / 100 ));
            int rectPadding = ( individualGridEntryWidth - rectSize ) / 2;
            c.Rectangle( x: topLeftGrid[ m.x, m.y ].x + innerGridPadding + rectPadding, y: topLeftGrid[ m.x, m.y ].y + innerGridPadding + rectPadding, width: rectSize, height: rectSize);
            c.Fill();
        }

        // If wrongClick is true, display the warning text
        if ( wrongClick ) {
            showWrongClickText( c );
        }

        return true;
    }

    // Called whenever the drawing area is pressed ( i.e. mouse click )
    protected override bool OnButtonPressEvent( EventButton e ) {
        if ( inClickLockout ) { return true; } // If we should be ignoring clicks, do nothing
        wrongClick = false;
        // Work out the grid coordinates of where the click occured
        int smallBoardWidth = ( areaSize - padding * 2 ) / 9;
        int gridX = (int) Math.Floor( ( e.X - padding - horizontalOffset ) / smallBoardWidth );
        int gridY = (int) Math.Floor( ( e.Y - padding - verticalOffset) / smallBoardWidth );
        if ( debug ) System.Console.WriteLine($"Clicked on board with coords { gridX }, { gridY }");
        // Handle a valid an invalid grid position accordingly
        if ( gridX > 8 || gridY > 8 || gridX < 0 || gridY < 0 ) wrongClick = true;
        else {
            bool result = game.move( new Move( gridX, gridY ), true );
            if ( !result ) wrongClick = true; else inClickLockout = true; computerShouldMove = true; if ( moveMade != null ) moveMade();
            // If a legal move was made by the player, call on all handlers of moveMade
        }
        QueueDraw();
        return true;
    }
}

// Driver code
class Program {
    static void Main() {
        Application.Init();
        MyWindow w = new MyWindow();
        w.ShowAll();
        Application.Run();
    }
}