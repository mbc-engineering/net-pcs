# CommandBase

## Basics

The CommandBase Function Block can be used to abstract a command Operation from PCS to TwinCat PLC. A Command can be executed and contain some input data from PCS to PLC and some output data from PLC to PCS. A ResultCode describes, a command was executed successfully or not.

The CommandBase should be extended by each project Commands with: `FUNCTION_BLOCK PUBLIC MyProjectCommand EXTENDS CommandBase`. There are the following multiple ways to call the POU. Only one is necessary!

1. In the derived `MyProjectCommand` function block call the basic function block by using `SUPER^();`. Without this call the command doesn't  work.
2. In the derived `MyProjectCommand` function block call the basic function block Method `Call();`. Without this call the command doesn't  work.
3. Call the instance Methode `fbMyProjectCommand.Call();` of `MyProjectCommand` function block. Without this call the command doesn't  work.

## States

The Button has the following default states represented as the `stHandshake.nResultCode` and defined in `E_CommandResultCode`. The following diagram shows in bold lines the regular state flow and in the dashed lines the possible alternative flow.
![CommandBase states Diagram](res/CommandBaseStates.svg)

> It is possible to set `stHandshake.nResultCode` to any `UINT` value for custom purpose. The result code can be set trouth the `Done` Methode.

## Methods

See in the TwinCat 3 Library Manager, in the library. Expand the Command folder and then expand also the `CommandBase` Class to show the methods. By selection of each Method it will show also the explanation.

- Call => The method is called in each cycle to execute the funktional part and the states of the command implementation.
- Init => Will be called when ``stHandshake.bExecute`` changes to ``true`` one cycle and in the same cycle.
- Task => Execute the funktional part of the command implementation in the state running. Will be executed in the same cycle ``stHandshake.bExecute`` changes to ``true`` after the ``Init`` Method. 
- Done => Will be called when ``Running`` state is done and the actual state is ``Done``.
- Cancelled => Will be called at the Cancel state when the execution is finished with the result code ``Cancelled`` or ``stHandshake.bExecute`` changes to ``false`` while ``stHandshake.bBusy`` is ```true``
- CalculateProgress => Can be used to calculate the ``stHandshake.nProgress`` and ``stHandshake.nSubTask``
- Finish => Can be use to finish the ``Task`` Execution with a ``nResultCode``. By using the ``Finish(E_CommandResultCode.Cancelled)`` or ``Finish(E_CommandResultCode.Done)`` calls, to the coresponding states methods ``Done`` and ``Cancelled`` will be called.

## Properties

See in the TwinCat 3 Library Manager, in the library. Expand the Command folder and then expand also the `CommandBase` Class to show the Properties. By selection of each Property it will show also the explanation.

- Progress => Set the Progress from everywhere in the application (Regular from CalculateProgress Method)
- SubTask => Set the SubTask Information from everywhere in the application (Regular from CalculateProgress Method)

## Communication structure to PCS

In the structure `ST_CommandBaseHandshake` is used to communicate with the PCS. The flags `bExecute` and `bBusy` has a Impact to command [state](#States).

| Symbol Name   | Datatype | PLC | PCS | Description                                                                                                                                                                                                                            |
|---------------|----------|-----|-----|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `bExecute`    | BOOL     | RW  | RW  | Will be set to true from the PCS to start the command. When the operation is finished, the PLC will it set to false. On a long running Task, the PCS can reset to false for Abort the operation. The state will change to `Cancelled`. |
| `bBusy`       | BOOL     | RW  | R   | Is True when the command is executing.                                                                                                                                                                                                 |
| `bEnabled`    | BOOL     | RW  | R   | Will be set from PLC. Enables or disables a command. Variable will be initialized as True. To disable a command it explicit has to set False from PLC.                                                                                                                                                                                             |
| `nResultCode` | UINT     | RW  | R   | Shows the operation result code of the commando state. Default Values see `CommandResultCode` Enum Type. It is possible to set other codes!                                                                                            |
| `nProgress`   | BYTE     | RW  | R   | Shows the optional calculated progress. The value is depend on the command implementation. (default can be 0..100%). Only necessary on long running operations.                                                                        |
| `nSubTask`    | UINT     | RW  | R   | Shows the optional state of long running Command Execution. It is possible to set it on own need.                                                                                                                                      |


### Values of CommandResultCode:

```
TYPE E_CommandResultCode :
(
	Init           := 0,
	Running        := 1,
	Done           := 2,
	Cancelled      := 3,
	StartCustom    := 100
) UINT;
END_TYPE
```

## Command initialisation

Sometimes it is helpfull to give some initial values to a generic implementation of a command. For example to set a different initial value at multiple command instances with the same behavor. Or set different delay times to a command. This can be done by creating a Method called `FB_init`. In the `VAR_INPUT` section define your inital variables and use it. *Be aware that the two from TwinCat 3 required leading variables `bInitRetains : BOOL` and `bInCopyCode : BOOL;` are present.* By declarating the POU you can set it like in the following example:

**Method MyDelayedCommand.FB_init:**
```C
METHOD FB_init : BOOL
VAR_INPUT
	bInitRetains : BOOL; // if TRUE, the retain variables are initialized (warm start / cold start)
	bInCopyCode  : BOOL; // if TRUE, the instance afterwards gets moved into the copy code (online change)
	tDelayTime   : TIME; // Time to deleay the Running state
END_VAR

// initialize the TON fb
fbTonDelay.PT := tDelayTime;
```

**Declaration of the function block MyDelayedCommand:**
```c
fbDelayedCommand1 : MyDelayedCommand(tDelayTime := T#4S);
```

> The technique behind is documented under [Infosys FB_init](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_plc_intro/5094414603.html&id=6967794353598129051).

> There is also a helpful attribute named `call_after_init` that calls a custom Method after processing the FB_init() initialisation method, see under [Infosys Attribute `call_after_init`](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_plc_intro/2529600907.html&id=1875029305085369793).

## Command Parameters

Some commands needs more informations than just when it should execute. There comes the `PlcCommandInput` and `PlcCommandOutput` attribute` in the game. Attributes are variable declaration decorators. This attributes will be read from the PCS .Net Library to identify, which of the Variables are needed for data exchange. The Advantage with attributes is, the `VAR_INPUT` and `VAR_OUTPUT` declaration is left free for regular PLC use.

Followed a example with defined In- and Output variables for a command. 

```C
FUNCTION_BLOCK AddCommand EXTENDS CommandBase
VAR
	{attribute 'PlcCommandInput'}
	Val1 			: REAL;				// Value 1
	{attribute 'PlcCommandInput'}
	Val2 			: REAL;				// Value 2
	{attribute 'PlcCommandOutput'}
	Result 			: REAL;				// The result of the addition command
END_VAR
```

> The technique behind User-defined attributes is documented under [Infosys User-defined attributes](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_plc_intro/2529572491.html&id=2923905819010174145).

> The defined attributes will be needed from the `Mbc.Pcs.Net` library version 1.2 and above.

## Quick start with your first Command

**Goal:** 
Get the information when the command are executed triggered from the PCS (there is maybe a button). We wanna only a simple impulse when that happen on the output `Q`. 

First add a new POU for the Command that extends the Library Class `CommandBase`.

Basely you have a working command that does nothing important. So we add a Output Parameter `Q`. Also a `bInit` for later use.

```
FUNCTION_BLOCK PUBLIC StartCommand EXTENDS CommandBase
VAR_INPUT
END_VAR
VAR_OUTPUT	
	Q : BOOL;
END_VAR
VAR
	bInit 	: BOOL;
END_VAR

SUPER^();
```

Then we ned to now when the Command is executed. For that we add a Method with the Name `Init` to the Class.

```
METHOD PROTECTED Init

// Init Code:
bInit := TRUE;
```

The Next step is to implement to logic Part of the command, to ged the information when the command are executing and to set the the impulse on `Q` when that happens. For this add a Method with the Name `Task` to the Class.

```
METHOD PROTECTED Task : BOOL
	
// Task Code:
// dedect task completed
IF (NOT bInit AND Q) THEN
	// Task comlete;
	Task := SUPER^.Task();;
END_IF

// set output trigger
Q := bInit;

// Reset after 1. cycle
bInit := FALSE;
```

The next step is to Declare the POU on a Global Variable List, in this example it is on `Commands`:

```
VAR_GLOBAL
	StartCommand1		: StartCommand;
END_VAR
```

In the main Program you should now call the Button in a cycle behavior.

```
Commands.StartCommand1();
	
fbTofStartCommand1(
	IN := Commands.StartCommand1.Q,
	PT := T#2S);		
```

Now there is simple Start Button created.

To Test the correctness of the behavior. Set the following internal `SartCommand` flag `Commands.StartCommand1.stHandshake.bExecute` in the online Scope to True.