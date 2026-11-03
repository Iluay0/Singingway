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
{
  "ids": [20240, 20243],
  "offsets": { "20243": -1.364 },
  "loopLine": 0,
  "lines": [
    {
      "text": "Long live the king!",
      "time": "00:14.439",
      "end": "00:16.627",
      "words": ["00:14.439", "00:15.271", "00:15.712", "00:15.953"]
    },
    { "text": "Of our fallen brothers", "time": "01:00.045" }
  ]
}
```

  * `ids`: the BGM ids this file serves (the row ids of the game's BGM sheet).
  * `offsets`: optional, seconds added to every time when the file is loaded for that id.
  * `loopLine`: optional, index of the first line after the song loops. The loop point of the track is then anchored to that line's time.
  * `lines`: `text` and `time` (`MM:SS.mmm`) are required. `end` is optional. `words` is optional; it holds one time per space-separated word of `text`, in order. A line without `words`, or whose word count does not match, is highlighted as a whole.
