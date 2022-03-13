You're in a repository that forked a web-based code editor, Monaco, from the Microsoft Corporation.

`rosploco.html` is the file which contains most modifications, as described in the repository description. It has the autocomplete code and the documentation. For unintellectuals using YouTube to learn to make Roblox exploits, this file what you replace `monaco.html` with. Remember to download everything in the repository, though, since we're using Monaco 0.18.1, which might be a different version then what you already have (if you already have Monaco at all).

To use this in your project, you must credit me and Microsoft. We provide an easy way to do this. Just insert this code at the top of the rendered document:

```lua
-- Monaco and Autocomplete by Microsoft and EthanMcBloxxer on GitHub under the MIT License.
```

This is already there by default, so you shouldn't have much trouble.

Since Rosploco is a web-based program (as is Monaco), you can also use it interactively inside of your web browser. Just go to [ethanmcbloxxer.github.io/Rosploco/rosploco.html](https://ethanmcbloxxer.github.io/Rosploco/rosploco.html).

<img src="/context.png" align="right"/>

**ProTip**: We also provide a rich context menu, with options to get help, save the document, clear the editor, and refresh it. If you had these buttons on your exploit previously, you might not need them anymore. Test to be sure, though.

If you realize that some functions or documentation is outdated, then fork this repository, make your changes, and pull request. This is also applied to actual exploit developers, you can add your own functions and documentation to Rosploco in the same way. Just use this exoskeleton:

```js
// Your Exploit

{
	label: "mycustomfunction", // Intellisense label
	kind: monaco.languages.CompletionItemKind.Function, // "Function", "Constant", or "Module" (for libraries, eg Crypt, Bit, etc.)
	detail: "Function", // Keep aligned with `kind`
	documentation: {value: "This is a custom function for example purposes."}, // Your documentation, in Markdown (what appears when you click more info)
  
	insertText: "mycustomfunction(${1:arg1}, \"${2|enumoption1,enumoption2|}\", $0)", // https://code.visualstudio.com/docs/editor/userdefinedsnippets#_snippet-syntax
	insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
},
```

then add that under the proper exploiting autofill section. Remove all the comments other than `// Your Exploit`. Other parameters for this do also exist. Read more the Monaco [CompletionItem](https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.completionitem.html) Interface documentation.

Supported:

* Keywords
* Lua Globals
* Roblox Globals
* bit + bit32
* coroutine
* debug
* math
* os
* string
* table
* utf8
* Methods
	* Instance
	* DataModel (ServiceProvider)
* Events
	* Instance
* Metatables
* Exploiting (Synapse, Script-Ware)
	* Environment
	* Script
	* Table
	* Input
	* Closure
	* Reflection
	* Filesystem
	* Drawing
	* Websocket
