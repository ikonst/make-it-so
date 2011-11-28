#pragma once

#ifdef WIN32
	#undef DLL_EXPORT
	#define DLL_EXPORT __declspec(dllexport)
#else
	#undef DLL_EXPORT
	#define DLL_EXPORT 
#endif


namespace rssmath
{

class MathFunctions
{
public:
	static DLL_EXPORT double add(double a, double b);
	static DLL_EXPORT double multiply(double a, double b);

};

} // namespace