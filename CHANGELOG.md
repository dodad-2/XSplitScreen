**4.0.1**

- Made swapping between multiple displays easier
- Updated language tokens

**4.0.0**

- Complete rewrite
- New UI
- Support for SotS
- More varied screen assignments
- Integrated credits

**3.1.4**

- Disabled dev mode

**3.1.3**

- Fixed: Crashing with the new update. Thanks Narl! (multiplayer support not guaranteed)
- Added: Translations by LuizFilipeRN
- Remove ko-fi button
- Lemonized the Discord button

**3.1.2**

- Removed Patreon & Paypal buttons
- Added ko-fi button
- Custom language files can be used

**3.1.1**

- Disabled dev mode

**3.1.0**

This patch is dedicated to BluJay in honor of his generous donation as well! Thank you!

- Added: Slider for HUD scale
- Added: Paypal button
- Bug: Dragging sliders was not working for controllers
- Bug: Misc console error
- Bug: Lemonization issue

**3.0.1**

This patch is dedicated to NickmitdemKopf in honor of his generous donation. Thank you!

- Added: Error for insufficient number of profiles (this will delete existing language files)
- Added: The HUD can now be scaled per user by manually editing the '\_hudScale' value in the assignments.json file. GUI option is in development.
- Bug: UI scaling made the assignment window unusable for certain resolutions
- Bug: Wrong bindings displayed in runs

**3.0.0**

This update is a full and final rebuild of the entire mod. Many bugs have been fixed, while a few have been added. A lot of time went into this project resulting in what will hopefully be considered the closest Risk of Rain 2 can get to native splitscreen. Instead of listing every change, only the most notable will follow:

- Gamepad cursors work on most UI and on all monitors
- Preferences are properly associated with profiles
- Added localization support via language file

**2.0.8**

- Fixed character selection bugs

**2.0.7**

- Cleaned up logging
- Aligned reset button
- Updated README

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

- First release