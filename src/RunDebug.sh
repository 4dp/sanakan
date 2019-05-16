#!/bin/bash
let rval=1
while ((rval == 1)); do
    dotnet ./bin/Debug/netcoreapp2.1/Sanakan.dll
    rval=$?
    if ((rval == 255)) 
    then 
        make all-update-debug
        rval=1
    fi
sleep 1
done