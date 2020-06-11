# Adaptating a Unity demo for Mobile and Web platforms

## Overview
Adapting a game to other platforms require some changes, especially if we come from a PC version and we're targeting a mobile edition. I made some tweaks to my projects so it could be ported for Mobile and Web versions.

## Demo execution
![](mobile1.gif)

You can clearly see I'm moving the joystick (with my left thumb), but I'm also tapping on the right button with the right thumb as well as quickly casting a magic towards somewhere else.

## Features
- This project was originally meant to be only for PC, so all the input was binded to keyboard keys.
 - "S" and "D" keys were used for movement.
 - "W" and "Space" for jumping.
 - "Shift" for running.
 - "Left click" to cast a spell.
- By using a virtual control, the game can now be controller in a mobile device.
 - Depending on the joystick position the character switches from walking to running.
 - Added an independent button for jumping.
 - User can tap anywhere and the game will cast a spell in that direction.
- Added a Quality screen meant for Mobile versions and old computers.

## Android build (APK)
You may find a Beta Android package in the "Android Build" folder.

## WEB Build (Unity WEBGL)
You may find a Beta WEBGL version in the "WEB Build" folder.

### Considerations:
Android APK tested with a real Android device (Android 7.1)
