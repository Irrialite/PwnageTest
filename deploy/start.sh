#!/bin/bash
# exit on any error code
set -e
cd /app/client/src/ConsoleApp1/
sudo dotnet restore
path=$(sudo dotnet publish --configuration Release | grep -Pio "(?<=published to )(.+)")
cd $path
# Redirect stdout, stderr, and stdin to dev null to let aws continue deployment
sudo dotnet ConsoleApp1.dll > /dev/null 2> /dev/null < /dev/null &