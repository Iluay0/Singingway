#  Singingway

*Ever wanted to sing along during savage progging, obliterating the ears of your static? Now you can!*  
Singingway displays the lyrics for the currently playing BGM on-screen, so you can sing to your heart's content.

## Installation

1.  Open the `/xlsettings` menu in-game.
2.  Go to the **Experimental** tab.
3.  Under **Custom Plugin Repositories**, add the following URL:
    `https://raw.githubusercontent.com/Iluay0/Singingway/main/repo.json`
4.  Click the `+` button and save.
5.  Open the Plugin Installer (`/xlplugins`), search for "Singingway", and install\!

## How it Works

Singingway reads custom JSON files containing timestamps and lyric text.

### Lyric File Format

Lyrics are written in JSON.  
By default, they are compiled into the plugin itself, but it is possible to set a folder for custom lyrics via the plugin's Development Settings.

```json
[
  { "Time": "00:00.00", "Text": "First line of the song" },
  { "Time": "00:10.00", "Text": "Chorus starts" },
  ...
  { "Time": "01:58.00", "Text": "Last line of the song" },
  { "Time": "02:00.00", "Text": "", "LoopToTime": "00:10.00" }
]
```

  * `Time`: Timestamp for the lyric (`MM:SS.ms`).
  * `Text`: The lyric to display.
  * `LoopToTime`: The exact timestamp the song jumps back to when it loops.

**Note on Loops:**  
To avoid frame-perfect desyncs, do not guess the loop times.  
Extract the `LoopStartSample` and `LoopEndSample` from the track's `.scd` file to get the exact floating-point timestamps.  
This can be done by using the [VFXEditor](https://github.com/0ceal0t/Dalamud-VFXEditor) plugin.
Important: As long as the loop duration is correct, it is entirely possible to move it around in order to match a lyric timestamp. This is heavily recommended so the lyrics can display correctly when looping.