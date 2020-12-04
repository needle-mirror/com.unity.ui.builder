#!/bin/bash

rootPath="$(cd $(dirname ${BASH_SOURCE[0]}); pwd)"
cd ${rootPath}
bin/Debug/CodeDump.exe -c C:/Trunk/u5/unity -p C:/Prime/Repos/Builder/com.unity.ui.builder
