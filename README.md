# Changelog - Location Connection Android
1.4 - 28 May 2020

Bug fixes
- The system navigation bar covers the bottom of page in profile view.
- Photo uploading errors, and on some phones (including LG G5 and Samsung A71), portrait photos appear in landscape orientation in the editor.
- When selecting a photo with the keyboard open, the photo will appear smaller than the cutting frame.
- On own profile, report/block menu appears, and results in crash when selected.
- If on the registration page the description text contains ;, upon leaving the page and returning to it, the text will be cut off.
- It is possible to click Images button twice.
- When unchecking location/distance sharing with friends, the "No one" option does not get checked.
- When rearranging pictures on the registration/profile edit page, they get invisible outside the dragging area.
- If device orientation changes on the main page, when viewing a profile the image will be shorter than normal or have extra space above and below.
- Delete account section remains open after deleting account and logging in with another user
- In some situations the back button on the profile page covers the name and the username.
- In a certain situation an empty space appears in place of the distance filters.

1.3 - 15. May 2020

An image editor was created for cropping pictures before uploading, and a tutorial added to the help center.

Bug fixes:
- Pressing the like and hide buttons repeatedly results in error.
- The app crashes when on the register page you long-tap the empty space next to the one uploaded picture.
- Blocking a user from their profile page which was opened from the chat results in a crash.
- Chat list pictures (if they weren't visible in the last 24 hours) do not load fully, and wrong picture is shown at the first chat.
- Texts containing certain characters do not display correctly.
- When stepping back from profile edit page with the keyboard open with location being turned off, introduction text does not align to the bottom.
- On a profile page, scrolling the pictures by dragging them does not immediately update the status indicator circles.

1.2 - 7. April 2020

Block/report feature have been added.

Furthermore, the following bugs have been fixed:

- In a chat window a new message appears even if it is from another match.
- If you receive a notification of a new message, and click on it, the keyboard does not hide if it was visible.
- Occasional crash when stepping back from the chat window to chat list.
- First incoming message does not show up in chat list. 
- When you change search distance radius, users that are now out of range remain on the map.
- When a message appears that no location was set, it does not disappear automatically once it is set.
- If you enter an invalid address, and then reload the list, the address is not reverted back to the previous valid value.
- When you are logged in at program start, changing the list type for the first time may not work.
- Android 6 crash on startup
- When a logged in user who has location enabled, but turned it off in their profile, clicks on the map icon, a message appears that "Location was not set or acquired", and the map is not shown.
- If device location is enabled, but disabled in your account, and you are filtering by distance from a given coordinate/address, but now want to use own location, the list is not refreshed.
- It is possible to delete uploaded pictures one after another too fast, which results incorrect view.
- When switching on location, map does not appear upon returning to your profile page.

1.1 - 31. December 2019

Performance impromevents (Map does not need to be set every time)
Bug fixed: Location is visible from opening profile from chat one, even though location sharing is not on with matches/friends.
Stopping real-time location updates on logout.
Small layout

1.0 - 16. December 2019

Character transmitting problems fixed, which affected using symbols like & # + in all text fields
Fixed-size marker added on the location history map.
Layout issue fixed: no space below sort by options
