## Files-from Option

### Since v0.9.0.0

#### Option ```--files-from```

| Shortcut | for Option         | Required  | Stored in Envir Var
| -------- | ----------         | --------  | -------------------
| ```-T``` | ```--files-from``` | FILE      | No

* Wild-card (```*``` and ```?```) is NOT allowed in the content of ```FILE``` to ```--files-from```

* Related path is allowed in the content of ```FILE``` to ```--files-from```

* Related path is allowed in the content of ```FILE``` to ```--files-from```

#### Command line ```FILE```

* A single char ```-``` to ```FILE``` stands for redirected console input.

* If not ```FILE``` is found, option ```--files-from``` is defined, and, console input is redirected, ```grep/cs``` would read redirected console input as ```FILE```.

[Back to README.md](https://github.com/ck-yung/grep/blob/master/docs/README.md)
