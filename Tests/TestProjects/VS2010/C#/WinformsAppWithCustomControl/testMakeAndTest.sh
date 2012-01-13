# Change to working folder...
cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd

# Build the test...
make

# Run the test and store the output...
pushd App/bin/monoDebug
./App.exe
popd

pushd App/bin/monoRelease
./App.exe
popd
