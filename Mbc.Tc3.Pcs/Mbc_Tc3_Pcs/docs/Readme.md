# Documentation of mbc engineering GmbH PCS Library

	Author: mbc engineering GmbH (MiHe)
	Date: 17.04.2018

This Library will help you to Interagte with the Process Control System (PCS) of mbc engineering GmbH. There are some default exchange logic and structures for reuse. This Library will help you on PLC side on some point. Example on handling PCS commands,

## Add the Library Reference



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
	
// set init
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