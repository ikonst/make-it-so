# Change to working folder...
cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd

# Build the test...
make

# Run the test and store the output...
pushd bin/monoDebug
./SimpleWinformsApp.exe
popd

pushd bin/monoRelease
./SimpleWinformsApp.exe
popd
