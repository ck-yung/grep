## Pattern

### Since v0.9.0.0

## Searching Pattern

### Syntax:

1. A single pattern string: ```grep [OPTIONS] PATTERN [FILE [FILE ..]]```

2. Multiple pattern strings on command line: ```grep [OPTIONS] -e PATTERN [-e PATTERN ..] [FILE [FILE ..]]```

3. Multiple pattern strings from a file: ```grep [OPTIONS] -f PATTERN-FILE [FILE [FILE ..]]```

### Examples

1. A single pattern string: ```grep -niF c++ *.md,*.txt```

2. Multiple pattern strings on command line: ```grep -e using -e var ,*.cs```

3. Multiple pattern strings from a file: ```grep -f regFind.txt *.cs,*.txt```

## Fixed Strings

1. ```grep/c#``` treats PATTERN as a raw text other than a regular expression if ```--fixed-strings``` is turn-on.

| Shortcut | Option                  | with         | Default   |
| -------- | ------                  | ----         | -------   |
| ```-F``` | ```--fixed-strings```   | ```on```     | ```off``` |

2. Examples,

	```grep -F c++ *.cs```  is same to ```grep c\+\+ *.cs```

[List of All Options](https://github.com/ck-yung/grep/blob/master/docs/info-all-options.md)

[Back to README.md](https://github.com/ck-yung/grep/blob/master/README.md)
