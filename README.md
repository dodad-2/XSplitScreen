# Description

Splitscreen for Risk of Rain 2

https://discord.gg/maHhJSv62G

![UI](https://cdn.discordapp.com/attachments/995168655618871360/1049369226558853162/image.png)

# Instructions

1. Launch the game and click on 'Splitscreen' above 'Singleplayer' in the title menu

2. Assign players and customize

3. Click the toggle to enable the mod

4. Start a game normally

## Features

- Supports up to 4 players in singleplayer or multiplayer
- Seamless UI assignments with icon designs by Claymaver
- Persistent configuration remembers screen positions and profiles
- Multiple monitor support
- Custom colors
- Config option to auto enable if controllers are available

## Planned

- Player handicaps
- Artifacts (moved to standalone mod)
- Items (moved to standalone mod)

## Known Issues

- Profiles sometimes 'forget' the last used character
- Any user can change the loadout of the user who last refreshed the loadout panel
- Users can equip skills their profiles haven't unlocked
- Some buttons can still become unbound when entering a run
- Potential poor compatibility with other mods will be addressed in the future
- When using the Command Cube / scrapping the camera will jump to another player
- Scoreboard UI does not work in multiplayer
- Some options in the assignment screen can only be interacted with using the mouse
- Moving the cursor before splitscreen is enabled causes the default cursor visual to remain
- Using multiple monitors may cause issues. Please join the Discord to report them
- Damage numbers only display on the first monitor

## Special thanks

- iDeathHD for creating FixedSplitScreen
- Claymaver for art assets (https://linktr.ee/claymaver)
- All the testers, supporters, and those who reported bugs including:

MemexJota,
PKPotential,
ThatBlueRacc,
Kaiben,
The_real_douchcanoe_,
KCaptainawesome,
O\_Linny_/O,
noahwubs,
Narl,
Hansei,
Aristhma,
Re-Class May,
AdaM,
Engi,
Coked Out Monkey,
Wumble,
God of Heck,
Pub,
Bloodgem64,
kwiki,
Wiism,
instasnipe

- Extra special thanks to my son: the best splitscreen partner in the world
- Finally, if you want to support the mod please visit https://www.patreon.com/user?u=84145799 to leave a tip!

## Changelog

**2.0.6**

- API change without adhering to versioning rules (expect more of this)
- Removed reset label

**2.0.5**

- Added assignment reset button

**2.0.4**

- Added button for Patreon

**2.0.3**

- Fixed bug where controllers were not able to interact with UI

**2.0.2**

- Added fancy color cycle for warning triangle
- Refactor namespace

**2.0.1**

- Added assignment notifications for invalid options
- Added log levels

**2.0.0**

- Added assignment screen
- Added config entry for auto enable on startup
- Changed player names to reflect local profile name in most cases
- Changed player highlights to reflect preferred color
- Changed cursor center to the center of assigned screen
- Fixed most issues for gamepad cursors
- Fixed character selection
- Fixed artifact selection
- Fixed most keybinds not loading
- Mod now uses BepInEx log and config file to store preferences
- Changing bindings in the settings menu will save the bind to the player who last moved their cursor (sometimes breaks in runs)

**1.2.1**

- Fixed character selection commando invasion bug
- Temporary fix for non-existant controllers being recognized. If you have problems with some controllers now NOT being recognized, you'll have to wait until the profile selection window has been completed. That should happen in the next month or so

**1.2.0**

- Added vertical splitscreen. If mods that manipulate the camera break fall back to 1.1.2

**1.1.2**

- Fixed chat and console
- Most clickable UI buttons should work for the gamepad cursor now
- Selecting items via the Command Artifact should be fixed
- Removed screen blur when viewing scoreboard
- Added Discord link and logging for better troubleshooting

**1.1.1**

- Added dependency string for R2API. This should fix the mod not loading if you didn't have R2API previously

**1.1.0**

- Added menu
- Enable splitscreen for connected devices with one click
- Temporarily removed the add and remove player buttons
- Cursors should work for all gamepads now
- Full gamepad support is being tested
- Bugfixes

**1.0.0**

* First release
