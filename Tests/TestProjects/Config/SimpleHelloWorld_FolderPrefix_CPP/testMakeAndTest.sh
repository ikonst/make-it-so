# Change to working folder...
cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd

# Build the test...
make

# Run the test and store the output...
pushd cppPrefixDebug
./App.exe > testOutput.txt
popd

pushd cppPrefixRelease
./App.exe > testOutput.txt
popd
