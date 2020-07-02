#!/bin/bash
rval=1
while ((rval == 1)); do
    dotnet ./bin/Release/netcoreapp2.1/Sanakan.dll
    rval=$?
    if ((rval == 255))
    then
        make all-update
        rval=1
    fi
sleep 1
done