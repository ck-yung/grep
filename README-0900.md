# grep
**v0.9.0.0**

## Syntax:
```
grep [OPTIONS] PATTERN  [FILE [FILE ..]]
```

### Examples
```
grep Syn.*x README.md

cat *cs | grep using.*Linq

dir2 -sb *cs | grep syn.*x -niT -

dir2 -sd | grep !lost+found

```

* ```grep``` does not support ```FILE``` in wild card format.
* ```PATTERN``` is a regular expression if it is NOT leading by a ```!``` char.

[Link to ```dir2```](https://www.nuget.org/packages/dir2)

## Options
```
  NAME                  DEFAULT  ALTERATIVE
  --color               RED      COLOR
                                 !COLOR
  --case-sensitive      on       off
  --word                off      on
  --line-number         off      on
  --count-only          off      on
  --file-match          off      on
  --invert-match        off      on
  --show-filename       on       off
  --pause               on       off
  --fixed-strings       off      on
  --max-count           UNLIMIT  NUMBER
  --files-from                   FILES-FROM
  --file                         REGEX-FILE
```
## Short-Cut
```
  -i       --case-sensitive off
  -w       --word on
  -n       --line-number on
  -c       --count on
  -l       --file-match on
  -v       --invert-match on
  -h       --show-filename off
  -F       --fixed-strings on
  -p       --pause off
  -m       --max-count
  -T       --files-from
  -f       --file
```

* Read redir console input if no ```FILE``` is given.

* Read redir console input if ```FILES-FROM``` is -

* Read redir console input if ```REGEX-FILE``` or  ```FIXED-TEXT-FILE```is -

## Demo

![Color Feature](https://raw.githubusercontent.com/ck-yung/grep/master/images/help.gif)

## Known Issuses

1. When using Windows Terminal, it CANNOT show black background color under the following color scheme.

* Tango Light

* Solarized Light

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2024 March
