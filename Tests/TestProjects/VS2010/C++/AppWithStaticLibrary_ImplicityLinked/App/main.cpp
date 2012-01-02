#include <TextUtils.h>
#include <stdio.h>

// Note that this is implicitly linked using a 'reference', not
// via project dependencies.
int main(int argc, char** argv)
{
	printf(TextUtils::getText().c_str());
}