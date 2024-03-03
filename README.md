# grep
**v0.0.3.0**

## Syntax:
```
grep [OPTIONS] REGEX FILE [FILE ..]
```

### Examples
```
grep Syntax README.md

dir2 -sb *cs | grep syntax -niT -
```

## Options
```
  NAME              DEFAULT  ALTERATIVE
  --color           READ     color
  --case-sensitive  on       off
  --word            off      on
  --line-number     off      on
  --count           off      on
  --file-match      off      on
  --invert-match    off      on
  --files-from               FILES-FROM
  --file                     REGEX-FILE
```
## Short-Cut
```
  -i       --case-sensitive off
  -w       --word on
  -n       --line-number on
  -c       --count on
  -l       --file-match on
  -v       --invert-match on
  -T       --files-from
  -f       --file
```

* Read redir console input if ```FILES-FROM``` or ```REG-FILE``` is -

## Demo

![Color Feature](https://raw.githubusercontent.com/ck-yung/grep/master/images/help.gif)

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2024 March
