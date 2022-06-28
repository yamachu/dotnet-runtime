const MONO = {}, BINDING = {}, INTERNAL = {};
let ENVIRONMENT_IS_GLOBAL = false;
if (typeof createDotnetRuntime === "function") {
    Module = { ready: Module.ready };
    const extension = createDotnetRuntime({ MONO, BINDING, INTERNAL, Module })
    if (extension.ready) {
        throw new Error("MONO_WASM: Module.ready couldn't be redefined.")
    }
    Object.assign(Module, extension);
    createDotnetRuntime = Module;
}
else if (typeof createDotnetRuntime === "object") {
    Module = { ready: Module.ready, __undefinedConfig: Object.keys(createDotnetRuntime).length === 1 };
    Object.assign(Module, createDotnetRuntime);
    createDotnetRuntime = Module;
}
else {
    throw new Error("MONO_WASM: Can't use moduleFactory callback of createDotnetRuntime function.")
}
var require = require || undefined;
var __dirname = __dirname || '';
if (Module["locateFile"] === undefined) {
    // URL class is undefined on V8(ENVIRONMENT_IS_SHELL)
    // To bypass assigning wasmBinaryFile variable in https://github.com/emscripten-core/emscripten/blob/4b5c4f0694174e081661944ab3ffdb43dd1949b8/src/preamble.js#L792-L793 ,
    // pass implementation of locateFile to Module
    Module["locateFile"] = function(path, prefix) {
        // Constants for environment are defined later.
        // https://github.com/emscripten-core/emscripten/blob/ae675c6abdd7e4a73dfc100b7cd258ef0cec01a2/src/shell.js#L93-L111
        // emcc emits _scriptDir variable top-level, it's initialized by import.meta.url
        if (ENVIRONMENT_IS_SHELL) {
            // it's only used when load dotnet.wasm
            if (prefix === "") {
                return _scriptDir.slice(0, _scriptDir.lastIndexOf("/")) + "/" + path;
            }
            return prefix + path;
        }
        if (ENVIRONMENT_IS_NODE) {
            // it's only used when load dotnet.wasm
            if (prefix === "/") {
                const fileExtensionTrimmedScriptDir = _scriptDir.replace(/^file:\/\//, "");
                return fileExtensionTrimmedScriptDir.slice(0, fileExtensionTrimmedScriptDir.lastIndexOf("/")) + "/" + path;
            }
            return prefix + path;
        }

        return prefix + path;
    }
}