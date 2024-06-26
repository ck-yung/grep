## Color option

### Since v0.9.0.0

* Color option can be stored in environment variable ```grep```. For example,

| OS/Shell  | Environement Setting |
| --------  | -------------------- |
| Win       | ```set grep=--color white,darkred,5,black,yellow;``` |
| bash      | ```export grep="--color white,darkred,5,black,yellow"``` |


    ** Any leading spaces will be ignored.
    ** Semi-comma is an optional separator just for friendly reading.

* The following demo would be printed by command ```grep --color -```

![Color Demo](https://raw.githubusercontent.com/ck-yung/grep/master/images/color-demo.gif)

* The feature can be disabled by command ```grep --color off```

* Background color can be set by command ```grep --color COLOR1,COLOR2```. For example,

    ```grep --color white,darkred```

* Under Windows OS, color can be inverted by command ```grep --color --```

* Group color by command ```grep --color COLOR1,COLOR2,NUMBER,COLOR3,COLOR4```.

    | Name   | Setting                                            |
    | ----   | -------                                            |
    | COLOR1 | Foreground color of the finding text               |
    | COLOR2 | Background color of the finding text               |
    | NUMBER | The number of lines of a group                     |
    | COLOR3 | Foreground color of the highlight line of a ground |
    | COLOR4 | Background color of the highlight line of a ground |

    *  For example,

    ```grep --color white,darkred,5,blue```

    ```grep --color white,darkred,5,black,yellow```

## Known Issuses

Under Windows Terminal, the program CANNOT display black background color to the following color scheme.

    * One Half Light
    * Solarized Light
    * Tango Light

[List of All Options](https://github.com/ck-yung/grep/blob/master/docs/info-all-options.md)

[Back to README.md](https://github.com/ck-yung/grep/blob/master/README.md)