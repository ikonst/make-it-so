# Change to working folder...
cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd

# Build the test...
make

# Run the test and store the output...
pushd bin/monoDebug
./App.exe > testOutput.txt
popd

pushd bin/monoRelease
./App.exe > testOutput.txt
popd
