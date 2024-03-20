# grep
**v0.9.0.0**

## Syntax:
```
grep [OPTIONS] PATTERN  [FILE [FILE ..]]

grep [OPTIONS] -f PATTERN-FILE  [FILE [FILE ..]]
```

### Examples
```
  grep -nsm 3 class *.cs --color black,yellow -X obj

  dir2 -sb *.cs --within 4hours | grep -n class -T -
```

* Read redir console input if no ```FILE``` is given, or it is -
* Options can be stored in envir var ```grep```.
* [Link to tool ```dir2```](https://www.nuget.org/packages/dir2)

## Options
| Shortcut | for Option             | with         | Stored in Envir Var
| -------- | ----------             | ----         | -------------------
| ```-c``` | ```--count-only```     | ```on```     | No
| ```-d``` | ```--sub-dir```        | ```on```     | No
| ```-f``` | ```--pattern-file```   | PATTERN-FILE | No, [Info](https://github.com/ck-yung/grep/blob/master/docs//info-pattern.md)
| ```-l``` | ```--file-match```     | ```on```     | No
| ```-T``` | ```--file-from```      | FILES-FROM   | No, [Info](https://github.com/ck-yung/grep/blob/master/docs//info-files-from.md)
| ```-v``` | ```--invert-match```   | ```on```     | No
|          | ```--color```          | COLOR        | Yes, [Info](https://github.com/ck-yung/grep/blob/master/docs//info-color.md)
|          | ```--total```          | ```on```     | Yes
| ```-F``` | ```--fixed-strings```  | ```on```     | Yes, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-pattern.md)
| ```-h``` | ```--show-filename```  | ```off```    | Yes
| ```-i``` | ```--case-sensitive``` | ```off```    | Yes
| ```-m``` | ```--max-count```      | NUMBER       | Yes
| ```-n``` | ```--line-number```    | ```on```     | Yes
| ```-p``` | ```--pause```          | ```off```    | Yes
| ```-q``` | ```--quiet```          | ```on```     | Yes
| ```-w``` | ```--word```           | ```on```     | Yes
| ```-x``` | ```--excl-file```      | FILE         | Yes, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-excl.md)
| ```-X``` | ```--excl-dir```       | DIR          | Yes, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-excl.md)




## Demo

![Color Feature](https://raw.githubusercontent.com/ck-yung/grep/master/images/help.gif)

## Known Issuses

1. Under Windows Terminal, it CANNOT display black background color to the following color scheme.

    * Tango Light

    * Solarized Light

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2024 March
