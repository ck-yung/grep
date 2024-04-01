## Map Shortcut Options

### Since v0.9.0.0

* An option ```--map-shortcut``` can be defined by environement variable ```grep``` to change shortcut setting. For example:

| OS/Shell  | Environement Setting |
| --------  | -------------------- |
| Win       | ```set grep=--map-shortcut s=r,Q=q; -X obj,bin;``` |
| bash      | ```export grep="--map-shortcut s=r,Q=q; -X obj,bin;"``` |

Then the following command will read each ```*.cs``` file on each sub-directory excluing ```obj``` and ```bin```.

```grep -s using ,*.cs -Q```

[Back to README.md](https://github.com/ck-yung/grep/blob/master/docs/README.md)
