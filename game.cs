﻿using static System.Console;

class Program {
    
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

        class SmallGame {
            internal int[,] grid;
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

        SmallGame[,] bigGrid;
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

        public bool move( Move m ) {
            //MAKE A MOVE - RETURN FALSE IF NOT A LEGAL MOVE
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

        double? winner() { //TO DO: REFACTOR WINNER CHECKS TO WORK FOR BOTH SMALL AND BIG GAME
            //RETURNS NULL IF NO WINNER, OTHERWISE ID OF PLAYER WHO WON
            if ( won == -1 ) {
                return null;
            }
            return (double) won * 1000;
        }

        public double heuristicEval() { //TO DO: IMPROVE
            double sum = 0.0;
            for ( int x = 0; x < 3; ++x ) {
                for ( int y = 0; y < 3; ++y ) {
                    if ( bigGrid[ x, y ].won != -1 ) {
                        if ( bigGrid[ x, y ].won == 0 ) {
                            continue;
                        } else {
                            sum += -1000 * ( ( bigGrid[ x, y ].won * 2 ) - 3 );
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
                if ( checkDirection( i, 0, 0, 1, out winner ) || checkDirection( 0, i, 1, 0, out winner ) ) {
                    if ( winner != 0 ) won = winner;
                    winnerFound = true;
                }
                    
            // Check diagonals
            if ( checkDirection( 0, 0, 1, 1, out winner ) || checkDirection( 2, 0, -1, 1, out winner ) ) {
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
        double[] outcomes = new double[] { 0.0, 1000.0, -1000.0 };

        public MinimaxPlayer( int maxDepth ) {
            this.maxDepth = maxDepth;
        }

        public Move? move( Game gamestate ) { //Given a gamestate returns who would win and what the optimal move to make is
            Move? returnMove = null;
            minimax( gamestate, 0, out returnMove );
            return returnMove;
        }

        double minimax( Game gamestate, int depth, out Move? best ) { //Same, but this is where all the good stuff gets done
            best = null;
            if ( gamestate.won > -1 ) return outcomes[ gamestate.won ];
            
            if ( depth >= maxDepth ) return gamestate.heuristicEval();

            bool maximizing = gamestate.turn == 1;
            double currOptimal = maximizing ? int.MinValue : int.MaxValue;
            
            foreach ( Move move in gamestate.possibleMoves() ) {
                gamestate.move( move );
                double thisMovesMinimaxVal = minimax( gamestate, depth + 1, out Move? _ );
                gamestate.unmove( move );
                if ( maximizing ? thisMovesMinimaxVal > currOptimal : thisMovesMinimaxVal < currOptimal ) {
                    currOptimal = thisMovesMinimaxVal;
                    best = move;
                }
            }

            return currOptimal;
        }
    }

    // public static void Main() {
    //     Game game = new Game();
    //     IPlayer player = new MinimaxPlayer(4);

    //     while ( game.won == -1 ) { //TO DO: CHECK IF IT PROPERLY CHECKS WIN CONDITIONS
    //         if ( game.turn == 1 ) {
    //             WriteLine( "Enter your turn in the form: x y" );
    //             string[] input = ReadLine()!.Split();
    //             int x = int.Parse(input[0]);
    //             int y = int.Parse(input[1]);
    //             Move move = new Move( x, y );
    //             game.move( move );
    //             move.print();
    //             game.prettyPrint();
    //         } else {
    //             Move? move = player.move( game );
    //             if ( move is not null ) {
    //                 game.move( move );
    //                 move.print();
    //                 game.prettyPrint();
    //             }
    //         }
    //     }
    // }

}