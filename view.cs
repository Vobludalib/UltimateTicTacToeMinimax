using Cairo;
using Gdk;
using Gtk;
using System;
using Color = Cairo.Color;
using Key = Gdk.Key;
using static Gdk.EventMask;
using Timeout = GLib.Timeout;

class GameArea : DrawingArea {

    public Game game;
    public IPlayer player;

    int areaSize;
    int padding;

    ImageSurface canvas;
    Color red = new Color( 1, 0, 0 ),
    blue = new Color( 0, 0, 1 ),
    black = new Color( 0, 0, 0 ),
    white = new Color( 1, 1, 1 ),
    transparentHighlight = new Color( 253, 255, 0, 0.5 );
    bool wrongClick;
    public bool inClickLockout;
    public bool computerShouldMove;

    public GameArea( int size, int padding ) {
        canvas = new ImageSurface( Format.Rgb24, size, size );
        this.padding = padding;
        areaSize = size;

        using (Context c = new Context(canvas)) {
            c.SetSourceColor( white );
            c.Paint();
        }

        AddEvents( (int) ButtonPressMask );
        wrongClick = false;
        inClickLockout = false;

        game = new Game();
        player = new MinimaxPlayer( 4 ); //TO DO: Make this be based off selection in GUI
    }

    void drawTicTacToeBoard( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, int stroke = 4 ) {
        System.Console.WriteLine($"Drawing board from {topLeftX}, {topLeftY} with width/height {totalWidth}, and padding {paddingFromTopLeft}");
        c.SetSourceColor( black );
        c.LineWidth = stroke;
        
        int boardSizeWithPadding = totalWidth - ( paddingFromTopLeft * 2 );
        int[] divisions = { boardSizeWithPadding / 3 + paddingFromTopLeft, ( boardSizeWithPadding / 3 ) * 2 + paddingFromTopLeft };

        for ( int i = 0; i < 2; ++i ) {
            c.MoveTo( topLeftX + divisions[ i ] , paddingFromTopLeft + topLeftY );
            c.LineTo( topLeftX + divisions[ i ], paddingFromTopLeft + boardSizeWithPadding + topLeftY );
            c.Stroke();
        }

        for ( int i = 0; i < 2; ++i ) {
            c.MoveTo( paddingFromTopLeft + topLeftX, topLeftY + divisions[ i ] );
            c.LineTo( paddingFromTopLeft + boardSizeWithPadding + topLeftX, topLeftY + divisions[ i ] );
            c.Stroke();
        }
    }

    void drawCross( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, Color color, int stroke = 2 ) { //TO DO: Figure out how to make colour an optional parameter
        System.Console.WriteLine($"Drawing cross from {topLeftX}, {topLeftY} with width/height {totalWidth}, and padding {paddingFromTopLeft}");
        c.SetSourceColor( color );
        c.LineWidth = stroke;

        c.MoveTo( topLeftX + paddingFromTopLeft, topLeftY + paddingFromTopLeft );
        c.LineTo( topLeftX + totalWidth - paddingFromTopLeft, topLeftY + totalWidth - paddingFromTopLeft );
        c.Stroke();

        c.MoveTo( topLeftX + totalWidth - paddingFromTopLeft, topLeftY + paddingFromTopLeft );
        c.LineTo( topLeftX + paddingFromTopLeft, topLeftY + totalWidth - paddingFromTopLeft );
        c.Stroke();
    }

    void drawCircle( Context c, int topLeftX, int topLeftY, int totalWidth, int paddingFromTopLeft, Color color, int stroke = 2 ) { //TO DO: Same here
        System.Console.WriteLine($"Drawing circle from {topLeftX}, {topLeftY} with width/height {totalWidth}, and padding {paddingFromTopLeft}");
        c.SetSourceColor( color );
        c.LineWidth = stroke;

        c.Arc( xc: topLeftX + totalWidth / 2, yc: topLeftY + totalWidth / 2, radius: totalWidth / 2 - paddingFromTopLeft, angle1: 0.0, angle2: 2 * Math.PI );
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
                drawTicTacToeBoard( c, bigBoardDivisions[ i ], bigBoardDivisions[ j ], ( areaSize - padding * 2 ) / 3, 10, stroke:2 );
            }
        }

        //Add displaying of all individual grid entries
        for ( int x = 0; x < 9; ++x ) {
            for ( int y = 0; y < 9; ++y ) {
                int player = game.bigGrid[ x / 3, y / 3 ].grid[ x % 3, y % 3 ];
                if ( player == 1 ) {
                    drawCircle( c, padding + ( areaSize - padding * 2 ) / 9 * x, padding + ( areaSize - padding * 2 ) / 9 * y, ( areaSize - padding * 2 ) / 9, 10, blue );
                } else if ( player == 2 ) {
                    drawCross( c, padding + ( areaSize - padding * 2 ) / 9 * x, padding + ( areaSize - padding * 2 ) / 9 * y, ( areaSize - padding * 2 ) / 9, 10, red );
                }
            }
        }

        //Cover over won big squares
        for ( int x = 0; x < 3; ++x ) {
            for ( int y = 0; y < 3; ++y ) {
                int? winner = game.bigGrid[ x, y ].winner();
                System.Console.WriteLine($"Game at {x}, {y} has winner {winner}");
                Color color = black;
                if ( winner is null ) continue;
                else if ( winner == 1 ) {
                    color = blue;
                } else if ( winner == 2 ) {
                    color = red;
                }
                c.SetSourceColor( color );
                c.Rectangle( x: padding + ( areaSize - padding * 2 ) / 3 * x, y: padding + ( areaSize - padding * 2 ) / 3 * y, width: ( areaSize - padding * 2 ) / 3, height: ( areaSize - padding * 2 ) / 3 );
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
            c.Rectangle( x: padding + ( areaSize - padding * 2 ) / 9 * m.x + 10, y: padding + ( areaSize - padding * 2 ) / 9 * m.y + 10, width: ( areaSize - padding * 2 ) / 9 - 20, height: ( areaSize - padding * 2 ) / 9 - 20 );
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
        int gridX = (int) Math.Floor( ( e.X - padding ) / smallBoardWidth );
        int gridY = (int) Math.Floor( ( e.Y - padding ) / smallBoardWidth );
        System.Console.WriteLine($"Clicked on board with coords { gridX }, { gridY }");
        if ( gridX > 8 || gridY > 8 || gridX < 0 || gridY < 0 ) wrongClick = true;
        else {
            bool result = game.move( new Move( gridX, gridY ), true );
            if ( !result ) wrongClick = true; else inClickLockout = true; computerShouldMove = true;
        }
        QueueDraw();
        return true;
    }
}

class MyWindow : Gtk.Window {

    GameArea area;
    public MyWindow() : base( "Ultimate Tic Tac Toe" ) {
        int size = 700;
        int padding = 95;
        Resize(size, size);
        area = new GameArea( size, padding );
        Add( area );
        Timeout.Add( 500, onTimeout );
    }

    bool onTimeout() {
        if ( area.inClickLockout && area.computerShouldMove ) {
            Move? move = area.player.move( area.game );
            if ( move is not null ) area.game.move( move );
            area.inClickLockout = false;
            area.computerShouldMove = false;
            QueueDraw();
        }
        return true;
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