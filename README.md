# tfvc-2-git

Take one or more folders from a Tfvc history and convert it to a Git repository.

## Why ?

There is already multiples tools for this purpose, but none of them satisfied these requirements :

* Update a converted git repository with new changesets from tfvc
* Clean message history
* Glue fake tfvc branches (aka folders) as real git branches
* Repository content & files hash check at each step
* Push converted repository to remote
* Control included/excluded files
* Control git authors (mapping from AD)
* Control branching & merging strategy

> Note : This tool is not designed to be friendly, but it cannot damage your tfvc instance, so feel free to experiment 😄

## Know issues

* The local git notes namespace is not pushed to remote, but it can be done manually with this command : `git push tfvc-2-git-upstream refs/notes/tfvc-2-git`

## Compatibility

* Only tested/used with TFS2017 but should work with any higher version.
* This will not work for cloud hosted tfvc collections, only with AD integration.

## Use cases

### One tfvc folder to one git branch

`tfvc2git convert --config one_folder_to_one_branch.json` [json](docs/samples/renamed_folder_to_one_branch.json)

### Renamed tfvc folder to one git branch

`tfvc2git convert --config renamed_folder_to_one_branch.json`

### Multiples tfvc folders to multiples git branches

`tfvc2git convert --config multiple_folders_to_multiple_branches.json`

### Check updates

`tfvc2git check-update --config config.json`

### Push to remote

`tfvc2git push-to-upstream --config config.json`

## Install

TODO : release..

## Build

### Prerequisites

* Visual Studio 2019 (Community or higher)
