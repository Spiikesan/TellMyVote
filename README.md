## Features

Tell My Vote displays a Cui Panel for Voting/Admin
* Up to 4 polls / 3 answers
* players can vote once by poll
* vote counts displayed on Panel, next to Answers
* A timer can be set to automatically ends the poll session.
* You can choose to wipe the polls results when the server wipes.

When a vote session has started, all online players will see a banner popup to ask for vote. (It's refreshed all 30 seconds by default)

The banner is configurable (color, size, text and position) and can be disabled. A chat message can also be sent to players.

If a Poll/Question is set to empty, it and its answers won't be displayed.

If an Answer is set to empty, these answer line won't be displayed.

## Permission
- No permission required to vote
- `tellmyvote.admin` --- to use `/myvote_poll` chat command and Start/End/Clear on Cui Panel


## HowTo - user
- Chat Command (configurable) : /myvote
- to vote, click on your choosen answer
- you can only vote once by question.

## HowTo - admin
- Chat Command (configurable) : /myvote --- admin buttons to Start/Stop/Clear will show on main Panel
- Config file --- edit and fill as you want Questions, Answers, and others customizations.
- Chat Command : /myvote_poll --- to set Questions and Answers with chat command

example :
- /myvote_poll 1 0 question       ---> set "question" for poll#1 title
- /myvote_poll 1 1 first choice   ---> set "first choice" for poll#1 answer#1 
- /myvote_poll 1 2 second choice   ---> set "second choice" for poll#1 answer#2
- /myvote_poll 2 3 second choice   ---> set "second choice" for poll#2 answer#3
- /myvote_poll 1 ---> will empty poll 1 (this poll won't show)
- /myvote_poll 3 2 ---> will empty poll#3 answer#2 (this answer won't be displayed)


## Future Plans
*You can make a suggestion on the plugin Help page*

## Localization
ENglish and FRench are included.

## Credits
 - BuzZ[PHOQUE] (Original plugin work 0.0.1 -> 1.0.0)
 - Spiikesan (Rework, maintain > 1.0.0)
