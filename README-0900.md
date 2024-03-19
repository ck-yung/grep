# grep
**v0.9.0.0**

## Syntax:
```
grep [OPTIONS] PATTERN  [FILE [FILE ..]]

grep [OPTIONS] -f PATTERN-FILE  [FILE [FILE ..]]
```

### Examples
```
grep -in syn.*ax *.md

grep -in public.*class -r *.cs --excl-dir obj --excl-dir bin

grep -inf regexThe.txt *.cs *.md

dir2 -sb *cs | grep syn.*x -niT -

```

* ```grep``` supports ```FILE``` in wild card format.
* Options can be stored in envir var ```grep```.

[Link to ```dir2```](https://www.nuget.org/packages/dir2)

## Options
```
Shortcut           Option  with           Envir
    -f     --pattern-file  PATTERN-FILE   Command line only
    -T       --files-from  FILES-FROM     Command line only
    -v     --invert-match  on             Command line only
    -l       --file-match  on             Command line only
    -c       --count-only  on             Command line only
    -r          --sub-dir  on             Command line only

    -i   --case-sensitive  off
    -h    --show-filename  off
    -n      --line-number  on
    -q            --quiet  on
    -w             --word  on
    -F    --fixed-strings  on
    -p            --pause  off
    -m        --max-count  NUMBER
                  --color  COLOR
                  --total  on
    -x        --excl-file  FILE-WILD ..
    -X         --excl-dir  DIR-WILD ..
```

* Read redir console input if no ```FILE``` is given, or it is -
* Read redir console input if ```PATTERN-FILE``` is -
* Read redir console input if ```FILES-FROM``` is -
* Options ```--excl-file``` and ```--excl-dir``` can be multiple.


## Demo

![Color Feature](https://raw.githubusercontent.com/ck-yung/grep/master/images/help.gif)

## Known Issuses

1. When using Windows Terminal, it CANNOT show black background color under the following color scheme.

* Tango Light

* Solarized Light

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2024 March
