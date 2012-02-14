# Change to working folder...
cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd

# Build the test...
make

# Run the test and store the output...
pushd Debug
./App.exe > testOutput.txt
popd

pushd Release
./App.exe > testOutput.txt
popd
