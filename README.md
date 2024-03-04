# grep
**v0.0.3.0**

## Syntax:
```
grep [OPTIONS] REGEX   [FILE [FILE ..]]

grep [OPTIONS] --file REGEX-FILE   [FILE [FILE ..]]
```

### Examples
```
grep Syn.*x README.md

dir2 -sb *cs | grep syn.*x -niT -

cat *cs | grep using.*Linq
```

* Remark: ```FILE``` does not support wild card.

## Options
```
  NAME              DEFAULT  ALTERATIVE
  --color           RED      color
  --case-sensitive  on       off
  --word            off      on
  --line-number     off      on
  --count           off      on
  --file-match      off      on
  --invert-match    off      on
  --show-filename   on       off
  --max-count       UNLIMIT  NUMBER
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
  -h       --show-filename off
  -m       --max-count
  -T       --files-from
  -f       --file
```

* Read redir console input if ```FILES-FROM``` or ```REG-FILE``` is -

* Read redir console input if no ```FILE``` is given.

## Demo

![Color Feature](https://raw.githubusercontent.com/ck-yung/grep/master/images/help.gif)

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2024 March
