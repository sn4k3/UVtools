# UVtools Scripting

UVtools has become a big toolset, but do you know you can do your own scripts not limited 
to what GUI offers you? With scripts you can have access to whole UVtools.Core, 
which expose all core access to you to use, or use your own implementations. 
If you have a workflow and need to speed up, scripts can save you time and do some out-mind modifications. 
You will get same performance as native calling, plus you can stack all your actions and much more!

UVtools scripts are based on the C# "Roslyn Scripting API", this mean you will have to use the C# language
and all its functionalities like a native C# program, in the end you will use the same language,
same syntax, same way of code of UVtools, programming scripts will fell like the same as programming the UVtools.
So many examples and how to do some stuff can be found under the UVtools source code.

Scripts are interpreted, compiled and run in the runtime just like any other scripting languages. 

**WARNING:** Running scripts are very powerfull and with wide access on your system.
Never run scripts from untrusted sources! Always inspect the script content before run something new from others. 
If you unsure about a script, run UVtools under a sandbox.

## How to run scripts (For users)

1. Go to Tools - Scripting
2. Load the script file into the dialog
3. Configure user inputs as needed
4. Execute

## How to write scripts (For developers)

The Roslyn Scripts runs C# code directly but require some common data structures to be removed from the common csharp file,
such as "namespace" which is obligatory on a normal csharp file, and also a csharp file requires an class to encapsulate your code, you can't have code outside classes. 

Scripts in otherway can run code directly without any namespace or class, so you can have a script just with "1 + 1",
that will be interpreted and return the integer of 2, while the namespace and/or class will not be required and namespace is forbidden to use on a script, that comes with a "problem":

As C# IDE's requires an valid file structure it will trigger a lot of errors if you try to write the script from there, because of file bad syntax.
So the alternative was to write a script without any hints, highlights, code completion etc, and that is a big no and would slow you down, even if are very good at "blind code and C#"
bad things would happen and your script will come full of bugs, syntax errors, bad names, etc.

To overcome this problem UVtools allows you to run a normal .cs file in place, wrote with the normal data scrutures that C# requires to compile, just like an regular program,
this is possible by add a layer of abstraction for the file within it class, which "emulate" the script globals, 
so you can write in any IDE, take advantage on code completion and other goodies, plus you can compile the code without any problem!

On runtime UVtools will parse and strip the file and remove everthing that must be removed in order to run the script, that is the namespace and class so far.
With this approach you dont need to convert your .cs file to a script file .csx, you can just write the code, refresh script on UVtools and it will load from there. 

### How to setup the development environment

1. Start by cloning/download UVtools code, this will give you access to all code completion, syntax, variables, constants etc. 
   - Git: https://github.com/sn4k3/UVtools.git
   - Download as zip: https://github.com/sn4k3/UVtools/archive/refs/heads/master.zip
2. Extract the contents to a folder, eg: UVtoolsDev
3. Now you need something to write code (IDE), choose one of:
   - [JetBrains Rider](https://www.jetbrains.com/rider):
      1. After installing, open the Rider
      2. Click on "Open"
      3. Locate and open the file: `UVtools/Scripts/UVtools.ScriptSample/UVtools.ScriptSample.csproj`
   - [Microsoft Visual Studio Code](https://code.visualstudio.com):
       1. After installing, open the Visual Code
       2. Go to File -> Open Folder (Ctrl + O)
       3. Locate and open the folder: `UVtools/Scripts/UVtools.ScriptSample`
       4. Now with the project open, locate and open "ScriptInsetSample.cs" inside Visual Code
       5. A popup message will show on bottom right, asking if you want to install the "C# dev kit" extension, accept and click install
       6. After get the confirmation of a successful installation i recommend to restart the Visual Code, you can close it and open again, last selected project will auto load with
   - [Microsoft Visual Studio Community](https://visualstudio.microsoft.com) (Windows-only):
      1. Install with ".NET Desktop development" workload selected
      1. After installing, open the Visual Studio
      2. Click on "Open a project or solution"
      3. Locate and open the file: `UVtools/Scripts/UVtools.ScriptSample/UVtools.ScriptSample.csproj`
4. Now open the "ScriptInsetSample.cs" again
5. Locate "public void ScriptInit()"
6. On "Script.xxxx" put the cursor after the dot and make sure the caret is located just after the dot, eg: Script.|
7. Now press Ctrl + Space, if extension is correctly loaded a popup will show with code completion, showing all possible variables and calls
8. If you hover the mouse over a variable it also must show a popup with information about it

### Script structure

```C#
// Tells your file to reference that library and import all from there
// References can be imported by IDE when required
using ReferenceName; 

namespace UVtools.ScriptSample // Require to compile and have the IDE help
{
    public class YourScriptName : ScriptGlobals // Require to compile and have the IDE help
    {
        // Put your user inputs here, for example:
        ScriptNumericalInput<ushort> InsetMarginFromEdge = new()
        {
            Label = "Inset from edge",
            ToolTip = "Margin in pixels to inset from object edge",
            Unit = "px",
            Minimum = 1,
            Maximum = ushort.MaxValue,
            Increment = 1,
            Value = 20
        };
        // ..

        /// <summary>
        /// Set configurations here, this function trigger just after load a script
        /// </summary>
        public void ScriptInit()
        {
            Script.Name = "Inset";
            Script.Description = "Performs a black inset around objects";
            Script.Author = "Tiago Conceição";
            Script.Version = new Version(0, 1);
            Script.UserInputs.AddRange(new[]
            {
                InsetMarginFromEdge,
            });
        }

        /// <summary>
        /// Validate user inputs here, this function trigger when user click on execute
        /// </summary>
        /// <returns>A error message, empty or null if validation passes.</returns>
        public string? ScriptValidate()
        {
            StringBuilder sb = new();
            
            if (InsetMarginFromEdge.Value < InsetMarginFromEdge.Minimum)
            {
                sb.AppendLine($"Inset edge margin must be at least {InsetMarginFromEdge.Minimum}{InsetMarginFromEdge.Unit}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Execute the script, this function trigger when when user click on execute and validation passes
        /// </summary>
        /// <returns>True if executes successfully to the end, otherwise false.</returns>
        public bool ScriptExecute()
        {
            // Your actual executation code, look at sample file
        }
    }
}
```

So in other words, your script must have the following methods: ScriptInit(), ScriptValidate() and ScriptExecute().
And they are executed in that same order when invoked (First init, then validate and then executes)


### How to bootstrap your script

1. First duplicate the "ScriptInsetSample.cs" and rename it to whatever makes sense to you
2. Open your file
3. Change the "public class ScriptInsetSample" to "public class TheFileNameYouJustGive", note that class name must not contain other than A-Z words, it can have diferent name from file
4. Locate "public void ScriptInit()" and modify with your information
5. Create your user inputs if any and add them to configuration, they will show under UVtools dialog and user can modify the values
6. Locate "public void ScriptValidate()" and put conditions to validate user inputs, if they exists
7. Locate "public void ScriptExecute()" and write your code there

The "ScriptExecute" sample provides a commum base for almost all operations, in most of the times you will loop
layers and apply some modification. The set example uses a Parallel.For which distribute all the workload to CPU cores, 
manipulating images which is heavy, but if you require a simple light task, like set or get information, 
you must use regular for or foreach loops. But if you are editing layers you can use this as base, and your actual code 
will start just after the "var target = Operation.GetRoiOrDefault(mat);" and before " Operation.ApplyMask(original, target);"

You can also see other samples, or if curious you can open UVtools.Core.Operations, pick one and find the Execute method to see how things are done,
there you can get some inspiration and even copy some blocks!

### Codding errors

IDE will provide code completion and syntax help, but when it show a error you must address it, red underline text is often errors..

When loading a script with errors on UVtools, a error message will pop and execution will stop.

### How to share your script

To share your script you must provide the file you just did, the .cs.

To public share you can use UVtools github: https://github.com/sn4k3/UVtools/discussions/categories/scripts

## Get help

This guide is more about to understand the scripting structure and setup the first script, this it's not about learn how to code, 
if you are a C# programmer than this is not a problem, 
but if you are starting you can search some tutorials on internet, often you can google what you want, 
for example Google: "C# get current time", and a lot of examples will tell you how to do that. 
It will take try, error and pratice but once you learn this skill it will be very easy to do this scripts.

If you need some help regarding with UVtools Core codding you can post your questions at github: https://github.com/sn4k3/UVtools/discussions/categories/scripts



