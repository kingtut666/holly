holly
=====

Speech UI oriented home automation system. C# based.


This project is currently very pre-alpha.



Project is made up of two parts at the moment:-
1) C# Based Server
Currently a Windows Form app for fast dev work, aim is to make it be a service with HTTP interface eventually

2) C remote client
Client which forwards audio from a locally connected Kinect to the server, and takes audio output from the server and plays it via alsa



Build
=====
1) Server
Uses Visual Studio 10 Express Edition
Need to stick the following 3rd party library/binaries in the root directory:-
- NAudio (1.5)    https://naudio.codeplex.com/releases/view/79035
- Newtonsoft JSON.Net        http://james.newtonking.com/projects/json-net.aspx


2) Client
The makefile builds, but doesn't do anything special at the moment. It's not an autoconfig etc
Need the following:-
-  libfreenect     http://openkinect.org/wiki/Main_Page
-  libasound


