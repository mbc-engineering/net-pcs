# Documentation of mbc engineering GmbH PCS Library

	Author: mbc engineering GmbH (MiHe)
	Date: 17.04.2018

This Library will help you to Interagte with the Process Control System (PCS) of mbc engineering GmbH. There are some default exchange logic and structures for reuse. This Library will help you on PLC side on some point. Example on handling PCS commands,

## Use the Library

### Install 
The library must be installed localy. The first time or on a new computer it must be installed the correct library version. For mor details see on infosys - [Using libraries](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_plc_intro/9007203443879435.html&id=8387830030110329229). 

For Installation go to *PLC -> Library Repository* and then press the *Install* button. Choose the Library file to install. In this case choose the file `Mbc_Tc3_Pcs_v1.0.0.0.library`. TwinCat3 will it install on the default behavor to the `system` repository into the folder `C:\TwinCAT\3.1\Components\Plc\Managed Libraries\mbc engineering GmbH\MBC TC3 PCS Library\1.0.0.0`. 

### Add the Library Reference to the PLC Project

TBD!

## Kick Start with a Command

First Add a new POU for the Command that extends the Library Class `CommandBase`.

```
FUNCTION_BLOCK PUBLIC StartCommand EXTENDS CommandBase
VAR_INPUT
END_VAR
VAR_OUTPUT	
	Q : BOOL;
END_VAR
VAR
END_VAR
```

Basicly you have a working command that does nothing importand. So we add a Output Parameter.

```
FUNCTION_BLOCK PUBLIC StartCommand EXTENDS CommandBase
VAR_INPUT
END_VAR
VAR_OUTPUT	
	Q : BOOL;
END_VAR
VAR
END_VAR
```

The Next step is to implement to logic Part of button, to ged the information when the button is pressed. We wana only a simple impulse.on `Q`. For this add a Method with the Name `Task` to the Class.

```
METHOD PROTECTED Task : BOOL
VAR_INPUT
	Init 	: BOOL;		// True the first time the Task is called after Execute is started
END_VAR
	
// Task Code:
IF (NOT Init AND Q) THEN
	// Task comlete;
	Task := SUPER^.Task(Init);;
END_IF
	
// set trigger
Q := Init;
```

The next step is to Declare the POU on a Global Variable List, in this example it is on `Commands`:

```
VAR_GLOBAL
	StartCommand1		: StartCommand;		
END_VAR
```

In the main Program you can now call the Button in a cycle behavior.

```
Commands.StartCommand1.Refresh();
	
fbTof(
	IN := Commands.StartCommand1.Q,
	PT := T#1S);		
```

Now there is simple Start Button created.

To Test the correctness of the behavor. Set the following internal `SartCommand` flag `Commands.StartCommand1.stHandshake.bExecute` in the Online Scope to True.