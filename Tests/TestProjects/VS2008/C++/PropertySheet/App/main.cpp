#include <Hello.h>
#include <World.h>
#include <stdio.h>

int main(int argc, char** argv)
{
	printf("%s, %s!", Hello::getText().c_str(), World::getText().c_str()  );
}