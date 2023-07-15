using static System.Console;
 
public interface IPlayer { // Interface representing what a player has to do
    public Move? move( Game gamestate );
}

public class Move { // Class representing a move on the game
    public int x, y;

    public Move( int x, int y ) {
        this.x = x;
        this.y = y;
    }

    public void print() {
        WriteLine( $"Made move {x}, {y}" );
    }
}

public class Game { // Class representing the game

    public class SmallGame { // Sub class that represents each small tic tac board
        public int[,] grid;
        internal int movesMade; // Keeps track of amount of moves to handle ties
        public int won; // Keeps track of which player has won this board, or if there is a tie

        public SmallGame() { 
            grid = new int[3,3];
            movesMade = 0;
            won = -1;
        }

        internal int? winner() { // Returns null if no winner, otherwise returns the winner (or 0 for a tie)
            if ( won == -1 ) {
                return null;
            }
            return won;
        }

        internal void updateWinner() { // Checks the current board state for a winner

            int winner = -1;
            bool winnerFound = false;

            // Check rows and columns
            for ( int i = 0 ; i < 3 ; ++i )
                if ( checkDirection( i, 0, 0, 1, out winner ) || checkDirection( 0, i, 1, 0, out winner ) ) {
                    if ( winner != 0 ) won = winner;
                    winnerFound = true;
                }
                    
            // Check diagonals
            if ( checkDirection( 0, 0, 1, 1, out winner ) || checkDirection( 2, 0, -1, 1, out winner ) ) {
                if ( winner != 0 ) won = winner;
                winnerFound = true;
            }

            if ( !winnerFound ) {
                if ( movesMade == 9 ) {
                    won = 0;
                    return;
                } else {
                    won = -1;
                }
            }

            return;
        }

        bool checkDirection( int x, int y, int dx, int dy, out int winner ) { // Helper method for updateWinner, which returns true if that specific direction is 'won'
            // the out parameter winner corresponds to 1 or 2 based on who won that direction
            winner = -1;
            int initPlayer = grid[ x, y ];

            if ( initPlayer == 0 ) {
                return false;
            }

            for ( int i = 0 ; i < 3 ; ++i ) 
                if ( grid[ x + i * dx, y + i * dy ] != initPlayer )
                    return false;
            winner = initPlayer;
            return true;
        }
    }

    // Grid of smallGames that represent the large board
    public SmallGame[,] bigGrid;
    int movesMade; // Again, needed here for tracking ties
    public int turn; // Tracker for which player's turn it is
    public int won; // Keeps track of the winner of the game
    Stack<Move> previousMoves; // Stack of previousMoves so that when making a move and then unmoving you keep track of previousMove
    // This is needed, as the next turn's allowed moves rely on the previous move
    Stack<bool> previousPlayAnywhere; // Boolean that stores if the player was sent to an already won square 
    bool playAnywhere; // Current state of if we were just sent to an already won square

    public Game() {
        // Creates the empty grid and sets inital values
        bigGrid = new SmallGame[3,3];
        for ( int i = 0; i < 3; ++i ) {
            for ( int j = 0; j < 3; ++j ) {
                bigGrid[ i, j ] = new SmallGame();
            }
        }
        movesMade = 0;
        turn = 1;
        won = -1;
        previousMoves = new Stack<Move>();
        previousPlayAnywhere = new Stack<bool>();
        playAnywhere = true;
    }

    public List<Move> possibleMoves() { //Generates a list of valid possible moves

        List<Move> returnList = new List<Move>();
        if ( playAnywhere ) { //If play anywhere we scan entire board
            for ( int x = 0; x < 9; ++x ) {
                for ( int y = 0; y < 9; ++y ) {
                    if ( bigGrid[ x/3, y/3 ].won == -1 && bigGrid[ x/3, y/3 ].grid[ x%3, y%3 ] == 0 ) {
                        returnList.Add( new Move( x, y ) );
                    }
                }
            }
        } else { //If not play anywhere, we only scan according to last made move
            if ( previousMoves.TryPop( out Move? previousMoveMade ) ) {
                if ( previousMoveMade is not null ) {
                    ( int smallGridX, int smallGridY ) nextLargeGridCoords = ( previousMoveMade.x % 3, previousMoveMade.y % 3 );

                    for ( int x = 0; x < 3; ++x ) {
                        for ( int y = 0; y < 3; ++y ) {
                            if ( bigGrid[ nextLargeGridCoords.smallGridX, nextLargeGridCoords.smallGridY ].grid[ x, y ] == 0 ) {
                                returnList.Add( new Move( nextLargeGridCoords.smallGridX * 3 + x, nextLargeGridCoords.smallGridY * 3 + y ) );
                            }
                        }
                    }

                    previousMoves.Push( previousMoveMade );
                }
            }
        }
        
        return returnList;   
    }

    public bool move( Move m, bool humanMove = false ) { // Method to make a function, returns false if its an invalid move
        if ( humanMove ) { //This is here so we only check through the list of possible legal moves on human turns, as the computer only will ever return legal moves
            bool foundEquivalentMove = false;
            foreach ( Move possibleMove in possibleMoves() ) { // Need to check this way, as I haven't implemented equivalance checking for Move class ( could be improved in future )
                if ( m.x == possibleMove.x && m.y == possibleMove.y ) {
                    foundEquivalentMove = true;
                    break;
                }
            }

            if ( !foundEquivalentMove ) return false;
        }

        // Sets the necessary values
        SmallGame smallGame = bigGrid[ m.x / 3, m.y / 3 ];
        smallGame.grid[ m.x % 3, m.y % 3 ] = turn;
        smallGame.movesMade += 1;
        smallGame.updateWinner();

        previousPlayAnywhere.Push( playAnywhere );
        previousMoves.Push( new Move( m.x, m.y ) );

        updateWinner();

        playAnywhere = bigGrid[ m.x % 3, m.y % 3 ].winner() is null ? false : true;
        turn = ( turn ) % 2 + 1;
        movesMade += 1;
        return true;
    }

    public bool unmove( Move m ) { //Unmove a certain move
        //This assumes that the move to unmove is the last-made move, otherwise problems are created
        SmallGame smallGame = bigGrid[ m.x / 3, m.y / 3 ];
        smallGame.grid[ m.x % 3, m.y % 3 ] = 0;
        smallGame.movesMade -= 1;
        smallGame.updateWinner();

        previousPlayAnywhere.TryPop( out playAnywhere );
        Move? dummy;
        previousMoves.TryPop( out dummy );

        updateWinner();
        
        turn = ( turn ) % 2 + 1;
        movesMade -= 1;
        return true;
    }

    public int? winner() { //Returns null if no winner, otherwise return ID of winner
        if ( won == -1 ) {
            return null;
        }
        return won;
    }

    void updateWinner() { // Method that updates the winner
        int sumOfFinishedGames = 0;
        for ( int x = 0; x < 3; ++x ) {
            for ( int y = 0; y < 3; ++y ) {
                if ( bigGrid[ x, y ].won != -1 ) ++sumOfFinishedGames;
            }
        }

        //Checking for ties
        if ( movesMade == 81 || sumOfFinishedGames == 9 ) {
            won = 0;
            return;
        }

        int winner = -1;
        bool winnerFound = false;

        // Check rows and columns
        for ( int i = 0 ; i < 3 ; ++i )
            if ( !winnerFound && ( checkDirection( i, 0, 0, 1, out winner ) || checkDirection( 0, i, 1, 0, out winner ) ) ) {
                if ( winner != 0 ) won = winner;
                winnerFound = true;
            }
                
        // Check diagonals
        if ( !winnerFound && ( checkDirection( 0, 0, 1, 1, out winner ) || checkDirection( 2, 0, -1, 1, out winner ) ) ) {
            if ( winner != 0 ) won = winner;
            winnerFound = true;
        }

        if ( !winnerFound ) won = -1;
        return;
    }

    bool checkDirection( int x, int y, int dx, int dy, out int winner ) { // Helper method, similar to smallGame
        winner = -1;
        int initPlayer = bigGrid[ x, y ].won;

        if ( initPlayer == 0 ) {
            return false;
        }

        for ( int i = 0 ; i < 3 ; ++i ) 
            if ( bigGrid[ x + i * dx, y + i * dy ].won != initPlayer )
                return false;
        winner = initPlayer;
        return true;
    }

    public void prettyPrint() { // Debugging console printing
        WriteLine( $"Printing game with {movesMade} moves made ");
        for ( int y = 0; y < 9; ++y ) {
            if ( y % 3 == 0 ) {
                WriteLine( "|---|---|---|" );
            }

            for ( int x = 0; x < 9; ++x ) {
                if ( x % 3 == 0 ) {
                    Write("|");
                }
                string outputString = bigGrid[ x/3, y/3 ].won != -1 ? bigGrid[ x/3, y/3 ].won.ToString() : bigGrid[ x/3, y/3 ].grid[ x%3, y%3 ].ToString();
                Write( outputString );
            }
            WriteLine();
        }
    }
}

public class MinimaxPlayer : IPlayer { // The implentation of the minimax agent

    int maxDepth; // Stores the maximum
    int[] outcomes = new int[] { 0, 1000, -1000 };

    public MinimaxPlayer( int maxDepth ) {
        this.maxDepth = maxDepth;
    }

    public Move? move( Game gamestate ) { //Given a gamestate returns who would win and what the optimal move to make is
        Move? returnMove = null;
        minimax( gamestate, 0, outcomes[2], outcomes[1], out returnMove );
        return returnMove;
    }

    int minimax( Game gamestate, int depth, int alpha, int beta, out Move? best ) { //Same, but this is where all the good stuff gets done
        best = null;
        if ( gamestate.won > -1 ) return outcomes[ gamestate.won ];
        
        if ( depth >= maxDepth ) return heuristicEval( gamestate );

        bool maximizing = gamestate.turn == 1;
        int currOptimal = maximizing ? int.MinValue : int.MaxValue;

        List<Move> bestMoves = new List<Move>();
        
        foreach ( Move move in gamestate.possibleMoves() ) {
            gamestate.move( move );
            int thisMovesMinimaxVal = minimax( gamestate, depth + 1, alpha, beta, out Move? _ );
            gamestate.unmove( move );
            if ( thisMovesMinimaxVal == currOptimal ) {
                bestMoves.Add( move );
            }
            if ( maximizing ? thisMovesMinimaxVal > currOptimal : thisMovesMinimaxVal < currOptimal ) {
                currOptimal = thisMovesMinimaxVal;
                bestMoves = new List<Move>();
                bestMoves.Add(move);
                best = move;
                if (maximizing) {
                    if ( currOptimal >= beta )
                        return currOptimal;
                    alpha = alpha > currOptimal ? alpha : currOptimal;
                } else {
                    if ( currOptimal <= alpha )
                        return currOptimal;
                    beta = beta < currOptimal ? beta : currOptimal;
                }
            }
        }

        int amountOfBestMoves = bestMoves.Count;
        var rand = new Random();
        best = bestMoves[rand.Next(0, amountOfBestMoves)];
        return currOptimal;
    }

    public int heuristicEval( Game gamestate ) { // Heuristic evaluation function
        // Works by adding emphasis on won big squares, then whoever has more placed in a small board
        int sum = 0;
        for ( int x = 0; x < 3; ++x ) {
            for ( int y = 0; y < 3; ++y ) {
                if ( gamestate.bigGrid[ x, y ].won != -1 ) {
                    if ( gamestate.bigGrid[ x, y ].won == 0 ) {
                        continue;
                    } else {
                        sum += -100 * ( ( gamestate.bigGrid[ x, y ].won * 2 ) - 3 );
                        continue;
                    }
                }

                int smallSum = 0;
                for ( int smallX = 0; smallX < 3; ++smallX ) {
                    for ( int smallY = 0; smallY < 3; ++smallY ) {
                        if ( gamestate.bigGrid[ x, y ].grid[ smallX, smallY ] >= 1 ) {
                            smallSum += -10 * ( ( gamestate.bigGrid[ x, y ].grid[ smallX, smallY ] * 2 ) - 3 );
                        }
                    }
                }
                sum += smallSum;
            }
        }

        // Required to make sure the heuristic will never beat out an actual winning move
        if ( sum >= 1000  ) sum = 999;
        else if ( sum <= -1000 ) sum = -999;
        return sum;
    }
}