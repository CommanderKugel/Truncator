EXE = Truncator

all:
	dotnet publish -c Release Truncator/ --output . --p:DebugType=None --p:AssemblyName=$(EXE)
