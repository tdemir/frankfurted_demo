#!/bin/bash
dotnet test --no-restore --verbosity normal /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/coverage.cobertura.xml
reportgenerator -reports:"**/TestResults/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
open coveragereport/index.html