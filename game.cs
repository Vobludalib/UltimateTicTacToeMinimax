using static System.Console;
 
public interface IPlayer {
    public Move? move( Game gamestate );
}

public class Move {
    public int x, y;

    public Move( int x, int y ) {
        this.x = x;
        this.y = y;
    }

    public void print() {
        WriteLine( $"Made move {x}, {y}" );
    }
}

public class Game {

    public class SmallGame {
        public int[,] grid;
        internal int movesMade;
        public int won;

        public SmallGame() {
            grid = new int[3,3];
            movesMade = 0;
            won = -1;
        }

        internal int? winner() {
            //RETURNS NULL IF NO WINNER, OTHERWISE ID OF PLAYER WHO WON
            if ( won == -1 ) {
                return null;
            }
            return won;
        }

        internal void updateWinner() {

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

        bool checkDirection( int x, int y, int dx, int dy, out int winner ) {
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

    public SmallGame[,] bigGrid;
    int movesMade;
    public int turn;
    public int won;
    Stack<Move> previousMoves;
    Stack<bool> previousPlayAnywhere;
    bool playAnywhere;

    public Game() {
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

    public List<Move> possibleMoves() {
        //Generates a list of valid possible moves

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

    public bool move( Move m, bool humanMove = false ) {
        //MAKE A MOVE - RETURN FALSE IF NOT A LEGAL MOVE
        if ( humanMove ) { //This is here so we only check through the list of possible legal moves on human turns, as the computer only will ever return legal moves
            bool foundEquivalentMove = false;
            foreach ( Move possibleMove in possibleMoves() ) {
                if ( m.x == possibleMove.x && m.y == possibleMove.y ) {
                    foundEquivalentMove = true;
                    break;
                }
            }

            if ( !foundEquivalentMove ) return false;
        }

        if ( bigGrid[ m.x / 3, m.y / 3 ].winner() is not null || bigGrid[ m.x / 3, m.y / 3 ].grid[ m.x % 3, m.y % 3 ] != 0 ) {
            if ( previousMoves.TryPop( out Move? previousMoveMade ) ) {
                if ( previousMoveMade != new Move( -1, -1 ) || ( m.x / 3 != previousMoveMade.x % 3 && m.y / 3 != previousMoveMade.y % 3 ) ) {
                    return false;
                }
                previousMoves.Push( previousMoveMade );
            }
        }

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

    public bool unmove( Move m ) {
        //UNMOVE A CERTAIN MOVE - IF NOT LEGAL, RETURN FALSE
        //THIS ASSUMES THAT THE MOVE TO UNMOVE WAS THE LAST MADE MOVE, IF NOT THIS WILL CREATE PROBLEMS
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

    public int? winner() { //TO DO: REFACTOR WINNER CHECKS TO WORK FOR BOTH SMALL AND BIG GAME - MAKE IT SO THAT IT ALSO CHECKS FOR TIES
        //RETURNS NULL IF NO WINNER, OTHERWISE ID OF PLAYER WHO WON
        if ( won == -1 ) {
            return null;
        }
        return won;
    }

    public int heuristicEval() {
        int sum = 0;
        for ( int x = 0; x < 3; ++x ) {
            for ( int y = 0; y < 3; ++y ) {
                if ( bigGrid[ x, y ].won != -1 ) {
                    if ( bigGrid[ x, y ].won == 0 ) {
                        continue;
                    } else {
                        sum += -100 * ( ( bigGrid[ x, y ].won * 2 ) - 3 );
                        continue;
                    }
                }

                int smallSum = 0;
                for ( int smallX = 0; smallX < 3; ++smallX ) {
                    for ( int smallY = 0; smallY < 3; ++smallY ) {
                        if ( bigGrid[ x, y ].grid[ smallX, smallY ] >= 1 ) {
                            smallSum += -10 * ( ( bigGrid[ x, y ].grid[ smallX, smallY ] * 2 ) - 3 );
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

    void updateWinner() {
        int sumOfFinishedGames = 0;
        for ( int x = 0; x < 3; ++x ) {
            for ( int y = 0; y < 3; ++y ) {
                if ( bigGrid[ x, y ].won != -1 ) ++sumOfFinishedGames;
            }
        }

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

    bool checkDirection( int x, int y, int dx, int dy, out int winner ) {
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

    public void prettyPrint() {
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

public class MinimaxPlayer : IPlayer {

    int maxDepth;
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
        
        if ( depth >= maxDepth ) return gamestate.heuristicEval();

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
}