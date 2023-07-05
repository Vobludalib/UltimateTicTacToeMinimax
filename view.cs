using Cairo;
using Gdk;
using Gtk;
using System;
using Color = Cairo.Color;
using Key = Gdk.Key;
using static Gdk.EventMask;
using Timeout = GLib.Timeout;

delegate void NotifyMove();

class GameArea : DrawingArea {
    bool debug = false;

    Game game;
    IPlayer player;
    event NotifyMove moveMade;

    public int areaSize;
    int padding;
    const int gridEntrySizePercentage = 70;
    const int innerGridPadding = 10;
    int individualGridEntryWidth;
    (int x, int y)[,] topLeftGrid;
    bool gridFilledOut;
    int horizontalOffset;
    int verticalOffset;

    ImageSurface canvas;
    Color red = new Color( 1, 0, 0 ),
    blue = new Color( 0, 0, 1 ),
    black = new Color( 0, 0, 0 ),
    white = new Color( 1, 1, 1 ),
    transparentHighlight = new Color( 253, 255, 0, 0.5 );
    bool wrongClick;
    public bool inClickLockout;
    public bool computerShouldMove;

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

        AddEvents( (int) ButtonPressMask );
        wrongClick = false;
        inClickLockout = false;

        topLeftGrid = new (int,int)[ 9, 9 ];
        gridFilledOut = false;
        individualGridEntryWidth = ( ( areaSize - padding * 2 ) / 3 - innerGridPadding * 2 ) / 3;
        moveMade = moveMadeEvent;

        horizontalOffset = 0;
        verticalOffset = 0;
    }

    public void handleNewSize( int newSize ) {
        areaSize = newSize;
        horizontalOffset = (int) ( AllocatedWidth - areaSize ) / 2;
        verticalOffset = (int) ( AllocatedHeight - areaSize ) / 2;
        individualGridEntryWidth = ( ( areaSize - padding * 2 ) / 3 - innerGridPadding * 2 ) / 3;
        gridFilledOut = false;
        QueueDraw();
    }

    public void changeGame(Game game, IPlayer player) {
        this.game = game;
        this.player = player;
        QueueDraw();
    }

    void drawTicTacToeBoard( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, int stroke = 4, bool isSmallBoard = false, int smallBoardX = -1, int smallBoardY = -1 ) {
        if ( debug ) System.Console.WriteLine($"Drawing board from {topLeftX + horizontalOffset}, {topLeftY + verticalOffset} with width/height {totalWidth}, and padding {paddingFromTopLeft}");
        c.SetSourceColor( black );
        c.LineWidth = stroke;
        
        int boardSizeWithPadding = totalWidth - ( paddingFromTopLeft * 2 );
        int[] divisions = { boardSizeWithPadding / 3 + paddingFromTopLeft, ( boardSizeWithPadding / 3 ) * 2 + paddingFromTopLeft };

        if ( isSmallBoard && !gridFilledOut ) {
            for ( int x = 0; x < 3; ++x ) {
                for ( int y = 0; y < 3; ++y ) {
                    int xDivision = x > 0 ? divisions[ x - 1 ] - paddingFromTopLeft : 0;
                    int yDivision = y > 0 ? divisions[ y - 1 ] - paddingFromTopLeft : 0;
                    topLeftGrid[ smallBoardX * 3 + x, smallBoardY * 3 + y ] = (topLeftX + xDivision + horizontalOffset, topLeftY + yDivision + verticalOffset);
                    if ( debug ) System.Console.WriteLine($"Top left of cell at: {topLeftX + xDivision + horizontalOffset}, {topLeftY + yDivision + verticalOffset}");
                }
            }
        }

        for ( int i = 0; i < 2; ++i ) {
            c.MoveTo( topLeftX + divisions[ i ] + horizontalOffset, paddingFromTopLeft + topLeftY + verticalOffset);
            c.LineTo( topLeftX + divisions[ i ] + horizontalOffset, paddingFromTopLeft + boardSizeWithPadding + topLeftY + verticalOffset);
            c.Stroke();
        }

        for ( int i = 0; i < 2; ++i ) {
            c.MoveTo( paddingFromTopLeft + topLeftX + horizontalOffset, topLeftY + divisions[ i ] + verticalOffset);
            c.LineTo( paddingFromTopLeft + boardSizeWithPadding + topLeftX + horizontalOffset, topLeftY + divisions[ i ] + verticalOffset);
            c.Stroke();
        }
    }

    void drawCross( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, Color color, int stroke = 2 ) { //TO DO: Figure out how to make colour an optional parameter
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

    void drawCircle( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, Color color, int stroke = 2 ) { //TO DO: Same here
        if (debug) System.Console.WriteLine($"Drawing circle from {topLeftX}, {topLeftY} with width/height {totalWidth}, and padding {paddingFromTopLeft}");
        c.SetSourceColor( color );
        c.LineWidth = stroke;

        c.Arc( xc: topLeftX + paddingFromTopLeft + totalWidth / 2, yc: topLeftY + paddingFromTopLeft + totalWidth / 2, radius: totalWidth / 2, angle1: 0.0, angle2: 2 * Math.PI );
        c.Stroke();
    }

    void showWrongClickText( Context c ) {
        c.SetSourceColor( black );
        string s = "Invalid selection";
        TextExtents te = c.TextExtents( s ); 
        c.MoveTo(areaSize / 2 - (te.Width / 2 + te.XBearing),
               padding - 40 - (te.Height / 2 + te.YBearing));
        c.ShowText(s);
    }

    protected override bool OnDrawn( Context c ) {
        drawTicTacToeBoard( c, 0, 0, areaSize, padding );
        int[] bigBoardDivisions = { padding, ( areaSize - padding * 2 ) / 3 + padding, ( areaSize - padding * 2 ) / 3 * 2 + padding };
        for ( int i = 0; i < 3; ++i ) {
            for ( int j = 0; j < 3; ++j ) {
                drawTicTacToeBoard( c, bigBoardDivisions[ i ], bigBoardDivisions[ j ], ( areaSize - padding * 2 ) / 3, innerGridPadding, stroke:2, isSmallBoard:true, smallBoardX:i, smallBoardY:j );
            }
        }
        gridFilledOut = true;

        //Add displaying of all individual grid entries
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

        //Cover over won big squares
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
            c.SetSourceColor( black );
            TextExtents te = c.TextExtents( s ); 
            c.MoveTo(areaSize / 2 - (te.Width / 2 + te.XBearing),
                padding - 40 - (te.Height / 2 + te.YBearing));
            c.ShowText(s);
            return true;
        }

        foreach ( Move m in game.possibleMoves() ) {
            c.SetSourceColor( transparentHighlight );
            int rectSize = ( int ) ( individualGridEntryWidth * ( (float) gridEntrySizePercentage / 100 ));
            int rectPadding = ( individualGridEntryWidth - rectSize ) / 2;
            c.Rectangle( x: topLeftGrid[ m.x, m.y ].x + innerGridPadding + rectPadding, y: topLeftGrid[ m.x, m.y ].y + innerGridPadding + rectPadding, width: rectSize, height: rectSize);
            c.Fill();
        }

        if ( wrongClick ) {
            showWrongClickText( c );
        }
        return true;
    }

    protected override bool OnButtonPressEvent( EventButton e ) {
        if ( inClickLockout ) { return true; }
        wrongClick = false;
        int smallBoardWidth = ( areaSize - padding * 2 ) / 9;
        int gridX = (int) Math.Floor( ( e.X - padding - horizontalOffset ) / smallBoardWidth );
        int gridY = (int) Math.Floor( ( e.Y - padding - verticalOffset) / smallBoardWidth );
        if ( debug ) System.Console.WriteLine($"Clicked on board with coords { gridX }, { gridY }");
        if ( gridX > 8 || gridY > 8 || gridX < 0 || gridY < 0 ) wrongClick = true;
        else {
            bool result = game.move( new Move( gridX, gridY ), true );
            if ( !result ) wrongClick = true; else inClickLockout = true; computerShouldMove = true; if ( moveMade != null ) moveMade();
        }
        QueueDraw();
        return true;
    }
}

class MyWindow : Gtk.Window {
    const int defaultWindowSize = 650;
    const int defaultPaddingForArea = 20;
    int currAreaSize;
    int currAreaPadding;

    int[] minimaxDepths = { 2, 4, 6 };
    GameArea area;
    Game game;
    IPlayer player;
    event NotifyMove moveMade;
    Label turnLabel;
    int minimaxDepth;
    bool playerPlaysFirst;
    int difficulty;

    public MyWindow() : base( "Ultimate Tic Tac Toe" ) {
        int size = defaultWindowSize;
        int padding = defaultPaddingForArea;
        currAreaSize = size;
        currAreaPadding = padding;
        Resize(size, size);

        difficulty = 0;
        minimaxDepth = minimaxDepths[difficulty];
        playerPlaysFirst = true;
        game = new Game();
        player = new MinimaxPlayer( minimaxDepth );

        Grid grid = new Grid();
        grid.ColumnSpacing = 100;
        grid.Hexpand = true;
        grid.Vexpand = false;

        Button newGameButton = new Button("New game");
        newGameButton.Halign = Align.Start;
        newGameButton.Clicked += handleNewGame;
        grid.Attach( newGameButton, 0, 0, 1, 1 );

        Button settingsButton = new Button("");
        settingsButton.Halign = Align.End;
        Image settingsImage = new Image("settings.png");
        settingsButton.AlwaysShowImage = true;
        settingsButton.Image = settingsImage;
        settingsButton.Clicked += handleSettings;
        grid.Attach( settingsButton, 2, 0, 1, 1);

        turnLabel = new Label("");
        handleMove();
        turnLabel.Halign = Align.Center;
        turnLabel.Hexpand = true;
        grid.Attach( turnLabel, 1, 0, 1, 1 );

        moveMade += handleMove;
        area = new GameArea( size, padding, game, player, moveMade );
        area.WidthRequest = size;
        area.HeightRequest = size;
        area.Hexpand = true;
        area.Vexpand = true;

        Box totalBox = new Box(Orientation.Vertical, 5);
        Grid totalGrid = new Grid();
        totalGrid.Attach( grid, 0, 0, 1, 1 );
        totalGrid.Attach( new Separator( Orientation.Horizontal ), 0, 1, 1, 1 );
        totalGrid.Attach( area, 0, 2, 1, 1 );
        totalGrid.Expand = true;
        totalGrid.RowHomogeneous = false;
        Add( totalGrid );

        Timeout.Add( 500, onTimeout );
    }

    bool onTimeout() {
        if ( area.inClickLockout && area.computerShouldMove ) {
            Move? move = player.move( game );
            if ( move is not null ) game.move( move );
            if ( moveMade != null ) moveMade();
            area.inClickLockout = false;
            area.computerShouldMove = false;
            QueueDraw();
        }
        return true;
    }

    protected override bool OnConfigureEvent(EventConfigure e)
    {
        base.OnConfigureEvent(e);
        currAreaSize = area.AllocatedHeight > area.AllocatedWidth ? area.AllocatedWidth : area.AllocatedHeight;
        System.Console.WriteLine($"Window actual size is: {this.AllocatedWidth} x {this.AllocatedHeight}");
        System.Console.WriteLine($"Area actual size is: {area.AllocatedWidth} x {area.AllocatedHeight}");
        area.handleNewSize(currAreaSize);
        return true;
    }

    void handleMove() {
        if ( ( playerPlaysFirst && game.turn == 1 ) || ( !playerPlaysFirst && game.turn == 2 ) ) turnLabel.Text = "Your turn";
        else turnLabel.Text = "Computer's turn";
    }

    void handleNewGame(object? sender, EventArgs args) {
        System.Console.WriteLine("New Game");
        game = new Game();
        player = new MinimaxPlayer( minimaxDepth );
        if ( !playerPlaysFirst ) { area.computerShouldMove = true; area.inClickLockout = true; }
        else { area.inClickLockout = false; area.computerShouldMove = false; }
        handleMove();
        area.changeGame(game, player);
    }

    void handleSettings(object? sender, EventArgs args) {
        Gtk.Window popup = new Gtk.Window("Settings");
        popup.Resize(400, 400);

        bool updatedPlayerFirst = playerPlaysFirst;
        int updatedDifficulty = difficulty;

        Grid grid = new Grid();
        grid.RowSpacing = 10;
        grid.ColumnHomogeneous = false;
        grid.Expand = true;

        grid.Attach( new Label(" "), 1, 0, 1, 1 );

        Label startLabel = new Label("Who starts:");
        startLabel.Hexpand = true;
        startLabel.Halign = Align.Center;
        grid.Attach( startLabel, 1, 1, 1, 1 );

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

        Label difficultyLabel = new Label("Choose a difficulty:");
        difficultyLabel.Hexpand = true;
        difficultyLabel.Halign = Align.Center;
        grid.Attach( difficultyLabel, 1, 3, 1, 1 );

        RadioButton easyButton = new RadioButton("Easy");
        easyButton.Halign = Align.Start;
        if (updatedDifficulty == 0) easyButton.Active = true;
        easyButton.Clicked += handleEasy;
        grid.Attach( easyButton, 0, 4, 1, 1 );

        void handleEasy(object? sender, EventArgs args) {
            updatedDifficulty = 0;
        }

        RadioButton intermediateButton = new RadioButton(easyButton, "Intermediate");
        intermediateButton.Halign = Align.Center;
        if (updatedDifficulty == 1) intermediateButton.Active = true;
        intermediateButton.Clicked += handleIntermediate;
        grid.Attach( intermediateButton, 1, 4, 1, 1 );

        void handleIntermediate(object? sender, EventArgs args) {
            updatedDifficulty = 1;
        }

        RadioButton hardButton = new RadioButton(easyButton, "Hard");
        hardButton.Halign = Align.End;
        if (updatedDifficulty == 2) hardButton.Active = true;
        hardButton.Clicked += handleHard;
        grid.Attach( hardButton, 2, 4, 1, 1 );

        void handleHard(object? sender, EventArgs args) {
            updatedDifficulty = 2;
        }

        Grid buttonGrid = new Grid();
        buttonGrid.Expand = true;

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
            popup.Destroy();
        }

        Box box = new Box(Orientation.Vertical, 5);
        box.Add( grid );
        box.Add( new Separator(Orientation.Horizontal) );
        box.Add( buttonGrid );

        popup.Add( box );
        popup.Modal = true;
        popup.ShowAll();
        popup.GrabFocus();
    }


    
    protected override bool OnDeleteEvent(Event ev) {
        Application.Quit();
        return true;
     }
}

class Hello {
    static void Main() {
        Application.Init();
        MyWindow w = new MyWindow();
        w.ShowAll();
        Application.Run();
    }
}