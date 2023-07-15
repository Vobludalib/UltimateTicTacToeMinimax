### Charles University Programming II Semester Project - Ultimate Tic Tac Toe Minimax Player

##### Šimon Libřický

# User documentation:
To run this project, either run an executable directly ( UPDATE WHEN BUILT ) or build the project yourself by navigating into the cloned repository directory and run the following command:

`dotnet run`

### Rules of the game
[Explanation of the rules](https://www.instructables.com/The-Beautiful-Game-of-Ultimate-Tic-Tac-Toe/)
***!!!** Change of rules for this application:*
When a player gets sent an already won square, that player can then play in any open square on the global board. This differs from the explanation in the link

### Quick start
When you launch the application, a 'default' game will be open at the start ( you play first with a Medium difficulty CPU ).

If you wish to change these settings, click the settings button in the top right of the window; this will bring up a window with options. Press Confirm to save these changes.

*Please note, to actually reflect the settings changes fully, you will be required to start a new game, by pressing the button in the top left of the window.*

**The first player to act has Blue O's; the second player has Red X's.**

### Playing the game
When it is your turn ( as per the label in the top middle of the window ), you can click on any highlighted square to play in that board space.

The game goes back and forth between you and the computer till someone wins or a tie occurs.

### Settings explained
1. The first option in the settings popup is who has the first move.
You can choose between you and the computer starting.

These changes are not reflected in any currently running game, they are only reflected when a new game is started.

2. The second option is the difficulty of the computer player. You can choose between easy, intermediate, and hard.

These changes **are** reflected immediately when the settings popup is confirmed. 

### Resizing the window

This app *should* ( with a few asterisks ) support dynamic resizing ( tested on Mac and Windows ).

I have not added supported for fullscreening the application ( see the Dev Documentation for full reasoning ), but to some extent it *might* work.

### UNRESPONSIVE WINDOW?
Check that somewhere in the background the settings popup is not active. The settings popup takes control of the app and doesn't allow interaction with the main window when it is active.

### Pre-built installation
INSERT HERE WHEN DONE

### Dev Documentation
See [devdocs.md](./devdocs.md)