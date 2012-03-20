C++
---
- Projects with property sheets, and inherited properties

- How do we know whether properties 
  - override parents?
  - merge with parents?
  - Some properties have a '<inherit from parent or project defaults>' setting
    - Where is this stored? It doesn't seem to be on the properties themselves.
  

- VCConfiguration
  - Intermediate folder
  - Output folder
  
- Tools["VCCLCompilerTool"]
  - AdditionalIncludeDirectories
  - PreprocessorDefinitions
  - WarningLevel
  - WarnAsError
  - Optimization
  
- Tools["VCLinkerTool"]
  - AdditionalLibraryDirectories
  - AdditionalDependencies
  - LinkLibraryDependencies  
  - GenerateDebugInformation
  
- Tools["VCLibrarianTool"]
  - LinkLibraryDependencies
  
- Tools["VCPreBuildEventTool"]
  - CommandLine
  
- Tools["VCPostBuildEventTool"]
  - CommandLine
  


General
-------
- Make path to cygwin configurable for the tester
- Output target names might not be the same as project names.
- 32-bit vs 64-bit generation?
- Config option to suppress checking in current folder for dlls. 
- Allow change to folder prefix "gcc" (maybe v 1.2) 


Custom build steps etc
----------------------
- Custom build steps
- Pre / post build steps



Tests
-----



WIKI
----
- Notes on the code
  - Notes on cygwin install for testings


QUESTIONS
---------
  

BUGS
----
- OnDemandPriceLogger: fails with 'Value cannot be null'

