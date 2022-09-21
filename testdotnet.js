// Import as CommonJS module.
const dotnet = require("./artifacts/publish/OmniSharp.WebAssembly.Driver/net6.0/dotnet.js");
// ... or as ECMAScript module in node v17 or later.
//import dotnet from "dotnet.js";

(async function () {
    // Booting the DotNet runtime and invoking entry point.
    await dotnet.boot();
    // Invoking 'GetName()' C# method defined in 'HelloWorld' assembly.
    const guestName = dotnet.OmniSharp.WebAssembly.Driver.GetName();
    console.log(`Welcome, ${guestName}! Enjoy your module space.`);
    dotnet.invokeAsync('OmniSharp.WebAssembly.Driver', 'InitializeAsync')
      .then(data => {
        console.log(data);
      });
})();