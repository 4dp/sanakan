all-update: backup update full-build

all-update-debug: update restore build-debug

full-build: restore build

run-debug:
	dotnet bin/Debug/netcoreapp3.1/Sanakan.dll

build-debug:
	dotnet build -c Debug

run:
	dotnet bin/Release/netcoreapp3.1/Sanakan.dll

build:
	dotnet build -c Release

restore:
	dotnet restore

cleanup:
	dotnet clean

backup:
	cp -r ./bin/Release/ ./bin/Backup/

update:
	git pull https://github.com/MrZnake/sanakan.git
