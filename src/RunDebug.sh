#!/bin/bash
rval=1
while ((rval == 1)); do
    dotnet ./bin/Debug/netcoreapp3.1/Sanakan.dll
    rval=$?
    if ((rval == 255))
    then
        make all-update-debug
        rval=1
    fi
sleep 1
done