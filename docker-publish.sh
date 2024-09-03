#!/bin/sh

dotnet.exe publish OengusWatcher -c Release -r linux-x64 -p:PublishProfile=DefaultContainer -p:InvariantGlobalization=true -p:ContainerFamily=alpine -p:ContainerRepository=eveldee/oengus-watcher
