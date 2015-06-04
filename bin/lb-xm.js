#!/usr/bin/env node
console.log('>> SDK Generator.');

var path = require('path');
var semver = require('semver');
var optimist = require('optimist');
var generator = require('../index.js');
var edgePath = path.resolve('./');
var edge = require ('edge');

var argv = optimist
  .usage('Generate an SDK for Loopback in C#.' +
    '\n\nUsage: node lb-xm [server_path] [flags]' +
    '\n\nSupported flags: ' +
    '\n\n\tdll\t\t Compile a dll containing the SDK' +
    '\n\tforms\t\t Ensure compatibility with Xamarin-Forms' +
    '\n\tforce\t\t Remove unsupported functions' +
    '\n\tcheck\t\t Check if the SDK compiles successfully as C# code' +
    '\n\nE.g. "node lb-xm c:/testServer/server/server.js" outputs a CS file.' +
    '\nE.g. "node lb-xm c:/testServer/server/server.js dll" outputs a compiled dll.')
  .demand(1)
  .argv;

var appFile = path.resolve(argv._[0]);
var silencerA = console.log;
var silencerB = console.error;
console.error = function(){};
console.log = function(){};
var app = require(appFile);
assertLoopBackVersion();
var ngModuleName = argv['module-name'] || 'lbServices';
var apiUrl = argv['url'] || app.get('restApiRoot') || '/api';
var result = generator.services(app, ngModuleName, apiUrl);
console.error = silencerB;
console.log = silencerA;
console.log('>> Server parsed, templating code...');
var sdkCreationFunction = edge.func(__dirname + '/LBXamarinSDKGenerator.dll');
var flagA = argv._[1];
var flagB = argv._[2];
var flagC = argv._[3];
var flagD = argv._[4];
var flagE = argv._[5];
var params = [result, __dirname, flagA, flagB, flagC, flagD, flagE];

if(sdkCreationFunction(params, true)) {
  console.log('>> Done.');
} else {
  console.log('>> Done with errors.');
};

process.nextTick(function() {
  process.exit();
});

//--- helpers ---//
function assertLoopBackVersion() {
  var Module = require('module');
  // Load the 'loopback' module in the context of the app.js file,
  // usually from node_modules/loopback of the project of app.js
  var loopback = Module._load('loopback', Module._cache[appFile]);

  if (semver.lt(loopback.version, '1.6.0')) {
    console.error(
      '\nThe code generator does not support applications based\n' +
        'on LoopBack versions older than 1.6.0. Please upgrade your\n' +
        'project to a recent version of LoopBack and run this tool again.\n');
    process.exit(1);
  }
}
