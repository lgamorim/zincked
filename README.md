# Zincked

Keep your game saves in sync across computers — even for games that don't support Steam Cloud.

## What it does

Some Steam games don't back their save files up to Steam Cloud, so your progress
stays trapped on whichever computer you played on. Zincked bridges that gap. You
point it at your local **game folder** and a shared **cloud folder** (for example a
OneDrive, Dropbox, Google Drive, or network-share folder that both computers can
see), and it keeps the two in sync.

By default synchronization is **two-way**: for every file, the newer copy wins and
is copied to the other side. You can also limit it to one direction — only push
your local saves up to the cloud, or only pull cloud saves down — using the
`--mode` option. Subfolders are included. Files are **never deleted** — Zincked
only ever adds or updates, so there's no risk of it wiping out a save.

This is an early alpha, so you run it by hand whenever you want to sync.

## Typical workflow

1. Finish playing on **Computer A**, then run Zincked to push your latest saves up
   to the cloud folder.
2. Sit down at **Computer B** and run Zincked there before playing to pull those
   saves down.
3. Play, and repeat the next time you switch machines.

Because the newer file always wins, running it again when nothing has changed is
harmless — it simply reports that everything is already in sync.

> **Tip:** Always let the cloud folder finish uploading/downloading (watch your
> cloud client's sync icon) before switching computers, so the newest saves are
> actually in place.

## Usage

Provide the game folder and the cloud folder. You can pass them as two plain
arguments:

```
Zincked <gameFolder> <cloudFolder>
```

…or by name, in any order:

```
Zincked --game <gameFolder> --cloud <cloudFolder>
```

### Options

| Option                      | Description            |
| --------------------------- | ---------------------- |
| `-g`, `--game`              | The local game folder. |
| `-c`, `--cloud`             | The shared cloud folder. |
| `-m`, `--mode <both\|up\|down>` | Sync direction (see below). Defaults to `both`. |
| `-h`, `--help`              | Show help and exit.    |

The `--mode` values are:

- `both` — two-way sync, the newer copy of each file wins (the default).
- `up` — push only: copy from the **game** folder to the **cloud** folder; the
  game folder is never modified.
- `down` — pull only: copy from the **cloud** folder to the **game** folder; the
  cloud folder is never modified.

If the cloud folder doesn't exist yet, Zincked creates it for you. If the game
folder doesn't exist, it stops and tells you, so a typo can't quietly sync the
wrong place.

### Examples

Sync a game's save folder with a folder in OneDrive:

```
Zincked "C:\Games\MyGame\Saves" "C:\Users\me\OneDrive\GameSaves\MyGame"
```

The same thing using named options:

```
Zincked --game "C:\Games\MyGame\Saves" --cloud "C:\Users\me\OneDrive\GameSaves\MyGame"
```

Push only — after playing on this computer, send your saves up to the cloud
(use `--mode down` on the other computer to pull them before playing):

```
Zincked "C:\Games\MyGame\Saves" "C:\Users\me\OneDrive\GameSaves\MyGame" --mode up
```

When it finishes, Zincked prints a short summary of how many files it copied each
way and how many were already up to date.
