# Code Dump script

<p>The code dump script is a tool to ease the synchronisation of overridable assemblies between two repositories. This script is more specificfly used to synchronize changes between UI Toolkit Core (version that resides in Unity's main repository) and UI Toolkit Package (com.unity.ui).</p>

## How it works

 <p>The script itself is simple. When run, it overrides all files and folders in the destination repository with the files and folders from the source repository. The mapping between paths is located in the `JsonFiles\` folder. To be noted that files in the `blacklist.json` will not be copied nor overridden. It is important to understand that after the Code Dump script has been run, both repositories are identical. With that in mind, when running the script, it is important that we rollback to the changest we last ran the script. This will prevent changes from being overridden. Once the script has been run, updating to the latest version of the repository and resolving conflicts will complete the synchronization process.</p>

 The important changeset can be found [here](https://docs.google.com/spreadsheets/d/1UDwbaQzE2Oy8LCSUZivBHRHWu1sXfmwKVH-fxT72fuE/edit#gid=1732357628).

# Step by step guide
## Prerequisites
2 terminals are required:

- **UIB**: Clone and update to the latest `com.unity.ui.builder`. The CodeDump script will run from this terminal.
- **Trunk**: Clone and update to the latest version of Unity Core (trunk, 2020.2, ...).

## Code drop (Package -> Core)
Use the same branches as used for the Code sync.

1. **UIB**: `cd Tools~/CodeDump`
1. **UIB**: open the `CodeDump.sln` and build it
1. **UIB**: `bin/Debug/CodeDump.exe -c <your-path-to-trunk>/unity -p <your-path-to-builder-repo>/com.unity.ui.builder`
1. **Trunk**: `hg branch ui-builder/code-drop-vX`
1. **Trunk**: `hg format` - this is important as there are still some differences between package and trunk
1. **Trunk**: `run -projectPath External/Resources/editor_resources` - this is to make sure all `.meta` files are updated to latest trunk revisions
1. **Trunk**: `hg add` (Adds all newly created files)
1. **Trunk**: `hg remove -A` (Removes all deleted files)
1. **Trunk**: `run b e` - build Unity
1. **Trunk**: `hg commit -m "Ran Code Drop at $PACKAGE_CHANGESET"`
1. **Trunk**: `perl utr.pl --suite=editor --testprojects=UIBuilder` (Run tests)
1. **Trunk**: `hg push --new-branch -b .`
1. Launch ABV.
1. Follow format from previous code drops: https://ono.unity3d.com/unity/unity/pull-request/115730/_/ui-builder/code-drop-v1

## Code sync (Core -> Package) ADAPTED FROM UITK, THIS IS NOT YET BEEN TESTED OR RAN WITH UI BUILDER!!
1. **UIB**: `git checkout $CHANGESET` (Update repo to last code sync changeset)
1. **UIB**: `git checkout -b "code-sync-vX" `
1. **UIB**: `cd Tools~/CodeDump`
1. **UIB**: open the `CodeDump.sln` and build it
1. **Trunk**: `hg pull -r trunk && hg update trunk`
1. **UIB**: `bin/Debug/CodeDump.exe -c <your-path-to-trunk>/unity -p <your-path-to-builder-repo>/com.unity.ui.builder/ --sync-core-to-package`
1. **UIB**: `git commit -m "Ran code sync script at #TRUNK_CHANGESET"`
1. **UIB**: `git pull origin master`
