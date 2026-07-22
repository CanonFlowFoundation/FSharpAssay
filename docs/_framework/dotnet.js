//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

var e=!1;const t=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,4,1,96,0,0,3,2,1,0,10,8,1,6,0,6,64,25,11,11])),o=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,5,1,96,0,1,123,3,2,1,0,10,15,1,13,0,65,1,253,15,65,2,253,15,253,128,2,11])),n=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,5,1,96,0,1,123,3,2,1,0,10,10,1,8,0,65,0,253,15,253,98,11])),r=Symbol.for("wasm promise_control");function i(e,t){let o=null;const n=new Promise((function(n,r){o={isDone:!1,promise:null,resolve:t=>{o.isDone||(o.isDone=!0,n(t),e&&e())},reject:e=>{o.isDone||(o.isDone=!0,r(e),t&&t())}}}));o.promise=n;const i=n;return i[r]=o,{promise:i,promise_control:o}}function s(e){return e[r]}function a(e){e&&function(e){return void 0!==e[r]}(e)||Be(!1,"Promise is not controllable")}const l="__mono_message__",c=["debug","log","trace","warn","info","error"],d="MONO_WASM: ";let u,f,m,g,p,h;function w(e){g=e}function b(e){if(Pe.diagnosticTracing){const t="function"==typeof e?e():e;console.debug(d+t)}}function y(e,...t){console.info(d+e,...t)}function v(e,...t){console.info(e,...t)}function E(e,...t){console.warn(d+e,...t)}function _(e,...t){if(t&&t.length>0&&t[0]&&"object"==typeof t[0]){if(t[0].silent)return;if(t[0].toString)return void console.error(d+e,t[0].toString())}console.error(d+e,...t)}function x(e,t,o){return function(...n){try{let r=n[0];if(void 0===r)r="undefined";else if(null===r)r="null";else if("function"==typeof r)r=r.toString();else if("string"!=typeof r)try{r=JSON.stringify(r)}catch(e){r=r.toString()}t(o?JSON.stringify({method:e,payload:r,arguments:n.slice(1)}):[e+r,...n.slice(1)])}catch(e){m.error(`proxyConsole failed: ${e}`)}}}function j(e,t,o){f=t,g=e,m={...t};const n=`${o}/console`.replace("https://","wss://").replace("http://","ws://");u=new WebSocket(n),u.addEventListener("error",A),u.addEventListener("close",S),function(){for(const e of c)f[e]=x(`console.${e}`,T,!0)}()}function R(e){let t=30;const o=()=>{u?0==u.bufferedAmount||0==t?(e&&v(e),function(){for(const e of c)f[e]=x(`console.${e}`,m.log,!1)}(),u.removeEventListener("error",A),u.removeEventListener("close",S),u.close(1e3,e),u=void 0):(t--,globalThis.setTimeout(o,100)):e&&m&&m.log(e)};o()}function T(e){u&&u.readyState===WebSocket.OPEN?u.send(e):m.log(e)}function A(e){m.error(`[${g}] proxy console websocket error: ${e}`,e)}function S(e){m.debug(`[${g}] proxy console websocket closed: ${e}`,e)}function D(){Pe.preferredIcuAsset=O(Pe.config);let e="invariant"==Pe.config.globalizationMode;if(!e)if(Pe.preferredIcuAsset)Pe.diagnosticTracing&&b("ICU data archive(s) available, disabling invariant mode");else{if("custom"===Pe.config.globalizationMode||"all"===Pe.config.globalizationMode||"sharded"===Pe.config.globalizationMode){const e="invariant globalization mode is inactive and no ICU data archives are available";throw _(`ERROR: ${e}`),new Error(e)}Pe.diagnosticTracing&&b("ICU data archive(s) not available, using invariant globalization mode"),e=!0,Pe.preferredIcuAsset=null}const t="DOTNET_SYSTEM_GLOBALIZATION_INVARIANT",o=Pe.config.environmentVariables;if(void 0===o[t]&&e&&(o[t]="1"),void 0===o.TZ)try{const e=Intl.DateTimeFormat().resolvedOptions().timeZone||null;e&&(o.TZ=e)}catch(e){y("failed to detect timezone, will fallback to UTC")}}function O(e){var t;if((null===(t=e.resources)||void 0===t?void 0:t.icu)&&"invariant"!=e.globalizationMode){const t=e.applicationCulture||(ke?globalThis.navigator&&globalThis.navigator.languages&&globalThis.navigator.languages[0]:Intl.DateTimeFormat().resolvedOptions().locale),o=e.resources.icu;let n=null;if("custom"===e.globalizationMode){if(o.length>=1)return o[0].name}else t&&"all"!==e.globalizationMode?"sharded"===e.globalizationMode&&(n=function(e){const t=e.split("-")[0];return"en"===t||["fr","fr-FR","it","it-IT","de","de-DE","es","es-ES"].includes(e)?"icudt_EFIGS.dat":["zh","ko","ja"].includes(t)?"icudt_CJK.dat":"icudt_no_CJK.dat"}(t)):n="icudt.dat";if(n)for(let e=0;e<o.length;e++){const t=o[e];if(t.virtualPath===n)return t.name}}return e.globalizationMode="invariant",null}(new Date).valueOf();const C=class{constructor(e){this.url=e}toString(){return this.url}};async function k(e,t){try{const o="function"==typeof globalThis.fetch;if(Se){const n=e.startsWith("file://");if(!n&&o)return globalThis.fetch(e,t||{credentials:"same-origin"});p||(h=Ne.require("url"),p=Ne.require("fs")),n&&(e=h.fileURLToPath(e));const r=await p.promises.readFile(e);return{ok:!0,headers:{length:0,get:()=>null},url:e,arrayBuffer:()=>r,json:()=>JSON.parse(r),text:()=>{throw new Error("NotImplementedException")}}}if(o)return globalThis.fetch(e,t||{credentials:"same-origin"});if("function"==typeof read)return{ok:!0,url:e,headers:{length:0,get:()=>null},arrayBuffer:()=>new Uint8Array(read(e,"binary")),json:()=>JSON.parse(read(e,"utf8")),text:()=>read(e,"utf8")}}catch(t){return{ok:!1,url:e,status:500,headers:{length:0,get:()=>null},statusText:"ERR28: "+t,arrayBuffer:()=>{throw t},json:()=>{throw t},text:()=>{throw t}}}throw new Error("No fetch implementation available")}function I(e){return"string"!=typeof e&&Be(!1,"url must be a string"),!M(e)&&0!==e.indexOf("./")&&0!==e.indexOf("../")&&globalThis.URL&&globalThis.document&&globalThis.document.baseURI&&(e=new URL(e,globalThis.document.baseURI).toString()),e}const U=/^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//,P=/[a-zA-Z]:[\\/]/;function M(e){return Se||Ie?e.startsWith("/")||e.startsWith("\\")||-1!==e.indexOf("///")||P.test(e):U.test(e)}let L,N=0;const $=[],z=[],W=new Map,F={"js-module-threads":!0,"js-module-runtime":!0,"js-module-dotnet":!0,"js-module-native":!0,"js-module-diagnostics":!0},B={...F,"js-module-library-initializer":!0},V={...F,dotnetwasm:!0,heap:!0,manifest:!0},q={...B,manifest:!0},H={...B,dotnetwasm:!0},J={dotnetwasm:!0,symbols:!0},Z={...B,dotnetwasm:!0,symbols:!0},Q={symbols:!0};function G(e){return!("icu"==e.behavior&&e.name!=Pe.preferredIcuAsset)}function K(e,t,o){null!=t||(t=[]),Be(1==t.length,`Expect to have one ${o} asset in resources`);const n=t[0];return n.behavior=o,X(n),e.push(n),n}function X(e){V[e.behavior]&&W.set(e.behavior,e)}function Y(e){Be(V[e],`Unknown single asset behavior ${e}`);const t=W.get(e);if(t&&!t.resolvedUrl)if(t.resolvedUrl=Pe.locateFile(t.name),F[t.behavior]){const e=ge(t);e?("string"!=typeof e&&Be(!1,"loadBootResource response for 'dotnetjs' type should be a URL string"),t.resolvedUrl=e):t.resolvedUrl=ce(t.resolvedUrl,t.behavior)}else if("dotnetwasm"!==t.behavior)throw new Error(`Unknown single asset behavior ${e}`);return t}function ee(e){const t=Y(e);return Be(t,`Single asset for ${e} not found`),t}let te=!1;async function oe(){if(!te){te=!0,Pe.diagnosticTracing&&b("mono_download_assets");try{const e=[],t=[],o=(e,t)=>{!Z[e.behavior]&&G(e)&&Pe.expected_instantiated_assets_count++,!H[e.behavior]&&G(e)&&(Pe.expected_downloaded_assets_count++,t.push(se(e)))};for(const t of $)o(t,e);for(const e of z)o(e,t);Pe.allDownloadsQueued.promise_control.resolve(),Promise.all([...e,...t]).then((()=>{Pe.allDownloadsFinished.promise_control.resolve()})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e})),await Pe.runtimeModuleLoaded.promise;const n=async e=>{const t=await e;if(t.buffer){if(!Z[t.behavior]){t.buffer&&"object"==typeof t.buffer||Be(!1,"asset buffer must be array-like or buffer-like or promise of these"),"string"!=typeof t.resolvedUrl&&Be(!1,"resolvedUrl must be string");const e=t.resolvedUrl,o=await t.buffer,n=new Uint8Array(o);pe(t),await Ue.beforeOnRuntimeInitialized.promise,Ue.instantiate_asset(t,e,n)}}else J[t.behavior]?("symbols"===t.behavior&&(await Ue.instantiate_symbols_asset(t),pe(t)),J[t.behavior]&&++Pe.actual_downloaded_assets_count):(t.isOptional||Be(!1,"Expected asset to have the downloaded buffer"),!H[t.behavior]&&G(t)&&Pe.expected_downloaded_assets_count--,!Z[t.behavior]&&G(t)&&Pe.expected_instantiated_assets_count--)},r=[],i=[];for(const t of e)r.push(n(t));for(const e of t)i.push(n(e));Promise.all(r).then((()=>{Ce||Ue.coreAssetsInMemory.promise_control.resolve()})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e})),Promise.all(i).then((async()=>{Ce||(await Ue.coreAssetsInMemory.promise,Ue.allAssetsInMemory.promise_control.resolve())})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e}))}catch(e){throw Pe.err("Error in mono_download_assets: "+e),e}}}let ne=!1;function re(){if(ne)return;ne=!0;const e=Pe.config,t=[];if(e.assets)for(const t of e.assets)"object"!=typeof t&&Be(!1,`asset must be object, it was ${typeof t} : ${t}`),"string"!=typeof t.behavior&&Be(!1,"asset behavior must be known string"),"string"!=typeof t.name&&Be(!1,"asset name must be string"),t.resolvedUrl&&"string"!=typeof t.resolvedUrl&&Be(!1,"asset resolvedUrl could be string"),t.hash&&"string"!=typeof t.hash&&Be(!1,"asset resolvedUrl could be string"),t.pendingDownload&&"object"!=typeof t.pendingDownload&&Be(!1,"asset pendingDownload could be object"),t.isCore?$.push(t):z.push(t),X(t);else if(e.resources){const o=e.resources;o.wasmNative||Be(!1,"resources.wasmNative must be defined"),o.jsModuleNative||Be(!1,"resources.jsModuleNative must be defined"),o.jsModuleRuntime||Be(!1,"resources.jsModuleRuntime must be defined"),K(z,o.wasmNative,"dotnetwasm"),K(t,o.jsModuleNative,"js-module-native"),K(t,o.jsModuleRuntime,"js-module-runtime"),o.jsModuleDiagnostics&&K(t,o.jsModuleDiagnostics,"js-module-diagnostics");const n=(e,t,o)=>{const n=e;n.behavior=t,o?(n.isCore=!0,$.push(n)):z.push(n)};if(o.coreAssembly)for(let e=0;e<o.coreAssembly.length;e++)n(o.coreAssembly[e],"assembly",!0);if(o.assembly)for(let e=0;e<o.assembly.length;e++)n(o.assembly[e],"assembly",!o.coreAssembly);if(0!=e.debugLevel&&Pe.isDebuggingSupported()){if(o.corePdb)for(let e=0;e<o.corePdb.length;e++)n(o.corePdb[e],"pdb",!0);if(o.pdb)for(let e=0;e<o.pdb.length;e++)n(o.pdb[e],"pdb",!o.corePdb)}if(e.loadAllSatelliteResources&&o.satelliteResources)for(const e in o.satelliteResources)for(let t=0;t<o.satelliteResources[e].length;t++){const r=o.satelliteResources[e][t];r.culture=e,n(r,"resource",!o.coreAssembly)}if(o.coreVfs)for(let e=0;e<o.coreVfs.length;e++)n(o.coreVfs[e],"vfs",!0);if(o.vfs)for(let e=0;e<o.vfs.length;e++)n(o.vfs[e],"vfs",!o.coreVfs);const r=O(e);if(r&&o.icu)for(let e=0;e<o.icu.length;e++){const t=o.icu[e];t.name===r&&n(t,"icu",!1)}if(o.wasmSymbols)for(let e=0;e<o.wasmSymbols.length;e++)n(o.wasmSymbols[e],"symbols",!1)}if(e.appsettings)for(let t=0;t<e.appsettings.length;t++){const o=e.appsettings[t],n=he(o);"appsettings.json"!==n&&n!==`appsettings.${e.applicationEnvironment}.json`||z.push({name:o,behavior:"vfs",cache:"no-cache",useCredentials:!0})}e.assets=[...$,...z,...t]}async function ie(e){const t=await se(e);return await t.pendingDownloadInternal.response,t.buffer}async function se(e){try{return await ae(e)}catch(t){if(!Pe.enableDownloadRetry)throw t;if(Ie||Se)throw t;if(e.pendingDownload&&e.pendingDownloadInternal==e.pendingDownload)throw t;if(e.resolvedUrl&&-1!=e.resolvedUrl.indexOf("file://"))throw t;if(t&&404==t.status)throw t;e.pendingDownloadInternal=void 0,await Pe.allDownloadsQueued.promise;try{return Pe.diagnosticTracing&&b(`Retrying download '${e.name}'`),await ae(e)}catch(t){return e.pendingDownloadInternal=void 0,await new Promise((e=>globalThis.setTimeout(e,100))),Pe.diagnosticTracing&&b(`Retrying download (2) '${e.name}' after delay`),await ae(e)}}}async function ae(e){for(;L;)await L.promise;try{++N,N==Pe.maxParallelDownloads&&(Pe.diagnosticTracing&&b("Throttling further parallel downloads"),L=i());const t=await async function(e){if(e.pendingDownload&&(e.pendingDownloadInternal=e.pendingDownload),e.pendingDownloadInternal&&e.pendingDownloadInternal.response)return e.pendingDownloadInternal.response;if(e.buffer){const t=await e.buffer;return e.resolvedUrl||(e.resolvedUrl="undefined://"+e.name),e.pendingDownloadInternal={url:e.resolvedUrl,name:e.name,response:Promise.resolve({ok:!0,arrayBuffer:()=>t,json:()=>JSON.parse(new TextDecoder("utf-8").decode(t)),text:()=>{throw new Error("NotImplementedException")},headers:{get:()=>{}}})},e.pendingDownloadInternal.response}const t=e.loadRemote&&Pe.config.remoteSources?Pe.config.remoteSources:[""];let o;for(let n of t){n=n.trim(),"./"===n&&(n="");const t=le(e,n);e.name===t?Pe.diagnosticTracing&&b(`Attempting to download '${t}'`):Pe.diagnosticTracing&&b(`Attempting to download '${t}' for ${e.name}`);try{e.resolvedUrl=t;const n=fe(e);if(e.pendingDownloadInternal=n,o=await n.response,!o||!o.ok)continue;return o}catch(e){o||(o={ok:!1,url:t,status:0,statusText:""+e});continue}}const n=e.isOptional||e.name.match(/\.pdb$/)&&Pe.config.ignorePdbLoadErrors;if(o||Be(!1,`Response undefined ${e.name}`),!n){const t=new Error(`download '${o.url}' for ${e.name} failed ${o.status} ${o.statusText}`);throw t.status=o.status,t}y(`optional download '${o.url}' for ${e.name} failed ${o.status} ${o.statusText}`)}(e);return t?(J[e.behavior]||(e.buffer=await t.arrayBuffer(),++Pe.actual_downloaded_assets_count),e):e}finally{if(--N,L&&N==Pe.maxParallelDownloads-1){Pe.diagnosticTracing&&b("Resuming more parallel downloads");const e=L;L=void 0,e.promise_control.resolve()}}}function le(e,t){let o;return null==t&&Be(!1,`sourcePrefix must be provided for ${e.name}`),e.resolvedUrl?o=e.resolvedUrl:(o=""===t?"assembly"===e.behavior||"pdb"===e.behavior?e.name:"resource"===e.behavior&&e.culture&&""!==e.culture?`${e.culture}/${e.name}`:e.name:t+e.name,o=ce(Pe.locateFile(o),e.behavior)),o&&"string"==typeof o||Be(!1,"attemptUrl need to be path or url string"),o}function ce(e,t){return Pe.modulesUniqueQuery&&q[t]&&(e+=Pe.modulesUniqueQuery),e}let de=0;const ue=new Set;function fe(e){try{e.resolvedUrl||Be(!1,"Request's resolvedUrl must be set");const t=function(e){let t=e.resolvedUrl;if(Pe.loadBootResource){const o=ge(e);if(o instanceof Promise)return o;"string"==typeof o&&(t=o)}const o={};return e.cache?o.cache=e.cache:Pe.config.disableNoCacheFetch||(o.cache="no-cache"),e.useCredentials?o.credentials="include":!Pe.config.disableIntegrityCheck&&e.hash&&(o.integrity=e.hash),Pe.fetch_like(t,o)}(e),o={name:e.name,url:e.resolvedUrl,response:t};return ue.add(e.name),o.response.then((()=>{"assembly"==e.behavior&&Pe.loadedAssemblies.push(e.name),de++,Pe.onDownloadResourceProgress&&Pe.onDownloadResourceProgress(de,ue.size)})),o}catch(t){const o={ok:!1,url:e.resolvedUrl,status:500,statusText:"ERR29: "+t,arrayBuffer:()=>{throw t},json:()=>{throw t}};return{name:e.name,url:e.resolvedUrl,response:Promise.resolve(o)}}}const me={resource:"assembly",assembly:"assembly",pdb:"pdb",icu:"globalization",vfs:"configuration",manifest:"manifest",dotnetwasm:"dotnetwasm","js-module-dotnet":"dotnetjs","js-module-native":"dotnetjs","js-module-runtime":"dotnetjs","js-module-threads":"dotnetjs"};function ge(e){var t;if(Pe.loadBootResource){const o=null!==(t=e.hash)&&void 0!==t?t:"",n=e.resolvedUrl,r=me[e.behavior];if(r){const t=Pe.loadBootResource(r,e.name,n,o,e.behavior);return"string"==typeof t?I(t):t}}}function pe(e){e.pendingDownloadInternal=null,e.pendingDownload=null,e.buffer=null,e.moduleExports=null}function he(e){let t=e.lastIndexOf("/");return t>=0&&t++,e.substring(t)}async function we(e){e&&await Promise.all((null!=e?e:[]).map((e=>async function(e){try{const t=e.name;if(!e.moduleExports){const o=ce(Pe.locateFile(t),"js-module-library-initializer");Pe.diagnosticTracing&&b(`Attempting to import '${o}' for ${e}`),e.moduleExports=await import(/*! webpackIgnore: true */o)}Pe.libraryInitializers.push({scriptName:t,exports:e.moduleExports})}catch(t){E(`Failed to import library initializer '${e}': ${t}`)}}(e))))}async function be(e,t){if(!Pe.libraryInitializers)return;const o=[];for(let n=0;n<Pe.libraryInitializers.length;n++){const r=Pe.libraryInitializers[n];r.exports[e]&&o.push(ye(r.scriptName,e,(()=>r.exports[e](...t))))}await Promise.all(o)}async function ye(e,t,o){try{await o()}catch(o){throw E(`Failed to invoke '${t}' on library initializer '${e}': ${o}`),Xe(1,o),o}}function ve(e,t){if(e===t)return e;const o={...t};return void 0!==o.assets&&o.assets!==e.assets&&(o.assets=[...e.assets||[],...o.assets||[]]),void 0!==o.resources&&(o.resources=_e(e.resources||{assembly:[],jsModuleNative:[],jsModuleRuntime:[],wasmNative:[]},o.resources)),void 0!==o.environmentVariables&&(o.environmentVariables={...e.environmentVariables||{},...o.environmentVariables||{}}),void 0!==o.runtimeOptions&&o.runtimeOptions!==e.runtimeOptions&&(o.runtimeOptions=[...e.runtimeOptions||[],...o.runtimeOptions||[]]),Object.assign(e,o)}function Ee(e,t){if(e===t)return e;const o={...t};return o.config&&(e.config||(e.config={}),o.config=ve(e.config,o.config)),Object.assign(e,o)}function _e(e,t){if(e===t)return e;const o={...t};return void 0!==o.coreAssembly&&(o.coreAssembly=[...e.coreAssembly||[],...o.coreAssembly||[]]),void 0!==o.assembly&&(o.assembly=[...e.assembly||[],...o.assembly||[]]),void 0!==o.lazyAssembly&&(o.lazyAssembly=[...e.lazyAssembly||[],...o.lazyAssembly||[]]),void 0!==o.corePdb&&(o.corePdb=[...e.corePdb||[],...o.corePdb||[]]),void 0!==o.pdb&&(o.pdb=[...e.pdb||[],...o.pdb||[]]),void 0!==o.jsModuleWorker&&(o.jsModuleWorker=[...e.jsModuleWorker||[],...o.jsModuleWorker||[]]),void 0!==o.jsModuleNative&&(o.jsModuleNative=[...e.jsModuleNative||[],...o.jsModuleNative||[]]),void 0!==o.jsModuleDiagnostics&&(o.jsModuleDiagnostics=[...e.jsModuleDiagnostics||[],...o.jsModuleDiagnostics||[]]),void 0!==o.jsModuleRuntime&&(o.jsModuleRuntime=[...e.jsModuleRuntime||[],...o.jsModuleRuntime||[]]),void 0!==o.wasmSymbols&&(o.wasmSymbols=[...e.wasmSymbols||[],...o.wasmSymbols||[]]),void 0!==o.wasmNative&&(o.wasmNative=[...e.wasmNative||[],...o.wasmNative||[]]),void 0!==o.icu&&(o.icu=[...e.icu||[],...o.icu||[]]),void 0!==o.satelliteResources&&(o.satelliteResources=function(e,t){if(e===t)return e;for(const o in t)e[o]=[...e[o]||[],...t[o]||[]];return e}(e.satelliteResources||{},o.satelliteResources||{})),void 0!==o.modulesAfterConfigLoaded&&(o.modulesAfterConfigLoaded=[...e.modulesAfterConfigLoaded||[],...o.modulesAfterConfigLoaded||[]]),void 0!==o.modulesAfterRuntimeReady&&(o.modulesAfterRuntimeReady=[...e.modulesAfterRuntimeReady||[],...o.modulesAfterRuntimeReady||[]]),void 0!==o.extensions&&(o.extensions={...e.extensions||{},...o.extensions||{}}),void 0!==o.vfs&&(o.vfs=[...e.vfs||[],...o.vfs||[]]),Object.assign(e,o)}function xe(){const e=Pe.config;if(e.environmentVariables=e.environmentVariables||{},e.runtimeOptions=e.runtimeOptions||[],e.resources=e.resources||{assembly:[],jsModuleNative:[],jsModuleWorker:[],jsModuleRuntime:[],wasmNative:[],vfs:[],satelliteResources:{}},e.assets){Pe.diagnosticTracing&&b("config.assets is deprecated, use config.resources instead");for(const t of e.assets){const o={};switch(t.behavior){case"assembly":o.assembly=[t];break;case"pdb":o.pdb=[t];break;case"resource":o.satelliteResources={},o.satelliteResources[t.culture]=[t];break;case"icu":o.icu=[t];break;case"symbols":o.wasmSymbols=[t];break;case"vfs":o.vfs=[t];break;case"dotnetwasm":o.wasmNative=[t];break;case"js-module-threads":o.jsModuleWorker=[t];break;case"js-module-runtime":o.jsModuleRuntime=[t];break;case"js-module-native":o.jsModuleNative=[t];break;case"js-module-diagnostics":o.jsModuleDiagnostics=[t];break;case"js-module-dotnet":break;default:throw new Error(`Unexpected behavior ${t.behavior} of asset ${t.name}`)}_e(e.resources,o)}}e.debugLevel,e.applicationEnvironment||(e.applicationEnvironment="Production"),e.applicationCulture&&(e.environmentVariables.LANG=`${e.applicationCulture}.UTF-8`),Ue.diagnosticTracing=Pe.diagnosticTracing=!!e.diagnosticTracing,Ue.waitForDebugger=e.waitForDebugger,Pe.maxParallelDownloads=e.maxParallelDownloads||Pe.maxParallelDownloads,Pe.enableDownloadRetry=void 0!==e.enableDownloadRetry?e.enableDownloadRetry:Pe.enableDownloadRetry}let je=!1;async function Re(e){var t;if(je)return void await Pe.afterConfigLoaded.promise;let o;try{if(e.configSrc||Pe.config&&0!==Object.keys(Pe.config).length&&(Pe.config.assets||Pe.config.resources)||(e.configSrc="dotnet.boot.js"),o=e.configSrc,je=!0,o&&(Pe.diagnosticTracing&&b("mono_wasm_load_config"),await async function(e){const t=e.configSrc,o=Pe.locateFile(t);let n=null;void 0!==Pe.loadBootResource&&(n=Pe.loadBootResource("manifest",t,o,"","manifest"));let r,i=null;if(n)if("string"==typeof n)n.includes(".json")?(i=await s(I(n)),r=await Ae(i)):r=(await import(I(n))).config;else{const e=await n;"function"==typeof e.json?(i=e,r=await Ae(i)):r=e.config}else o.includes(".json")?(i=await s(ce(o,"manifest")),r=await Ae(i)):r=(await import(ce(o,"manifest"))).config;function s(e){return Pe.fetch_like(e,{method:"GET",credentials:"include",cache:"no-cache"})}Pe.config.applicationEnvironment&&(r.applicationEnvironment=Pe.config.applicationEnvironment),ve(Pe.config,r)}(e)),xe(),await we(null===(t=Pe.config.resources)||void 0===t?void 0:t.modulesAfterConfigLoaded),await be("onRuntimeConfigLoaded",[Pe.config]),e.onConfigLoaded)try{await e.onConfigLoaded(Pe.config,Le),xe()}catch(e){throw _("onConfigLoaded() failed",e),e}xe(),Pe.afterConfigLoaded.promise_control.resolve(Pe.config)}catch(t){const n=`Failed to load config file ${o} ${t} ${null==t?void 0:t.stack}`;throw Pe.config=e.config=Object.assign(Pe.config,{message:n,error:t,isError:!0}),Xe(1,new Error(n)),t}}function Te(){return!!globalThis.navigator&&(Pe.isChromium||Pe.isFirefox)}async function Ae(e){const t=Pe.config,o=await e.json();t.applicationEnvironment||o.applicationEnvironment||(o.applicationEnvironment=e.headers.get("Blazor-Environment")||e.headers.get("DotNet-Environment")||void 0),o.environmentVariables||(o.environmentVariables={});const n=e.headers.get("DOTNET-MODIFIABLE-ASSEMBLIES");n&&(o.environmentVariables.DOTNET_MODIFIABLE_ASSEMBLIES=n);const r=e.headers.get("ASPNETCORE-BROWSER-TOOLS");return r&&(o.environmentVariables.__ASPNETCORE_BROWSER_TOOLS=r),o}"function"!=typeof importScripts||globalThis.onmessage||(globalThis.dotnetSidecar=!0);const Se="object"==typeof process&&"object"==typeof process.versions&&"string"==typeof process.versions.node,De="function"==typeof importScripts,Oe=De&&"undefined"!=typeof dotnetSidecar,Ce=De&&!Oe,ke="object"==typeof window||De&&!Se,Ie=!ke&&!Se;let Ue={},Pe={},Me={},Le={},Ne={},$e=!1;const ze={},We={config:ze},Fe={mono:{},binding:{},internal:Ne,module:We,loaderHelpers:Pe,runtimeHelpers:Ue,diagnosticHelpers:Me,api:Le};function Be(e,t){if(e)return;const o="Assert failed: "+("function"==typeof t?t():t),n=new Error(o);_(o,n),Ue.nativeAbort(n)}function Ve(){return void 0!==Pe.exitCode}function qe(){return Ue.runtimeReady&&!Ve()}function He(){Ve()&&Be(!1,`.NET runtime already exited with ${Pe.exitCode} ${Pe.exitReason}. You can use runtime.runMain() which doesn't exit the runtime.`),Ue.runtimeReady||Be(!1,".NET runtime didn't start yet. Please call dotnet.create() first.")}function Je(){ke&&(globalThis.addEventListener("unhandledrejection",et),globalThis.addEventListener("error",tt))}let Ze,Qe;function Ge(e){Qe&&Qe(e),Xe(e,Pe.exitReason)}function Ke(e){Ze&&Ze(e||Pe.exitReason),Xe(1,e||Pe.exitReason)}function Xe(t,o){var n,r;const i=o&&"object"==typeof o;t=i&&"number"==typeof o.status?o.status:void 0===t?-1:t;const s=i&&"string"==typeof o.message?o.message:""+o;(o=i?o:Ue.ExitStatus?function(e,t){const o=new Ue.ExitStatus(e);return o.message=t,o.toString=()=>t,o}(t,s):new Error("Exit with code "+t+" "+s)).status=t,o.message||(o.message=s);const a=""+(o.stack||(new Error).stack);try{Object.defineProperty(o,"stack",{get:()=>a})}catch(e){}const l=!!o.silent;if(o.silent=!0,Ve())Pe.diagnosticTracing&&b("mono_exit called after exit");else{try{We.onAbort==Ke&&(We.onAbort=Ze),We.onExit==Ge&&(We.onExit=Qe),ke&&(globalThis.removeEventListener("unhandledrejection",et),globalThis.removeEventListener("error",tt)),Ue.runtimeReady?(Ue.jiterpreter_dump_stats&&Ue.jiterpreter_dump_stats(!1),0===t&&(null===(n=Pe.config)||void 0===n?void 0:n.interopCleanupOnExit)&&Ue.forceDisposeProxies(!0,!0),e&&0!==t&&(null===(r=Pe.config)||void 0===r||r.dumpThreadsOnNonZeroExit)):(Pe.diagnosticTracing&&b(`abort_startup, reason: ${o}`),function(e){Pe.allDownloadsQueued.promise_control.reject(e),Pe.allDownloadsFinished.promise_control.reject(e),Pe.afterConfigLoaded.promise_control.reject(e),Pe.wasmCompilePromise.promise_control.reject(e),Pe.runtimeModuleLoaded.promise_control.reject(e),Ue.dotnetReady&&(Ue.dotnetReady.promise_control.reject(e),Ue.afterInstantiateWasm.promise_control.reject(e),Ue.beforePreInit.promise_control.reject(e),Ue.afterPreInit.promise_control.reject(e),Ue.afterPreRun.promise_control.reject(e),Ue.beforeOnRuntimeInitialized.promise_control.reject(e),Ue.afterOnRuntimeInitialized.promise_control.reject(e),Ue.afterPostRun.promise_control.reject(e))}(o))}catch(e){E("mono_exit A failed",e)}try{l||(function(e,t){if(0!==e&&t){const e=Ue.ExitStatus&&t instanceof Ue.ExitStatus?b:_;"string"==typeof t?e(t):(void 0===t.stack&&(t.stack=(new Error).stack+""),t.message?e(Ue.stringify_as_error_with_stack?Ue.stringify_as_error_with_stack(t.message+"\n"+t.stack):t.message+"\n"+t.stack):e(JSON.stringify(t)))}!Ce&&Pe.config&&(Pe.config.logExitCode?Pe.config.forwardConsoleLogsToWS?R("WASM EXIT "+e):v("WASM EXIT "+e):Pe.config.forwardConsoleLogsToWS&&R())}(t,o),function(e){if(ke&&!Ce&&Pe.config&&Pe.config.appendElementOnExit&&document){const t=document.createElement("label");t.id="tests_done",0!==e&&(t.style.background="red"),t.innerHTML=""+e,document.body.appendChild(t)}}(t))}catch(e){E("mono_exit B failed",e)}Pe.exitCode=t,Pe.exitReason||(Pe.exitReason=o),!Ce&&Ue.runtimeReady&&We.runtimeKeepalivePop()}if(Pe.config&&Pe.config.asyncFlushOnExit&&0===t)throw(async()=>{try{await async function(){try{const e=await import(/*! webpackIgnore: true */"process"),t=e=>new Promise(((t,o)=>{e.on("error",o),e.end("","utf8",t)})),o=t(e.stderr),n=t(e.stdout);let r;const i=new Promise((e=>{r=setTimeout((()=>e("timeout")),1e3)}));await Promise.race([Promise.all([n,o]),i]),clearTimeout(r)}catch(e){_(`flushing std* streams failed: ${e}`)}}()}finally{Ye(t,o)}})(),o;Ye(t,o)}function Ye(e,t){if(Ue.runtimeReady&&Ue.nativeExit)try{Ue.nativeExit(e)}catch(e){!Ue.ExitStatus||e instanceof Ue.ExitStatus||E("set_exit_code_and_quit_now failed: "+e.toString())}if(0!==e||!ke)throw Se&&Ne.process?Ne.process.exit(e):Ue.quit&&Ue.quit(e,t),t}function et(e){ot(e,e.reason,"rejection")}function tt(e){ot(e,e.error,"error")}function ot(e,t,o){e.preventDefault();try{t||(t=new Error("Unhandled "+o)),void 0===t.stack&&(t.stack=(new Error).stack),t.stack=t.stack+"",t.silent||(_("Unhandled error:",t),Xe(1,t))}catch(e){}}!function(e){if($e)throw new Error("Loader module already loaded");$e=!0,Ue=e.runtimeHelpers,Pe=e.loaderHelpers,Me=e.diagnosticHelpers,Le=e.api,Ne=e.internal,Object.assign(Le,{INTERNAL:Ne,invokeLibraryInitializers:be}),Object.assign(e.module,{config:ve(ze,{environmentVariables:{}})});const r={mono_wasm_bindings_is_ready:!1,config:e.module.config,diagnosticTracing:!1,nativeAbort:e=>{throw e||new Error("abort")},nativeExit:e=>{throw new Error("exit:"+e)}},l={gitHash:"901ca941248413c79832d2fdbd709da0c4386353",config:e.module.config,diagnosticTracing:!1,maxParallelDownloads:16,enableDownloadRetry:!0,_loaded_files:[],loadedFiles:[],loadedAssemblies:[],libraryInitializers:[],workerNextNumber:1,actual_downloaded_assets_count:0,actual_instantiated_assets_count:0,expected_downloaded_assets_count:0,expected_instantiated_assets_count:0,afterConfigLoaded:i(),allDownloadsQueued:i(),allDownloadsFinished:i(),wasmCompilePromise:i(),runtimeModuleLoaded:i(),loadingWorkers:i(),is_exited:Ve,is_runtime_running:qe,assert_runtime_running:He,mono_exit:Xe,createPromiseController:i,getPromiseController:s,assertIsControllablePromise:a,mono_download_assets:oe,resolve_single_asset_path:ee,setup_proxy_console:j,set_thread_prefix:w,installUnhandledErrorHandler:Je,retrieve_asset_download:ie,invokeLibraryInitializers:be,isDebuggingSupported:Te,exceptions:t,simd:n,relaxedSimd:o};Object.assign(Ue,r),Object.assign(Pe,l)}(Fe);let nt,rt,it,st=!1,at=!1;async function lt(e){if(!at){if(at=!0,ke&&Pe.config.forwardConsoleLogsToWS&&void 0!==globalThis.WebSocket&&j("main",globalThis.console,globalThis.location.origin),We||Be(!1,"Null moduleConfig"),Pe.config||Be(!1,"Null moduleConfig.config"),"function"==typeof e){const t=e(Fe.api);if(t.ready)throw new Error("Module.ready couldn't be redefined.");Object.assign(We,t),Ee(We,t)}else{if("object"!=typeof e)throw new Error("Can't use moduleFactory callback of createDotnetRuntime function.");Ee(We,e)}await async function(e){if(Se){const e=await import(/*! webpackIgnore: true */"process"),t=14;if(e.versions.node.split(".")[0]<t)throw new Error(`NodeJS at '${e.execPath}' has too low version '${e.versions.node}', please use at least ${t}. See also https://aka.ms/dotnet-wasm-features`)}const t=/*! webpackIgnore: true */import.meta.url,o=t.indexOf("?");var n;if(o>0&&(Pe.modulesUniqueQuery=t.substring(o)),Pe.scriptUrl=t.replace(/\\/g,"/").replace(/[?#].*/,""),Pe.scriptDirectory=(n=Pe.scriptUrl).slice(0,n.lastIndexOf("/"))+"/",Pe.locateFile=e=>"URL"in globalThis&&globalThis.URL!==C?new URL(e,Pe.scriptDirectory).toString():M(e)?e:Pe.scriptDirectory+e,Pe.fetch_like=k,Pe.out=console.log,Pe.err=console.error,Pe.onDownloadResourceProgress=e.onDownloadResourceProgress,ke&&globalThis.navigator){const e=globalThis.navigator,t=e.userAgentData&&e.userAgentData.brands;t&&t.length>0?Pe.isChromium=t.some((e=>"Google Chrome"===e.brand||"Microsoft Edge"===e.brand||"Chromium"===e.brand)):e.userAgent&&(Pe.isChromium=e.userAgent.includes("Chrome"),Pe.isFirefox=e.userAgent.includes("Firefox"))}Ne.require=Se?await import(/*! webpackIgnore: true */"module").then((e=>e.createRequire(/*! webpackIgnore: true */import.meta.url))):Promise.resolve((()=>{throw new Error("require not supported")})),void 0===globalThis.URL&&(globalThis.URL=C)}(We)}}async function ct(e){return await lt(e),Ze=We.onAbort,Qe=We.onExit,We.onAbort=Ke,We.onExit=Ge,We.ENVIRONMENT_IS_PTHREAD?async function(){(function(){const e=new MessageChannel,t=e.port1,o=e.port2;t.addEventListener("message",(e=>{var n,r;n=JSON.parse(e.data.config),r=JSON.parse(e.data.monoThreadInfo),st?Pe.diagnosticTracing&&b("mono config already received"):(ve(Pe.config,n),Ue.monoThreadInfo=r,xe(),Pe.diagnosticTracing&&b("mono config received"),st=!0,Pe.afterConfigLoaded.promise_control.resolve(Pe.config),ke&&n.forwardConsoleLogsToWS&&void 0!==globalThis.WebSocket&&Pe.setup_proxy_console("worker-idle",console,globalThis.location.origin)),t.close(),o.close()}),{once:!0}),t.start(),self.postMessage({[l]:{monoCmd:"preload",port:o}},[o])})(),await Pe.afterConfigLoaded.promise,function(){const e=Pe.config;e.assets||Be(!1,"config.assets must be defined");for(const t of e.assets)X(t),Q[t.behavior]&&z.push(t)}(),setTimeout((async()=>{try{await oe()}catch(e){Xe(1,e)}}),0);const e=dt(),t=await Promise.all(e);return await ut(t),We}():async function(){var e;await Re(We),re();const t=dt();(async function(){try{const e=ee("dotnetwasm");await se(e),e&&e.pendingDownloadInternal&&e.pendingDownloadInternal.response||Be(!1,"Can't load dotnet.native.wasm");const t=await e.pendingDownloadInternal.response,o=t.headers&&t.headers.get?t.headers.get("Content-Type"):void 0;let n;if("function"==typeof WebAssembly.compileStreaming&&"application/wasm"===o)n=await WebAssembly.compileStreaming(t);else{ke&&"application/wasm"!==o&&E('WebAssembly resource does not have the expected content type "application/wasm", so falling back to slower ArrayBuffer instantiation.');const e=await t.arrayBuffer();Pe.diagnosticTracing&&b("instantiate_wasm_module buffered"),n=Ie?await Promise.resolve(new WebAssembly.Module(e)):await WebAssembly.compile(e)}e.pendingDownloadInternal=null,e.pendingDownload=null,e.buffer=null,e.moduleExports=null,Pe.wasmCompilePromise.promise_control.resolve(n)}catch(e){Pe.wasmCompilePromise.promise_control.reject(e)}})(),setTimeout((async()=>{try{D(),await oe()}catch(e){Xe(1,e)}}),0);const o=await Promise.all(t);return await ut(o),await Ue.dotnetReady.promise,await we(null===(e=Pe.config.resources)||void 0===e?void 0:e.modulesAfterRuntimeReady),await be("onRuntimeReady",[Fe.api]),Le}()}function dt(){const e=ee("js-module-runtime"),t=ee("js-module-native");if(nt&&rt)return[nt,rt,it];"object"==typeof e.moduleExports?nt=e.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${e.resolvedUrl}' for ${e.name}`),nt=import(/*! webpackIgnore: true */e.resolvedUrl)),"object"==typeof t.moduleExports?rt=t.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${t.resolvedUrl}' for ${t.name}`),rt=import(/*! webpackIgnore: true */t.resolvedUrl));const o=Y("js-module-diagnostics");return o&&("object"==typeof o.moduleExports?it=o.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${o.resolvedUrl}' for ${o.name}`),it=import(/*! webpackIgnore: true */o.resolvedUrl))),[nt,rt,it]}async function ut(e){const{initializeExports:t,initializeReplacements:o,configureRuntimeStartup:n,configureEmscriptenStartup:r,configureWorkerStartup:i,setRuntimeGlobals:s,passEmscriptenInternals:a}=e[0],{default:l}=e[1],c=e[2];s(Fe),t(Fe),c&&c.setRuntimeGlobals(Fe),await n(We),Pe.runtimeModuleLoaded.promise_control.resolve(),l((e=>(Object.assign(We,{ready:e.ready,__dotnet_runtime:{initializeReplacements:o,configureEmscriptenStartup:r,configureWorkerStartup:i,passEmscriptenInternals:a}}),We))).catch((e=>{if(e.message&&e.message.toLowerCase().includes("out of memory"))throw new Error(".NET runtime has failed to start, because too much memory was requested. Please decrease the memory by adjusting EmccMaximumHeapSize. See also https://aka.ms/dotnet-wasm-features");throw e}))}const ft=new class{withModuleConfig(e){try{return Ee(We,e),this}catch(e){throw Xe(1,e),e}}withOnConfigLoaded(e){try{return Ee(We,{onConfigLoaded:e}),this}catch(e){throw Xe(1,e),e}}withConsoleForwarding(){try{return ve(ze,{forwardConsoleLogsToWS:!0}),this}catch(e){throw Xe(1,e),e}}withExitOnUnhandledError(){try{return ve(ze,{exitOnUnhandledError:!0}),Je(),this}catch(e){throw Xe(1,e),e}}withAsyncFlushOnExit(){try{return ve(ze,{asyncFlushOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withExitCodeLogging(){try{return ve(ze,{logExitCode:!0}),this}catch(e){throw Xe(1,e),e}}withElementOnExit(){try{return ve(ze,{appendElementOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withInteropCleanupOnExit(){try{return ve(ze,{interopCleanupOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withDumpThreadsOnNonZeroExit(){try{return ve(ze,{dumpThreadsOnNonZeroExit:!0}),this}catch(e){throw Xe(1,e),e}}withWaitingForDebugger(e){try{return ve(ze,{waitForDebugger:e}),this}catch(e){throw Xe(1,e),e}}withInterpreterPgo(e,t){try{return ve(ze,{interpreterPgo:e,interpreterPgoSaveDelay:t}),ze.runtimeOptions?ze.runtimeOptions.push("--interp-pgo-recording"):ze.runtimeOptions=["--interp-pgo-recording"],this}catch(e){throw Xe(1,e),e}}withConfig(e){try{return ve(ze,e),this}catch(e){throw Xe(1,e),e}}withConfigSrc(e){try{return e&&"string"==typeof e||Be(!1,"must be file path or URL"),Ee(We,{configSrc:e}),this}catch(e){throw Xe(1,e),e}}withVirtualWorkingDirectory(e){try{return e&&"string"==typeof e||Be(!1,"must be directory path"),ve(ze,{virtualWorkingDirectory:e}),this}catch(e){throw Xe(1,e),e}}withEnvironmentVariable(e,t){try{const o={};return o[e]=t,ve(ze,{environmentVariables:o}),this}catch(e){throw Xe(1,e),e}}withEnvironmentVariables(e){try{return e&&"object"==typeof e||Be(!1,"must be dictionary object"),ve(ze,{environmentVariables:e}),this}catch(e){throw Xe(1,e),e}}withDiagnosticTracing(e){try{return"boolean"!=typeof e&&Be(!1,"must be boolean"),ve(ze,{diagnosticTracing:e}),this}catch(e){throw Xe(1,e),e}}withDebugging(e){try{return null!=e&&"number"==typeof e||Be(!1,"must be number"),ve(ze,{debugLevel:e}),this}catch(e){throw Xe(1,e),e}}withApplicationArguments(...e){try{return e&&Array.isArray(e)||Be(!1,"must be array of strings"),ve(ze,{applicationArguments:e}),this}catch(e){throw Xe(1,e),e}}withRuntimeOptions(e){try{return e&&Array.isArray(e)||Be(!1,"must be array of strings"),ze.runtimeOptions?ze.runtimeOptions.push(...e):ze.runtimeOptions=e,this}catch(e){throw Xe(1,e),e}}withMainAssembly(e){try{return ve(ze,{mainAssemblyName:e}),this}catch(e){throw Xe(1,e),e}}withApplicationArgumentsFromQuery(){try{if(!globalThis.window)throw new Error("Missing window to the query parameters from");if(void 0===globalThis.URLSearchParams)throw new Error("URLSearchParams is supported");const e=new URLSearchParams(globalThis.window.location.search).getAll("arg");return this.withApplicationArguments(...e)}catch(e){throw Xe(1,e),e}}withApplicationEnvironment(e){try{return ve(ze,{applicationEnvironment:e}),this}catch(e){throw Xe(1,e),e}}withApplicationCulture(e){try{return ve(ze,{applicationCulture:e}),this}catch(e){throw Xe(1,e),e}}withResourceLoader(e){try{return Pe.loadBootResource=e,this}catch(e){throw Xe(1,e),e}}async download(){try{await async function(){lt(We),await Re(We),re(),D(),oe(),await Pe.allDownloadsFinished.promise}()}catch(e){throw Xe(1,e),e}}async create(){try{return this.instance||(this.instance=await async function(){return await ct(We),Fe.api}()),this.instance}catch(e){throw Xe(1,e),e}}async run(){try{return We.config||Be(!1,"Null moduleConfig.config"),this.instance||await this.create(),this.instance.runMainAndExit()}catch(e){throw Xe(1,e),e}}},mt=Xe,gt=ct;Ie||"function"==typeof globalThis.URL||Be(!1,"This browser/engine doesn't support URL API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features"),"function"!=typeof globalThis.BigInt64Array&&Be(!1,"This browser/engine doesn't support BigInt64Array API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features"),ft.withConfig(/*json-start*/{
  "mainAssemblyName": "FsAssay.Web.Client",
  "resources": {
    "hash": "sha256-e6SUygXT/U68yHNvTCy4632xU6VXuyWm9d2NIBiWS/0=",
    "jsModuleNative": [
      {
        "name": "dotnet.native.ikrs475e5v.js"
      }
    ],
    "jsModuleRuntime": [
      {
        "name": "dotnet.runtime.a6jcqbs390.js"
      }
    ],
    "wasmNative": [
      {
        "name": "dotnet.native.veuqw8a0w9.wasm",
        "hash": "sha256-iQOJ2Ignl/X3n6mOHRQ4zWYcute0MGlaiRFi2J3HXWk=",
        "cache": "force-cache"
      }
    ],
    "icu": [
      {
        "virtualPath": "icudt_CJK.dat",
        "name": "icudt_CJK.tjcz0u77k5.dat",
        "hash": "sha256-SZLtQnRc0JkwqHab0VUVP7T3uBPSeYzxzDnpxPpUnHk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "icudt_EFIGS.dat",
        "name": "icudt_EFIGS.tptq2av103.dat",
        "hash": "sha256-8fItetYY8kQ0ww6oxwTLiT3oXlBwHKumbeP2pRF4yTc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "icudt_no_CJK.dat",
        "name": "icudt_no_CJK.lfu7j35m59.dat",
        "hash": "sha256-L7sV7NEYP37/Qr2FPCePo5cJqRgTXRwGHuwF5Q+0Nfs=",
        "cache": "force-cache"
      }
    ],
    "coreAssembly": [
      {
        "virtualPath": "System.Private.CoreLib.wasm",
        "name": "System.Private.CoreLib.iq57ok7u77.wasm",
        "hash": "sha256-Ng935omot2eEb7p5zCnXgC1Z8ZTi3bjqVKxzOAWpva0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.InteropServices.JavaScript.wasm",
        "name": "System.Runtime.InteropServices.JavaScript.uzoakn3mog.wasm",
        "hash": "sha256-Cp2bQOeRFVxMIA/IQbNc6pVyPdKopwM4EnwXZAse2KM=",
        "cache": "force-cache"
      }
    ],
    "assembly": [
      {
        "virtualPath": "BlazorMonaco.wasm",
        "name": "BlazorMonaco.ef1a4jimsf.wasm",
        "hash": "sha256-Ror3QOMwP8ju3jnvYpqX7VBSOF9xXoK9QrBwKN+EEW8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Bolero.Html.wasm",
        "name": "Bolero.Html.ae3rg3l8oq.wasm",
        "hash": "sha256-Dmr0N+4l3D3kVTxmiraIZY6A2kjs+5kSvdRzxCFdM6o=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Bolero.wasm",
        "name": "Bolero.2vztmcz117.wasm",
        "hash": "sha256-Mq+cja6M12fDZoevUtdQTeHyDYQxqDa2Xi5aejmRR1g=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Elmish.wasm",
        "name": "Elmish.2g8jgsxlrc.wasm",
        "hash": "sha256-fv72F794SxrYCDkggLOY1lvddX6aqWPhIa5C4hb6sBw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "FSharp.Analyzers.SDK.wasm",
        "name": "FSharp.Analyzers.SDK.oo4dz0rsme.wasm",
        "hash": "sha256-CPRhWJ9P9ySuFsmTAeg/NtP+yAbnADD2WaBFzLM/X2Q=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "FSharp.Compiler.Service.wasm",
        "name": "FSharp.Compiler.Service.dd54rblekr.wasm",
        "hash": "sha256-zzQQE2ZgsI+59IJhchOE9vir1JO+/DXiTDAZQ/fTT4M=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "FSharp.Core.wasm",
        "name": "FSharp.Core.p9o26y7r9y.wasm",
        "hash": "sha256-XaWQ2epAG6x2u2wXAV6CibJzO90wiFMAMbMLl/yG7Bg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "FSharp.DependencyManager.Nuget.wasm",
        "name": "FSharp.DependencyManager.Nuget.y4jm3sfweq.wasm",
        "hash": "sha256-Ys3G+r7wcKd9B7x373bPPJjIz3WZ2OZKuamJ0bKPoe0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "FSharp.SystemTextJson.wasm",
        "name": "FSharp.SystemTextJson.yngbwu0lng.wasm",
        "hash": "sha256-HsEtgV5HcshSPIFdd52ahl2b7lGikYonFRYsskpM89E=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "FsAssay.Analyzers.wasm",
        "name": "FsAssay.Analyzers.vy4zxg3m6p.wasm",
        "hash": "sha256-GUsO/JNPz7nn96AZdpZUeAHxTGyxYL8oWJZXuCzkk6k=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "FsAssay.Web.Client.wasm",
        "name": "FsAssay.Web.Client.0eh7h6mu5j.wasm",
        "hash": "sha256-yLwwHOKHrI1TdkBRVug9WFCjobsmV7+/0xUFP2TONWU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "McMaster.NETCore.Plugins.wasm",
        "name": "McMaster.NETCore.Plugins.4onfl6zn9s.wasm",
        "hash": "sha256-bN0aybYaBueFIxzcrdaJsuTPdYq0OyG/xueQ+eXSg6A=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.Web.wasm",
        "name": "Microsoft.AspNetCore.Components.Web.zs8swhzrh1.wasm",
        "hash": "sha256-kw/Sv534tVwP7nZ/ePmZI1CAUQNBjqoyE5onyeTeYJc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.WebAssembly.wasm",
        "name": "Microsoft.AspNetCore.Components.WebAssembly.iynn4uwnck.wasm",
        "hash": "sha256-9qozfy0A2IKWCC0u5V5U1NAQpmgijJ1bxEjfy3zuACI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.wasm",
        "name": "Microsoft.AspNetCore.Components.5g6jmuoadz.wasm",
        "hash": "sha256-m0oObU3atzKvqt+qvyyLhVEq7pSGoZZqvYDXASDNP0A=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.wasm",
        "name": "Microsoft.Extensions.Configuration.8pvavhimuq.wasm",
        "hash": "sha256-/ME194X23cN/0T6VmgEIBQfNmPo7GEfFIrtX20/5Aco=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.Abstractions.wasm",
        "name": "Microsoft.Extensions.Configuration.Abstractions.qmkmcck29i.wasm",
        "hash": "sha256-DvIRgF5FEiDYCg4ZdsjRpxXPxyGifconC9I2K92y81c=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.Json.wasm",
        "name": "Microsoft.Extensions.Configuration.Json.7smls8uilt.wasm",
        "hash": "sha256-aX0AsPwdds39FVlPr5tWxfBGJnwUNMcI/ziHtwAmH18=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.DependencyInjection.wasm",
        "name": "Microsoft.Extensions.DependencyInjection.robeas4psm.wasm",
        "hash": "sha256-VRMHsWV5WU3hTbYTmsSBAN30HIksBxk0AoWGjRmQwJM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.DependencyInjection.Abstractions.wasm",
        "name": "Microsoft.Extensions.DependencyInjection.Abstractions.80sit6zdtv.wasm",
        "hash": "sha256-uBwaZ082i0qPiEzN3BQJj4VqCKVK400Ee7hBz8MN4EQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Localization.Abstractions.wasm",
        "name": "Microsoft.Extensions.Localization.Abstractions.ae89paw0py.wasm",
        "hash": "sha256-PkwLOyrHX2POJ7In/xwUvSjwfdKGqLQI6MU2nA8GVKw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Localization.wasm",
        "name": "Microsoft.Extensions.Localization.bvn14pws96.wasm",
        "hash": "sha256-6UgMJoVZBfDdfzYR0aKVK6BWArxpXC1qiQDDjiXw/L4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.wasm",
        "name": "Microsoft.Extensions.Logging.eqhkfrhsc3.wasm",
        "hash": "sha256-Op6qUvxJcV2DrLUl78vcTP9b3VLTsRgaMWyFhil9BGE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.Abstractions.wasm",
        "name": "Microsoft.Extensions.Logging.Abstractions.tresrvzgiz.wasm",
        "hash": "sha256-N78cTCrfvRKbgYw1m54OC+hcqs3wGsa9e3DORO5QWqQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Options.wasm",
        "name": "Microsoft.Extensions.Options.yqbr4imbd0.wasm",
        "hash": "sha256-xl8ht9pvv4+mmgA7wT/YmXllfeN+4bTiJs4Mrnh8LMM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Primitives.wasm",
        "name": "Microsoft.Extensions.Primitives.eds8m5mm6q.wasm",
        "hash": "sha256-XtQ9n0ATkXHpPq/7NF7hfDMq/UBm9PgsDAiaqho5CB0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.JSInterop.WebAssembly.wasm",
        "name": "Microsoft.JSInterop.WebAssembly.hvsthvqq9c.wasm",
        "hash": "sha256-kxuq0Bp3dXDn8hiV2GztJXSei2CMrWw1qoWwJhXL4eM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.JSInterop.wasm",
        "name": "Microsoft.JSInterop.mpq2c2ybmn.wasm",
        "hash": "sha256-RljmBrqZ4HzaE6y/Rn+1xbGO6VdgISG/TwrA0PSRn74=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "MudBlazor.wasm",
        "name": "MudBlazor.7jb2j263as.wasm",
        "hash": "sha256-Hf4yDcOiV+iXFjCeRO3J3W1n7xxo/vawp4HUP5mn5P4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Buffers.wasm",
        "name": "System.Buffers.upn1b4azkj.wasm",
        "hash": "sha256-Uglfk26Pd3JRyrT3E9G42/Un8Xnxb5S6er14WasQWhc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.wasm",
        "name": "System.Collections.elvlo6mln7.wasm",
        "hash": "sha256-MiMQTKv/yxkU7ID75Dz+sbFKKsA49rERzM3CgvqV2eo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Concurrent.wasm",
        "name": "System.Collections.Concurrent.uso9ekbwch.wasm",
        "hash": "sha256-Msyf+F2lRA+yfVTuvScNcgb4gyYsYawkGkjj3qly+zI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Immutable.wasm",
        "name": "System.Collections.Immutable.nco3mefezy.wasm",
        "hash": "sha256-B9zvb/Gx7hE/qJT38rIOrq9W+MxOgZIalB/xRJ6rf6Q=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.NonGeneric.wasm",
        "name": "System.Collections.NonGeneric.st62hf4cxv.wasm",
        "hash": "sha256-8OPGo/oZHfTuUz/GsGKIqnld7+u6PX65T7z5CMAqErs=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Specialized.wasm",
        "name": "System.Collections.Specialized.rmifzdhi95.wasm",
        "hash": "sha256-E/MT97zDSe9VuPa3uk+0olD78eNS9BIYptW5/0GiU5k=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.EventBasedAsync.wasm",
        "name": "System.ComponentModel.EventBasedAsync.v9ntbqgczo.wasm",
        "hash": "sha256-E64Cx2WfHLBP60YIbpC3UZ8Wlo0YsmlRNlecW5T3He4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.Primitives.wasm",
        "name": "System.ComponentModel.Primitives.hxwela9pld.wasm",
        "hash": "sha256-NQuUdnFWdnUqdxdTUDdNslLDweNJBX+/8VTSGmC0LoQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.TypeConverter.wasm",
        "name": "System.ComponentModel.TypeConverter.kel8ow2lpg.wasm",
        "hash": "sha256-XBak/Xn08RlVMlGosWo8aZg4vFduOR3xIx5lqQRvg+I=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.wasm",
        "name": "System.ComponentModel.e6bm8wniqf.wasm",
        "hash": "sha256-FlqirVL9sIZuu5B2uysMetgDBYcJW58SWqLwNYQFKzY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Console.wasm",
        "name": "System.Console.masom4swgp.wasm",
        "hash": "sha256-PsuxmhzZs9VAD7t5dnTdZO/3NATousTGoLLGWb5wD2c=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.DiagnosticSource.wasm",
        "name": "System.Diagnostics.DiagnosticSource.f41nx3fmta.wasm",
        "hash": "sha256-kX1hYxOXy4XHneW5ye1/IDJRbhivuIKD7UnGqizcnZI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.Process.wasm",
        "name": "System.Diagnostics.Process.3jxhh02g6h.wasm",
        "hash": "sha256-/5TMlSvcRQJZd+QbxIKlqGSnx3V4hf0vut7nDHZEyO4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.TraceSource.wasm",
        "name": "System.Diagnostics.TraceSource.vempnvjbfj.wasm",
        "hash": "sha256-t4CPoKyD1sB27dk4kcDquk08sH61tWOsDX4OwgWgQW0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Compression.wasm",
        "name": "System.IO.Compression.5dprdqxysk.wasm",
        "hash": "sha256-KR4XQLWxBfglktCxNf/H+AJA89MLVJMOO6Yf5B2b6lU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.FileSystem.Watcher.wasm",
        "name": "System.IO.FileSystem.Watcher.r4qi9kpcjw.wasm",
        "hash": "sha256-NaLQfZ/7g26z4qpJe9DT0Wr5E7GZSZkXOWyO0HZaOvE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.MemoryMappedFiles.wasm",
        "name": "System.IO.MemoryMappedFiles.f5si6dql3q.wasm",
        "hash": "sha256-/aaQjTReXwLOkcGY4cOrb0a36uHHIb9Dr3cLof3TJV0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Pipelines.wasm",
        "name": "System.IO.Pipelines.s624opig07.wasm",
        "hash": "sha256-7N+scuN8nmiU+QbLyZpP/FeRS5FYzMOKMTSW7NAJetA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Pipes.wasm",
        "name": "System.IO.Pipes.dagj0raxo3.wasm",
        "hash": "sha256-NQVOtMlgA4k9Sy/lzWD7T6elzfqMBKgyOQKnLLPEOmk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.Expressions.wasm",
        "name": "System.Linq.Expressions.u3iqrxa9zz.wasm",
        "hash": "sha256-Kt17n8qhUkVmjfdaPiKBaw3blYChTYmv3/sNHhlwdMQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.Queryable.wasm",
        "name": "System.Linq.Queryable.nu83qkgwlw.wasm",
        "hash": "sha256-j3nPcEiElMcgOz/AfzGzifP1KvWXfF/NK2/pbs5kdd4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.wasm",
        "name": "System.Linq.ffkxtme9rs.wasm",
        "hash": "sha256-VaXIJjWntvSaYeJpvFnqfrtsZ9hAX5AyEIHiZzlO22k=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Memory.wasm",
        "name": "System.Memory.v2kj5p06gc.wasm",
        "hash": "sha256-Z5Udc7ZaFSNXUGyiJAKhuZPBuRWcwnTSyU/a4ttoyzA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.Http.wasm",
        "name": "System.Net.Http.4melx2mxt1.wasm",
        "hash": "sha256-PObkX7nnbFoKH4mFtPo84KgQJMe7IJk6DiivXpOqdqU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.Primitives.wasm",
        "name": "System.Net.Primitives.g0joan8ok8.wasm",
        "hash": "sha256-wAP39lrPxd24Aksv911Fc9QeQhlMtivWi6RANnW+0+Q=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.Requests.wasm",
        "name": "System.Net.Requests.2nedp9efct.wasm",
        "hash": "sha256-XqhSX+0WslYDjnpRGS4LZs4UdcJpGzEjFX5UGQRVHp4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.WebClient.wasm",
        "name": "System.Net.WebClient.dcq6kyxo8y.wasm",
        "hash": "sha256-I8n5YXPIWgPzly97a4Aj8lxDvXNHaJ/bHZiwoeXJ1ns=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ObjectModel.wasm",
        "name": "System.ObjectModel.l6j66dc2id.wasm",
        "hash": "sha256-Y/8vhZtx8y6DtVRpsdIzjrlWJaAxROOe/UKWfggw/Bk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Uri.wasm",
        "name": "System.Private.Uri.t1k3s7gzdz.wasm",
        "hash": "sha256-QWAHtcIyoDsPlgY4a1pUTXIT9C0orol4JfimQkkeuRA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Xml.wasm",
        "name": "System.Private.Xml.jgyotbf7lm.wasm",
        "hash": "sha256-jUKvlrZhqzLe9rLASoINAKweXV8/yE7Si7dz45OJvi0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Xml.Linq.wasm",
        "name": "System.Private.Xml.Linq.z57sw09n5p.wasm",
        "hash": "sha256-R4Mw0pItSr1auDoQ0jTYROYiJkMEGtJ7yyCxhGt0M/c=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Reflection.Emit.ILGeneration.wasm",
        "name": "System.Reflection.Emit.ILGeneration.jsj4oxfxpz.wasm",
        "hash": "sha256-zVbZkkJtsHWToL2kAtYti7gd5tRpRCW+DicEwBlMcRI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Reflection.Emit.wasm",
        "name": "System.Reflection.Emit.3uvj0p6tp3.wasm",
        "hash": "sha256-FTSrzGV4o+qByDPgvIp7EB0EjnyYiTcO4kz5QfzedWo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Reflection.Metadata.wasm",
        "name": "System.Reflection.Metadata.9g8mjde5lj.wasm",
        "hash": "sha256-STts/xWCFbNIR3qMGaKqa3RC0bnlIcm6iCB4pKTVjrQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.wasm",
        "name": "System.Runtime.03l6ogc0qc.wasm",
        "hash": "sha256-xDerkHhq7GSg9ORaYpdRHQf3agD+YBffRpg4aslhJD0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.InteropServices.wasm",
        "name": "System.Runtime.InteropServices.tfhhmq3p8w.wasm",
        "hash": "sha256-iWCeR2gPOHxC4sPVn3l0JxpJFbZmIg8g5NDfuoWDn5g=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.InteropServices.RuntimeInformation.wasm",
        "name": "System.Runtime.InteropServices.RuntimeInformation.i4bhubkf3u.wasm",
        "hash": "sha256-bsOPOpydTUnT0T318Ywd09MNUq35G317La5Z+L3Y2nM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Loader.wasm",
        "name": "System.Runtime.Loader.dd8d551tc5.wasm",
        "hash": "sha256-X+tUaLgdPnSrmT31LMHVpH50sHVsNNdIg0HtIAzAEvY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Numerics.wasm",
        "name": "System.Runtime.Numerics.5mdxduuwzn.wasm",
        "hash": "sha256-LQifdMdWmzgko2Wczae8N8R4DKzb9kaldYwvJ4/T5ow=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Security.Cryptography.wasm",
        "name": "System.Security.Cryptography.ldp2vhwz0d.wasm",
        "hash": "sha256-B/AVUXw8KMrYFqKJmVzuklWkuugb5kMGdsAtHpTu+X4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.Encodings.Web.wasm",
        "name": "System.Text.Encodings.Web.paer4h7dy2.wasm",
        "hash": "sha256-32Y0c+wpN8zNK6PuCoCjspbFDUmdnxQpRMM6iJ1JO6g=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.Json.wasm",
        "name": "System.Text.Json.qkz739du2j.wasm",
        "hash": "sha256-2qbPwTL/+NJRH1duiJY1EB5KGCILySOKmPJdV0HIY1A=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.RegularExpressions.wasm",
        "name": "System.Text.RegularExpressions.5sp61dwoij.wasm",
        "hash": "sha256-Bb0OLsL6ko18hirYrvp2UkT0Fva/utes5SRLQuCCUIc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Threading.wasm",
        "name": "System.Threading.nkzgr5bf96.wasm",
        "hash": "sha256-F7dx1aAxBvoE/ZFQxWsnOJP7hZ9kDsLF+pHpLWCRLeE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Threading.Tasks.Parallel.wasm",
        "name": "System.Threading.Tasks.Parallel.ujdx8o0suy.wasm",
        "hash": "sha256-JtVm2jWl2xewq17LA1I00T37355XHv70PdFYxEbVe3o=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Xml.Linq.wasm",
        "name": "System.Xml.Linq.7qmldn65vo.wasm",
        "hash": "sha256-T+9RdfUBfnlvro21wk8VmmYe3ZDfKiYEBbJPCfLiuds=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.wasm",
        "name": "System.nnqvudo0vd.wasm",
        "hash": "sha256-TbXBiQa38E2TrjWSwZYk9l9sTztp8pQ4MLJyVPBpaEk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "netstandard.wasm",
        "name": "netstandard.05ruelz3kb.wasm",
        "hash": "sha256-HOm5yXlqGxahloun8kZadMGI/qdDBgGPNGFyyN3vTvg=",
        "cache": "force-cache"
      }
    ],
    "satelliteResources": {
      "cs": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.bono1utac7.wasm",
          "hash": "sha256-JUnKUEJSQ+v2qIED6MxNO9CE1GEsjE5uyrVgeNU7JUM=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.ua52ttwp06.wasm",
          "hash": "sha256-qmdUpoA7rSeZ1lW+V1+2jWdNO0uLzHXSQR7C9fprZno=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.6flc52l32x.wasm",
          "hash": "sha256-cnT7lRXPnspR5x04tO77wKC2mmz+V+3weYK3B6A9WwE=",
          "cache": "force-cache"
        }
      ],
      "de": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.6qz215jbgs.wasm",
          "hash": "sha256-XubrVvoVGLJzH3Zr4x5AP6Xxc5vCjvchlM49Gd9gqME=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.go62vp4mdw.wasm",
          "hash": "sha256-UNHoGP/MnpQmcVTgDd12XJGsa6QqS/5unpU52KB/4wM=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.asdbamu3h8.wasm",
          "hash": "sha256-ihyS2caT3Tt3iJEinPWY62ZEGsM7cAujsXCG6rF32EY=",
          "cache": "force-cache"
        }
      ],
      "es": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.c1puwokes4.wasm",
          "hash": "sha256-QBo6KxBKRpNRi6jeDayFSRhGV/vpQdFSiezZ8l226ek=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.fgufbw6h5q.wasm",
          "hash": "sha256-71LjAWsPIfo2K1IkWmluZqtUiAr7E9ktrOZeV2/anJg=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.h1ldy8p9cc.wasm",
          "hash": "sha256-xf1aq1xhdRgdT1PPLAojmlIYcLZTfMldNVL2gdknkYc=",
          "cache": "force-cache"
        }
      ],
      "fr": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.o2vx0bcn0z.wasm",
          "hash": "sha256-kKeFEomp8sNV3t2ENTLn1kxS+Bve2HUvxNc5qyayJp8=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.fmcrnffenm.wasm",
          "hash": "sha256-p/uoT+KN9jUxbE5qVUfwXEhV7fmnp+ifXyC8Ovbsp/Q=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.ueyro6iaed.wasm",
          "hash": "sha256-9nZqeqEVfFQdYGVd9LjjeUbl6SOwkocEpL9+88IYaCY=",
          "cache": "force-cache"
        }
      ],
      "it": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.odtbkln4jl.wasm",
          "hash": "sha256-hB5N4UtMo0yh0emZ8KEuEjSuXOZToFbZTCgBnz9blIo=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.51bkzdbubk.wasm",
          "hash": "sha256-JwQKk1Buso/vQsEOkNIaK9/cAvOUN7457GjlB+9mhTU=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.7viacjaixm.wasm",
          "hash": "sha256-A1aF/Wk2kwdTyoNzQEkKVmhtahfZmKkG54UYDX43btI=",
          "cache": "force-cache"
        }
      ],
      "ja": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.hr3xn0xlp7.wasm",
          "hash": "sha256-TVh2EyqaJLsKdAQpgLVI0XlQQsHf6/ivOQ5TgvNdxLc=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.qby9k91s67.wasm",
          "hash": "sha256-Bn5n/JMOEcZ1atAC61hKc8t0PD/5sjRyUJRw5KngFKQ=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.2ulm3la8aj.wasm",
          "hash": "sha256-9vAEXO8RZOW8CmFq5BbpXrFBXuie6ScgoMIKmJg6vAk=",
          "cache": "force-cache"
        }
      ],
      "ko": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.z3orfrdu7i.wasm",
          "hash": "sha256-MUIe8KCG7eLSl7YVNFtykERNePVqLzmR1kjgrRQIQE8=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.oim1f9vo2v.wasm",
          "hash": "sha256-wLSn2YYT55eQHrL2mZnFYkq6YYoNntUDvf5lDGczqZA=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.xdxzydpfu1.wasm",
          "hash": "sha256-O2Z0eTm/hGCoactvmHiLW9l53wxNKcm95epDTAJY0J4=",
          "cache": "force-cache"
        }
      ],
      "pl": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.jq29xpgbtq.wasm",
          "hash": "sha256-5QRfKTbrrA7XmAE+JF6wRffAITJ3cW9Y+VH/d0dQsCM=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.9t2loyqfla.wasm",
          "hash": "sha256-fUHa7VQPh6ghppFATufPbaAgjRIq/4eRhXLxG41z48E=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.bdrv35066i.wasm",
          "hash": "sha256-TzOslb+k0QU4YOsoo6XxKi5Yc3xeAmW+6x7f28zGZp4=",
          "cache": "force-cache"
        }
      ],
      "pt-BR": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.uusu6gh1iv.wasm",
          "hash": "sha256-lkNW4OeRQYsGv5KY5ysPnKzOJu1Lr5rSEnwFD6K3N6U=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.vxqwp715bw.wasm",
          "hash": "sha256-nSrMRhN/6BX0DBdHVO67j9I7A+pI6jySPiZxmaMCw5E=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.sqcgdh04e4.wasm",
          "hash": "sha256-hLWkhyBSGexSjIe6u2sNVvr7v4QAolIvwgBKsnoptIs=",
          "cache": "force-cache"
        }
      ],
      "ru": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.6j7kgq3nxn.wasm",
          "hash": "sha256-IguP0NUjQg410IjRfrrXHtb4N1Qi9Gw348vA5BrAqAs=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.0i5x2cyi8z.wasm",
          "hash": "sha256-GCwvbYAdgTNX/JB24Rw3LIT/ojcgZkt1w8J9RjA+GOo=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.8kyi66ns3f.wasm",
          "hash": "sha256-iHJgK9ouTsTRdn7cPyRVsu/Rhg10GIhUWFxitXEcqhw=",
          "cache": "force-cache"
        }
      ],
      "tr": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.uikmw9je3v.wasm",
          "hash": "sha256-mlrHbMhTwAeia955g/rJX3onJvBZK5EkCW7tUAT9GQk=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.ce3ouw5hqp.wasm",
          "hash": "sha256-zOpdK7mwOz6jS2kPcrwXX8uahuBEQglklBnv9aKOaXY=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.sl0qymwaks.wasm",
          "hash": "sha256-kH9IyCgarVQF6VEVgmOORucuwFI1ZGQnEE7mvYF7gZg=",
          "cache": "force-cache"
        }
      ],
      "zh-Hans": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.km97ls08ug.wasm",
          "hash": "sha256-fFFpbcm4EORejaKICdQvamL9YFbJmYfbZVnG9sNot0I=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.yw72ollf6o.wasm",
          "hash": "sha256-kjDAHn9qB7BOWUrFcOcWRfnZeh9u6qVHraF9Y5zVzkg=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.witnhd3qi0.wasm",
          "hash": "sha256-yFXfiI88jDiB4aDakxh/F0QOPr1X9VvPBxYG5ua0770=",
          "cache": "force-cache"
        }
      ],
      "zh-Hant": [
        {
          "virtualPath": "FSharp.Compiler.Service.resources.wasm",
          "name": "FSharp.Compiler.Service.resources.aykefp6d6y.wasm",
          "hash": "sha256-bot5I5F7OwWmzuuCglBZdrXEbau84oeiW6/hqLYoaHc=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.Core.resources.wasm",
          "name": "FSharp.Core.resources.3us0nmi5w5.wasm",
          "hash": "sha256-BX+NOlQKNLXbe33I4FwmyQPF5P1Yq7Ocv6cOWVTJqLY=",
          "cache": "force-cache"
        },
        {
          "virtualPath": "FSharp.DependencyManager.Nuget.resources.wasm",
          "name": "FSharp.DependencyManager.Nuget.resources.mqv50jrhgw.wasm",
          "hash": "sha256-ErQo/QCTRkPalwqUanq/iXxEJxiqTpwOd9PBi9hckW0=",
          "cache": "force-cache"
        }
      ]
    }
  },
  "debugLevel": 0,
  "linkerEnabled": true,
  "globalizationMode": "sharded",
  "extensions": {
    "blazor": {}
  },
  "runtimeConfig": {
    "runtimeOptions": {
      "configProperties": {
        "Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability": true,
        "System.ComponentModel.DefaultValueAttribute.IsSupported": false,
        "System.ComponentModel.Design.IDesignerHost.IsSupported": false,
        "System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization": false,
        "System.ComponentModel.TypeDescriptor.IsComObjectDescriptorSupported": false,
        "System.Data.DataSet.XmlSerializationIsSupported": false,
        "System.Diagnostics.Debugger.IsSupported": false,
        "System.Diagnostics.Metrics.Meter.IsSupported": false,
        "System.Diagnostics.Tracing.EventSource.IsSupported": false,
        "System.GC.Server": true,
        "System.Globalization.Invariant": false,
        "System.TimeZoneInfo.Invariant": false,
        "System.Linq.Enumerable.IsSizeOptimized": true,
        "System.Net.Http.EnableActivityPropagation": false,
        "System.Net.Http.WasmEnableStreamingResponse": true,
        "System.Net.SocketsHttpHandler.Http3Support": false,
        "System.Reflection.Metadata.MetadataUpdater.IsSupported": false,
        "System.Resources.ResourceManager.AllowCustomResourceTypes": false,
        "System.Resources.UseSystemResourceKeys": true,
        "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported": true,
        "System.Runtime.InteropServices.BuiltInComInterop.IsSupported": false,
        "System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting": false,
        "System.Runtime.InteropServices.EnableCppCLIHostActivation": false,
        "System.Runtime.InteropServices.Marshalling.EnableGeneratedComInterfaceComImportInterop": false,
        "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false,
        "System.StartupHookProvider.IsSupported": false,
        "System.Text.Encoding.EnableUnsafeUTF7Encoding": false,
        "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault": true,
        "System.Threading.Thread.EnableAutoreleasePool": false,
        "Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException": false
      }
    }
  }
}/*json-end*/);export{gt as default,ft as dotnet,mt as exit};
