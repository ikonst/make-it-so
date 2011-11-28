:: We remove any old version of the runtime folder...
del Runtime /S /Q
rmdir Runtime /S /Q

:: We copy the test solutions to the runtime folder...
robocopy ./TestProjects ./Runtime/TestProjects *.* /S /E

:: We copy the test-runner to the runtime folder...
robocopy ../TestRunner/bin/Release ./Runtime TestRunner.exe

:: We copy MakeItSo to the runtime folder...
robocopy ../MakeItSo/bin/Release ./Runtime MakeItSo.exe
robocopy ../MakeItSo/bin/Release ./Runtime *.dll

:: We run the test-runner...
cd ./Runtime
start TestRunner.exe


