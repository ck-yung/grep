## All Options

### Since v0.9.0.0

| Shortcut | for Option                  | with         | Required  | Stored in Envir Var
| -------- | ----------                  | ----         | --------  | -------------------
| ```-c``` | ```--count-only```          | ```on```     |           | No
| ```-e``` | ```--search```              |              | PATTERN   | No
| ```-f``` | ```--pattern-file```        |              | FILE      | No, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-pattern.md)
| ```-F``` | ```--fixed-strings```       | ```on```     |           | No, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-pattern.md)
| ```-l``` | ```--file-match```          | ```on```     |           | No
| ```-r``` | ```--sub-dir```             | ```on```     |           | No
| ```-T``` | ```--files-from```          |              | FILE      | No, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-files-from.md)
| ```-v``` | ```--invert-match```        | ```on```     |           | No
| ```-i``` | ```--case-sensitive```      | ```off```    |           | Yes
| ```-h``` | ```--show-filename```       | ```off```    |           | Yes
| ```-H``` | ```--show-filename```       | ```on```     |           | Yes
| ```-m``` | ```--max-count```           |              | NUMBER    | Yes
| ```-n``` | ```--line-number```         | ```on```     |           | Yes
| ```-N``` | ```--line-number```         | ```off```    |           | Yes
| ```-p``` | ```--pause```               | ```off```    |           | Yes
| ```-q``` | ```--quiet```               | ```on```     |           | Yes
| ```-w``` | ```--word```                | ```on```     |           | Yes
| ```-x``` | ```--excl-file```           |              | FILE      | Yes, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-excl.md)
| ```-X``` | ```--excl-dir```            |              | DIR       | Yes, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-excl.md)
|          | ```--color```               |              | COLOR[,...] | Yes, [Info](https://github.com/ck-yung/grep/blob/master/docs/info-color.md)
|          | ```--color```               |              | -         | No, it shows help for color setting.
|          | ```--filename-case-sensitive``` |          | ```on```｜```off``` | Yes
|          | ```--map-shortcut```        |              | a=b[,x=y]           | Envir only [Info](https://github.com/ck-yung/grep/blob/master/docs/info-map-shortcut.md)
|          | ```--max-file-not-found```  |              | ```off```｜NUMBER   | Yes
|          | ```--skip-arg```            |              | ```auto```｜```off```｜TEXT     | Yes
|          | ```--split-file-by-comma``` |              | ```on```｜```off```            | Yes
|          | ```--total```               |              | ```on```｜```off```｜```only``` | Yes
|          | ```--trim```                |              | ```off```｜```start```｜```end```｜```both``` | Yes

* Value ```only``` to option ```--total``` in environment variable ```grep``` is ignore. That is, ```--total only``` is only allowed by command line.

[Back to README.md](https://github.com/ck-yung/grep/blob/master/README.md)

[Back to nuget](https://www.nuget.org/packages/grep)
