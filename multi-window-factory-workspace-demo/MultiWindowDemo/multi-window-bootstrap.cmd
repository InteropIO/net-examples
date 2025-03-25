@echo off
echo This simulates a bootstrapping logic (launcher) of a Glue-enabled app with multiple child apps

echo What it does is pass all the arguments passed to it to the main app - MultiWindowDemo

start MultiWindowDemo %*