#!/bin/sh

set -e

(
	cd gui
	dotnet publish --runtime linux-x64 --self-contained true  -p:PublishSingleFile=true
	dotnet publish --runtime win-x86 --self-contained true  -p:PublishSingleFile=true
)
