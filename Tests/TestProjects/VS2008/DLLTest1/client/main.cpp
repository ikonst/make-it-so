#include <stdio.h>
#include <MathFunctions.h>

using namespace rssmath;

int main(int argc, char** argv)
{
	double a = MathFunctions::add(2, 3);
	double b = MathFunctions::multiply(4, 5);
	printf("2+3is%.2lf,4*5is%.2lf", a, b);

	return 0;
}