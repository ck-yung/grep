## Excluding Options

### Since v0.9.0.0

| Shortcut | for Option         | Required  | Can be stored in Envir ```grep```?
| -------- | ----------         | --------  | ----------------------------------
| ```-x``` | ```--excl-file```  | FILE      | Yes
| ```-X``` | ```--excl-dir```   | DIR       | Yes

* Excluding filenames can be combined by comma. e.g. ```-x test*.cs,demo*.cs```

* Excluding directories can be combined by comma. e.g. ```-X bin,obj,packag*```

* The above feature of comma-combining can be turn-off by option ```--split-file-by-comma off```.

* The options can be stored in environment. For example:

| OS/Shell  | Environement Setting |
| --------  | -------------------- |
| Win       | ```set grep= -X bin,obj,.git,package*; -x *sample*,*.md;``` |
| bash      | ```export grep=" -X bin,obj,.git,package*; -x *sample*,*.md;"``` |


    ** Any leading spaces will be ignored.
    ** Semi-comma is an optional separator just for friendly reading.

[List of All Options](https://github.com/ck-yung/grep/blob/master/docs/info-all-options.md)

[Back to README.md](https://github.com/ck-yung/grep/blob/master/README.md)
