# List of manual tests

### Launching the app :heavy_check_mark:

When the app is launched through running `dotnet run`, the app should launch without crashing or hanging

Verified: Simon Libricky

`Windows 10 10.0.19045`<br>
`64bit WindowsPE AMD64 Family 23 Model 113 Stepping 0, AuthenticAMD`<br>
`2023-07-16 19:42:13.749681`

### Settings popup appears and dissapears :heavy_check_mark:

The settings button should make a popup appear. This popup should only go away when the window's X is pressed, or Cancel/Confirm are clicked

Verified: Simon Libricky

`Windows 10 10.0.19045`<br>
`64bit WindowsPE AMD64 Family 23 Model 113 Stepping 0, AuthenticAMD`<br>
`2023-07-16 19:47:57.720424`

### Changing the settings persists :heave_check_mark:

When the settings are changed and Confirm is pressed, these settings remain when the setting are re-opened. These changes do not persist when the X or Cancel is pressed.

Verified: Simon Libricky

`Windows 10 10.0.19045`<br>
`64bit WindowsPE AMD64 Family 23 Model 113 Stepping 0, AuthenticAMD`<br>
`2023-07-16 19:50:10.152072`

### Changing the who plays first setting only works on New Game :heave_check_mark:

When the setting for who plays first is confirmed, that change only occurs when a new game is started

Verified: Simon Libricky

`Windows 10 10.0.19045`<br>
`64bit WindowsPE AMD64 Family 23 Model 113 Stepping 0, AuthenticAMD`<br>
`2023-07-16 19:53:07.541029`

### Replacing current game when new game pressed :heavy_check_mark:

When the new game button is pressed, the current game state is replaced with a blank new game

Verified: Simon Libricky

`Windows 10 10.0.19045`<br>
`64bit WindowsPE AMD64 Family 23 Model 113 Stepping 0, AuthenticAMD`<br>
`2023-07-16 19:54:05.930828`

### Different computer first moves :heavy_check_mark:

When the compute starts a new game, the first move is a random square and not consistently the same

Verified: Simon Libricky

`Windows 10 10.0.19045`<br>
`64bit WindowsPE AMD64 Family 23 Model 113 Stepping 0, AuthenticAMD`<br>
`2023-07-16 19:54:05.930828`

### Input sanitation :heavy_check_mark:

When it's the human's turn, only valid moves are selectable

Verified: Simon Libricky

`Windows 10 10.0.19045`<br>
`64bit WindowsPE AMD64 Family 23 Model 113 Stepping 0, AuthenticAMD`<br>
`2023-07-16 19:59:11.612195`

### Win checking / lockout

When the game is won or tied, the player is locked out of making moves, the computer does not make a move, and the winner is correctly displayed

### Resizing window :heave_check_mark:

When the window is resized, but not fullscreened, the app scales correctly

Verified: Simon Libricky

`Windows 10 10.0.19045`<br>
`64bit WindowsPE AMD64 Family 23 Model 113 Stepping 0, AuthenticAMD`<br>
`2023-07-16 20:02:45.541540`