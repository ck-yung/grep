 grep/c#
**v0.9.0.0**

Inspired by GNU ```grep```

## Syntax:
```
grep [OPTIONS] PATTERN  [FILE [FILE ..]]

```

### Examples
```
  grep -nm 3 class *.cs --color black,yellow -X obj,bin

  dir2 -sb *.cs --within 4hours | grep -n class -T -
```

* Options can be stored in envir var ```grep```. [Link](https://github.com/ck-yung/grep/blob/master/docs/info-envir.md)
* [Link to tool ```dir2```](https://www.nuget.org/packages/dir2)

## Common Options

| Shortcut | for Option             | with         | Required  | Can be stored in Envir ```grep```?
| -------- | ----------             | ----         | --------  | ----------------------------------
| ```-c``` | ```--count-only```     | ```on```     |           | No
| ```-l``` | ```--file-match```     | ```on```     |           | No
| ```-r``` | ```--sub-dir```        | ```on```     |           | No
| ```-v``` | ```--invert-match```   | ```on```     |           | No
|          | ```--color```          |              | COLOR     | Yes, [Link](https://github.com/ck-yung/grep/blob/master/docs/info-color.md)
|          | ```--map-shortcut```   |              | a=b[,x=y] | Stored only in the envir [Link](https://github.com/ck-yung/grep/blob/master/docs/info-map-shortcut.md)
| ```-i``` | ```--case-sensitive``` | ```off```    |           | Yes
| ```-m``` | ```--max-count```      |              | NUMBER    | Yes
| ```-n``` | ```--line-number```    | ```on```     |           | Yes
| ```-w``` | ```--word```           | ```on```     |           | Yes
| ```-x``` | ```--excl-file```      |              | FILE      | Yes, [Link](https://github.com/ck-yung/grep/blob/master/docs/info-excl.md)
| ```-X``` | ```--excl-dir```       |              | DIR       | Yes, [Link](https://github.com/ck-yung/grep/blob/master/docs/info-excl.md)

## List of All Options
[Link](https://github.com/ck-yung/grep/blob/master/docs/info-all-options.md)

## Demo

![Color Feature](https://raw.githubusercontent.com/ck-yung/grep/master/images/help.gif)

## Major Bug Fix to v0.0.2

* "Word search" now properly shows "the beginning of a line" and "the end of a line".

## Major Imporvement to v0.0.2

* Command line parameter ```FILE``` now can be a wild-card (e.g. ```*.cs```) and including a path (e.g. ```zip2\*.cs```).

* A color option ```--color``` is added for highlight background and group highlight.

* An option ```--search``` (```-e```) is added for multiple patterns.

* An option ```--sub-dir``` (```-r```) is added for recursivly reading files under directories.

* An option ```--fixed-strings``` (```-F```) is added. That is, ```c++ -F``` is same to ```c\+\+```

* An option ```--max-count``` (```-m```) is added for max finding count. The reporting finding count could be more than the number if the last find matched line contains several findings.

* Some options can be stored in environment variable ```grep```. For example,

| OS/Shell  | Environement Setting |
| --------  | -------------------- |
| Win       | ```set grep= -ni; --color red,yellow,9,black,gray;``` |
| bash      | ```export grep=" -ni; --color red,yellow,9,black,gray;"``` |

    ** Any leading spaces will be ignored.
    ** Semi-comma is an optional separator just for friendly reading.


## Important Differences to GNU ```grep``` in Linux and macos

* GNU option ```--include=GLOB``` is NOT provided. And wild-card parameters can be leading by a comma, e.g. ```,*.cs``` and ```,*.cs,*.py```.

* The above feature of comma-combining can be turn-off by option ```--split-file-by-comma off```.

* ```grep/c#``` ignores command line parameter ```--color=auto``` by a default option ```--skip-arg auto```.

## Major Differences to GNU ```grep```

* Excluding filenames can be combined by comma. e.g. ```-x test*.cs,demo*.cs```.

* Excluding directories can be combined by comma. e.g. ```-X bin,obj,packag*```.

* The above feature of comma-combining can be turn-off by option ```--split-file-by-comma off```.

* Directory separator can be included in excluding directory. e.g. ```-X obj\re*e```.

* Pause option ```--pause``` is added with default value ```on```.

* An option ```--total``` is added for grand total sum reporting. ```--total only``` prints the grand total sum line only.

* An option ```--trim``` is added for removal of spaces at the beginning and at the end of a matched line.

* An option ```--map-shortcut``` can be defined by environement variable ```grep``` to change shortcut setting. For example:

| OS/Shell  | Environement Setting |
| --------  | -------------------- |
| Win       | ```set grep=--map-shortcut s=r,Q=q; -X obj,bin;``` |
| bash      | ```export grep="--map-shortcut s=r,Q=q; -X obj,bin;"``` |

Then the following command will read each ```*.cs``` file on each sub-directory excluing ```obj``` and ```bin```.

```grep -s using ,*.cs```

## Known Issuses

Under Windows Terminal, the program CANNOT display black background color to the following color scheme.

    * One Half Light
    * Solarized Light
    * Tango Light

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2024 April
