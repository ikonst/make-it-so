# Change to working folder...
cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd

# Build the test...
make

# Run the test and store the output...
pushd gccDebugStatic
"./TheApp.exe" > testOutput.txt
popd

pushd gccReleaseStatic
"./TheApp.exe" > testOutput.txt
popd
