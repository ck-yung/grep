# grep
**v0.0.2.0**

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
* Read redir console input if ```FILES-FROM``` is -
* Read console keyboard input if ```FILES-FROM``` is --

## Demo

![Color Feature](https://dev.azure.com/yungchunkau/_git/info?path=/grep_demo01.gif)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fck-yung%2Fgrep.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fck-yung%2Fgrep?ref=badge_shield)

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2024 March


## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fck-yung%2Fgrep.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Fck-yung%2Fgrep?ref=badge_large)