# README #

### What is this repository for? ###

Creation of Loopback SDK for Xamarin Studio or C# project.

* Quick summary: The Repository contains 3 folders. "lb-xm" folder contains the SDK Generator: JS Code running a compiled c# code that creates the SDK. E.g. In shell "node lb-xm d:\someserver\server\server.js sdk.dll". Second folder, "LBXamarinSDK", contains the open source of the c# part of the generator. Thirdly, "SDK Example" folder contains the example server and Xamarin solution of an App using a compiled SDK.

* Version 1.0

### How do I get set up? ###

* Summary of set up

    To compile an SDK, take the folder "lb-xm", make sure you have all the dependencies and run "node lb-xm d:\someserver\server\server.js sdk.dll" in shell, where first parameter is the server.js file of the Loopback server, and sdk.dll is the filename of the output compiled SDK.
    To review the C# part of the Generator (Source code of lb-xm/bin/LBXamarinSDKGenerator.dll), take the folder "LBXamarinSDK" and "LBXamarinSDK.sln".
    To review the example App using a compiled SDK, take the folder "SDK Example". This folder in turn contains a Loopback server and a Xamarin solution of an Android App using the SDK.

### Who do I talk to? ###

* Repo owner or admin
yehuda@perfectedtech.com

* Other community or team contact