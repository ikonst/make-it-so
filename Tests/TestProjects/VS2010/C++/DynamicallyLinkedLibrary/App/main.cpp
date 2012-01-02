#include <TextUtils.h>
#include <stdio.h>

int main(int argc, char** argv)
{
	std::string message = TextUtils::getHello() + ", " + TextUtils::getWorld();
	printf(message.c_str());
}

