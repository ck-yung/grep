# grep
**v0.0.3.0**

## Syntax:
```
grep [OPTIONS] REGEX FILE [FILE ..]
```

### Examples
```
grep Syntax README.md

dir2 -sb *cs | grep using -T -
```

## Options
```
OPTIONS:            DEFAULT  ALTERATIVE
  --files-from               FILES-FROM
  --verbose         off      on
  --case-sensitive  on       off
  --word            off      on
  --line-number     off      on
  --count           off      on
  --file-match      off      on
  --invert-match    off      on
  --color           RED      COLOR
```
## Short-Cut
```
  -T       --files-from
  -i       --case-sensitive off
  -w       --word on
  -n       --line-number on
  -c       --count on
  -l       --file-match on
  -v       --invert-match on
```
* Read redir console input if ```FILE``` is -
* Read redir console input if ```FILES-FROM``` is -
* Read console keyboard input if ```FILES-FROM``` is --

## Demo

![Color Feature](https://raw.githubusercontent.com/ck-yung/grep/master/images/help.gif)

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2024 March
