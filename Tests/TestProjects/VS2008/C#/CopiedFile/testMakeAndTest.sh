# Change to working folder...
cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd

# Build the test...
make

# Run the test and store the output...
pushd bin/monoDebug
./CopiedFile.exe > testOutput.txt
popd

pushd bin/monoRelease
./CopiedFile.exe > testOutput.txt
popd
