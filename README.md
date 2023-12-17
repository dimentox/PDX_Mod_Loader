# PDX_Mod_Loader
This will run the moding toolchain that you can use if its not installed...

You will need a project you can use mine 
https://github.com/dimentox/PDXToolChainTest/tree/main
or  run the postprocessess exe that is in the c2 dir under moding.



If you want to publicize assemblies:

Download NStrip https://github.com/BepInEx/NStrip/releases
Download latest release and unzip
Open console command in the folder where you unzipped
```.\NStrip.exe -p -cg -cg-exclude-events -remove-readonly <FULL_PATH_TO_C2.DLL_IN_GAME_MANAGED_FOLDER>``` 

If you want to keep method bodies, add -n, but don't upload that dll on github or somewhere else if you do that


Inside your plugin, create a new empty source file
Add the following code to it: using System.Security; using System.Security.Permissions;
```
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission( SecurityAction.RequestMinimum, SkipVerification = true )]
```


You can now access any private, internal, or readonly types, methods, properties, fields, and events in the cs2 assembly
