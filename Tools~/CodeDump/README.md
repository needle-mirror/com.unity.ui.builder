# Code Dump script

<p>The code dump script is a tool to ease the synchronisation of overridable assemblies between two repositories. This script is more specificfly used to synchronize changes between UI Toolkit Core (version that resides in Unity's main repository) and UI Toolkit Package (com.unity.ui).</p>

## How it works

 <p>The script itself is simple. When run, it overrides all files and folders in the destination repository with the files and folders from the source repository. The mapping between paths is located in the `JsonFiles\` folder. To be noted that files in the `blacklist.json` will not be copied nor overridden. It is important to understand that after the Code Dump script has been run, both repositories are identical. With that in mind, when running the script, it is important that we rollback to the changest we last ran the script. This will prevent changes from being overridden. Once the script has been run, updating to the latest version of the repository and resolving conflicts will complete the synchronization process.</p>

 The important changeset can be found [here](https://docs.google.com/spreadsheets/d/1UDwbaQzE2Oy8LCSUZivBHRHWu1sXfmwKVH-fxT72fuE/edit#gid=1732357628).

# Step by step guide
## Prerequisites
3 terminals are required:

- Clone and update to the latest com.unity.ui. The CodeDump script will run from this terminal.
- Clone and update the latest com.unity.ui. The CodeSync will be done in this terminal.
- Clone and update to the latest version of Unity Core (trunk, 2020.2, ...). The CodeDrop will be done in this terminal.


## Code sync (Core -> Package)
1- `git checkout $CHANGESET` (Update repo to last code sync changeset)

2- `git checkout -b "code-sync-vX" `

3- `hg pull -r trunk && hg up -r trunk`

4- Build the CodeDump script. Run `mono CodeDump.exe --help` for more informations on the script.

5- `mono CodeDump.exe -c ~/Documents/Mercurial/trunk -p ~/Documents/Git/Packages/CodeDrop/com.unity.ui/ --sync-core-to-package`

6- `git commit -m "Run code sync script at #TRUNK_CHANGESET"`

7- `git pull origin master`

8- (optional) Run UIElementsStyleSheetGenerator project.

9- (optional) Run EditAndPlayModeTests

10- Update changeset for CI in test_editors.metafile.

11- Add an entry to this [spreadsheet](https://docs.google.com/spreadsheets/d/1UDwbaQzE2Oy8LCSUZivBHRHWu1sXfmwKVH-fxT72fuE/).


## Code drop (Package -> Core)
Use the same branches as used for the Code sync.


1- `hg branch editor/tech/code-drop-vX`

2- `mono CodeDump.exe -c ~/Documents/Mercurial/trunk -p ~/Documents/Git/Packages/CodeDrop/com.unity.ui/`

3- `hg add` (Adds all newly created files)

4- `hg remove -A` (Removes all deleted files)

5- `./jam ProjectFiles && ./jam MacEditor && ./jam AllAssemblies`

6- `hg commit -m "Ran Code Drop at $PACKAGE_CHANGESET"`

7- `perl utr.pl --suite=editor --testprojects=UIElements` (Run tests)

8- `hg push -b . --new-branch`

9- Launch ABV.

10- Add an entry to this [spreadsheet](https://docs.google.com/spreadsheets/d/1UDwbaQzE2Oy8LCSUZivBHRHWu1sXfmwKVH-fxT72fuE/).

11- Add release notes and resolve corresponding fogbugz cases. The changelogs can be found using `is:pr merged:2020-06-30T11:03:00Z..2020-07-15 changelog base:2020.2/staging` and this [spreadsheet](https://docs.google.com/spreadsheets/d/1UDwbaQzE2Oy8LCSUZivBHRHWu1sXfmwKVH-fxT72fuE/edit#gid=541037734).
